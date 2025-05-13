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
        int _Movement = 6;
        public override int Movement => _Movement;
        [SerializeField, Range(1, 32)]
        int _modelCount = 12;
        public override int ModelCount => _modelCount;
        [SerializeField, Range(1, 20)]
        int _defence = 5;
        public override int Defence => _defence;
        [SerializeField, Range(1, 20)]
        int _leadership = 5;
        public override int Leadership => _leadership;
        [SerializeField, Range(1, 20)]
        int _attackPower = 5;
        public override int AttackPower => _attackPower;
        [SerializeField, Range(1, 20)]
        int _attackSpeed = 5;
        public override int AttackSpeed => _attackSpeed;
        [SerializeField, Range(1, 20)]
        int _shootSpeed = 5;
        public override int ShootSpeed => _shootSpeed;
        [SerializeField, Range(1, 20)]
        int _accuracy = 5;
        public override int Accuracy => _accuracy;
    }
}

