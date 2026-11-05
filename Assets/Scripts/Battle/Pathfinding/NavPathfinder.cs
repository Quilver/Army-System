using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MovementSystem
{
    // Plans a troop's route on the baked 2D NavMesh (NavMeshPlus). The troop pivot (this object's parent) and the
    // destination are snapped onto the NavMesh first, because a pivot pushed into the eroded band next to a river or a
    // map edge, or a destination clicked in water, is not on it and NavMesh.CalculatePath would fail. A straight line is
    // used only when both the NavMesh and the terrain colliders are clear between the two points (SU-73).
    // Without a NavMesh under the troop it falls back to a straight line and warns once: the pivot is then stopped at
    // terrain by Troop.ConstrainPivotStep instead of crossing it.
    public class NavPathfinder : MonoBehaviour, IPathfinder
    {
        public static int MASK = NavMesh.AllAreas;
        // How far a pivot may be from the NavMesh and still be snapped onto it.
        const float OriginSnapDistance = 6f;
        // How far a destination may be from the NavMesh (for example a click in a lake) and still be snapped onto it.
        const float DestinationSnapDistance = 25f;

        NavMeshPath path;
        [SerializeField] LayerMask _layerMask;
        Vector2 _navigationSize;
        bool _warnedNoNavMesh;
        bool _warnedNoRoute;

        void Awake()
        {
            if (_layerMask.value == 0)
                _layerMask = LayerMask.GetMask("Terrain", "Water", "Water/flat impassible terrain", "Impassable Terrain", "Impassable");
        }

        public void SetNavigationSize(Vector2 size)
        {
            _navigationSize = size;
        }

        Vector2 Origin => transform.parent != null ? transform.parent.position : transform.position;

        // True when a box the size of the troop can sweep from the pivot to location without touching terrain.
        public bool CanMoveTo(Vector2 location) => CanMoveBetween(Origin, location);

        bool CanMoveBetween(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance <= 0.0001f) return true;
            float angle = Vector2.SignedAngle(Vector2.up, direction);
            Vector2 size = _navigationSize.sqrMagnitude > 0.0001f ? _navigationSize : (Vector2)transform.localScale * 0.5f;
            return !Physics2D.BoxCast(from, size, angle, direction, distance, _layerMask);
        }

        public List<Vector2> GetPath(Vector2 position)
        {
            Vector2 origin = Origin;
            if (!NavMesh.SamplePosition(origin, out NavMeshHit start, OriginSnapDistance, MASK))
                return StraightWithoutNavMesh(origin, position);

            if (!NavMesh.SamplePosition(position, out NavMeshHit end, DestinationSnapDistance, MASK))
                return NoRoute(origin, position);

            Vector2 target = end.position;
            Vector2 snappedOrigin = start.position;
            if (!NavMesh.Raycast(start.position, end.position, out _, MASK) && CanMoveBetween(origin, target))
                return new List<Vector2> { origin, target };

            path ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, end.position, MASK, path)
                || path.status == NavMeshPathStatus.PathInvalid
                || path.corners.Length == 0)
                return NoRoute(origin, position);

            List<Vector2> route = Convert(path.corners);
            // The first corner is the snapped pivot; start from the pivot itself when it is off the NavMesh.
            if ((origin - snappedOrigin).sqrMagnitude > 0.0001f) route.Insert(0, origin);
            return route;
        }

        List<Vector2> StraightWithoutNavMesh(Vector2 origin, Vector2 destination)
        {
            if (!_warnedNoNavMesh && !CanMoveBetween(origin, destination))
            {
                _warnedNoNavMesh = true;
                Debug.LogWarning($"{name} of '{(transform.parent != null ? transform.parent.name : name)}' is not on a NavMesh, " +
                    "so it cannot path around terrain. Bake the scene's NavMeshSurface (NavMeshPlus) and make sure it is " +
                    "assigned in the scene.", this);
            }
            return new List<Vector2> { origin, destination };
        }

        List<Vector2> NoRoute(Vector2 origin, Vector2 destination)
        {
            if (!_warnedNoRoute)
            {
                _warnedNoRoute = true;
                Debug.LogWarning($"No NavMesh route from {origin} to {destination} for " +
                    $"'{(transform.parent != null ? transform.parent.name : name)}'.", this);
            }
            return new List<Vector2>();
        }

        static List<Vector2> Convert(Vector3[] corners)
        {
            List<Vector2> newPath = new(corners.Length);
            foreach (Vector3 p in corners)
                newPath.Add(p);
            return newPath;
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
