using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class StopAndTurn : ISteeringBehaviour
    {

        public override void AddForce()
        {
            if (GetMoveOrders.FaceTowards == null)
            {
                enabled = false;
                return;
            }
            GetSteerDirection.AddTurnForce(GetTurnToForce(), 1);

        }

        public override Vector2 GetForce()
        {
            return Vector2.zero;
        }
        public override Vector2 GetTurnToForce()
        {
            return GetMoveOrders.FaceTowards.Value;
        }
        protected override void OnDrawGizmos()
        {
            if(!DrawGizmo || !enabled || GetMoveOrders.FaceTowards == null) return;
            Gizmos.DrawLine(transform.position, GetMoveOrders.FaceTowards.Value);
        }
    }
}