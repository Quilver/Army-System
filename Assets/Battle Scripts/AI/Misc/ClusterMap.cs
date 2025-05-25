using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace InfluenceMap
{
    //Uses minimum spanning tree to find groupings of units
    [RequireComponent(typeof(Army))]
    public class ClusterMap : MonoBehaviour
    {
        Army army;
        [SerializeField, Range(3, 30)]
        float clusterSize;
        private void Start()
        {
            army= GetComponent<Army>();
            StartCoroutine(UpdateClusters());
        }
        IEnumerator UpdateClusters()
        {
            yield return null;
            while (true)
            {

                yield return new WaitForSeconds(2f);
            }

        }
        [SerializeField]
        List<Cluster> clusters;
        Dictionary<IUnit, Cluster> unitAndTheirCluster;
        void MakeClusters()
        {
            if(army == null) army = GetComponent<Army>();
            List<Dictionary<IUnit, IUnit>> graphs = new();
            HashSet<IUnit> unitsThatAreLinked = new();
            var units = army.Units;
            foreach (var unit in units)
            {
                if (unitsThatAreLinked.Contains(unit)) continue;
                else
                    graphs.Add(MST(unit, units, unitsThatAreLinked));
                
            }
            clusters = new List<Cluster>();
            unitAndTheirCluster = new();
            foreach (var graph in graphs)
            {
                var cluster = new Cluster(clusterSize, graph);
                clusters.Add(cluster);
                foreach(var unit in graph)
                    unitAndTheirCluster.Add(unit.Key, cluster);
            }
        }
        Dictionary<IUnit, IUnit> MST(IUnit unit, List<IUnit> units, HashSet<IUnit> connectedUnits)
        {
            if(connectedUnits.Contains(unit)) return null;
            Dictionary<IUnit, IUnit> childParent = new Dictionary<IUnit, IUnit>();
            List<IUnit> unitsInTree = new List<IUnit>() { unit };
            connectedUnits.Add(unit);
            childParent.Add(unit, null);
            AdjacentUnit nearest = GetNearestUnit(unitsInTree, units, connectedUnits);
            while(nearest.child != null)
            {
                unitsInTree.Add(nearest.child);
                connectedUnits.Add(nearest.child);
                childParent.Add(nearest.child, nearest.parent);
                nearest = GetNearestUnit(unitsInTree, units, connectedUnits);
            }

            return childParent;
        }
        struct AdjacentUnit
        {
            public IUnit parent, child;
            public float distance;
            public AdjacentUnit(IUnit parent, List<IUnit> units, HashSet<IUnit> connectedUnits, float maxDistance)
            {
                this.parent = parent;
                distance = maxDistance;
                child=null;
                foreach (var child in units)
                {
                    if (child == parent) continue;
                    if (connectedUnits.Contains(child))
                        continue;
                    var distance = Vector2.Distance(parent.transform.position, child.transform.position);
                    if (distance > this.distance) continue;
                    this.child = child;
                    this.distance = distance;
                }
            }
        }
        //Will not get a unit that can cause a loop
        AdjacentUnit GetNearestUnit(List<IUnit> graph, List<IUnit> units, HashSet<IUnit> connectedUnits)
        {
            List<AdjacentUnit> neighbors = new();
            foreach(var child in graph)
                neighbors.Add(new AdjacentUnit(child, units, connectedUnits, clusterSize));
            return neighbors.OrderBy(x => x.distance).ToList()[0];
        }
        public Cluster NearestCluster(Vector2 position)
        {
            float currentDistance = float.MaxValue;
            Cluster p = null;
            foreach (var cluster in clusters)
            {
                if (Vector2.Distance(position, cluster.Center) < currentDistance)
                {
                    p = cluster;
                    currentDistance = Vector2.Distance(position, cluster.Center);
                }
            }
            return p;
        }
        public Cluster GetCluster(IUnit unit)
        {
            if(unit == null || !unitAndTheirCluster.ContainsKey(unit)) return null;
            else 
                return unitAndTheirCluster[unit];
        }
        private void OnDrawGizmosSelected()
        {
            MakeClusters();
            if (clusters == null) return;
            foreach (var cluster in clusters)
            {
                cluster.DrawMST();
            }
        }
        [Serializable]
        public class Cluster
        {
            public readonly float nodeSize;
            public Vector2 Center, facing;
            public Dictionary<IUnit, IUnit> UnitConnections;
            public List<IUnit> unitsInCluster;
            public IUnit leftMostUnit, rightMostUnit;
            public Cluster(float size, Dictionary<IUnit, IUnit> units)
            {
                Debug.Assert(units.Count > 0);
                this.nodeSize = size;
                Center = Vector2.zero;
                facing = Vector2.zero;
                unitsInCluster = new List<IUnit>();
                foreach(var unit in units.Keys)
                {
                    Debug.Assert(unit != null);
                    Center += (Vector2)unit.transform.position / units.Count;
                    facing += (Vector2)unit.transform.up / units.Count;
                    unitsInCluster.Add(unit);
                }
                UnitConnections = units;
                FlankUnits();
            }
            public void FlankUnits()
            {
                if(unitsInCluster.Count == 1)
                {
                    leftMostUnit = unitsInCluster[0];
                    rightMostUnit = unitsInCluster[0];
                    return;
                }
                Vector2 perpendicular = Vector2.Perpendicular(facing);
                float leftDistance = 0; float rightDistance = 0;
                foreach (var unit in unitsInCluster)
                {
                    var direction = (Vector2)unit.transform.position - Center;
                    var perpendicularDistance = Vector2.Dot(perpendicular, direction);
                    if(perpendicularDistance > leftDistance)
                    {
                        leftDistance = perpendicularDistance;
                        leftMostUnit = unit;
                    }
                    else if(perpendicularDistance < rightDistance)
                    {
                        rightDistance = perpendicularDistance;
                        rightMostUnit = unit;
                    }
                }
            }
            public void DrawMST()
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Center, nodeSize/2);
                Gizmos.DrawRay(Center, 3*facing);
                foreach (var unit in UnitConnections)
                    if(unit.Value != null)
                        Gizmos.DrawLine(unit.Key.transform.position, unit.Value.transform.position);
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(leftMostUnit.transform.position, 0.7f);
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(rightMostUnit.transform.position, 0.7f);
            }
        }
    }
}