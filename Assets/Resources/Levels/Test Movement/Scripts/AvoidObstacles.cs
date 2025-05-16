using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    [RequireComponent (typeof (SensorSystem.Obstacles))]
    public class AvoidObstacles : ISteeringBehaviour
    {
        SensorSystem.Obstacles _obstacles;
        SensorSystem.Obstacles Obstacles
        {
            get
            {
                if(_obstacles == null)_obstacles=GetComponent<SensorSystem.Obstacles>();
                return _obstacles;
            }
        }
        public override void AddForce()
        {
            var force = GetForce();
            if (force == Vector2.zero) return;

            GetSteerDirection.AddForce(GetForce().normalized, GetForce().magnitude);
        }
        [SerializeField, Range(0.5f, 10)] float _priority;
        public override Vector2 GetForce()
        {
            return (AvoidForwardObstacles()) * _priority;
        }

        [SerializeField, Range(3, 10)] float maxStartSteerDistance;
        [SerializeField, Range(1, 3)] float MinAvoidSteerDistance;
        Vector2 AvoidForwardObstacles()
        {
            if (!Obstacles.Forward || Obstacles.ForwardVec.magnitude == 0) return Vector2.zero;

            var direction = Obstacles.Forward.point - (Vector2)transform.position;
            if (direction.magnitude > maxStartSteerDistance * Obstacles.ForwardVec.magnitude) return Vector2.zero;
            var collisionDistance = Vector2.Dot(direction, Obstacles.ForwardVec);
            var collisionAlignedVector = collisionDistance * Obstacles.ForwardVec.normalized;
            var collisionDistanceRatio = Mathf.InverseLerp(maxStartSteerDistance, MinAvoidSteerDistance, direction.magnitude);

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

            var quadWeight = (collisionDistanceRatio); quadWeight*=quadWeight;
            var breakingDirection = -Obstacles.ForwardVec.normalized;
            return breakingDirection;// * quadWeight;
        }
        Vector2 AvoidForwardSteer()
        {
            if (!Obstacles.Forward || Obstacles.ForwardVec.magnitude == 0) return Vector2.zero;

            var direction = Obstacles.Forward.point - (Vector2)transform.position;
            if (direction.magnitude > maxStartSteerDistance * Obstacles.ForwardVec.magnitude) return Vector2.zero;
            var collisionDistance = Vector2.Dot(direction, Obstacles.ForwardVec);
            var collisionAlignedVector = collisionDistance * Obstacles.ForwardVec.normalized;
            var collisionDistanceRatio = Mathf.InverseLerp(maxStartSteerDistance, MinAvoidSteerDistance, direction.magnitude);

            return SteerDirection(direction, collisionAlignedVector, collisionDistanceRatio);
        }
        Vector2 AvoidForwardBrake()
        {
            if (!Obstacles.Forward || Obstacles.ForwardVec.magnitude == 0) return Vector2.zero;

            var direction = Obstacles.Forward.point - (Vector2)transform.position;
            if (direction.magnitude > maxStartSteerDistance * Obstacles.ForwardVec.magnitude) return Vector2.zero;
            var collisionDistance = Vector2.Dot(direction, Obstacles.ForwardVec);
            var collisionAlignedVector = collisionDistance * Obstacles.ForwardVec.normalized;
            var collisionDistanceRatio = Mathf.InverseLerp(maxStartSteerDistance, MinAvoidSteerDistance, direction.magnitude);

            return BrakingDirection(direction, collisionAlignedVector, collisionDistanceRatio);
        }
        protected override void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (!Obstacles.Forward) return;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, maxStartSteerDistance * Obstacles.ForwardVec.magnitude);
            Gizmos.DrawLine(transform.position, Obstacles.FuturePos);
            Gizmos.DrawSphere(Obstacles.FuturePos, 0.1f);
            Gizmos.color = Color.black;
            Gizmos.DrawRay(Obstacles.FuturePos, GetForce());
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Obstacles.FuturePos, AvoidForwardSteer());   
            Gizmos.color = Color.red;
            Gizmos.DrawRay(Obstacles.FuturePos, AvoidForwardBrake());
        }
    }
}