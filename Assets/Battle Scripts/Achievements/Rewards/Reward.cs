using System;
using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace AchievementSystem
{
    [Serializable]
    public abstract class Reward
    {
        public abstract void GiveReward(int levelOfReward);
        public abstract void GiveReward(RosterCharacter character, int levelOfReward);

        public void GiveReward(List<RosterCharacter> characters, int levelOfReward)
        {
            foreach (var character in characters)
                GiveReward(character, levelOfReward);
        }
    }

    [Serializable]
    public class Gold : Reward
    {
        [SerializeField] int GoldPerLevel;
        public override void GiveReward(int levelOfReward) => CampaignDataManager.Data.Money += GoldPerLevel * levelOfReward;
        public override void GiveReward(RosterCharacter character, int levelOfReward) => GiveReward(levelOfReward);
    }

    [Serializable]
    public class Prestige : Reward
    {
        [SerializeField] int PrestigePerLevel;

        public override void GiveReward(int levelOfReward)
        {
            CampaignDataManager.Data.MaxPrestige += PrestigePerLevel * levelOfReward;
            CampaignDataManager.Data.CurrentPrestige += PrestigePerLevel * levelOfReward;
        }

        public override void GiveReward(RosterCharacter character, int levelOfReward) => GiveReward(levelOfReward);
    }

    // XP for one character, or for the whole roster when the achievement is global.
    [Serializable]
    public class GiveXP : Reward
    {
        [SerializeField] int xpPerLevel;

        public override void GiveReward(int levelOfReward)
        {
            foreach (var character in CampaignDataManager.Data.Roster)
                GiveReward(character, levelOfReward);
        }

        public override void GiveReward(RosterCharacter character, int levelOfReward)
        {
            if (character == null) return;
            character.AddXp(xpPerLevel * levelOfReward);
            BattleReport.CharacterUpdate?.Invoke(character);
        }
    }
}
