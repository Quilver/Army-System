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
        [SerializeField] float _speed;
        public Vector2 Velocity=> _velocity;
        public event System.Action UpdatePos;
        [SerializeField]
        Vector2 _position, _facing, _velocity;
        int counter;
        void FixedUpdate()
        {
            counter = 0; _position = Vector2.zero; _facing = Vector2.zero; _velocity = Vector2.zero;
            UpdatePos?.Invoke();
            if(counter == 0 ) return;
            _position/=counter; _facing/=counter; _velocity/=counter;

            _speed=_velocity.magnitude;

            //Setting position and facing
            transform.parent.position = _position;
            transform.parent.up = _facing;
        }
        public void UpdatePosAndFacing(Vector2 pos, Vector2 facing, Vector2 velocity)
        {
            counter++;
            _position += pos + (Vector2)transform.position;
            _facing += facing;
            _velocity += velocity;
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