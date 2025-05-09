using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class SimpleAvoid : MonoBehaviour, ISteeringBehaviour
    {
        Sensors _sensors;
        IGetSteerDirection _getSteerDirection;
        IMovementData _movementData;
        IMoveOrders _moveOrders;
        private void Start()
        {
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _movementData = GetComponentInParent<IMovementData>();
            _sensors = GetComponentInParent<Sensors>();
            _getSteerDirection = GetComponentInParent<IGetSteerDirection>();
            _getSteerDirection.updateSteeringForces += AddForce;
        }
        public void AddForce()
        {
            _getSteerDirection.AddForce(GetForce(), GetForce().magnitude);
        }
        [SerializeField, Range(1, 10)]
        float _priority;
        public Vector2 GetForce()
        {
            Vector2 sumVector = Vector2.zero;
            foreach (var sensor in _sensors.Sensors)
            {
                sumVector += ForceFromSensor(sensor);
            }
            return sumVector;
        }
        Vector2 ForceFromSensor(RaycastHit2D sensor)
        {
            if (!sensor) return Vector2.zero;
            if (_moveOrders != null && _moveOrders.Target == sensor.transform) return Vector2.zero;
            float magnitude = _sensors.SensorLength * _priority / sensor.distance;
            return (_movementData.Center - sensor.point).normalized * magnitude;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<Sensors>();
            if (_getSteerDirection == null) _getSteerDirection = GetComponentInParent<IGetSteerDirection>();
            if(_movementData == null) _movementData = GetComponentInParent<IMovementData>();
            if(_moveOrders == null) _moveOrders = GetComponentInParent<IMoveOrders>();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}