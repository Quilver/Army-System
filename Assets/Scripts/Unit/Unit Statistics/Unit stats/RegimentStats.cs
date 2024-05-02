using MyNamespace;
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

        int IRangedStats.ReloadRate => FireRate.BaseStat + levelBonuses[FireRate.StatType];

        int IRangedStats.Accuracy => Accuracy.BaseStat + levelBonuses[Accuracy.StatType];

        int IDefenceStats.Defence => Defence.BaseStat + levelBonuses[Defence.StatType];

        int IMovementStats.MoveSpeed => MoveSpeed.BaseStat + levelBonuses[MoveSpeed.StatType];

        int ICombatStats.AttackPower => AttackPower.BaseStat + levelBonuses[AttackPower.StatType];
        List<Stat> _stats;
        public override List<Stat> Stats()
        {
            if(_stats != null ) return _stats;
            _stats = new List<Stat>
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