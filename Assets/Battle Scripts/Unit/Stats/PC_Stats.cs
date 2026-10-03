using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    [CreateAssetMenu(menuName = "Stats/PC Stats")]
    public class PC_Stats : IUnitStatBlock
    {
        [SerializeField]
        IUnit _unitPrefab;
        public IUnit UnitPrefab=> _unitPrefab;
        public List<AchievementSystem.CharacterAchievement> achievements;
        //public RangedWeapons.IProjectile _projectile;
        public RangedWeapons.IRangedWeaponData _rangedWeapon;
        [SerializeField]    
        Stat _Movement;
        public override int Movement => _Movement.value;
        [SerializeField]
        Stat _MoveForce;
        public override int MoveForce => _MoveForce.value;
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
            if (LevelStat(_modelCount)) levelledStats.Add("ModelCount");

            if (LevelStat(_defence)) levelledStats.Add("Defence");
            if (LevelStat(_leadership)) levelledStats.Add("Leadership");

            if (LevelStat(_attackPower)) levelledStats.Add("AttackPower");
            if (LevelStat(_attackSpeed)) levelledStats.Add("AttackSpeed");

            if (LevelStat(_shootSpeed)) levelledStats.Add("ShootSpeed");
            if (LevelStat(_accuracy)) levelledStats.Add("Accuracy");


            return levelledStats;
        }
        bool LevelStat(Stat stat)=> (stat.Max > stat.value)? Random.Range(0, 100) < stat.GrowthChange : false;
    }
}

