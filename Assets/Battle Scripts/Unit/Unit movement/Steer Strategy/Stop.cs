using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.Reaction
{
    public class Stop : ISteerStrategy
    {
        IMoveOrders _moverOrders;
        IMoveOrders MoveOrders { 
            get { 
                if (_moverOrders == null)_moverOrders=GetComponentInParent<IMoveOrders>();
                return _moverOrders; 
            } 
        }
        IUnit _unit;
        IUnit Unit
        {
            get
            {
                if(_unit == null)_unit=GetComponentInParent<IUnit>();
                return _unit;
            }
        }
        IMovementData _movementData;
        IMovementData MovementData
        {
            get
            {
                if (_movementData == null)_movementData=GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        protected override void EnableEvents()
        {
            MoveOrders.finishedMovement += Enter;
            Unit.StateChanged += Disrupt;
        }
        void Disrupt(UnitState state)
        {
            if(state == UnitState.Idle)
            {
                Enter();
            }
            else if(state == UnitState.Moving || state == UnitState.Fighting || state== UnitState.Fleeing)
            {
                
            }
        }
        protected override void DisableEvents()
        {
            MoveOrders.finishedMovement -= Enter;
            if(Unit == null)return;
            Unit.StateChanged-= Disrupt;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

    }
}
