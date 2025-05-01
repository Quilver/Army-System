using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public interface IMoveOrders
    {
        #region Events
        delegate void Moving(Vector2 target);
        delegate void MovingUnit(Vector2 target, Transform unit);
        event Moving moving;
        static event MovingUnit movingUnit;
        public void InvokeMove(Vector2 position, Transform unit)
        {
            movingUnit?.Invoke(position, unit);
        }

        delegate void Pursuing(Transform target);
        delegate void PursuingUnit(Transform target, Transform unit);
        event Pursuing pursuing;
        static event PursuingUnit pursuingUnit;
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