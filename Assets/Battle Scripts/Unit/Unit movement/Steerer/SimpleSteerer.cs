using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class SimpleSteerer : IGetSteerDirection
    {
        Vector2 _sumDirection;
        float _totalPriority;
        
        public override void AddForce(Vector2 direction, float priority)
        {
            _totalPriority += priority;
            _sumDirection += direction * priority;
        }

        public override Vector2 GetDirection()
        {
            if(_totalPriority==0)return Vector2.zero;
            return _sumDirection/_totalPriority;
        }

        void Update()
        {
            _sumDirection = Vector2.zero;
            _totalPriority = 0;
            UpdateForces();
        }
    }
}