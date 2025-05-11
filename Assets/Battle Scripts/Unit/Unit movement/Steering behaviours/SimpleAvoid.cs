using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    class SimpleAvoid : ISteeringBehaviour
    {
        Sensors _sensors;
        IMovementData _movementData;
        IMoveOrders _moveOrders;
        private void Awake()
        {
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _movementData = GetComponentInParent<IMovementData>();
            _sensors = GetComponentInParent<Sensors>();
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), GetForce().magnitude);
        }
        [SerializeField, Range(1, 10)]
        float _priority;
        public override Vector2 GetForce()
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
        
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<Sensors>();
            if(_movementData == null) _movementData = GetComponentInParent<IMovementData>();
            if(_moveOrders == null) _moveOrders = GetComponentInParent<IMoveOrders>();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}