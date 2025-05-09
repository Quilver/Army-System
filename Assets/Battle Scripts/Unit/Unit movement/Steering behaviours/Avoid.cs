using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class Avoid : MonoBehaviour, ISteeringBehaviour
    {
        Sensors _sensors;
        IGetSteerDirection _getSteerDirection;
        private void Start()
        {
            _sensors = GetComponentInParent<Sensors>();
            _getSteerDirection = GetComponentInParent<IGetSteerDirection>();
            _getSteerDirection.updateSteeringForces += AddForce;
        }
        public void AddForce()
        {
            _getSteerDirection.AddForce(GetForce(), GetForce().magnitude);
        }

        public Vector2 GetForce()
        {
            Vector2 sumVector = Vector2.zero;
            foreach (var sensor in _sensors.Sensors)
            {
                if(!sensor)continue;
                float magnitude = _sensors.SensorLength / sensor.distance;
                sumVector-=_getSteerDirection.Seek(sensor.point).normalized * magnitude;
            }
            return sumVector;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<Sensors>();
            if (_getSteerDirection == null) _getSteerDirection = GetComponentInParent<IGetSteerDirection>();
            Gizmos.color=Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}