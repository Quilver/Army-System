using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    class GoTo : ISteeringBehaviour
    {
        [SerializeField, Range(0, 10)]
        float priority;
        IPathfinder _pathfinder;
        IPathfinder Pathfinder { 
            get { 
                if (_pathfinder == null) _pathfinder = GetComponentInParent<IPathfinder>();
                return _pathfinder; 
            } 
        }
        [SerializeField] Vector2 _force;
        [SerializeField] float _speed;
        public override void AddForce()
        {
            _force=GetForce();_speed=_force.magnitude;
            GetSteerDirection.AddForce(GetForce(), priority);
        }

        public override Vector2 GetForce()
        {
            if(!GetMoveOrders.IsMoving)return Vector2.zero;
            var path = Pathfinder.GetPath(GetMoveOrders.TargetPosition);
            Vector2 targetPoint;
            if (path == null || path.Count < 2)
                targetPoint = GetMoveOrders.TargetPosition;
            else
                targetPoint = path[1];
            return (GetSteerDirection.Seek(targetPoint));
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled) return;
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.parent.position, GetForce());
        }
    }
}