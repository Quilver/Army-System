using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class SmoothedMovement : MonoBehaviour, IMoveUnit
    {
        [SerializeField]
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if (_body == null) _body = GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        IUnit _unit;
        IGetSteerDirection _direction;
        IGetSteerDirection GetSteerDirection
        {
            get
            {
                if (_direction == null) _direction = GetComponent<IGetSteerDirection>();
                return _direction;
            }
        }
        Formation.IFormationData _formation;
        float Mass
        {
            get
            {
                if (_formation == null) transform.parent.GetComponentInChildren<Formation.IFormationData>();
                return _formation.ModelCount + 4;
            }
        }
        float Force
        {
            get
            {
                return 3 * (3 + _unit.Stats.Movement);
            }
        }
        public void MoveUnit(Vector2 direction)
        {
            Body.AddForce(Mass * Force * direction * Time.deltaTime);
        }
        Vector2 force;
        void Start()
        {
            _direction = GetComponent<IGetSteerDirection>();
            _formation = transform.parent.GetComponentInChildren<Formation.IFormationData>();
            _unit = GetComponentInParent<IUnit>();

        }
        [SerializeField, Range(1, 10)]
        float _forceChangeRate;
        void Update()
        {
            force = Vector2.MoveTowards(force, _direction.GetDirection(), _forceChangeRate * Time.deltaTime);
            MoveUnit(force);
        }

        [SerializeField]
        bool DrawGizmo;
        void OnDrawGizmo()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, force);
        }
    }
}