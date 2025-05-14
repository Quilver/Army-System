using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    [CreateAssetMenu(menuName = "Stats/Refactor/PC Stats")]
    public class PC_Stats : IUnitStatBlock
    {
        [SerializeField]
        IUnit _unitPrefab;
        public IUnit UnitPrefab=> _unitPrefab;
        public RangedWeapons.IProjectile _projectile;
        [SerializeField]    
        Stat _Movement;
        public override int Movement => _Movement.value;
        [SerializeField]
        Stat _modelCount;
        public override int ModelCount => _modelCount.value;
        [SerializeField]
        Stat _defence;
        public override int Defence => _defence.value;
        [SerializeField]
        Stat _leadership;
        public override int Leadership => _leadership.value;
        [SerializeField]
        Stat _attackPower;
        public override int AttackPower => _attackPower.value;
        [SerializeField]
        Stat _attackSpeed;
        public override int AttackSpeed => _attackSpeed.value;
        [SerializeField]
        Stat _shootSpeed;
        public override int ShootSpeed => _shootSpeed.value;
        [SerializeField]
        Stat _accuracy;
        public override int Accuracy => _accuracy.value;
        public List<string> LevelUp()
        {
            List<string> levelledStats = new List<string>();

            if (LevelStat(_Movement)) levelledStats.Add("Movement");
            if (LevelStat(_Movement)) levelledStats.Add("ModelCount");

            if (LevelStat(_Movement)) levelledStats.Add("Defence");
            if (LevelStat(_Movement)) levelledStats.Add("Leadership");

            if (LevelStat(_Movement)) levelledStats.Add("AttackPower");
            if (LevelStat(_Movement)) levelledStats.Add("AttackSpeed");

            if (LevelStat(_Movement)) levelledStats.Add("ShootSpeed");
            if (LevelStat(_Movement)) levelledStats.Add("Accuracy");


            return levelledStats;
        }
        bool LevelStat(Stat stat) => Random.Range(0, 100) < stat.GrowthChange;
    }
}

