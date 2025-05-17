using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class KinematicMoveData : MonoBehaviour, IMovementData
    {
        IUnit _unitTemplate;
        public float Mass
        {
            get
            {
                Debug.LogWarning("Kinematic unit should not be using mass");
                return 1;
            }
        }
        Vector2 _oldPosition;
        [SerializeField] Vector2 _velocity;
        [SerializeField] float _speed;
        public Vector2 Velocity=> _velocity;
        
        void FixedUpdate()
        {
            _velocity = ((Vector2)transform.parent.position - _oldPosition)/Time.deltaTime;
            _speed=_velocity.magnitude;
            _oldPosition = transform.parent.position;
        }
        public Vector2 Center
        {
            get
            {
                return transform.position;
            }
        }
        [SerializeField, Range(0.3f, 3)]
        float _lookAhead;
        public Vector2 FuturePosition
        {
            get
            {
                return Center + _lookAhead*Velocity;
            }
        }

        public float MaxSpeed
        {
            get
            {
                if (_unitTemplate == null) _unitTemplate = GetComponentInParent<IUnit>();
                return _unitTemplate.Stats.Movement;
            }
        }
        public float Force
        {
            get
            {
                return MaxSpeed * 15 * Time.deltaTime;
            }
        }
    }
}