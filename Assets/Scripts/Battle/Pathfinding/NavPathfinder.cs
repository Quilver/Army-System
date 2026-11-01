using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace MovementSystem
{
    public class NavPathfinder : MonoBehaviour, IPathfinder
    {
        //Needed for pathfinder, I don't know why it is this value though
        public static int MASK = -1;
        NavMeshPath path;
        [SerializeField] LayerMask _layerMask;
        Vector2 _navigationSize;

        void Awake()
        {
            if (_layerMask.value == 0)
                _layerMask = LayerMask.GetMask("Terrain", "Water", "Water/flat impassible terrain", "Impassable Terrain", "Impassable");
        }

        public void SetNavigationSize(Vector2 size)
        {
            _navigationSize = size;
        }

        public bool CanMoveTo(Vector2 location)
        {
            Vector2 direction = location - (Vector2)transform.position;
            float angle = Vector2.SignedAngle(Vector2.up, direction);
            float distance = direction.magnitude;
            Vector2 size = _navigationSize.sqrMagnitude > 0.0001f ? _navigationSize : (Vector2)transform.localScale * 0.5f;
            var hit = Physics2D.BoxCast(transform.position, size, angle, direction, distance, _layerMask);
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
            path = new NavMeshPath();
            Vector3 origin = transform.parent != null ? transform.parent.position : transform.position;
            if (!NavMesh.CalculatePath(origin, position, MASK, path)
                || path.status == NavMeshPathStatus.PathInvalid
                || path.corners.Length == 0)
                return new List<Vector2>();
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
            if(!DrawGizmo || path == null || path.corners.Length == 0)
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
