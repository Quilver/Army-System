using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class SimpleSteerer : IGetSteerDirection
    {
        [SerializeField] Vector2 _sumDirection;
        [SerializeField] float _totalPriority;
        [SerializeField] List<Vector2> forces;
        [SerializeField] List<float> prior;
        public override void AddForce(Vector2 direction, float priority)
        {
            forces.Add(direction);prior.Add(priority);
            _totalPriority += priority;
            _sumDirection += direction * priority;
        }

        public override Vector2 GetDirection()
        {
            if(_totalPriority==0)return Vector2.zero;
            return _sumDirection/_totalPriority;
        }

        void FixedUpdate()
        {
            forces=new();prior=new();
            _sumDirection = Vector2.zero;
            _totalPriority = 0;
            UpdateForces();
        }
    }
}