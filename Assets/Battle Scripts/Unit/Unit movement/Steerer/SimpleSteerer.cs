using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    public class SimpleSteerer : IGetSteerDirection
    {
        Vector2 _sumDirection;

        protected override float MaxSpeed => 5;

        public override void AddForce(Vector2 direction, float priority)
        {
            _sumDirection += direction * priority;
        }

        public override Vector2 GetDirection()
        {
            return _sumDirection.normalized;
        }

        void Update()
        {
            _sumDirection = Vector2.zero;
            UpdateForces();
        }
    }
}