using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace MovementSystem
{
    public class MaxSpeedMovement : MonoBehaviour, IMoveUnit
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
        Rigidbody2D _body;
        Vector2 force;
        void Start()
        {
            _direction = GetComponent<IGetSteerDirection>();
            _moveData = GetComponent<IMovementData>();
            if (_body == null) _body = transform.parent.GetComponent<Rigidbody2D>();

        }
        [SerializeField] Vector2 _velocity;
        [SerializeField] float _speed, _force;
        void FixedUpdate()
        {
            _velocity = _direction.GetDirection();
            _speed = _velocity.magnitude;
            MoveUnit(_velocity);
        }
        public void MoveUnit(Vector2 direction)
        {
            forceGain = CalcForce();
            _force += forceGain;
            //_body.AddForce(_force * Time.deltaTime * Vector2.up);
            //_body.AddForce(_force * Time.deltaTime * direction);
            _body.AddForce(_moveData.Force * Time.deltaTime * direction);
        }
        [SerializeField]
        float ProportionalGain = 5; //increase to improve response time
        [SerializeField]
        float DerivativeGain = 3; //increase to improve smoothness
        [SerializeField]
        float previousVelocity_error, error, forceGain, derivative;
        float CalcForce()
        {
            error = _moveData.MaxSpeed - _body.velocity.magnitude;
            derivative = (error - previousVelocity_error) / Time.deltaTime;
            previousVelocity_error = error;
            return ProportionalGain * error + DerivativeGain * derivative;            
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