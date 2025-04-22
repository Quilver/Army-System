using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    [System.Serializable]
    public class Stat
    {
        [HideInInspector]
        public string StatType;
        [SerializeField, Range(1, 30)]
        int baseStat;
        [SerializeField]
        int _levelBonus = 0;
        public int LevelBonus { 
            get
            {  return _levelBonus; } 
        }
        public void LevelUp()
        {
            if(LevelBonus + 1 >= StatMaximum)return;
            _levelBonus++;
        }
        public int CurrentStat
        {
            get { return baseStat + LevelBonus; }
        }
        [SerializeField, Range(3, 30)]
        int StatMaximum;
        [SerializeField, Range(0, 100)]
        int statGrowthChancePercent;
        public int StatGrowthChance
        {
            get { return statGrowthChancePercent; }
        }

    }
}