using Campaign;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [Serializable]
    public abstract class RequirementsPCs 
    {
        protected PCWrapper _pc;
        protected IUnit _PCsUnit;
        public event System.Action<bool> UpdatedSuccess;
        protected void UpdateSuccess(bool success)=>UpdatedSuccess?.Invoke(success);
        public void Setup(Campaign.PCWrapper pc, IUnit PCsUnit)
        {
            _pc = pc;
            _PCsUnit = PCsUnit;
            if(Valid())
                SetupListeners();
        }
        public virtual bool Valid()=>true;
        protected abstract void SetupListeners();
        public abstract int Succeeded();
    }
    [Serializable]
    public class KillTracker : RequirementsPCs
    {
        [SerializeField, Range(1, 100)]
        int killsRequired;
        int killsMade;
        protected override void SetupListeners()
        {
            ModelComponents.ITakeDamage.kill += RecordKills;
        }
        void RecordKills(IUnit attacker, IUnit victim)
        {
            if (attacker != _PCsUnit) return;
            killsMade++;
            if(killsMade == killsRequired) UpdateSuccess(true);
        }
        public override int Succeeded()
        {
            if (killsMade < killsRequired) return 0;
            return 1;
        }
    }
    [Serializable]
    public class CharacterSurvived : RequirementsPCs
    {
        bool _alive = true;
        protected override void SetupListeners()
        {
            _PCsUnit.UnitDestroyed += () => _alive = false;
            UpdateSuccess(false);
        }

        public override int Succeeded()
        {
            return (_alive) ? 1 : 0;
        }
    }
    [Serializable]
    public class CapturePoints : RequirementsPCs
    {
        Army army;
        List<CapturePoint> capturePoints;
        protected override void SetupListeners()
        {
            army = _PCsUnit.GetComponentInParent<Army>();
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
        public override int Succeeded()
        {
            return capturePoints.Count;
        }

        
    }

}