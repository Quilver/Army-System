using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
namespace MovementSystem.SteeringBehaviour
{
    public class AvoidFullUnit : ISteeringBehaviour
    {
        ISensors _sensors;
        ISensors Sensors
        {
            get
            {
                if (_sensors == null) _sensors=GetComponentInParent<ISensors>();
                return _sensors;
            }
        }
        IMovementData _movementData;
        IMovementData MoveData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        
        public override void AddForce()
        {
            var force = GetForce();
            if (force.magnitude < 0.1f) return;
            GetSteerDirection.AddForce(force.normalized, force.magnitude);
        }
        Vector2 ModifyForNotSlowing(Vector2 avoidForce, Vector2 currentHeading)
        {
            if(avoidForce==Vector2.zero) return Vector2.zero;
            Vector2 _avoid = avoidForce;
            float dotTest = Mathf.Abs(Vector2.Dot(currentHeading.normalized, avoidForce.normalized));
            Vector2 dotForceTest = dotTest * avoidForce.magnitude * currentHeading.normalized;
            Vector2 reducedForce = avoidForce + dotForceTest;
            float remainingMagnitude=reducedForce.magnitude;
            return reducedForce;
        }
        [SerializeField, Range(0.5f, 10)] float _priority;
        public bool _slowDown;
        public override Vector2 GetForce()
        {
            var sumForce = (AvoidForwardObstacles(Sensors.ForwardSensor) + AvoidForwardObstacles(Sensors.RightWhisker) + AvoidForwardObstacles(Sensors.LeftWhisker));
            sumForce += RepulseFromObstacles();
            return sumForce * _priority;
        }

        [SerializeField, Range(3, 10)] float maxStartSteerDistance, maxAvoidDistance;
        [SerializeField, Range(1, 3)] float MinAvoidSteerDistance, MinAvoidDistance;
        Vector2 AvoidForwardObstacles(RaycastHit2D raycast)
        {
            if (!raycast || raycast.transform == GetMoveOrders.Target || MoveData.Velocity.magnitude == 0) return Vector2.zero;

            var direction = raycast.point - MoveData.Center;
            if (direction.magnitude > maxStartSteerDistance * MoveData.Velocity.magnitude) return Vector2.zero;
            var collisionDistance = Vector2.Dot(direction, MoveData.Velocity);
            var collisionAlignedVector = collisionDistance * MoveData.Velocity.normalized;
            var collisionDistanceRatio = Mathf.InverseLerp(maxStartSteerDistance, MinAvoidSteerDistance, direction.magnitude);

            if(direction.magnitude > MinAvoidSteerDistance) 
                return ModifyForNotSlowing(SteerDirection(direction, collisionAlignedVector, collisionDistanceRatio), MoveData.Velocity);
            _slowDown = true;
            return SteerDirection(direction, collisionAlignedVector, collisionDistanceRatio)
                + BrakingDirection(direction, collisionAlignedVector, collisionDistanceRatio);
        }
        Vector2 SteerDirection(Vector2 direction, Vector2 collisionAlignedVector, float collisionDistanceRatio)
        {
            var linearWeight = collisionDistanceRatio;
            var steeringDirection = (collisionAlignedVector - direction).normalized;
            return steeringDirection;// * linearWeight;
        }
        Vector2 BrakingDirection(Vector2 direction, Vector2 collisionAlignedVector, float collisionDistanceRatio)
        {

            var quadWeight = (collisionDistanceRatio); quadWeight *= quadWeight;
            var breakingDirection = -MoveData.Velocity.normalized;
            return breakingDirection;// * quadWeight;
        }
        Vector2 RepulseFromObstacles()
        {
            Vector2 sum = Vector2.zero;
            foreach (var sensor in Sensors.Sensors)
            {
                if(!sensor || sensor.transform == GetMoveOrders.Target) continue;
                float magnitude = Mathf.InverseLerp(maxAvoidDistance, MinAvoidDistance, sensor.distance);
                Vector2 dir = (Vector2)transform.parent.position-sensor.point;
                sum += magnitude/Sensors.Sensors.Count*dir.normalized;
            }
            return sum;
        }



        public bool _GizmoDrawSteerOrSensor;
        protected override void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.white;
            if(_GizmoDrawSteerOrSensor)
                Gizmos.DrawWireSphere(transform.position, maxStartSteerDistance * MoveData.Velocity.magnitude);
            else
                Gizmos.DrawWireSphere(transform.position, maxAvoidDistance);
            Gizmos.DrawRay(MoveData.Center, MoveData.Velocity);
            Gizmos.DrawSphere(MoveData.FuturePosition, 0.1f);
            Gizmos.color = Color.black;
            if(_GizmoDrawSteerOrSensor)
                Gizmos.DrawWireSphere(MoveData.Center, MinAvoidSteerDistance);
            else
                Gizmos.DrawWireSphere(MoveData.Center, MinAvoidDistance);
        }
    }
}