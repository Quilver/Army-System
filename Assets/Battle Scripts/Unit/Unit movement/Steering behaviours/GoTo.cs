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
        IMoveOrders _moveOrders;
        IMoveOrders MoveOrders
        {
            get {
                if(_moveOrders == null) _moveOrders = GetComponentInParent<IMoveOrders>();
                return _moveOrders;
            }
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), priority);
        }

        public override Vector2 GetForce()
        {
            if(!MoveOrders.IsMoving)return Vector2.zero;
            var path = Pathfinder.GetPath(MoveOrders.TargetPosition);
            if (path == null || path.Count < 2)
                return GetSteerDirection.Seek(MoveOrders.TargetPosition);
            return GetSteerDirection.Seek(path[1]);
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled) return;
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.parent.position, GetForce());
        }
    }
}