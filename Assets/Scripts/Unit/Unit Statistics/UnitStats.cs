using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    public abstract class UnitStats : ScriptableObject
    {
        public string UnitName;
        public Sprite portrait;
        [SerializeField, Range(0, 1000)]
        List<int> ExperienceToReachLevel;
        [SerializeField, Min(0)]
        int startingExperience;
        public int CurrentLevel
        {
            get
            {
                for (int i = 1; i < ExperienceToReachLevel.Count; i++)
                {
                    if (ExperienceToReachLevel[i] > startingExperience)
                        return i;
                }
                return ExperienceToReachLevel.Count;
            }
        }
        public void AddXP(int XP)
        {
            var currentLevel=CurrentLevel;
            startingExperience += XP;
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
                float roll = Random.Range(0, 1);
                if (roll > chanceToGrow) return;
                levelBonuses[stat.StatType]++;
            }
        }
        private void Awake()
        {
            LoadStats();
        }
        public void SaveStats()
        {
            //save current experience
            //save bonus stats from level ups
        }
        public void LoadStats()
        {
            levelBonuses = new();
            foreach (var stat in Stats())
            {
                levelBonuses.Add(stat.StatType, 0);
            }
        }
        protected Dictionary<string, int> levelBonuses;
        public abstract List<Stat> Stats();
    }
}