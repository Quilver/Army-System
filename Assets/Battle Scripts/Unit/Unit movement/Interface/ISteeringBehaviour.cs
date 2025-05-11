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
        private void OnEnable()
        {
            GetSteerDirection.updateSteeringForces += AddForce;
        }
        private void OnDisable()
        {
            GetSteerDirection.updateSteeringForces += AddForce;
        }
        public abstract Vector2 GetForce();
        public abstract void AddForce();
        [SerializeField]
        protected bool DrawGizmo;
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }


    }

}
