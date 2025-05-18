using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Campaign
{
    [System.Serializable]
    public class PCWrapper
    {
        public event System.Action LevelledUp;
        public event System.Action<int> GainedXP;
        public event System.Action<StatSystem.Refactor.StatType, int> GainedStatBonus;
        [SerializeField,Range(0, 1000)]
        int XP = 0;
        public int CurrentXP => XP;
        public int Level
        {
            get
            {
                if(levelTable == null)return 0;
                return levelTable.CurrentLevel(XP);
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
            GainedXP?.Invoke(xp);
            int currentLevel = Level;
            XP += xp;
            if(Level > currentLevel) 
                for(int i =0; i < Level - currentLevel; i++)
                    LevelUp();
        }
        void LevelUp()
        {
            Debug.Log("Level up");
            LevelledUp?.Invoke();
            foreach (var stat in statBase.LevelUp())
            {
                StatSystem.Refactor.StatType? statType = StatSystem.Refactor.Stat.StringToStatType(stat);
                if (statType.HasValue)
                    GainedStatBonus?.Invoke(statType.Value, 1);
                if(statsGained.ContainsKey(stat)) statsGained[stat]++;
                else statsGained.Add(stat, 1);
            }
        }
        public void Update() { }
        public float PercentToNextLevel()=>levelTable.fractionToNextLevel(XP);
        public float PercentToNextLevel(int _XP)=>levelTable.fractionToNextLevel(_XP);
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