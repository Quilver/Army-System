using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem.SteeringBehaviour
{
    public class FleeFromNearestEnemy : ISteeringBehaviour
    {
        [SerializeField, Range(0.1f, 5)]
        float _priority;
        IMovementData _movementData;
        IMovementData MovementData
        {
            get
            {
                if (_movementData == null) _movementData = GetComponentInParent<IMovementData>();
                return _movementData;
            }
        }
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if (_body == null) _body = GetComponentInParent<Rigidbody2D>();
                return _body;
            }
        }
        public override void AddForce()
        {
            GetSteerDirection.AddForce(GetForce(), _priority);
        }
        [SerializeField, Range(3, 20)]
        float RADIUS = 12;
        public Transform NearestEnemy()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, RADIUS);
            Transform nearestEnemy=null;
            float BestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.transform.parent == transform.parent.parent.parent)
                    continue;
                if(Vector2.Distance(hit.transform.position, transform.position)  < BestDist)
                {
                    BestDist = Vector2.Distance(hit.transform.position, transform.position);
                    nearestEnemy=hit.transform;
                }

            }
            return nearestEnemy;
        }
        public override Vector2 GetForce()
        {
            var enemy = NearestEnemy();
            if (enemy == null) return Vector2.zero;
            return (transform.position - enemy.position).normalized * 5;
        }
        protected override void OnDrawGizmos()
        {
            if(!DrawGizmo)return;
            Gizmos.DrawWireSphere(transform.position, RADIUS);
            Gizmos.DrawRay(transform.position, GetForce());
        }
    }
}
