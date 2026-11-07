using System.Collections.Generic;
using AchievementSystem;
using Campaign;
using EndGameUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// The achievement system (SU-114): level rules, the melee timer, the requirement lifecycle, granting rewards once, and
// the achievement assets in Resources/Achievement.
public sealed class AchievementTests
{
    readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
    }

    [System.Serializable]
    sealed class CountingReward : Reward
    {
        public int Grants;
        public int LastLevel;

        public override void Grant(in RewardContext context)
        {
            Grants++;
            LastLevel = context.Level;
        }

        public override string Describe(int level) => $"+{level} test";
    }

    [TestCase(0, 1, false, 3, ExpectedResult = 0)]
    [TestCase(1, 1, false, 3, ExpectedResult = 1)]
    [TestCase(4, 5, false, 3, ExpectedResult = 0)]
    [TestCase(9, 5, false, 3, ExpectedResult = 1)]
    [TestCase(9, 5, true, 3, ExpectedResult = 1)]
    [TestCase(10, 5, true, 3, ExpectedResult = 2)]
    [TestCase(50, 5, true, 3, ExpectedResult = 3)]
    [TestCase(1, 0, false, 3, ExpectedResult = 1)]
    public int CountedLevel(int count, int required, bool repeatable, int maxLevel) =>
        AchievementCount.Level(count, required, repeatable, maxLevel);

    [Test]
    public void MeleeTimer_ShortGapsKeepTheEngagement_LongGapsAndBreaksEndIt()
    {
        var timer = new MeleeHoldTimer(2f);
        for (int i = 0; i < 30; i++) timer.Tick(true, 1f);
        timer.Tick(false, 1.5f);                     // pushed back, inside the grace
        for (int i = 0; i < 30; i++) timer.Tick(true, 1f);
        Assert.AreEqual(60f, timer.Longest, 1e-4f);

        timer.Tick(false, 3f);                       // a long gap ends it
        timer.Tick(true, 1f);
        Assert.AreEqual(1f, timer.Current, 1e-4f);

        timer.Break();                               // our troop retreated
        Assert.AreEqual(0f, timer.Current);
        Assert.AreEqual(60f, timer.Longest, 1e-4f);
    }

    [TestCase(11, 10, 1f, ExpectedResult = true)]
    [TestCase(10, 10, 1f, ExpectedResult = false)]
    [TestCase(15, 10, 1.5f, ExpectedResult = false)]
    [TestCase(16, 10, 1.5f, ExpectedResult = true)]
    [TestCase(5, 0, 1f, ExpectedResult = false)]
    public bool Outnumbered(int enemy, int ours, float ratio) => DefeatWhileOutnumbered.Outnumbers(enemy, ours, ratio);

    [Test]
    public void CharacterRequirement_WithoutTroop_IsNotTracked()
    {
        var achievement = CharacterAchievement.CreateCustom(new CustomCharacterAchievement
        {
            _requirement = new KillTracker(),
            rewards = new List<Reward>(),
        });
        _created.Add(achievement);
        Assert.IsFalse(achievement.Begin(new AchievementContext(null)));
        Assert.IsFalse(achievement.Achieved());
    }

    [Test]
    public void Victory_IsEarnedOnlyByWinning_AndRewardsAreGrantedOnce()
    {
        var reward = new CountingReward();
        GlobalAchievement won = MakeVictory(reward);
        Assert.IsTrue(won.Begin(new AchievementContext(null)));
        Assert.IsFalse(won.Achieved(), "not earned before the battle ends");
        won.Finish(true);
        Assert.IsTrue(won.Achieved());
        Assert.AreEqual("+1 test", won.RewardSummary);

        won.GrantRewards();
        won.GrantRewards();
        Achievement.GrantAll(new Achievement[] { won });
        Assert.AreEqual(1, reward.Grants);
        Assert.AreEqual(1, reward.LastLevel);
        Assert.IsTrue(won.RewardsGranted);

        var lostReward = new CountingReward();
        GlobalAchievement lost = MakeVictory(lostReward);
        lost.Begin(new AchievementContext(null));
        lost.Finish(false);
        lost.GrantRewards();
        Assert.IsFalse(lost.Achieved());
        Assert.AreEqual(0, lostReward.Grants);
    }

    GlobalAchievement MakeVictory(Reward reward)
    {
        var achievement = GlobalAchievement.CreateCustom(new CustomGlobalAchievement
        {
            _title = "Test victory",
            _requirement = new Victory(),
            rewards = new List<Reward> { reward },
        });
        _created.Add(achievement);
        return achievement;
    }

    [Test]
    public void AchievementAssets_HaveARequirementAnIconAndRewards()
    {
        int checkedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources/Achievement" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var achievement = AssetDatabase.LoadAssetAtPath<Achievement>(path);
            if (achievement == null) continue;  // the class table
            checkedCount++;
            Assert.IsNotNull(achievement.Requirement, path + ": requirement");
            Assert.IsNotNull(achievement.icon, path + ": icon");
            foreach (var reward in achievement.Rewards)
                Assert.IsNotNull(reward, path + ": empty reward slot");
        }
        Assert.Greater(checkedCount, 0);
    }

    [Test]
    public void ClassAchievements_LoadsAndEveryEntryIsSet()
    {
        ClassAchievements table = ClassAchievements.Load();
        Assert.IsNotNull(table, "Resources/" + ClassAchievements.ResourcePath);
        Assert.Greater(table.Classes.Count, 0);
        foreach (var entry in table.Classes)
        {
            Assert.IsNotNull(entry.characterClass);
            foreach (var achievement in entry.achievements) Assert.IsNotNull(achievement, entry.characterClass.name);
        }
        int forNoClass = 0;
        foreach (var _ in table.For(null)) forNoClass++;
        Assert.AreEqual(0, forNoClass);
    }

    [Test]
    public void AchievementTuning_HasEveryCharacterOnesAndMissionComplete()
    {
        AchievementTuning tuning = AchievementTuning.Active;
        Assert.IsNotNull(tuning, "Resources/" + AchievementTuning.ResourcePath);
        Assert.Greater(tuning.EveryCharacter.Count, 0);
        foreach (var achievement in tuning.EveryCharacter) Assert.IsNotNull(achievement);
        GlobalAchievement missionComplete = null;
        foreach (var achievement in tuning.EveryBattle)
            if (achievement != null && achievement.name == "Mission Complete") missionComplete = achievement;
        Assert.IsNotNull(missionComplete);
        Assert.IsInstanceOf<Victory>(missionComplete.Requirement);
        Assert.AreEqual(200, missionComplete.GoldReward);
    }

    // --- Levels and post-battle results (SU-115) ---

    [System.Serializable]
    sealed class XpReward : Reward
    {
        public RosterCharacter Target;
        public int Amount;
        public override void Grant(in RewardContext context) => Target.AddXp(Amount);
        public override string Describe(int level) => $"+{Amount} XP";
    }

    [Test]
    public void TenXp_IsOneLevel_AndEveryLevelRollsEachStat()
    {
        var saved = RosterCharacter.GrowthRoll;
        try
        {
            RosterCharacter.GrowthRoll = _ => true;
            var character = ScriptableObject.CreateInstance<CampaignCharacter>();
            _created.Add(character);
            var roster = new RosterCharacter { character = character, level = 1, xp = 0 };
            var levelUps = new List<LevelUpResult>();
            roster.LevelledUp += levelUps.Add;

            roster.AddXp(23);
            Assert.AreEqual(3, roster.level);
            Assert.AreEqual(3, roster.xp);
            Assert.AreEqual(2, levelUps.Count);
            Assert.AreEqual(RosterCharacter.GrowthOrder.Length + character.WeaponSkills.Count, levelUps[0].Rolls.Count);
            foreach (var roll in levelUps[0].Rolls)
            {
                int cap = roll.IsWeaponSkill ? character.CapFor(roll.WeaponClass) : character.CapFor(roll.Stat);
                Assert.AreEqual(roll.Before < cap, roll.Grew, roll.Stat.ToString());
            }

            RosterCharacter.GrowthRoll = _ => false;
            int before = roster.Stat(CharacterStat.Strength);
            roster.AddXp(RosterCharacter.XpPerLevel);
            Assert.AreEqual(before, roster.Stat(CharacterStat.Strength), "a failed roll adds nothing");
        }
        finally
        {
            RosterCharacter.GrowthRoll = saved;
        }
    }

    [Test]
    public void PostBattleResults_GrantEverythingOnce_AndRecordEachCharactersXpAndLevelUps()
    {
        var saved = RosterCharacter.GrowthRoll;
        try
        {
            RosterCharacter.GrowthRoll = _ => false;
            var character = ScriptableObject.CreateInstance<CampaignCharacter>();
            _created.Add(character);
            var roster = new RosterCharacter { character = character, level = 2, xp = 7 };

            var won = MakeVictory(new XpReward { Target = roster, Amount = 5 });
            won.Begin(new AchievementContext(null));
            won.Finish(true);
            var lost = MakeVictory(new XpReward { Target = roster, Amount = 50 });
            lost.Begin(new AchievementContext(null));
            lost.Finish(false);

            var results = PostBattleResults.GrantAll(new Achievement[] { won, lost }, new[] { roster });
            Assert.AreEqual(1, results.Earned.Count);
            Assert.AreEqual(1, results.Characters.Count);
            var result = results.Characters[0];
            Assert.AreEqual(2, result.Before.Level);
            Assert.AreEqual(7, result.Before.Xp);
            Assert.AreEqual(1, result.Events.Count);
            Assert.AreEqual(5, result.Events[0].Xp);
            Assert.AreEqual(1, result.Events[0].LevelUps.Count);
            Assert.AreEqual(3, result.Events[0].LevelUps[0].Level);
            Assert.AreEqual(3, roster.level);
            Assert.AreEqual(2, roster.xp);

            PostBattleResults.GrantAll(new Achievement[] { won }, new[] { roster });
            Assert.AreEqual(3, roster.level, "rewards are granted once");
            Assert.AreEqual(2, roster.xp);
        }
        finally
        {
            RosterCharacter.GrowthRoll = saved;
        }
    }
}
