using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Shooting
{
    public class TargetNearestEnemy : MonoBehaviour, IRangedTargeter
    {
        FieldofView _fieldOfView;
        ArmyData _armyData;
        void Start()
        {
            _fieldOfView = GetComponentInChildren<FieldofView>();
            _armyData = GetComponentInParent<ArmyData>();
        }
        public List<Transform> ValidTargets
        {
            get
            {
                if(_fieldOfView == null || _fieldOfView._targets == null)
                    return new List<Transform>();
                var unvlaidatedTargets = _fieldOfView._targets.Keys;
                var validatedTargets = unvlaidatedTargets.Where(target => target.GetComponentInParent<Army>() != GetComponentInParent<Army>()).ToList();
                return validatedTargets;
            }
        }

        public Transform Target
        {
            get
            {
                if(_fieldOfView == null || _fieldOfView._targets == null || _fieldOfView._targets.Count == 0)
                    return null;
                float min = _fieldOfView._targets[ValidTargets[0]];
                var bestMatch = ValidTargets[0];
                foreach( var target in ValidTargets )
                {
                    if (_fieldOfView._targets[target] < min)
                    {
                        bestMatch = target;
                        min = _fieldOfView._targets[target];
                    }
                }
                return bestMatch;
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var target in ValidTargets) {
                if (target == Target)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
                Gizmos.DrawSphere(target.transform.position, 0.2f);
            }
        }
    }
}