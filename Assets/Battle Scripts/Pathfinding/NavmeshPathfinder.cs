using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Pathfinding
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavmeshPathfinder : MonoBehaviour, IPathfinder
    {
        [SerializeField]
        NavMeshPath path;
        NavMeshAgent agent;
        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
        }
        public Vector2 GoTowards(Vector2 destination)
        {
            path = new();
            //agent.CalculatePath(destination, path);
            NavMesh.CalculatePath(transform.parent.position, destination, agent.areaMask, path);
            //agent.SetDestination(destination);
            
            return path.corners[0];
        }

        public Vector2 GoTowards(Transform target)
        {
            agent.CalculatePath(target.position, path);
            //agent.SetDestination(target.position);
            return path.corners[0];
        }
        private void OnDrawGizmos()
        {
            if(path == null || path.corners == null) return;
            var previousPoint = path.corners[0];
            for(int i = 0; i < path.corners.Length; i++)
            {
                Gizmos.DrawLine(previousPoint, path.corners[i]);
                previousPoint = path.corners[i];
            }
        }
    }
}
