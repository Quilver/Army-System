using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [Serializable]
    public abstract class RequirementGlobal
    {
        public event System.Action<bool> UpdatedSuccess;
        protected void UpdateSuccess(bool success) => UpdatedSuccess?.Invoke(success);
        public abstract void Setup();
        public abstract int Succeeded();
    }
    [Serializable]
    public class Default : RequirementGlobal
    {
        public override void Setup()
        {
            
        }
        [SerializeField, Min(0)] int _successLevel;
        public override int Succeeded() => _successLevel;
    }
    [Serializable]
    public class Victory : RequirementGlobal
    {
        bool _victory;
        public override void Setup()
        {
            Battle.Instance.BattleOver += EndBattle;
        }
        void EndBattle(bool victory)
        {
            _victory = victory;
            UpdateSuccess(_victory);    
        }
        public override int Succeeded()
        {
            return (_victory) ? 1 : 0;
        }
    }
    [Serializable]
    public class ArmyDestroyed : RequirementGlobal
    {
        [SerializeField] Army _army;
        bool _destroyed = false;
        public override void Setup()
        {
            IUnit.OnUnitDestroyed += CheckIfArmyIsDestroyed;
        }
        void CheckIfArmyIsDestroyed(IUnit unit) 
        {
            if(_army.Units.Count > 1)return; 
            _destroyed = true;
            IUnit.OnUnitDestroyed -= CheckIfArmyIsDestroyed;
        }
        public override int Succeeded()
        {
            return (_destroyed) ? 1 : 0;
        }
    }
}