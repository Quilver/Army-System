using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class ResistMovement : ISteeringBehaviour
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
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if (_body == null) _body = GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), 1);
            Body.angularVelocity = -Body.angularVelocity/2;
        }

        public override Vector2 GetForce()
        {
            return -Body.velocity * 5;
        }
    }
}
