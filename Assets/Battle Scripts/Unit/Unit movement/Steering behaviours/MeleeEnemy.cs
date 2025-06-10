using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class MeleeEnemy : ISteeringBehaviour
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
        public override void AddForce()
        {
            
            GetSteerDirection.AddForce(GetForce(), 1);
            if(GetTurnToForce() != Vector2.zero)
                GetSteerDirection.AddTurnForce(GetTurnToForce(), 4);
        }
        [SerializeField, Range(2, 6)] float MinPursuitRange = 3;
        [SerializeField, Range(1, 4)] float PursuitSpeedBonus = 1.5f;
        public override Vector2 GetForce()
        {
            if(GetMoveOrders.Target == null) return Vector2.zero;
            var direction = GetMoveOrders.Target.position - Unit.transform.position;
            var distance = direction.magnitude;
            //if not in contact speed up for charge
            if (distance > MinPursuitRange)
                return PursuitSpeedBonus * (GetMoveOrders.Target.position - Unit.transform.position).normalized;
            //else push as long as it is not too close
            else
                return direction.normalized;// Vector2.Lerp(Vector2.zero, direction.normalized, distance / MinPursuitRange);
            //Vector2.Lerp(Vector2.zero, Unit.transform.up)
            //MoveOrders.Target.
            //return Unit.transform.up;
        }
        public override Vector2 GetTurnToForce()
        {
            if (GetMoveOrders.Target == null) return Vector2.zero;
            var direction = GetMoveOrders.Target.position - Unit.transform.position;
            var distance = direction.magnitude;
            //if not in contact speed up for charge
            if (distance > MinPursuitRange)
                return Vector2.zero;
            return GetMoveOrders.Target.position;
        }
        private void OnDrawGizmosSelected()
        {
            if(!enabled || GetMoveOrders.Target == null) return;
            if(Vector2.Distance(Unit.transform.position, GetMoveOrders.Target.position) > MinPursuitRange)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}