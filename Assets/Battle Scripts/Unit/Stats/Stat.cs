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
    }
}