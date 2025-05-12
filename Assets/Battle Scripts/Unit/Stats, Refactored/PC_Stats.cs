using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    [CreateAssetMenu(menuName = "Stats/Refactor/PC Stats")]
    public class PC_Stats : IUnitStatBlock
    {
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
    }
}

