using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class LazySteeringCalculator : IGetSteerDirection
    {
        [SerializeField] Vector2 _sumDirection, _previousForce;
        [SerializeField] float _totalPriority;
        [SerializeField, Range(0.1f, 1)] float _timeToChange = 0.3f;
        public override void AddForce(Vector2 direction, float priority)
        {
            _totalPriority += priority;
            _sumDirection += direction * priority;
        }

        public override Vector2 GetDirection()
        {
            if (_totalPriority == 0) return Vector2.zero;
            return _previousForce;
        }

        void FixedUpdate()
        {
            _sumDirection = Vector2.zero;
            _totalPriority = 0;
            UpdateForces();
            _previousForce = Vector2.MoveTowards(_previousForce, _sumDirection.normalized, Time.deltaTime/_timeToChange);
        }
    }
}