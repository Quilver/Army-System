using StatSystem.Refactor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StatSystem.Refactor
{
    [CreateAssetMenu(menuName = "Stats/Refactor/NPC Stats")]
    public class NPC_Stats : IUnitStatBlock
    {
        [SerializeField, Range(1, 15)]
        int _Movement;
        public override int Movement => _Movement;
        [SerializeField, Range(1, 32)]
        int _modelCount;
        public override int ModelCount => _modelCount;
        [SerializeField, Range(1, 20)]
        int _defence;
        public override int Defence => _defence;
        [SerializeField, Range(1, 20)]
        int _leadership;
        public override int Leadership => _leadership;
        [SerializeField, Range(1, 20)]
        int _attackPower;
        public override int AttackPower => _attackPower;
        [SerializeField, Range(1, 20)]
        int _attackSpeed;
        public override int AttackSpeed => _attackSpeed;
        [SerializeField, Range(1, 20)]
        int _shootSpeed;
        public override int ShootSpeed => _shootSpeed;
        [SerializeField, Range(1, 20)]
        int _accuracy;
        public override int Accuracy => _accuracy;
    }
}

