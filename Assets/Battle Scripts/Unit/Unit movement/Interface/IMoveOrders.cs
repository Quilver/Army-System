using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
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
        public event Action OrderReceived;
        public event Action<Vector2> moving;
        public static event Action<Vector2, Transform> movingUnit;
        public void InvokeMove(Vector2 position)
        {
            OrderReceived?.Invoke();
            moving?.Invoke(position);
            movingUnit?.Invoke(position, transform);
        }
        //Unit moving towards target
        public event Action<Transform> pursuing;
        static event Action<Transform, Transform> pursuingUnit;
        public void InvokePursuit(Transform target)
        {
            OrderReceived?.Invoke();
            pursuing?.Invoke(target);
            pursuingUnit?.Invoke(target, transform);
        }
        #endregion
        public void Halt()=>FinishedMovement();
        public abstract void MoveTo(Vector2 position, Vector2? faceDirection = null);
        public abstract void MoveTo(Transform target);
        public abstract bool IsMoving { get; }
        public abstract Vector2 TargetPosition { get; }
        public abstract Transform Target { get; }
        public abstract Vector2? FaceTowards { get; }
    }
}