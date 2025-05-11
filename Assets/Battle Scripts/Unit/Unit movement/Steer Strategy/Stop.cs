using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
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
        protected override void DisableEvents()
        {
            MoveOrders.finishedMovement -= Enter;
            GetComponentInParent<IUnit>().StateChanged -= Disrupt;
        }

        protected override void EnableEvents()
        {
            MoveOrders.finishedMovement += Enter;
            GetComponentInParent<IUnit>().StateChanged += Disrupt;
        }
        void Disrupt(UnitState state)
        {
            if(state == UnitState.Moving || state == UnitState.Fighting || state== UnitState.Fleeing)
                Exit();
        }
    }
}
