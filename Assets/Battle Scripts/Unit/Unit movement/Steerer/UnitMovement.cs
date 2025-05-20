using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class UnitMovement : MonoBehaviour, IMoveUnit
    {
        [SerializeField]
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if(_body == null) _body=GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        IUnit _unit;
        IGetSteerDirection _direction;
        IGetSteerDirection GetSteerDirection
        {
            get
            {
                if(_direction == null) _direction=GetComponent<IGetSteerDirection>();
                return _direction;
            }
        }
        Formation.IFormationData _formation;
        float Mass
        {
            get
            {
                if(_formation == null) transform.parent.GetComponentInChildren<Formation.IFormationData>();
                return _formation.ModelCount + 4;
            }
        }
        float Force
        {
            get
            {
                return 3 * (3+_unit.Stats.Movement);
            }
        }
        public void MoveUnit(Vector2 direction, Vector2 faceTowards)
        {
            Body.AddForce(Mass * Force * direction * Time.deltaTime);
        }
        void Start()
        {
            _direction=GetComponent<IGetSteerDirection>();
            _formation = transform.parent.GetComponentInChildren<Formation.IFormationData>();
            _unit=GetComponentInParent<IUnit>();
        }
        void Update()
        {
            MoveUnit(_direction.GetDirection(), Vector2.zero);
        }
    }
}