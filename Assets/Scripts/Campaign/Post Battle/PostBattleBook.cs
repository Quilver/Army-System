using System.Collections;
using System.Collections.Generic;
using AchievementSystem;
using BattlePrep.Tent;
using Campaign;
using CampaignUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndGameUI
{
    // The post-battle screen as a book, like the roster ledger (SU-115). Built by Leveraged Wars > Post-Battle > Build
    // Post-Battle Book.
    // - Spread 1, the battle report: time taken, enemy slain, casualties, troops routed on each side, gold and prestige
    //   earned, achievements won; facing it, the army's honours (global achievements) stamped one by one while the
    //   treasury counts up.
    // - One spread per deployed character: its ledger page (as in the tent, XP as notches) and its battle record: kills,
    //   casualties, XP earned and its achievements, stamped one by one. Each stamp's XP lights the notches; a full row is a
    //   level-up: the notches pulse together, then each stat glows in turn and either gains +1 or goes back to normal.
    // Every reward is granted when the book opens (PostBattleResults), so the animations only show what happened. A spread
    // animates the first time it is opened; Space / Enter / click skips to its end. Timings: PostBattleTuning.
    public sealed class PostBattleBook : MonoBehaviour
    {
        const float SkipGraceSeconds = 0.25f;   // a click that turned the page does not also skip the new spread

        [SerializeField] LedgerPage _ledgerPage;
        [SerializeField] RecordPage _leftRecord, _rightRecord;
        [SerializeField] Button _previous, _next, _continue;
        [SerializeField] TextMeshProUGUI _spreadLabel;
        [SerializeField] AudioSource _audio;
        [SerializeField, Tooltip("In the editor, show sample data (Julian and Lyra) when no deployed characters were " +
            "passed in the BattleReport (PostBattleDebugData, SU-116).")]
        bool _debugDataWhenEmpty = true;

        PostBattleResults _results;
        bool[] _played;
        int _spread;
        float _spreadOpenedAt;
        UiSequence _sequence;
        Coroutine _playing;

        PostBattleTuning Tuning => PostBattleTuning.Active;
        int SpreadCount => 1 + _results.Characters.Count;

        void Start()
        {
            Cursor.visible = true;
            if (_debugDataWhenEmpty && Application.isEditor) PostBattleDebugData.FillIfEmpty();
            Debug.Log($"Post-battle: {BattleReport.DeployedCharacters?.Count ?? 0} deployed character(s), " +
                      $"{BattleReport.Achievements?.Count ?? 0} achievement(s) from the battle report.");
            _results = PostBattleResults.GrantAll(BattleReport.Achievements, BattleReport.DeployedCharacters);
            _played = new bool[SpreadCount];
            _previous.onClick.AddListener(() => Turn(-1));
            _next.onClick.AddListener(() => Turn(1));
            _continue.onClick.AddListener(EndGameButton.Continue);
            ShowSpread(0);
        }

        void Update()
        {
            if (TentInput.PagePrevPressed) Turn(-1);
            else if (TentInput.PageNextPressed) Turn(1);
            else if (_playing != null && TentInput.AdvancePressed &&
                     Time.unscaledTime - _spreadOpenedAt > SkipGraceSeconds)
                _sequence.Skipping = true;
        }

        void Turn(int direction) => ShowSpread(_spread + direction);

        void ShowSpread(int spread)
        {
            if (_playing != null) StopCoroutine(_playing);
            _playing = null;
            _spread = Mathf.Clamp(spread, 0, SpreadCount - 1);
            _spreadOpenedAt = Time.unscaledTime;
            _previous.interactable = _spread > 0;
            _next.interactable = _spread < SpreadCount - 1;
            _spreadLabel.text = _spread == 0
                ? $"Battle report   ·   {SpreadCount - 1} characters deployed"
                : $"{_results.Characters[_spread - 1].Character.Name}   ·   pages {_spread * 2 + 1}-{_spread * 2 + 2} of {SpreadCount * 2}";

            // A spread animates once; opened again it shows its end state at once.
            _sequence = new UiSequence { Skipping = _played[_spread] };
            _played[_spread] = true;
            _playing = StartCoroutine(Play(_spread, _sequence));
        }

        IEnumerator Play(int spread, UiSequence sequence)
        {
            if (spread == 0) yield return Summary(sequence);
            else yield return CharacterSpread(_results.Characters[spread - 1], spread, sequence);
            _playing = null;
        }

        // --- Spread 1: the battle report and the army's honours ---

        IEnumerator Summary(UiSequence s)
        {
            var t = Tuning;
            _ledgerPage.Hide();
            _leftRecord.Show("Battle Report", "Victory", null, 1);
            _rightRecord.Show("Battle Honours", "Rewards won by the whole army", "Honours", 2);

            // Rows appear empty first, then count up one after another.
            var time = _leftRecord.AddRow("Time taken");
            var slain = _leftRecord.AddRow("Enemy slain");
            var casualties = _leftRecord.AddRow("Casualties");
            var enemyRouted = _leftRecord.AddRow("Enemy troops routed");
            var ownRouted = _leftRecord.AddRow("Our troops routed");
            var gold = _leftRecord.AddRow("Gold earned");
            var prestige = _results.PrestigeEarned != 0 ? _leftRecord.AddRow("Prestige earned") : null;
            var won = _leftRecord.AddRow("Achievements won");
            var treasury = _rightRecord.AddRow("Treasury", $"{_results.GoldBefore}G");

            yield return s.Wait(t.PageIntroSeconds);
            // The report's figures count up together (SU-116).
            bool report = BattleReport.Exists;
            var figures = new List<Figure>
            {
                new(time, Mathf.RoundToInt(report ? BattleReport.timeTaken : 0f), v => $"{v / 60}:{v % 60:00}"),
                new(slain, report ? BattleReport.kills : 0),
                new(casualties, report ? BattleReport.deaths : 0),
                new(enemyRouted, report ? BattleReport.EnemyTroopsRouted : 0),
                new(ownRouted, report ? BattleReport.PlayerTroopsRouted : 0),
                new(gold, _results.GoldEarned, v => $"{v}G"),
                new(won, _results.Earned.Count),
            };
            if (prestige != null) figures.Add(new Figure(prestige, _results.PrestigeEarned));
            yield return CountTogether(s, figures);
            yield return s.Wait(t.RowIntervalSeconds);

            // Honours: each global achievement stamps, then the treasury counts up by its gold.
            int shownGold = _results.GoldBefore;
            if (_results.GlobalEarned.Count == 0) _rightRecord.ShowNoStamps("No honours this battle.");
            foreach (var achievement in _results.GlobalEarned)
            {
                var stamp = _rightRecord.AddStamp(achievement, achievement.RewardSummary);
                yield return stamp.Play(s, t, _audio);
                int target = shownGold + achievement.GoldReward;
                yield return s.CountUp(treasury.Value, shownGold, target, t.CountUpDuration(target - shownGold),
                    v => $"{v}G", Tick);
                shownGold = target;
                yield return s.Wait(t.StampIntervalSeconds);
            }
            // Gold from the characters' achievements too.
            yield return s.CountUp(treasury.Value, shownGold, _results.GoldAfter,
                t.CountUpDuration(_results.GoldAfter - shownGold), v => $"{v}G", Tick);
        }

        // --- One spread per deployed character ---

        IEnumerator CharacterSpread(PostBattleResults.CharacterResult result, int spread, UiSequence s)
        {
            var t = Tuning;
            var character = result.Character;
            int page = spread * 2 + 1;
            _leftRecord.Hide();
            _ledgerPage.Show(character, page, result.Before);
            string fate = result.Record != null && result.Record.Routed ? "Routed from the field" : "Held the field";
            _rightRecord.Show("Battle Record", fate, "Achievements", page + 1);

            var kills = _rightRecord.AddRow("Enemy slain");
            var casualties = _rightRecord.AddRow("Casualties");
            var xpRow = _rightRecord.AddRow("Experience", "+0 XP");

            yield return s.Wait(t.PageIntroSeconds);
            yield return CountTogether(s, new List<Figure>
            {
                new(kills, result.Record != null ? result.Record.Kills : 0),
                new(casualties, result.Record != null ? result.Record.Casualties : 0),
            });
            yield return s.Wait(t.RowIntervalSeconds);

            int level = result.Before.Level;
            int xp = result.Before.Xp;
            int gained = 0;
            if (result.Events.Count == 0) _rightRecord.ShowNoStamps("No achievements this battle.");
            foreach (var reward in result.Events)
            {
                string text = reward.Achievement is CharacterAchievement
                    ? reward.Achievement.RewardSummary
                    : (reward.Xp > 0 ? $"+{reward.Xp} XP" : "");
                var stamp = _rightRecord.AddStamp(reward.Achievement, text);
                yield return stamp.Play(s, t, _audio);

                int levelUp = 0;
                for (int i = 0; i < reward.Xp; i++)
                {
                    xp++;
                    gained++;
                    xpRow.Value.text = $"+{gained} XP";
                    _ledgerPage.SetLevel(level, xp);
                    Tick();
                    yield return _ledgerPage.Notches.Fill(xp, s, t.NotchFillSeconds);
                    if (xp < RosterCharacter.XpPerLevel) continue;

                    LevelUpResult result1 = levelUp < reward.LevelUps.Count ? reward.LevelUps[levelUp++] : null;
                    yield return LevelUp(result1, level, s);
                    level = result1 != null ? result1.Level : level + 1;
                    xp = 0;
                    _ledgerPage.SetLevel(level, xp);
                }
                yield return s.Wait(t.StampIntervalSeconds);
            }
        }

        // All notches pulse together, the level goes up, then each stat glows in turn and gains +1 or goes back to normal.
        IEnumerator LevelUp(LevelUpResult levelUp, int oldLevel, UiSequence s)
        {
            var t = Tuning;
            PlaySound(s, t.LevelUpSound);
            yield return _ledgerPage.Notches.Pulse(s, t.LevelUpPulses, t.LevelUpPulseSeconds, t.LevelUpPulseScale);
            _ledgerPage.SetLevel(levelUp != null ? levelUp.Level : oldLevel + 1, 0);
            if (levelUp == null) yield break;

            string gain = $" <color=#{ColorUtility.ToHtmlStringRGB(t.StatGainColor)}><b>+1</b></color>";
            foreach (var roll in levelUp.Rolls)
            {
                _ledgerPage.HighlightRoll(roll, true, t.StatGlowColor);
                yield return s.Wait(t.StatGlowSeconds);
                _ledgerPage.HighlightRoll(roll, false, t.StatGlowColor);
                if (roll.Grew)
                {
                    _ledgerPage.SetRollValue(roll, roll.After, gain);
                    PlaySound(s, t.StatGainSound);
                }
                yield return s.Wait(t.StatResultSeconds);
            }
        }

        // --- Helpers ---

        readonly struct Figure
        {
            public readonly RecordRow Row;
            public readonly int Value;
            public readonly System.Func<int, string> Format;

            public Figure(RecordRow row, int value, System.Func<int, string> format = null)
            {
                Row = row;
                Value = value;
                Format = format ?? (v => v.ToString());
            }
        }

        // Counts every figure up at once; each takes its own CountUpDuration, the longest sets the total time.
        IEnumerator CountTogether(UiSequence s, List<Figure> figures)
        {
            float longest = 0f;
            foreach (var figure in figures) longest = Mathf.Max(longest, Tuning.CountUpDuration(figure.Value));
            var shown = new int[figures.Count];
            for (int i = 0; i < figures.Count; i++) figures[i].Row.Value.text = figures[i].Format(0);
            yield return s.Tween(longest, f =>
            {
                for (int i = 0; i < figures.Count; i++)
                {
                    var figure = figures[i];
                    float duration = Tuning.CountUpDuration(figure.Value);
                    float own = duration > 0f ? Mathf.Clamp01(f * longest / duration) : 1f;
                    int value = Mathf.RoundToInt(Mathf.Lerp(0, figure.Value, own));
                    if (value == shown[i]) continue;
                    shown[i] = value;
                    figure.Row.Value.text = figure.Format(value);
                    if (!s.Skipping) Tick();
                }
            });
        }

        // Ticks are spaced out, so a fast count does not buzz.
        void Tick()
        {
            if (Time.unscaledTime - _lastTick < 0.06f) return;
            _lastTick = Time.unscaledTime;
            PlaySound(_sequence, Tuning.TickSound);
        }

        float _lastTick;

        void PlaySound(UiSequence s, AudioClip clip)
        {
            if (clip == null || _audio == null || (s != null && s.Skipping)) return;
            _audio.PlayOneShot(clip);
        }
    }
}
