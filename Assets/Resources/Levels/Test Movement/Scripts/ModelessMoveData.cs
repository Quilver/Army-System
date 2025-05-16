using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class ModelessMoveData : MonoBehaviour, IMovementData
    {
        Rigidbody2D _body;
        IUnit _unitTemplate;
        public float Mass
        {
            get
            {
                if (!_body) _body = GetComponentInParent<Rigidbody2D>();
                return _body.mass;
            }
        }
        public Vector2 Velocity
        {
            get
            {
                if (!_body) _body = GetComponentInParent<Rigidbody2D>();
                return _body.velocity;
            }
        }

        public Vector2 Center
        {
            get
            {
                return transform.position;
            }
        }

        public Vector2 FuturePosition
        {
            get
            {
                if (!_body) _body = GetComponentInParent<Rigidbody2D>();
                return Center + _body.velocity;
            }
        }

        public float MaxSpeed
        {
            get
            {
                if (_unitTemplate == null) _unitTemplate = GetComponentInParent<IUnit>();
                return _unitTemplate.Stats.Movement/5;
            }
        }
        public float Force
        {
            get
            {
                return MaxSpeed * 50 * Mass;
                //return (MaxSpeed * Mass);
            }
        }
    }
}