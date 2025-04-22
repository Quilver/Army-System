using Campaign;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    public abstract class Achievments : ScriptableObject
    {
        public Sprite icon;
        public abstract void Initialise();
        public abstract string Description { get; }
        public abstract bool Achieved();
        public abstract void Reward();
        protected void GiveExperience(StatSystem.UnitStats characterStatBlock, int xp)
        {
            var character =CampaignDataManager.Data.characters.Find( x => x.statBase.UnitName == characterStatBlock.UnitName);
            if(character == null ) return;
            character.XP += xp;
            character.statBase.AddXP(xp);
            BattleReport.CharacterUpdate(character);    
        }
        protected void GiveGoldAndPrestige(int gold, int prestige) {
            CampaignDataManager.Data.CurrentPrestige += prestige;
            CampaignDataManager.Data.Money += gold;
        }

    }
}