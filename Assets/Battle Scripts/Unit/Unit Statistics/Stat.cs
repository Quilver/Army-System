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
        public int BaseStat
        {
            get { return baseStat; }
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