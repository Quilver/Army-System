using System;
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
        [SerializeField, Min(0)] int _successLevel;
        public override void Setup() { }
        public override int Succeeded() => _successLevel;
    }

    [Serializable]
    public class Victory : RequirementGlobal
    {
        bool _victory;

        public override void Setup()
        {
            if (Battle.Instance != null) Battle.Instance.BattleOver += EndBattle;
        }

        void EndBattle(bool victory)
        {
            _victory = victory;
            UpdateSuccess(_victory);
        }

        public override int Succeeded() => _victory ? 1 : 0;
    }

    // Every troop of the army is out of the battle.
    [Serializable]
    public class ArmyDestroyed : RequirementGlobal
    {
        [SerializeField] Army _army;
        bool _destroyed;

        public override void Setup()
        {
            BattleEvents.TroopDefeated += CheckIfArmyIsDestroyed;
        }

        void CheckIfArmyIsDestroyed(Troop troop)
        {
            if (_army == null || _army.ActiveTroopCount > 0) return;
            _destroyed = true;
            BattleEvents.TroopDefeated -= CheckIfArmyIsDestroyed;
        }

        public override int Succeeded() => _destroyed ? 1 : 0;
    }
}
