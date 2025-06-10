using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class MoveData : MonoBehaviour, IMovementData
    {
        Rigidbody2D _body;
        Formation.IFormationData _formationData;
        Formation.IShape _formationShape;
        IUnit _unitTemplate;
        public float Mass
        {
            get
            {
                if(_formationData == null) _formationData = transform.parent.GetComponentInChildren<Formation.IFormationData>();
                if (!_body) _body = GetComponentInParent<Rigidbody2D>();
                _body.mass = _formationData.ModelCount;
                return _body.mass * 2;
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
                if(_formationShape == null) _formationShape = transform.parent.GetComponentInChildren<Formation.IShape>();
                Vector3 offset = transform.up;
                offset.x *= _formationShape.OffsetFromUnit.y;
                offset.y *= _formationShape.OffsetFromUnit.y;
                return transform.position + offset;
            }
        }

        public Vector2 FuturePosition
        {
            get
            {
                if (!_body) _body = GetComponentInParent<Rigidbody2D>();
                return Center+_body.velocity;
            }
            }

        public float MaxSpeed
        {
            get {
                if(_unitTemplate==null) _unitTemplate=GetComponentInParent<IUnit>();
                return _unitTemplate.Stats.Movement/5;
            }
        }

        public float Force
        {
            get
            {
                return (MaxSpeed * Mass * 50);
            }
        }

        public event Action<Vector2> ApplyForce;
    }
}