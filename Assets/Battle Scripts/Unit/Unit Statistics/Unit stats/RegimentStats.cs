using MyNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem
{
    [CreateAssetMenu(menuName ="Stats/Regiment Stats")]
    public class RegimentStats : UnitStats, IMovementStats, IDefenceStats, ICombatStats, IRangedStats
    {
        public Stat ModelCount, Defence;
        public Stat MoveSpeed;
        public Stat AttackPower;
        //Shooting data
        public Stat FireRate, Accuracy;

        int IRangedStats.ReloadRate => FireRate.CurrentStat;

        int IRangedStats.Accuracy => Accuracy.CurrentStat;

        int IDefenceStats.Defence => Defence.CurrentStat;

        int IMovementStats.MoveSpeed => MoveSpeed.CurrentStat;

        int ICombatStats.AttackPower => AttackPower.CurrentStat;
        void Initialise()
        {
            ModelCount.StatType = "Model Count";
            Defence.StatType = "Defence";
            MoveSpeed.StatType = "Move Speed";
            AttackPower.StatType = "Attack Power";
            FireRate.StatType = "Fire Rate";
            Accuracy.StatType = "Accuracy";
        }
        public override List<Stat> Stats()
        {
            Initialise();
            List<Stat> _stats = new List<Stat>
            {
                ModelCount,
                MoveSpeed,
                Defence,
                AttackPower,
                FireRate,
                Accuracy
            };
            return _stats;
        }
    }
}