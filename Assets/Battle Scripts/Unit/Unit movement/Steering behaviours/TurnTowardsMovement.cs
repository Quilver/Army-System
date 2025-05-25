using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class TurnTowardsMovement : ISteeringBehaviour
    {

        public override void AddForce()
        {
            if (GetMovementData.Velocity.magnitude > 0.1f) 
                GetSteerDirection.AddTurnForce(GetTurnToForce(), 1);

        }

        public override Vector2 GetForce()
        {
            return Vector2.zero;
        }
        public override Vector2 GetTurnToForce()
        {
            return GetMovementData.FuturePosition;
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo || !enabled || GetMoveOrders.FaceTowards == null) return;
            Gizmos.DrawLine(transform.position, GetTurnToForce());
        }
    }
}