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
        protected void GiveExperience(StatSystem.Refactor.PC_Stats characterStatBlock, int xp)
        {
            var character =CampaignDataManager.Data.Characters.Find( x => x.statBase.UnitName == characterStatBlock.UnitName);
            Debug.Log(character);
            if(character == null ) return;
            character.XP += xp;
            character.AddXP(xp);
            BattleReport.CharacterUpdate(character);    
        }
        protected void GiveGoldAndPrestige(int gold, int prestige) {
            CampaignDataManager.Data.CurrentPrestige += prestige;
            CampaignDataManager.Data.Money += gold;
        }

    }
}