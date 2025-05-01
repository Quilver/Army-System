using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace SteeringSystem
{
    class CalculateSteering : IGetSteerDirection
    {
        
        float _maxSpeed = 3;
        protected override float MaxSpeed => _maxSpeed;
        [SerializeField, Range(8, 32)]
        int _movementSlot = 16;
        int MovementSlots
        {
            get
            {
                return _movementSlot;
            }
        }
        Vector2 GetDirection(float angle)
        {
            //angle *= Mathf.Deg2Rad;
            return Quaternion.AngleAxis(angle, transform.forward) * transform.up;
        }
        [SerializeField]
        float[] _interest;
        float[] _avoid;
        void Update()
        {
            _interest= new float[MovementSlots];
            _avoid = new float[MovementSlots];
            UpdateForces();
        }
        [SerializeField]
        Vector2 forceAdded, nearestSlotDir;
        [SerializeField]
        float forceAngle, nearestAngle;
        public override void AddForce(Vector2 direction, float priority)
        {
            if(priority==0)return;
            float angleOfForce = Vector2.SignedAngle(Vector2.up, direction);
            if(angleOfForce < 0) angleOfForce+=360;
            forceAdded = direction; forceAngle=angleOfForce;
            float increments = 360f/MovementSlots;
            int slot =(int)(angleOfForce / increments);
            nearestAngle=increments*slot; nearestSlotDir = GetDirection(nearestAngle);
            _interest[slot] = Vector2.Dot(direction.normalized, GetDirection(increments * slot)) * priority;

            slot = (slot+1)%MovementSlots;
            _interest[slot] = Vector2.Dot(direction.normalized, GetDirection(increments * slot)) * priority;

            

        }


        public override Vector2 GetDirection()
        {
            if(_interest==null || _avoid==null) return Vector2.zero;
            Vector2 total = Vector2.zero;
            for (int i = 0; i < MovementSlots; i++)
            {
                float pull = Mathf.Max(0, _interest[i] - _avoid[i]);
                total += GetDirection(i * 360 /MovementSlots) * pull;

            }
            return total.normalized;
        }
        [SerializeField]
        bool DrawGizmos;
        [SerializeField, Range(0, 15)]
        int _slot;
        private void OnDrawGizmos()
        {
            if (!DrawGizmos) return;
            if (_interest == null || _interest.Length != MovementSlots) return;
            Gizmos.color = Color.yellow;
            //Gizmos.DrawRay(transform.position, GetDirection(_slot * 360f/MovementSlots).normalized * 2);
            for (int i = 0; i < MovementSlots; i++)
            {
                if(_interest[i] == 0) continue;
                float angle = i * 360/MovementSlots;
                Gizmos.DrawRay(transform.position, GetDirection(angle).normalized * 3* _interest[i]);
            }
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position, GetDirection());
        }
    }
}