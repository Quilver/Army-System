using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SteeringSystem
{
    class AvoidForwardObstacle : MonoBehaviour, ISteeringBehaviour
    {
        [SerializeField, Range(0.1f, 10)]
        float _priorityMultiplier;
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
            sumVector += ForceForSensor(_sensors.Sensors[0]);
            sumVector += ForceForSensor(_sensors.Sensors[1]);
            sumVector += ForceForSensor(_sensors.Sensors[_sensors.Sensors.Count -1]);
            return sumVector;
        }
        Vector2 ForceForSensor(RaycastHit2D sensor)
        {
            if (!sensor) return Vector2.zero;
            float magnitude = 1 - sensor.distance / _sensors.SensorLength;
            return -_getSteerDirection.Seek(sensor.point+sensor.normal).normalized * magnitude * _priorityMultiplier;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<Sensors>();
            if (_getSteerDirection == null) _getSteerDirection = GetComponentInParent<IGetSteerDirection>();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
            Gizmos.color = new Color(0.5f, 0, 0.5f);
            Gizmos.DrawRay(transform.position, _sensors.Sensors[0].point);
            Gizmos.DrawRay(transform.position, ForceForSensor(_sensors.Sensors[0]));
            Gizmos.DrawRay(transform.position, ForceForSensor(_sensors.Sensors[1]));
            Gizmos.DrawRay(transform.position, ForceForSensor(_sensors.Sensors.Last()));
        }
    }
}