using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    [System.Serializable]   
    public struct UnitOrder
    {
        public bool moving;
        public Transform target;
        public Vector2? position, faceDirection;
        #region Constructors
        public UnitOrder(Transform target, bool moving = true)
        {
            this.target = target;
            position = null;
            faceDirection = null;
            this.moving = moving;
        }
        public UnitOrder(Vector2 position, bool moving = true)
        {
            this.target = null;
            this.position = position;
            faceDirection = null;
            this.moving = moving;
        }
        public UnitOrder(Vector2 position, Vector2 face, bool moving = true)
        {
            this.target = null;
            this.position = position;
            faceDirection = face;
            this.moving = moving;
        }
        public UnitOrder(Transform target, Vector2 position, Vector2 face, bool moving = true)
        {
            this.target = target;
            this.position = position;
            faceDirection = face;
            this.moving = moving;
        }
        #endregion
        public void MakeOrder(IUnit unit)
        {
            if (moving)
                MoveOrder(unit);
            else
                SetTarget(unit);
        }
        void MoveOrder(IUnit unit)
        {
            var move = unit.GetComponent<MovementSystem.IMoveOrders>();
            if (position.HasValue && faceDirection.HasValue) move.MoveTo(position.Value, faceDirection);
            else if (target != null) move.MoveTo(target);
            else if (position != null) move.MoveTo(position.Value);
            else Debug.LogError($"Unset halt for {unit.name} is currently invalid");
        }
        void SetTarget(IUnit unit)
        {
            var order = unit.GetComponent<OrderTarget>();
            if(target != null && position.HasValue && faceDirection.HasValue)
            {
                MoveOrder(unit);
                order.Order(target);
            }
            else if (position.HasValue && faceDirection.HasValue) order.Order(position.Value, faceDirection.Value);
            else if (target != null) order.Order(target);
            else if (position != null) order.Order(position.Value);
        }
        public bool ValidOrder=>target != null || position.HasValue;
        public override string ToString()
        {
            if(moving)
                return ToMoveString();
            else
                return ToTargetString();
        }
        string ToMoveString()
        {
            if (position.HasValue && faceDirection.HasValue)
                return $"Move to {position.Value} and face towards {faceDirection.Value}";
            else if (position.HasValue)
                return $"Move to {position.Value}";
            if (target != null)
                return $"Charge {target.gameObject.name}";
            else
                return $"Currently illegal, but should be halt";
        }
        string ToTargetString()
        {
            if (position.HasValue && faceDirection.HasValue)
                return $"Set target between {position.Value} and {faceDirection.Value}";
            else if (position.HasValue)
                return $"Set target to {position.Value}";
            if (target != null)
                return $"Set target to {target.gameObject.name}";
            else
                return $"Currently illegal, but should be remove target";
        }
        public override bool Equals(object obj)=>base.Equals(obj);
        public override int GetHashCode()=>base.GetHashCode();
        public static bool operator ==(UnitOrder a, UnitOrder b)
        {
            if(a.moving != b.moving) return false;
            if(a.position != b.position) return false;
            if(a.target != b.target) return false;
            if (a.faceDirection != b.faceDirection) return false;
            return true;
        }
        public static bool operator !=(UnitOrder a, UnitOrder b) => !(a==b);
    }
}