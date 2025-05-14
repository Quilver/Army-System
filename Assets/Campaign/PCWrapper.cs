using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Campaign
{
    [System.Serializable]
    public class PCWrapper
    {
        [Range(0, 1000)]
        public int XP = 0;
        public int Level
        {
            get
            {
                if(levelTable == null)return 0;
                int XpRequired = 0;
                for (int i = 0; i < levelTable.ExperienceToReachLevel.Count; i++)
                {
                    XpRequired += levelTable.ExperienceToReachLevel[i];
                    if (XpRequired > XP)
                        return i + 1;
                }
                return levelTable.ExperienceToReachLevel.Count;
            }
        }
        [Range(5, 50)]
        public int CostToField = 10;
        [SerializeField]
        StatSystem.LevelTable levelTable;
        public StatSystem.Refactor.PC_Stats statBase;
        [SerializeField]
        SerializableDictionary<string, int> statsGained;
        public void Load(PCWrapper stats)
        {

        }
        public void AddXP(int xp) {
            Debug.Log("Adding xp");
            int currentLevel = Level;
            XP += xp;
            if(Level > currentLevel) 
                LevelUp();
        }
        void LevelUp()
        {
            Debug.Log("Level up");
            foreach (var stat in statBase.LevelUp())
            {
                if(statsGained.ContainsKey(stat)) statsGained[stat]++;
                else statsGained.Add(stat, 1);
            }
        }
        public void Update() { }
        public float PercentToNextLevel()
        {
            int XpRequired = 0;
            for (int i = 0; i < levelTable.ExperienceToReachLevel.Count; i++)
            {
                float prevXpRequired=XpRequired;
                XpRequired += levelTable.ExperienceToReachLevel[i];
                if (XpRequired > XP)
                    return (XP-prevXpRequired) / (float)levelTable.ExperienceToReachLevel[i];
            }
            return 1;
        }
        public StatSystem.Refactor.IUnitStatBlock GetStatsForBattle()
        {
            return StatSystem.Refactor.NPC_Stats.CreateStats(statBase, statsGained);
        }
        public string LevelBonuses()
        {
            string statString = "";
            foreach (var bonus in statsGained)
            {
                statString += bonus.Key + $": {bonus.Value}, ";
            }
            return statString;
        }
    }
}