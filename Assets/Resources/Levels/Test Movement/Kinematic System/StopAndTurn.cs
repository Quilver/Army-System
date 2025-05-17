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
            var dir = ((Vector3)GetMoveOrders.FaceTowards.Value - transform.position).normalized;
            if (Vector2.Distance(transform.parent.parent.up, dir) < 0.1f)
                return;
            transform.parent.parent.up = Vector3.MoveTowards(transform.parent.parent.up, dir, GetMovementData.MaxSpeed * Time.deltaTime / 5);

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