using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.Reaction
{
    public class RunThrough : ISteerStrategy
    {
        IMoveOrders _moverOrders;
        IMoveOrders MoveOrders
        {
            get
            {
                if (_moverOrders == null) _moverOrders = GetComponentInParent<IMoveOrders>();
                return _moverOrders;
            }
        }
        IUnit _unit;
        IUnit Unit
        {
            get
            {
                if (_unit == null) _unit = GetComponentInParent<IUnit>();
                return _unit;
            }
        }
        IMovementData _movementData;
        IMovementData MovementData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        protected override void DisableEvents()
        {
            if (Unit == null) return;
            Unit.EnteredMelee += Enter;
            Unit.ExitedMelee += Exit;
        }

        protected override void EnableEvents()
        {
            Unit.EnteredMelee += Enter;
            Unit.ExitedMelee += Exit;
        }
        void Melee(bool combat)
        {
            if (combat) Enter();
            else Exit();
        }

        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

    }
}