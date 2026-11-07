using System;
using UnityEngine;

namespace AchievementSystem
{
    // A fixed level, for testing the post-battle screen.
    [Serializable]
    public class Default : GlobalRequirement
    {
        [SerializeField, Min(0)] int _successLevel;
        public override int Level => _successLevel;
    }

    // The player won the battle.
    [Serializable]
    public class Victory : GlobalRequirement
    {
        [NonSerialized] bool _victory;
        protected override void OnBattleEnded(bool victory) => _victory = victory;
        public override int Level => _victory ? 1 : 0;
    }

    // Every troop of an army is out of the battle. Without an army set (an achievement asset cannot reference a scene
    // object) it is the enemy army.
    [Serializable]
    public class ArmyDestroyed : GlobalRequirement
    {
        [SerializeField] Army _army;
        [NonSerialized] bool _destroyed;

        Army Target => _army != null ? _army : Context?.EnemyArmy;

        protected override void OnBegin() => BattleEvents.TroopDefeated += OnTroopDefeated;
        protected override void OnEnd() => BattleEvents.TroopDefeated -= OnTroopDefeated;

        void OnTroopDefeated(Troop troop)
        {
            Army army = Target;
            if (army != null && army.ActiveTroopCount == 0) _destroyed = true;
        }

        public override int Level => _destroyed ? 1 : 0;
    }
}
