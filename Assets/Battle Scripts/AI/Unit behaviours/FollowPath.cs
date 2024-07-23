using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnitBehaviour
{
    public class FollowPath : UnitAISystem.UnitBehaviour
    {
        [SerializeField]
        List<Vector2> waypoints;
        Vector2 nextPoint;
        UnitBase unit;
        private void Start()
        {
            unit = GetComponentInParent<UnitBase>();
        }
        private void Update()
        {
            GetNextPoint();
        }
        void GetNextPoint()
        {
            if (Vector3.Distance(unit.Movement.Location, waypoints[0]) < 1)
            {
                waypoints.RemoveAt(0);
            }
            nextPoint = waypoints[0];
            GetComponent<UnitBase>().Movement.MoveTo(nextPoint);
        }
        private void OnDrawGizmosSelected()
        {
            if (unit == null || waypoints == null || waypoints.Count == 0) return;
            Vector2 lastPoint = unit.LeadModelPosition;
            foreach (var point in waypoints)
            {
                Gizmos.DrawLine(lastPoint, point);
                lastPoint = point;
            }
        }
    }
}