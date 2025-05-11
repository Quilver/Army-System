using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    class AvoidForwardObstacle : ISteeringBehaviour
    {
        [SerializeField, Range(0.1f, 10)]
        float _priorityMultiplier;
        Sensors _sensors;
        IMoveOrders _moveOrders;
        private void Start()
        {
            _moveOrders = GetComponentInParent<IMoveOrders>();
            _sensors = GetComponentInParent<Sensors>();
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), GetForce().magnitude);
        }
        
        public override Vector2 GetForce()
        {
            Vector2 sumVector = Vector2.zero;
            sumVector += ForceForSensor(_sensors.Sensors[0]);
            sumVector += ForceForSensor(_sensors.Sensors[1]);
            sumVector += ForceForSensor(_sensors.Sensors.Last());
            return sumVector;
        }
        [SerializeField, Range(1, 10)]
        float k;
        [SerializeField, Range(0, 1)]
        float PerpendicularNormalRatio;
        //TEST
        [SerializeField]
        float distancePriority;
        [SerializeField]
        float priority;
        [SerializeField]
        Transform _obstacle;
        Vector2 ForceForSensor(RaycastHit2D sensor)
        {
            if (!sensor) return Vector2.zero;
            _obstacle=sensor.transform;
            if (_moveOrders != null && _moveOrders.Target == sensor.transform) return Vector2.zero;  
            float magnitude = (1-sensor.fraction);
            Vector2 avoidanceDirection = GetSteerDirection.Seek(AvoidPoint(sensor)).normalized;
            distancePriority = magnitude;
            return avoidanceDirection * magnitude * _priorityMultiplier;
        }
        [SerializeField]
        float delta;
        Vector2 AvoidPoint(RaycastHit2D sensor)
        {
            Vector2 forward = _sensors.SensorDirection(_sensors.Sensors[0]);
            Vector2 perpendicular = Vector2.Perpendicular(forward);
            delta = Vector2.SignedAngle(forward, sensor.point-(Vector2)transform.position);
            if (Vector2.SignedAngle(forward, sensor.point - (Vector2)transform.position) > 0)
                perpendicular = -Vector2.Perpendicular(forward);
            Vector2 direction = PerpendicularNormalRatio * perpendicular.normalized + (1 - PerpendicularNormalRatio) * sensor.normal;
            //Vector2 direction = PerpendicularNormalRatio * (_sensors.SensorDirection(sensor)).normalized + (1- PerpendicularNormalRatio) * sensor.normal;
            return (Vector2)transform.position + perpendicular * k;
        }
        void DrawRayReaction(RaycastHit2D sensor)
        {
            if (!sensor) return;
            priority = ForceForSensor(sensor).magnitude;
            Gizmos.DrawLine(sensor.point, transform.position);
            //
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, sensor.point);
            Gizmos.DrawSphere(sensor.point, 0.2f);
            //
            Gizmos.color = Color.white;
            Gizmos.DrawLine(sensor.point, AvoidPoint(sensor));
            Gizmos.DrawSphere(AvoidPoint(sensor), 0.2f);
            //
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, ForceForSensor(sensor));
        }
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            if (_sensors == null) _sensors = GetComponentInParent<Sensors>();
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, _sensors.SensorDirection(_sensors.Sensors[0])*3);
            Gizmos.color = new Color(0.8f, 0.5f, 0.5f);
            //Center
            DrawRayReaction(_sensors.Sensors[0]);

            //Left
            DrawRayReaction(_sensors.Sensors[1]);

            //Right
            DrawRayReaction(_sensors.Sensors[2]);
        }
    }
}