using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MovementSystem.SensorSystem
{
    public class Obstacles : MonoBehaviour
    {
        Rigidbody2D _body;
        Rigidbody2D Body { get
            {
                if (_body == null)_body = GetComponentInParent<Rigidbody2D>();
                return _body;
            } }
        public Vector2 FuturePos
        {
            get => (Vector2)transform.position + Body.velocity;
        }
        public Vector2 ForwardVec
        {
            get => Body.velocity;
        }
        public RaycastHit2D Forward
        {
            get =>ISensors.UnitCast(transform.position, Body.velocity, Vector2.one);
        }
        [SerializeField, Range(2, 5)] float _obstacleDetection = 3;
        [SerializeField, Range(12, 36)] int _rayCount;
        RaycastHit2D[] rays;
        public RaycastHit2D[] NearbyObstacles
        {

            get
            {
                if(rays == null) rays=new RaycastHit2D[_rayCount];
                for (int i = 0; i < rays.Length; i++)
                    rays[i] = ISensors.RayCast(FuturePos, i * 360f / _rayCount, _obstacleDetection);
                return rays;
            }
        }
        [SerializeField] bool _drawGizmo;
        public Vector2 pos;
        private void OnDrawGizmos()
        {
            if (!_drawGizmo) return;
            DrawForward();
            DrawSphere();
            
        }
        void DrawSphere()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(FuturePos, _obstacleDetection);
            for (int i = 0; i < NearbyObstacles.Length; i++)
            {
                if (!rays[i]) continue;
                Gizmos.DrawLine(FuturePos, rays[i].point);
            }
        }
        void DrawForward()
        {
            Gizmos.color = Color.white;
            if (!Forward) return;
            pos = Forward.point;
            Gizmos.DrawLine(Body.position, Forward.point);
            
        }

    }
}
