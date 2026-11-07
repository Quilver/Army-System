using System;
using Campaign;
using UnityEngine;

namespace AchievementSystem
{
    // What an earned achievement grants: the achievement, the character it was earned by (null for a global achievement)
    // and its level (rewards are multiplied by it).
    public readonly struct RewardContext
    {
        public readonly Achievement Achievement;
        public readonly RosterCharacter Character;
        public readonly int Level;

        public RewardContext(Achievement achievement, RosterCharacter character, int level)
        {
            Achievement = achievement;
            Character = character;
            Level = Mathf.Max(0, level);
        }

        public bool IsCharacterAchievement => Character != null;

        // The campaign data, or null when a level is played on its own without a campaign.
        public static CampaignData CurrentCampaign => CampaignDataManager.instance != null ? CampaignDataManager.Data : null;
    }

    // A reward of an achievement (SU-114). To add a new kind (e.g. a story flag for campaign events), derive from this,
    // mark it [Serializable], and implement Grant and Describe; it then appears in the achievement's reward list.
    [Serializable]
    public abstract class Reward
    {
        public abstract void Grant(in RewardContext context);

        // Short text for the post-battle card, e.g. "+2 XP". Null or empty to show nothing.
        public abstract string Describe(int level);

        // Gold and XP this reward gives at a level, for the post-battle figures (SU-115).
        public virtual int GoldFor(int level) => 0;
        public virtual int XpFor(int level) => 0;
    }

    [Serializable]
    public class Gold : Reward
    {
        [SerializeField] int GoldPerLevel;

        public override void Grant(in RewardContext context)
        {
            CampaignData campaign = RewardContext.CurrentCampaign;
            if (campaign != null) campaign.Money += GoldPerLevel * context.Level;
        }

        public override string Describe(int level) => GoldPerLevel != 0 ? $"+{GoldPerLevel * level} Gold" : null;
        public override int GoldFor(int level) => GoldPerLevel * level;
    }

    [Serializable]
    public class Prestige : Reward
    {
        [SerializeField] int PrestigePerLevel;

        public override void Grant(in RewardContext context)
        {
            CampaignData campaign = RewardContext.CurrentCampaign;
            if (campaign == null) return;
            campaign.MaxPrestige += PrestigePerLevel * context.Level;
            campaign.CurrentPrestige += PrestigePerLevel * context.Level;
        }

        public override string Describe(int level) => PrestigePerLevel != 0 ? $"+{PrestigePerLevel * level} Prestige" : null;
    }

    // XP for the character who earned it; for a global achievement, for every roster character.
    [Serializable]
    public class GiveXP : Reward
    {
        [SerializeField] int xpPerLevel;

        public override void Grant(in RewardContext context)
        {
            int amount = xpPerLevel * context.Level;
            if (context.Character != null)
            {
                GiveTo(context.Character, amount);
                return;
            }
            CampaignData campaign = RewardContext.CurrentCampaign;
            if (campaign == null) return;
            foreach (var character in campaign.Roster)
                GiveTo(character, amount);
        }

        static void GiveTo(RosterCharacter character, int amount)
        {
            if (character == null || amount <= 0) return;
            character.AddXp(amount);
            BattleReport.CharacterUpdate?.Invoke(character);
        }

        public override string Describe(int level) => xpPerLevel != 0 ? $"+{xpPerLevel * level} XP" : null;
        public override int XpFor(int level) => xpPerLevel * level;
    }
}
