using System.Collections.Generic;
using AchievementSystem;
using Campaign;

namespace EndGameUI
{
    // The battle's rewards, granted all at once when the post-battle book opens, with a record of what each one did so the
    // book can animate it afterwards (SU-115): gold and prestige before and after, every earned achievement, and for each
    // deployed character its state before the rewards and, in order, the XP and level-ups each achievement gave it.
    // Granting first means no reward is lost if the player turns pages or leaves before an animation plays.
    public sealed class PostBattleResults
    {
        // One achievement's effect on one character.
        public sealed class RewardEvent
        {
            public readonly Achievement Achievement;
            public int Xp;
            public readonly List<LevelUpResult> LevelUps = new();

            public RewardEvent(Achievement achievement) => Achievement = achievement;
        }

        public sealed class CharacterResult
        {
            public RosterCharacter Character;
            public CharacterBattleRecord Record;
            public CharacterSnapshot Before;
            public readonly List<RewardEvent> Events = new();

            public int XpGained
            {
                get
                {
                    int total = 0;
                    foreach (var e in Events) total += e.Xp;
                    return total;
                }
            }
        }

        public readonly List<Achievement> Earned = new();
        public readonly List<Achievement> GlobalEarned = new();
        public readonly List<CharacterResult> Characters = new();
        public int GoldBefore, GoldAfter, PrestigeBefore, PrestigeAfter;

        public int GoldEarned => GoldAfter - GoldBefore;
        public int PrestigeEarned => PrestigeAfter - PrestigeBefore;

        public static PostBattleResults GrantAll(IEnumerable<Achievement> achievements, IEnumerable<RosterCharacter> deployed)
        {
            var results = new PostBattleResults();
            CampaignData campaign = RewardContext.CurrentCampaign;
            results.GoldBefore = campaign != null ? campaign.Money : 0;
            results.PrestigeBefore = campaign != null ? campaign.CurrentPrestige : 0;

            var byCharacter = new Dictionary<RosterCharacter, CharacterResult>();
            if (deployed != null)
                foreach (var character in deployed)
                {
                    if (character == null || byCharacter.ContainsKey(character)) continue;
                    var result = new CharacterResult
                    {
                        Character = character,
                        Record = BattleReport.Exists ? BattleReport.RecordFor(character) : null,
                        Before = character.Snapshot(),
                    };
                    byCharacter[character] = result;
                    results.Characters.Add(result);
                }

            // While an achievement is granted, its XP and level-ups are caught per character.
            Achievement current = null;
            var pending = new Dictionary<RosterCharacter, RewardEvent>();
            RewardEvent EventOf(RosterCharacter character)
            {
                if (!pending.TryGetValue(character, out var e)) pending[character] = e = new RewardEvent(current);
                return e;
            }
            var xpHandlers = new Dictionary<RosterCharacter, System.Action<int>>();
            var levelHandlers = new Dictionary<RosterCharacter, System.Action<LevelUpResult>>();
            foreach (var character in byCharacter.Keys)
            {
                var c = character;
                xpHandlers[c] = amount => EventOf(c).Xp += amount;
                levelHandlers[c] = levelUp => EventOf(c).LevelUps.Add(levelUp);
                c.GainedXp += xpHandlers[c];
                c.LevelledUp += levelHandlers[c];
            }

            try
            {
                if (achievements != null)
                    foreach (var achievement in achievements)
                    {
                        if (achievement == null || !achievement.Achieved()) continue;
                        results.Earned.Add(achievement);
                        if (achievement is GlobalAchievement) results.GlobalEarned.Add(achievement);
                        if (achievement.RewardsGranted) continue;

                        current = achievement;
                        pending.Clear();
                        // A character achievement shows on its character's page even when it gives no XP.
                        if (achievement.Character != null && byCharacter.ContainsKey(achievement.Character))
                            EventOf(achievement.Character);
                        achievement.GrantRewards();
                        foreach (var pair in pending) byCharacter[pair.Key].Events.Add(pair.Value);
                    }
            }
            finally
            {
                foreach (var character in byCharacter.Keys)
                {
                    character.GainedXp -= xpHandlers[character];
                    character.LevelledUp -= levelHandlers[character];
                }
            }

            results.GoldAfter = campaign != null ? campaign.Money : 0;
            results.PrestigeAfter = campaign != null ? campaign.CurrentPrestige : 0;
            return results;
        }
    }
}
