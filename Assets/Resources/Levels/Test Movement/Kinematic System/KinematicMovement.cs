using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace MovementSystem
{
    public class KinematicMovement : MonoBehaviour, IMoveUnit
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
        void Start()
        {
            GetComponentInParent<IUnit>().UnitDestroyed += () => Destroy(gameObject);
            _direction = GetComponent<IGetSteerDirection>();
            _moveData = GetComponent<IMovementData>();
        }
        [SerializeField] Vector2 _velocity;
        [SerializeField] float _speed, _force;
        void FixedUpdate()
        {
            MoveUnit(Vector2.zero);
        }
        public event System.Action UpdatePos;
        public event System.Action<Vector2> SetDirection;
        Vector2 _meanPos, _meanFacing, _meanVelocity;float _total;
        public void UpdatePosAndFacing(Vector2 pos, Vector2 facing, Vector2 velocity)
        {
            _total++;
            _meanPos += pos;
            _meanFacing += facing;
            _meanVelocity += velocity;
        }
        public float _averageSpeed;
        [SerializeField, Range(2, 10)] float _maxTurnSpeed = 5;
        void UpdateTransform()
        {

            _total = 0; _meanFacing = Vector2.zero; _meanPos = Vector2.zero;
            UpdatePos?.Invoke();
            if(_total==0)_averageSpeed = 0;else _averageSpeed = (_meanVelocity/_total).magnitude;
            if (_total == 0)return;
            transform.parent.position += (Vector3)_meanPos / _total;
            transform.parent.up = Vector3.MoveTowards(transform.parent.up, _meanFacing/_total, _maxTurnSpeed * Time.deltaTime);  
            //transform.parent.up += (Vector3)_meanFacing / _total;
        }
        public void MoveUnit(Vector2 direction)
        {
            UpdateTransform();
            _velocity = _direction.GetDirection();
            _speed = _velocity.magnitude;
            SetDirection?.Invoke(_velocity * _moveData.MaxSpeed);
        }
        [SerializeField]
        bool DrawGizmo;
        void OnDrawGizmo()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, _velocity);
        }
    }
}