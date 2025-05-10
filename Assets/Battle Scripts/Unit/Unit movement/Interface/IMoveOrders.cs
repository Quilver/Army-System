using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public abstract class IMoveOrders: MonoBehaviour
    {
        #region Events
        //Unit reached position
        public event Action finishedMovement;
        protected void FinishedMovement()=>finishedMovement?.Invoke();
        public static event Action<Transform> unitFinishedMovement;
        public void InvokeReached(Transform transform)
        {
            unitFinishedMovement?.Invoke(transform);

        }        
        //Unit moving to position
        public event Action<Vector2> moving;
        public static event Action<Vector2, Transform> movingUnit;
        public void InvokeMove(Vector2 position)
        {
            moving?.Invoke(position);
            movingUnit?.Invoke(position, transform);
        }
        //Unit moving towards target
        event Action<Transform> pursuing;
        static event Action<Transform, Transform> pursuingUnit;
        public void InvokePursuit(Transform target)
        {
            pursuing?.Invoke(target);
            pursuingUnit?.Invoke(target, transform);
        }
        #endregion
        public abstract void MoveTo(Vector2 position);
        public abstract void MoveTo(Transform target);
        public abstract bool IsMoving { get; }
        public abstract Vector2 TargetPosition { get; }
        public abstract Transform Target { get; }

    }
}