using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MyNamespace
{
    public interface ICombatStats
    {
        public int AttackPower { get; }
        float CombatSpeed => 1 / 30f;
        public float AttackSpeed => AttackPower * CombatSpeed;
    }
}