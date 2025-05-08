using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class MoveData : MonoBehaviour, IMovementData
    {
        Rigidbody2D _body;
        Formation.FormationData _formationData;
        Formation.IShape _formationShape;
        IUnit _unitTemplate;
        public float Mass
        {
            get
            {
                if(_formationData == null) _formationData = transform.parent.GetComponentInChildren<Formation.FormationData>();
                return _formationData.ModelCount + 3;
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
                return _unitTemplate.Stats.MoveSpeed.CurrentStat;
            }
        }

        public float Force
        {
            get
            {
                return (MaxSpeed * 3 + 5);
            }
        }
    }
}