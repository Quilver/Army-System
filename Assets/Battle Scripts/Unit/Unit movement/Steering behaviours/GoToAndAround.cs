using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    class GoToAndAround : ISteeringBehaviour
    {
        [SerializeField, Range(0, 10)]
        float priority;
        IPathfinder _pathfinder;
        IPathfinder Pathfinder
        {
            get
            {
                if (_pathfinder == null) _pathfinder = GetComponentInParent<IPathfinder>();
                return _pathfinder;
            }
        }
        ISensors _sensors;
        ISensors Sensors
        {
            get
            {
                if (_sensors == null) _sensors = GetComponentInParent<ISensors>();
                return _sensors;
            }
        }
        Formation.IShape _shape;
        Formation.IShape Shape
        {
            get
            {
                if (_shape == null) _shape = transform.parent.parent.GetComponentInChildren<Formation.IShape>();
                return _shape;
            }
        }
        Vector2 _force;
        float _speed;
        public override void AddForce()
        {
            _force = GetForce(); _speed = _force.magnitude;
            GetSteerDirection.AddForce(GetForce(), priority);
        }
        
        public override Vector2 GetForce()
        {
            if (!GetMoveOrders.IsMoving) return Vector2.zero;
            var path = Pathfinder.GetPath(GetMoveOrders.TargetPosition);
            Vector2 targetPoint;
            if (path != null && path.Count >= 2)
                targetPoint = PathAroundCollision(path[0], path[1]);
            else
                return Vector2.zero;
            return (GetSteerDirection.Seek(targetPoint));
        }
        Vector2 direction, dirToCollision, hitpoint, start, end, away;
        Vector2 PathAroundCollision(Vector2 start, Vector2 nextPoint)
        {
            this.start = start; this.end = nextPoint;
            direction = nextPoint - start;
            var hit = ISensors.UnitCast(start, direction, Shape.SizeOfFormation / 2);
            if (!hit || hit.transform == GetMoveOrders.Target) return nextPoint;
            hitpoint = hit.point;
            dirToCollision = hit.point - start;
            direction = Vector2.Dot(direction.normalized, dirToCollision) * direction.normalized;
            away = (Shape.SizeOfFormation.x+ 1) * (direction - dirToCollision);
            return away + hit.point;
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled || GetMoveOrders == null) return;
            Gizmos.DrawLine(start, hitpoint);
            Gizmos.color = Color.red;Gizmos.DrawRay(start, direction);
            Gizmos.color = Color.blue; Gizmos.DrawRay(start, dirToCollision);
            Gizmos.color = Color.green; Gizmos.DrawRay(hitpoint, away);

            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.parent.position, GetForce() * 5);
        }
    }
}