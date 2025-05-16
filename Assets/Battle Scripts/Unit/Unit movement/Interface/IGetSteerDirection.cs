using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public abstract class IGetSteerDirection : MonoBehaviour
    {
        #region Events
        public event System.Action updateSteeringForces;
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
        public Vector2 SeekError(Vector2 target)
        {
            Vector2 desiredVelocity = (target - (Vector2)transform.position).normalized * MaxSpeed;
            Vector2 errorVelocity = desiredVelocity - movementData.Velocity;
            return errorVelocity;
        }
        public Vector2 Seek(Vector2 target, bool normalised = true)
        {
            if (movementData == null) movementData = GetComponent<IMovementData>();
            return (movementData.Velocity + SeekError(target)) / MaxSpeed;
        }

        public abstract Vector2 GetDirection();
    }
}