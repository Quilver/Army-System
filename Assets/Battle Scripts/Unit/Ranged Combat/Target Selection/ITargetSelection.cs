using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RangedWeapons.Target
{
    [System.Serializable]
    public abstract class ITargetSelection
    {
        protected IUnit unit;
        [System.Serializable]
        public struct Target
        {
            public Transform target;
            public Vector2? targetPoint, towards;
            public bool ValidTarget=>target != null || targetPoint != null;
            public Target(Transform target) {  this.target = target;  targetPoint = null; towards = null; }
            public Target(Vector2 target) { this.target = null; targetPoint = target; towards = null; }
            public Target(Vector2 target, Vector2 targetFacing) { this.target = null; targetPoint = target; towards = targetFacing; }
        }
        public virtual void Setup(IUnit unit)
        {
            this.unit = unit;
        }
        public abstract Target CurrentTarget { get; }
        public abstract List<Target> ValidTargets { get; }
    }
    [System.Serializable]
    public abstract class ITargetEnemy : ITargetSelection
    {
        FieldofView _fieldOfView;
        Army _army;
        public override void Setup(IUnit unit)
        {
            base.Setup(unit);
            _fieldOfView = unit.GetComponentInChildren<FieldofView>();
            _army = unit.GetComponentInParent<Army>();

        }
        public Target NearestTarget()
        {
            if (ValidTargets.Count == 0) return new Target();
            float distance = float.MaxValue;
            Target target = ValidTargets[0];
            foreach (Target _target in ValidTargets)
            {
                float _distance = Vector2.Distance(_target.target.transform.position, unit.transform.position);
                if (_distance < distance)
                {
                    distance = _distance;
                    target = _target;
                }
            }
            return target;
        }
        [SerializeField] List<Target> _validTargets; 
        public override List<Target> ValidTargets
        {
            get
            {
                _validTargets = new List<Target>();
                if (_fieldOfView == null || _fieldOfView._targets == null)
                {
                    //Debug.LogWarning("missing field of view");
                    return _validTargets;
                }
                var unvalidatedTargets = _fieldOfView._targets.Keys;
                foreach (var target in unvalidatedTargets)
                {
                    if (target == null) continue;
                    var unit = target.GetComponent<IUnit>();
                    if (unit == null || !_army.Enemies.Contains(unit)) continue;
                    _validTargets.Add(new (target));
                }
                return _validTargets;
            }
        }
    }
    [System.Serializable]
    public class VolleyTarget : ITargetEnemy
    {
        public override void Setup(IUnit unit)
        {
            base.Setup(unit);
            unit.GetComponent<OrderTarget>().TargetObject+=SetTarget;
            unit.GetComponent<OrderTarget>().TargetPosition+=SetTarget;

        }
        Target _desiredTarget;
        void SetTarget(Transform target)=>_desiredTarget = new(target);
        void SetTarget(Vector2 target)=>_desiredTarget = new(target);
        [SerializeField] Target currenTarget;
        public override Target CurrentTarget
        {
            get
            {
                if (!_desiredTarget.ValidTarget && ValidTargets.Contains(_desiredTarget))
                    return _desiredTarget;
                else
                    return NearestTarget();
            }
        }
    }
}