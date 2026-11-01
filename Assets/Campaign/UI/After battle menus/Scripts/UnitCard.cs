using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndGameUI
{
    // Post-battle card for one roster character: portrait, name, stats, and an XP bar that fills up to the XP the
    // character has after the battle's rewards, playing the level-up sound each time it wraps.
    public class UnitCard : MonoBehaviour
    {
        [SerializeField]
        Image portrait;
        [SerializeField]
        RectTransform levelBar, XPBar;
        [SerializeField]
        TextMeshProUGUI unitName, stats, levelUps;
        [SerializeField]
        AudioSource LevelUp, xpRising;
        [SerializeField, Range(0.2f, 1)] float _minTrackerUpdateSpeed = 0.3f;
        [SerializeField, Range(1, 10)] float _maxTrackerCompletionTime = 5;

        Campaign.RosterCharacter unit;
        int _lazyXP;

        public void Set(Campaign.RosterCharacter character)
        {
            unit = character;
            BattleReport.CharacterUpdate += CharacterUpdate;
            portrait.sprite = character.Portrait;
            unitName.text = character.Name;
            stats.text = StatString(character);
            levelUps.text = $"Level {character.level}";
            StartCoroutine(TrackXP());
        }

        void OnDestroy() => BattleReport.CharacterUpdate -= CharacterUpdate;

        static string StatString(Campaign.RosterCharacter character)
        {
            CampaignCharacter c = character.character;
            if (c == null) return "";
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(c.ClassName)) sb.Append(c.ClassName).Append('\n');
            sb.Append($"Soldiers {c.Numbers}  Speed {c.Speed}  Morale {c.Morale}  Discipline {c.Discipline}\n");
            sb.Append($"Strength {c.Strength}  Defence {c.Defence}  Charge {c.Charge}  HP {c.MaxHealth}");
            return sb.ToString();
        }

        IEnumerator TrackXP()
        {
            // Start from the XP before this battle's rewards are counted in, if the card was set before them.
            _lazyXP = unit.TotalXp;
            SetBar(_lazyXP);
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.2f));
            while (enabled)
            {
                if (unit.TotalXp > _lazyXP)
                {
                    _lazyXP++;
                    SetBar(_lazyXP);
                    if (Campaign.RosterCharacter.FractionToNextLevel(_lazyXP) == 0)
                    {
                        if (LevelUp != null) LevelUp.Play();
                        levelUps.text = $"Level {_lazyXP / Campaign.RosterCharacter.XpPerLevel + 1}";
                    }
                    else if (xpRising != null) xpRising.Play();
                }
                float xpDelta = Mathf.Max(1, unit.TotalXp - _lazyXP);
                yield return new WaitForSecondsRealtime(Mathf.Min(_minTrackerUpdateSpeed * xpDelta, _maxTrackerCompletionTime) / xpDelta);
            }
        }

        void SetBar(int totalXp)
        {
            float width = Campaign.RosterCharacter.FractionToNextLevel(totalXp) * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }

        void CharacterUpdate(Campaign.RosterCharacter character)
        {
            if (unit != character) return;
            stats.text = StatString(character);
        }
    }
}
