using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    [System.Serializable]
    public struct Stat
    {
        [Range(0, 32)]
        public int value;
        [Range(15, 32)]
        public int Max;
        [Range(0, 100)]
        public int GrowthChange;
        public static string StatTypeToString(StatType statType)=>statType.ToString();
        public static StatType? StringToStatType(string statType)
        {
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                if (type.ToString() == statType)
                    return type;
            }
            Debug.LogError($"{statType} is not a valid stat");
            return null;
        }
    }
    public enum StatType {
        Movement,
        ModelCount,
        Defence,
        Leadership,
        AttackPower,
        AttackSpeed,
        ShootSpeed,
        Accuracy
    }
}