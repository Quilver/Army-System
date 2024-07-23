using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    public abstract class UnitStats : ScriptableObject
    {
        public string UnitName;
        public GameObject UnitPrefab;
        public Sprite portrait;
        [SerializeField]
        LevelTable levelTable;
        //List<int> ExperienceToReachLevel;
        [SerializeField, Min(0)]
        int experience;
        public int CurrentLevel
        {
            get
            {
                for (int i = 1; i < levelTable.ExperienceToReachLevel.Count; i++)
                {
                    if (levelTable.ExperienceToReachLevel[i] > experience)
                        return i;
                }
                return levelTable.ExperienceToReachLevel.Count;
            }
        }
        public void AddXP(int XP)
        {
            var currentLevel=CurrentLevel;
            experience += XP;
            for (int i = 0; i < CurrentLevel - currentLevel; i++)
            {
                LevelUp();
            }
        }
        
        void LevelUp()
        {
            foreach (var stat in Stats())
            {
                float chanceToGrow = stat.StatGrowthChance / 100f;
                float roll = UnityEngine.Random.Range(0, 1);
                if (roll > chanceToGrow) return;
                levelBonuses[stat.StatType]++;
            }
        }
        public void Load()
        {
            foreach (var character in Campaign.CampaignDataManager.Data.characters)
            {
                if (character.statBase != this) continue;

                return;
            }
            Campaign.StatWrapper _character = new()
            {
                statBase = this,
                CostToField = 5
            };
            Campaign.CampaignDataManager.Data.characters.Add(_character);
        }
        protected Dictionary<string, int> levelBonuses;
        public abstract List<Stat> Stats();
        public float FractionToNextLevel()
        {
            if (levelTable.ExperienceToReachLevel.Count == CurrentLevel)
                return 1;
            int exp = experience - levelTable.ExperienceToReachLevel[CurrentLevel-1];
            int expToNextLevel = levelTable.ExperienceToReachLevel[CurrentLevel] - levelTable.ExperienceToReachLevel[CurrentLevel - 1];
            return ((float)exp)/ ((float)expToNextLevel);
        }
        public string StatString()
        {
            string stats = "| ";
            foreach (var stat in Stats())
                stats += stat.StatType + ": " + stat.CurrentStat + " | ";
            return stats;
        }
        public override string ToString()
        {
            string stats = UnitName + " Level: " + CurrentLevel + "\n";
            foreach (var stat in Stats())
            {
                stats += stat.StatType + ": " + stat.CurrentStat + "\n";
            }
            return stats;
        }
    }
}