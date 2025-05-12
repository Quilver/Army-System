using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    [System.Serializable]   
    public struct UnitOrder
    {
        public Transform target;
        public Vector2? position;
        #region Constructors
        public UnitOrder(Transform target)
        {
            this.target = target;
            position = null;
        }
        public UnitOrder(Vector2 position)
        {
            this.target = null;
            this.position = position;
        }
        #endregion
        public void MakeOrder(IUnit unit)
        {
            var move = unit.GetComponent<MovementSystem.IMoveOrders>();
            if (target != null) move.MoveTo(target);
            else if (position != null) move.MoveTo(position.Value);
        }
    }
}