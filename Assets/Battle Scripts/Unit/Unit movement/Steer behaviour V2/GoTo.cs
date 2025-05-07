using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class GoTo : MonoBehaviour, ISteeringBehaviour
    {
        [SerializeField, Range(0, 10)]
        float priority;
        IGetSteerDirection _direction;
        IPathfinder _pathfinder;
        IMoveOrders _moveOrders;
        void Start()
        {
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _pathfinder = GetComponentInParent<IPathfinder>();
            _direction = GetComponentInParent<IGetSteerDirection>();
            _direction.updateSteeringForces += AddForce;
        }
        public void AddForce()
        {
            _direction.AddForce(GetForce(), priority);
        }

        public Vector2 GetForce()
        {
            if(!_moveOrders.IsMoving)return Vector2.zero;
            var path = _pathfinder.GetPath(_moveOrders.TargetPosition);
            if (path == null || path.Count < 2)
                return _direction.Seek(_moveOrders.TargetPosition);
            return _direction.Seek(_pathfinder.GetPath(_moveOrders.TargetPosition)[1]);
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled) return;
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _pathfinder = GetComponentInParent<IPathfinder>();
            _direction = GetComponentInParent<IGetSteerDirection>();
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.parent.position, GetForce());
        }
    }
}