using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    class Avoid : ISteeringBehaviour
    {
        ISensors _sensors;
        private void Start()
        {
            _sensors = GetComponentInParent<ISensors>();
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), GetForce().magnitude);
        }

        public override Vector2 GetForce()
        {
            Vector2 sumVector = Vector2.zero;
            foreach (var sensor in _sensors.Sensors)
            {
                if(!sensor)continue;
                float magnitude = _sensors.SensorLength / sensor.distance;
                sumVector-=GetSteerDirection.Seek(sensor.point).normalized * magnitude;
            }
            return sumVector;
        }
        protected override void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<ISensors>();
            Gizmos.color=Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}