using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace InfluenceMap
{
    [RequireComponent(typeof(Army))]
    public class ClusterMap : MonoBehaviour
    {
        Army army;
        [SerializeField, Range(3, 15)]
        float clusterSize;
        private void Start()
        {
            army= GetComponent<Army>();
        }
        void Update ()
        {
            CreateCluster();
        }
        public int ClusterCount
        {
            get
            {
                if (clusters == null) return 0;
                else return clusters.Count;
            }
        }
        Dictionary<Cluster, List<UnitInterface>> clusters;
        void CreateCluster()
        {
            clusters = new();
            Dictionary<Vector2, List<UnitInterface>> testCluster = new();
            foreach (var unit in army.Units)
            {
                bool flag = true;
                foreach (var cluster in testCluster)
                {
                    if(Vector2.Distance(cluster.Key, unit.LeadModelPosition) < clusterSize)
                    {
                        flag= false;
                        testCluster[cluster.Key].Add(unit);
                        break;
                    }
                }
                if (flag)
                {
                    testCluster.Add(unit.LeadModelPosition, new List<UnitInterface>());
                    testCluster[unit.LeadModelPosition].Add(unit);
                }

            }
            foreach (var cluster in testCluster)
            {
                Vector2 center = Vector2.zero;
                foreach (var unit in cluster.Value) { center += (Vector2)unit.LeadModelPosition; }
                center/=cluster.Value.Count;
                Cluster node= new(clusterSize, center);
                clusters.Add(node, cluster.Value);
            }
        }
        public Vector2 NearestCluster(Vector2 position)
        {
            float currentDistance = float.MaxValue;
            Vector2 p=Vector2.zero;
            foreach (var cluster in clusters)
            {
                if(Vector2.Distance(position, cluster.Key.position) < currentDistance)
                {
                    p = cluster.Key.position;
                    currentDistance = Vector2.Distance(position, cluster.Key.position);
                }
            }
            return p;
        }
        private void OnDrawGizmosSelected()
        {
            if (clusters == null) return;
            foreach (var node in clusters)
            {
                node.Key.DrawCircleGizmo(clusters[node.Key]);
            }
        }
        public class Cluster
        {
            public readonly float nodeSize;
            public readonly Vector2 position;
            public List<UnitInterface> unitsInCluster;
            public Cluster(float size, Vector2 position)
            {
                //size *= 0.75f;
                this.nodeSize = size;
                this.position = position;
                unitsInCluster = new();
            }
            public void DrawCircleGizmo(List<UnitInterface> units)
            {
                Gizmos.DrawSphere(position, 0.3f);
                if (units == null) return;
                foreach (var unit in units)
                {
                    Gizmos.DrawLine(position, unit.LeadModelPosition);
                }
            }
        }
    }
}