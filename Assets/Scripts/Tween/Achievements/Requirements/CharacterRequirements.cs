using System;
using System.Collections.Generic;
using UnityEngine;

namespace AchievementSystem
{
    // "Survive the battle": the character's troop was not routed or wiped out when the battle ended (SU-61).
    [Serializable]
    public class CharacterSurvived : CharacterRequirement
    {
        [NonSerialized] Troop _troop;
        [NonSerialized] bool _defeated;
        [NonSerialized] bool _ended;

        protected override void OnBegin()
        {
            _troop = Troop;
            _defeated = _troop.IsDefeated;
            _troop.Defeated += OnDefeated;
        }

        // The C# event outlives a destroyed troop, so unsubscribe by reference.
        protected override void OnEnd()
        {
            if (!ReferenceEquals(_troop, null)) _troop.Defeated -= OnDefeated;
        }

        void OnDefeated(Troop _) => _defeated = true;
        protected override void OnBattleEnded(bool victory) => _ended = true;

        public override int Level => _ended && !_defeated ? 1 : 0;
    }

    // "Make X kills": enemy soldiers whose killing blow came from the character's troop, melee or ranged (SU-61).
    [Serializable]
    public class KillTracker : CountedRequirement
    {
        protected override void OnBegin() => BattleEvents.SoldierKilled += OnSoldierKilled;
        protected override void OnEnd() => BattleEvents.SoldierKilled -= OnSoldierKilled;

        void OnSoldierKilled(Troop victim, SoldierAgentState soldier, Troop attacker)
        {
            if (!ReferenceEquals(attacker, Troop) || attacker == null || !IsEnemy(victim)) return;
            Add();
        }

        public override string Progress => $"{Count}/{Required} kills";
    }

    // Capture points held by the character's army; the level is the number held at the end.
    [Serializable]
    public class CapturePoints : CharacterRequirement
    {
        [NonSerialized] Army _army;
        [NonSerialized] List<CapturePoint> _held;

        protected override void OnBegin()
        {
            _army = Troop.Army;
            _held = new List<CapturePoint>();
            CapturePoint.CapturedBy += OnCaptured;
        }

        protected override void OnEnd() => CapturePoint.CapturedBy -= OnCaptured;

        void OnCaptured(CapturePoint capturePoint, Army controller)
        {
            if (capturePoint == null) return;
            if (controller == _army && !_held.Contains(capturePoint)) _held.Add(capturePoint);
            else if (controller != _army) _held.Remove(capturePoint);
        }

        public override int Level => _held != null ? _held.Count : 0;
    }
}
