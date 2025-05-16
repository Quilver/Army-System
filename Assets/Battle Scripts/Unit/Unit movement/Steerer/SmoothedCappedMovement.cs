using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace MovementSystem
{
    public class SmoothedCappedMovement : MonoBehaviour, IMoveUnit
    {
        
        IGetSteerDirection _direction;
        IGetSteerDirection GetSteerDirection
        {
            get
            {
                if (_direction == null) _direction = GetComponent<IGetSteerDirection>();
                return _direction;
            }
        }
        [SerializeField]
        IMovementData _moveData;
        [SerializeField]
        float _currentSpeed;
        [SerializeField]
        float _speed;
        [SerializeField]
        Rigidbody2D _body;
        public void MoveUnit(Vector2 direction)
        {
            _body.AddForce(_moveData.Force * direction * Time.deltaTime);
            /*
            _speed=_direction.MaxSpeed;
            _currentSpeed = _body.velocity.magnitude;
            _body.AddForce(_moveData.Mass * _moveData.Force * direction * Time.deltaTime);
            if (_body.velocity.magnitude > _currentSpeed && _direction.MaxSpeed < _currentSpeed)
                _body.velocity = _body.velocity.normalized * _currentSpeed;
            else if (_body.velocity.magnitude > _direction.MaxSpeed)
                _body.velocity = _body.velocity.normalized * _direction.MaxSpeed;
            */
        }
        Vector2 force;
        void Start()
        {
            _direction = GetComponent<IGetSteerDirection>();
            _moveData = GetComponent<IMovementData>();
            if(_body ==null)_body = transform.parent.GetComponent<Rigidbody2D>();

        }
        [SerializeField, Range(1, 10)]
        float _forceChangeRate;
        void FixedUpdate()
        {
            var newForce = Vector2.ClampMagnitude(_direction.GetDirection(), _forceChangeRate);
            force = Vector2.MoveTowards(force, newForce, _forceChangeRate * Time.deltaTime);
            MoveUnit(_direction.GetDirection());
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