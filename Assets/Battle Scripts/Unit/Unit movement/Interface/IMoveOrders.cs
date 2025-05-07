using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface IMoveOrders
    {
        #region Events
        //Unit reached position
        public event Action finishedMovement;
        public static event Action<Transform> unitFinishedMovement;
        public void InvokeReached(Transform transform)
        {
            unitFinishedMovement?.Invoke(transform);

        }        
        //Unit moving to position
        public event Action<Vector2> moving;
        public static event Action<Vector2, Transform> movingUnit;
        public void InvokeMove(Vector2 position, Transform unit)
        {
            movingUnit?.Invoke(position, unit);
        }
        //Unit moving towards target
        event Action<Transform> pursuing;
        static event Action<Transform, Transform> pursuingUnit;
        public void InvokePursuit(Transform target, Transform unit)
        {
            pursuingUnit?.Invoke(target, unit);
        }
        #endregion
        public void MoveTo(Vector2 position);
        public void MoveTo(Transform target);
        public bool IsMoving { get; }
        public Vector2 TargetPosition { get; }
        public Transform Target { get; }

    }
}