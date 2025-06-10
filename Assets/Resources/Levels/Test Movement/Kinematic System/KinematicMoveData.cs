using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class KinematicMoveData : MonoBehaviour, IMovementData
    {
        IUnit _unit;
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
        public event Action<Vector2> ApplyForce;

        [SerializeField]
        Vector2 _position, _facing, _velocity;
        int counter;
        Collider2D _collider;
        public Vector2 SeparationForce()
        {
            if(_collider == null) _collider = transform.parent.GetComponent<Collider2D>();
            var separation = Vector2.zero;
            Collider2D[] results = new Collider2D[10];
            ContactFilter2D contactFilter = new();
            contactFilter.SetLayerMask(1 << 6);
            int overlaps = _collider.OverlapCollider(contactFilter, results);
            for (int i = 0; i < overlaps; i++)
            {
                Debug.Log($"{transform.parent.name} is overlapping with: {results[i].name}");
                var overlap = _collider.Distance(results[i]);
                separation += overlap.distance * overlap.normal;
            }
            return separation;
        }
        void FixedUpdate()
        {
            counter = 0; _position = Vector2.zero; _facing = Vector2.zero; _velocity = Vector2.zero;
            UpdatePos?.Invoke();
            if(counter == 0 ) return;
            _position/=counter; _facing/=counter; _velocity/=counter;

            _speed=_velocity.magnitude;

            //Setting position and facing
            transform.parent.position = _position;// + 2 * SeparationForce();
            transform.parent.up = _facing;
            ApplyForce?.Invoke(SeparationForce());
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
                if (_unit == null) _unit = GetComponentInParent<IUnit>();
                return _unit.Stats.Movement/5f;
            }
        }
        public float Force
        {
            get
            {
                return MaxSpeed * 15 * Time.deltaTime;
            }
        }
        [SerializeField] bool drawGizmo = true;
        private void OnDrawGizmos()
        {
            if (!drawGizmo)return;
            Gizmos.DrawRay(transform.parent.position, SeparationForce());
        }
    }
}