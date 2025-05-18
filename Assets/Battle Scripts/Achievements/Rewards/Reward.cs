using Campaign;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [Serializable]
    public abstract class Reward
    {
        public abstract void GiveReward(int levelOfReward);
        public abstract void GiveReward(Campaign.PCWrapper pc, int levelOfReward);
        public abstract void GiveReward(List<Campaign.PCWrapper> pc, int levelOfReward);

    }
    [Serializable]
    public class Gold : Reward
    {
        [SerializeField] int GoldPerLevel;
        public override void GiveReward(int levelOfReward)
        {
            CampaignDataManager.Data.Money += GoldPerLevel * levelOfReward;
        }
        public override void GiveReward(PCWrapper pc, int levelOfReward)
        {
            CampaignDataManager.Data.Money += GoldPerLevel*levelOfReward;
        }

        public override void GiveReward(List<PCWrapper> pc, int levelOfReward)
        {
            CampaignDataManager.Data.Money += GoldPerLevel * levelOfReward;
        }
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
        public override void GiveReward(PCWrapper pc, int levelOfReward)
        {
            CampaignDataManager.Data.MaxPrestige += PrestigePerLevel * levelOfReward;
            CampaignDataManager.Data.CurrentPrestige += PrestigePerLevel * levelOfReward;
        }

        public override void GiveReward(List<PCWrapper> pc, int levelOfReward)
        {
            CampaignDataManager.Data.MaxPrestige += PrestigePerLevel * levelOfReward;
            CampaignDataManager.Data.CurrentPrestige += PrestigePerLevel * levelOfReward;
        }
    }
    [Serializable]
    public class GiveXP : Reward
    {
        [SerializeField] int xpPerLevel;
        public override void GiveReward(int levelOfReward)
        {
            foreach (var pc in Campaign.CampaignDataManager.Data.Characters)
            {
                GiveReward(pc, levelOfReward);
            }
        }
        public override void GiveReward(PCWrapper pc, int levelOfReward)
        {
            var character = CampaignDataManager.Data.Characters.Find(x => x.statBase.UnitName == pc.statBase.UnitName);
            Debug.Log(character);
            if (character == null) return;
            character.AddXP(xpPerLevel * levelOfReward);
            BattleReport.CharacterUpdate(character);
        }

        public override void GiveReward(List<PCWrapper> pcs, int levelOfReward)
        {
            foreach (var pc in pcs)
                GiveReward(pc, levelOfReward);
        }
    }
}
