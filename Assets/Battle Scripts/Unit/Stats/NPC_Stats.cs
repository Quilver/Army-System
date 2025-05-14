using Campaign;
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
        public static NPC_Stats CreateStats(PC_Stats baseStats, Dictionary<string,int> modifiers)
        {
            var NPC = ScriptableObject.CreateInstance<NPC_Stats>();
            NPC.SetDescription(baseStats.UnitName, baseStats.Portrait, baseStats.ModelPrefab);
            NPC.SetBattleStats(baseStats, modifiers);
            return NPC;
        }
        void SetBattleStats(PC_Stats baseStats, Dictionary<string,int> modifiers)
        {
            _Movement = baseStats.Movement;
            if (modifiers.ContainsKey("Movement"))
                _Movement+=modifiers["Movement"];
            _modelCount = baseStats.ModelCount;
            if (modifiers.ContainsKey("ModelCount"))
                _modelCount += modifiers["ModelCount"];
            
            _defence = baseStats.Defence;
            if (modifiers.ContainsKey("Defence"))
                _defence += modifiers["Defence"];
            _leadership = baseStats.Leadership;
            if (modifiers.ContainsKey("Leadership"))
                 _leadership += modifiers["Leadership"];

            _attackPower = baseStats.AttackPower;
            if (modifiers.ContainsKey("AttackPower"))
                 _attackPower += modifiers["AttackPower"];
            _attackSpeed = baseStats.AttackSpeed;
            if (modifiers.ContainsKey("AttackSpeed"))
                 _attackSpeed += modifiers["AttackSpeed"];

            _accuracy = baseStats.Accuracy;
            if (modifiers.ContainsKey("Accuracy"))
                 _accuracy += modifiers["Accuracy"];
            _shootSpeed = baseStats.ShootSpeed;
            if (modifiers.ContainsKey("ShootSpeed"))
                 _shootSpeed += modifiers["ShootSpeed"];
        }
    }
}

