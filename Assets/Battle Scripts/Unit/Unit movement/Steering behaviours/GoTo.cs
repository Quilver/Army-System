using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class GoTo : ISteeringBehaviour
    {
        [SerializeField, Range(0, 10)]
        float priority;
        IPathfinder _pathfinder;
        IMoveOrders _moveOrders;
        void Start()
        {
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _pathfinder = GetComponentInParent<IPathfinder>();
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), priority);
        }

        public override Vector2 GetForce()
        {
            if(!_moveOrders.IsMoving)return Vector2.zero;
            var path = _pathfinder.GetPath(_moveOrders.TargetPosition);
            if (path == null || path.Count < 2)
                return GetSteerDirection.Seek(_moveOrders.TargetPosition);
            return GetSteerDirection.Seek(_pathfinder.GetPath(_moveOrders.TargetPosition)[1]);
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled) return;
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _pathfinder = GetComponentInParent<IPathfinder>();
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.parent.position, GetForce());
        }
    }
}