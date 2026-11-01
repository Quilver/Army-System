using System;
using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace AchievementSystem
{
    // A requirement for one roster character, measured on the troop that character leads in this battle.
    [Serializable]
    public abstract class RequirementsPCs
    {
        protected RosterCharacter _character;
        protected Troop _troop;
        public event System.Action<bool> UpdatedSuccess;
        protected void UpdateSuccess(bool success) => UpdatedSuccess?.Invoke(success);

        public void Setup(RosterCharacter character, Troop troop)
        {
            _character = character;
            _troop = troop;
            if (Valid())
                SetupListeners();
        }

        public virtual bool Valid() => true;
        protected abstract void SetupListeners();
        public abstract int Succeeded();
    }

    // Enemy soldiers killed by the character's troop.
    [Serializable]
    public class KillTracker : RequirementsPCs
    {
        [SerializeField, Range(1, 100)]
        int killsRequired;
        int killsMade;

        protected override void SetupListeners()
        {
            BattleEvents.SoldierKilled += RecordKill;
        }

        void RecordKill(Troop victim, SoldierAgentState soldier, Troop attacker)
        {
            if (attacker == null || attacker != _troop) return;
            killsMade++;
            if (killsMade == killsRequired) UpdateSuccess(true);
        }

        public override int Succeeded() => killsMade < killsRequired ? 0 : 1;
    }

    // The character's troop was not routed or wiped out.
    [Serializable]
    public class CharacterSurvived : RequirementsPCs
    {
        bool _alive = true;

        protected override void SetupListeners()
        {
            _troop.Defeated += _ => _alive = false;
            UpdateSuccess(false);
        }

        public override int Succeeded() => _alive ? 1 : 0;
    }

    // Capture points held by the character's army.
    [Serializable]
    public class CapturePoints : RequirementsPCs
    {
        Army army;
        readonly List<CapturePoint> capturePoints = new();

        protected override void SetupListeners()
        {
            army = _troop.Army;
            CapturePoint.CapturedBy += CaptureEvent;
        }

        void CaptureEvent(CapturePoint capturePoint, Army controller)
        {
            if (capturePoint == null) return;
            if (controller == army && !capturePoints.Contains(capturePoint))
                capturePoints.Add(capturePoint);
            else if (controller != army)
                capturePoints.Remove(capturePoint);
        }

        public override int Succeeded() => capturePoints.Count;
    }
}
