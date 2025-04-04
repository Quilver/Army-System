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
        Dictionary<Cluster, List<UnitTemplate>> clusters;
        void CreateCluster()
        {
            clusters = new();
            Dictionary<Vector2, List<UnitTemplate>> testCluster = new();
            foreach (var unit in GetComponentsInChildren<UnitTemplate>())
            {
                bool flag = true;
                foreach (var cluster in testCluster)
                {
                    if(Vector2.Distance(cluster.Key, unit.transform.position) < clusterSize)
                    {
                        flag= false;
                        testCluster[cluster.Key].Add(unit);
                        break;
                    }
                }
                if (flag)
                {
                    if(testCluster.ContainsKey(unit.transform.position))
                        continue;
                    testCluster.Add(unit.transform.position, new List<UnitTemplate>());
                    testCluster[unit.transform.position].Add(unit);
                }

            }
            foreach (var cluster in testCluster)
            {
                Vector2 center = Vector2.zero;
                foreach (var unit in cluster.Value) { center += (Vector2)unit.transform.position; }
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
            public List<UnitTemplate> unitsInCluster;
            public Cluster(float size, Vector2 position)
            {
                //size *= 0.75f;
                this.nodeSize = size;
                this.position = position;
                unitsInCluster = new();
            }
            public void DrawCircleGizmo(List<UnitTemplate> units)
            {
                Gizmos.DrawSphere(position, 0.3f);
                if (units == null) return;
                foreach (var unit in units)
                {
                    Gizmos.DrawLine(position, unit.transform.position);
                }
            }
        }
    }
}