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
        protected override void DisableEvents()
        {
            MoveOrders.finishedMovement -= Enter;
            if(Unit == null)return;
            Unit.StateChanged-= Disrupt;
        }

        protected override void EnableEvents()
        {
            MoveOrders.finishedMovement += Enter;
            Unit.StateChanged += Disrupt;
        }
        bool _stopping;
        void Disrupt(UnitState state)
        {
            if(state == UnitState.Idle)
            {
                Enter();
                _stopping=true;
            }
            else if(state == UnitState.Moving || state == UnitState.Fighting || state== UnitState.Fleeing)
            {
                Exit();
            }
        }
        Rigidbody2D _rigidbody;
        Rigidbody2D Body
        {
            get
            {
                if(_rigidbody == null)_rigidbody=GetComponentInParent<Rigidbody2D>();
                return _rigidbody;
            }
        }
        protected override void Enter()
        {
            base.Enter();
            Invoke("Exit", 0.2f);
            Body.AddForce(-Body.velocity * MovementData.Mass * MovementData.MaxSpeed);
            Body.angularVelocity = 0;
        }
        protected override void Exit()
        {
            base.Exit();
            _stopping=false;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo && !_stopping) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

    }
}
