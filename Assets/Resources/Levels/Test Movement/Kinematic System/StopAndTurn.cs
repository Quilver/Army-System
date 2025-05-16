using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class StopAndTurn : ISteeringBehaviour
    {

        public override void AddForce()
        {
            if (GetMoveOrders.FaceTowards == null) return;
            transform.parent.parent.up = ((Vector3)GetMoveOrders.FaceTowards.Value - transform.position).normalized;
        }

        public override Vector2 GetForce()
        {
            return Vector2.zero;
        }
        protected override void OnDrawGizmos()
        {
            if(!DrawGizmo || !enabled || GetMoveOrders.FaceTowards == null) return;
            Gizmos.DrawLine(transform.position, GetMoveOrders.FaceTowards.Value);
        }
    }
}