using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Shooting
{
    public class TargetOrdered: MonoBehaviour, IRangedTargeter
    {
        FieldofView _fieldOfView;
        Army _armyData;
        void Start()
        {
            _fieldOfView = GetComponentInChildren<FieldofView>();
            _armyData = GetComponentInParent<Army>();
        }
        void OnEnable()=> GetComponentInParent<OrderTarget>().TargetObject += SetTarget;
        void OnDisable() {
            var order = GetComponentInParent<OrderTarget>();
            if (order != null)
                order.TargetObject-= SetTarget;
        }
        Transform _desiredTarget;
        void SetTarget(Transform target)
        {
            Debug.Log($"Setting Target to {target.gameObject.name}");
            _desiredTarget = target;
        }
        void SetTarget(Vector2 target)
        {

        }
        public List<Transform> ValidTargets
        {
            get => _validTargets;
        }
        [SerializeField]
        List<Transform> _validTargets;
        List<Transform> CalculateValidTargets()
        {
            if (_fieldOfView == null || _fieldOfView._targets == null)
            {
                Debug.LogWarning("missing field of view");
                return new List<Transform>();
            }
            var unvlaidatedTargets = _fieldOfView._targets.Keys;
            var validatedTargets = unvlaidatedTargets.Where(target => target.GetComponentInParent<Army>() != GetComponentInParent<Army>()).ToList();
            return validatedTargets;
        }
        void Update() => _validTargets = CalculateValidTargets();
        public Transform Target
        {
            get
            {
                if(ValidTargets.Contains(_desiredTarget)) return _desiredTarget;
                return null;
            }
        }

        private void OnDrawGizmos()
        {
            if(Target == null) return;
            foreach (var target in ValidTargets)
            {
                if (target == Target)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
                Gizmos.DrawSphere(target.transform.position, 0.2f);
            }
        }
    }
}