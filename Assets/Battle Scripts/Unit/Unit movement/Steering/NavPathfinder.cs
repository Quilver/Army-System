using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace SteeringSystem
{
    class NavPathfinder : MonoBehaviour, IPathfinder
    {
        //Needed for pathfinder, I don't know why it is this value though
        int mask = -1;
        NavMeshPath path;
        [SerializeField]
        LayerMask _layerMask;
        public bool CanMoveTo(Vector2 location)
        {
            Vector2 direction = location - (Vector2)transform.position;
            float angle = Vector2.SignedAngle(Vector2.up, direction);
            float distance = direction.magnitude;
            var hit = Physics2D.BoxCast(transform.position, transform.localScale * 0.5f, 0, direction, distance, _layerMask);
            return !hit;
        }
        public List<Vector2> GetPath(Vector2 position)
        {
            if (CanMoveTo(position)) { 
                List<Vector2> route = new List<Vector2>();
                route.Add(transform.position);
                route.Add(position);
                return route;
            }
            path = new();
            //agent.CalculatePath(destination, path);
            NavMesh.CalculatePath(transform.parent.position, position, mask, path);
            return Convert(path.corners);
        }
        List<Vector2> Convert(Vector3[] path)
        {
            List<Vector2> _newPath = new();
            foreach (Vector3 p in path)
                _newPath.Add(p);
            return _newPath;
        }
        [SerializeField]
        bool DrawGizmo;
        private void OnDrawGizmos()
        {
            if(!DrawGizmo || path == null)
                return;
            var corner = path.corners[0];
            for (int i = 1; i < path.corners.Length; i++)
            {
                Gizmos.DrawSphere(path.corners[i], 0.1f);
                Gizmos.DrawLine(path.corners[i], corner);
                corner = path.corners[i];
            }
        }
    }
}