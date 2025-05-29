using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.Reaction
{
    public class GoTo : ISteerStrategy
    {
        IUnit _unit;
        IUnit Unit
        {
            get
            {
                if (_unit == null) _unit = GetComponentInParent<IUnit>();
                return _unit;
            }
        }
        IMoveOrders _moveOrders;
        IMoveOrders MoveOrders
        {
            get
            {
                if (_moveOrders == null) _moveOrders = GetComponentInParent<IMoveOrders>();
                return _moveOrders;
            }
        }
        protected override void EnableEvents()
        {
            MoveOrders.moving += Moving;
            MoveOrders.finishedMovement += Exit;
        }
        protected override void DisableEvents()
        {
            MoveOrders.moving -= Moving;
            MoveOrders.finishedMovement -= Exit;
        }
        void Moving(Vector2 pos) => Enter();
        //void Moving(Transform pos) => Enter();
    }
}