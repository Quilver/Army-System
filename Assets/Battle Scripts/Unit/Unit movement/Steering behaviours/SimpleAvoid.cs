using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MovementSystem
{
    class SimpleAvoid : ISteeringBehaviour
    {
        ISensors _sensors;
        private void Awake()
        {
            _sensors = GetComponentInParent<ISensors>();
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce().normalized, GetForce().magnitude);
        }
        [SerializeField, Range(1, 10)]
        float _priority;
        public override Vector2 GetForce()
        {
            Vector2 sumVector = Vector2.zero;
            sumVector += ForceFromSensor(_sensors.Sensors[0]);
            for (int i = 1; i < _sensors.Sensors.Count; i++)
                sumVector += ForceFromSensor(_sensors.Sensors[i]) * 0.3f;
            return sumVector;
        }
        Vector2 ForceFromSensor(RaycastHit2D sensor)
        {
            if (!sensor) return Vector2.zero;
            if (GetMoveOrders.Target == sensor.transform) return Vector2.zero;
            float magnitude = _priority * sensor.fraction;
            return (GetMovementData.Center - sensor.point).normalized * magnitude;
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<ISensors>();
            Gizmos.color = Color.yellow;
            foreach (var sensor in _sensors.Sensors)
            {
                Gizmos.DrawRay(sensor.point, ForceFromSensor(sensor));
            }
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}