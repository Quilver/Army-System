using InfluenceMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Squads
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public class MoveToNearestCluster : ISquadOrder
    {
        public ClusterMap clusterMap;
        [SerializeField]
        Vector2 GoToPosition
        {
            get
            {
                if(clusterMap == null) clusterMap = GetComponentInParent<ClusterMap>();
                var cluster = clusterMap.NearestCluster(transform.position);
                if(cluster == null ) return transform.position;
                return cluster.Center;
            }
        }
        [SerializeField, Range(1, 10)]
        float speed = 3;
        protected override IEnumerator RunOrder()
        {
            yield return null;
            while (enabled)
            {
                if (Vector2.Distance(squad.MeanPos(), transform.position) < 10)
                {
                    transform.position = Vector3.MoveTowards(transform.position, GoToPosition, speed * Time.fixedDeltaTime);
                    map.directionOfSquadTravel = GoToPosition - (Vector2)transform.position;
                }
                else
                    map.directionOfSquadTravel = Vector2.zero;
                yield return new WaitForFixedUpdate();
            }

        }
        private void OnDrawGizmosSelected()
        {
            if(!enabled) return;
            Gizmos.DrawLine(transform.position, GoToPosition);
        }

        
    }
}