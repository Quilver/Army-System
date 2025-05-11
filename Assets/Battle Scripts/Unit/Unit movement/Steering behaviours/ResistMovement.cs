using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public class ResistMovement : ISteeringBehaviour
    {
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
        }

        public override Vector2 GetForce()
        {
            return -Body.totalForce / 2;
        }
    }
}
