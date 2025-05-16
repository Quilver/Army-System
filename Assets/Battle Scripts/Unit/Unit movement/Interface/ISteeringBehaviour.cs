using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public abstract class ISteeringBehaviour : MonoBehaviour
    {
        IGetSteerDirection _getSteerDirection;
        protected IGetSteerDirection GetSteerDirection
        {
            get
            {
                if (_getSteerDirection == null)_getSteerDirection=GetComponentInParent<IGetSteerDirection>();
                return _getSteerDirection;
            }
        }
        MovementSystem.IMovementData _movementData;
        protected IMovementData GetMovementData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        IMoveOrders _moveOrders;
        protected IMoveOrders GetMoveOrders
        {
            get
            {
                if(_moveOrders==null)_moveOrders= GetComponentInParent<IMoveOrders>();
                return _moveOrders;
            }
        }
        private void OnEnable()
        {
            GetSteerDirection.updateSteeringForces += AddForce;
        }
        private void OnDisable()
        {
            GetSteerDirection.updateSteeringForces -= AddForce;
        }
        public abstract Vector2 GetForce();
        public abstract void AddForce();
        [SerializeField]
        protected bool DrawGizmo;
        protected virtual void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }


    }

}
