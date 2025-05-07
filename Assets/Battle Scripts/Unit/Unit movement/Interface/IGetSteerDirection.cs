using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public abstract class IGetSteerDirection : MonoBehaviour
    {
        #region Events
        public delegate void UpdateSteeringForces();
        public event UpdateSteeringForces updateSteeringForces;
        protected void UpdateForces()=>updateSteeringForces?.Invoke();
        #endregion

        //Negative priority can be passed as means to avoid a location
        public abstract void AddForce(Vector2 direction, float priority);
        IMovementData movementData;
        public float MaxSpeed {
            get
            {
                if(movementData == null) movementData=GetComponent<IMovementData>();
                return movementData.MaxSpeed;
            }

        }
        Rigidbody2D _body;
        protected Rigidbody2D Body
        {
            get
            {
                if (_body == null) _body = GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        public Vector2 Seek(Vector2 target)
        {
            Vector2 desiredVelocity = (target - (Vector2)transform.position).normalized * MaxSpeed;
            Vector2 steerVelocity = desiredVelocity - Body.velocity;
            return steerVelocity;
        }

        public abstract Vector2 GetDirection();
    }
}