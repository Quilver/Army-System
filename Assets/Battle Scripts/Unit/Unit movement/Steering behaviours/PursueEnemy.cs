using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class PursueEnemy : ISteeringBehaviour
    {
        IMovementData _movementData;
        IMovementData MovementData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), 1);
        }

        public override Vector2 GetForce()
        {
            return transform.up;
        }
    }
}