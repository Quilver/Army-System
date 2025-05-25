using InfluenceMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Squads
{
    [RequireComponent(typeof(ISquad), typeof(InfluenceMap))]
    public class MoveToNearestCluster : MonoBehaviour
    {
        public ISquad squad;
        InfluenceMap map;
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
        void Start()
        {
            squad = GetComponent<ISquad>();
            map = GetComponent<InfluenceMap>();
        }

        private void FixedUpdate()
        {
            if (Vector2.Distance(squad.MeanPos(), transform.position) < 10)
            {
                transform.position = Vector3.MoveTowards(transform.position, GoToPosition, speed * Time.fixedDeltaTime);
                map.directionOfSquadTravel = GoToPosition - (Vector2)transform.position;
            }
            else
                map.directionOfSquadTravel = Vector2.zero;

        }
        private void OnDrawGizmosSelected()
        {
            if(!enabled) return;
            Gizmos.DrawLine(transform.position, GoToPosition);
        }
    }
}