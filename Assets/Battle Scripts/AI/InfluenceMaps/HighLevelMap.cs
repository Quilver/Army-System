using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;


namespace InfluenceMap
{
    //[ExecuteInEditMode]
    public class HighLevelMap : MonoBehaviour
    {
        [SerializeField, Range(100, 1500)]
        int maxNodeCount;
        [SerializeField, Range(3, 10)]
        float gridSize;
        //HashSet<Node> nodes;
        Dictionary<Vector2, Node> points;
        Dictionary<Vector2Int, Vector2> roundedPoints;
        void AddPoint(Node node)
        {
            points.Add(node.position, node);
            roundedPoints.Add(new((int)node.position.x, (int)node.position.y), node.position);
        }
        Vector2 mapCenter;
        //public static HighLevelMap instance { get;private set; }
        public void Awake()
        {
            if(transform == null)
                mapCenter= Vector2.zero;
            else mapCenter = transform.position;
            CreateMap();

        }
        void OnEnable()
        {
            Notifications.Reached += UpdateMap;
        }
        
        void UpdateMap(UnitBase unit, PositionR position)
        {

            foreach (var point in GetAdjacentNodes(position.Location))
            {
                if (roundedPoints.ContainsKey(point))
                    points[roundedPoints[point]].UpdateNode();
            }
        }
        private void OnDisable()
        {
            
        }
        #region Create Map
        private void OnValidate()
        {
            points = new();
            roundedPoints = new();
            CreateMap();
        }
        [SerializeField, ReadOnly]
        int nodeCount;
        void CreateMap()
        {
            Stack<Node> UnsearchedNodes = new();
            Node startNode = new(gridSize, mapCenter);
            roundedPoints = new();
            points = new();
            UnsearchedNodes.Push(startNode);
            AddPoint(startNode);
            while (UnsearchedNodes.Count > 0 && points.Count < maxNodeCount)
            {
                Node node = UnsearchedNodes.Pop();
                ExpandNode(node, UnsearchedNodes);
            }
            nodeCount = points.Count;
        }
        void ExpandNode(Node node, Stack<Node> UnsearchedNodes)
        {
            TestNode(node, Vector2.up, UnsearchedNodes);
            TestNode(node, Vector2.down, UnsearchedNodes);
            TestNode(node, Vector2.left, UnsearchedNodes);
            TestNode(node, Vector2.right, UnsearchedNodes);

            TestNode(node, new Vector2(-1, -1), UnsearchedNodes);
            TestNode(node, new Vector2(-1, 1), UnsearchedNodes);
            TestNode(node, new Vector2(1, -1), UnsearchedNodes);
            TestNode(node, new Vector2(1, 1), UnsearchedNodes);


        }

        void TestNode(Node node, Vector2 direction, Stack<Node> UnsearchedNodes)
        {
            int mask = 1 << 8;
            var position = node.position + gridSize * direction;
            var raycast = Physics2D.Raycast(node.position, direction, Vector2.Distance(node.position, position));
            if (raycast && raycast.collider.gameObject.layer == mask)
                return;
            var newNode = new Node(gridSize, position);
            if (points.ContainsKey(newNode.position))
            {
                node.adjacentNodes.Add(newNode);
                return;
            }
            var boxCollision = Physics2D.OverlapBox(position, newNode.nodeSize, 0, mask);
            if (boxCollision)
            {
                return;
            }
            AddPoint(newNode);
            node.adjacentNodes.Add(newNode);
            UnsearchedNodes.Push(newNode);
        }
        #endregion
        float Delta(float axis)
        {
            float delta;
            if (axis < 0)
                delta = Mathf.Abs(gridSize + axis % gridSize);
            else
                delta = axis % gridSize;
            return delta;
        }
        float Axis(float axis)
        {
            float delta = Delta(axis);
            axis -= delta;
            if (delta * 2 > gridSize)
                axis += gridSize;
            return axis;
        }

        List<Vector2Int> GetAdjacentNodes(Vector2 position)
        {
            Vector2Int Round(float x, float y) { return new Vector2Int((int)x, (int)y); }
            List<Vector2Int> AdjactentNodes(Vector2 position, float x, float y)
            {
                List<Vector2Int> options = new List<Vector2Int>();
                options.Add(Round(x, y));
                options.Add(Round(x + gridSize, y));
                options.Add(Round(x - gridSize, y));
                options.Add(Round(x, y + gridSize));
                options.Add(Round(x, y - gridSize));

                options.Add(Round(x + gridSize, y + gridSize));
                options.Add(Round(x - gridSize, y - gridSize));
                options.Add(Round(x - gridSize, y + gridSize));
                options.Add(Round(x + gridSize, y - gridSize));
                options = options.OrderBy(x => Vector2.Distance(position, x)).ToList();
                return options;
            }
            float x = Axis(position.x) + mapCenter.x % gridSize;
            float y = Axis(position.y) + mapCenter.y % gridSize;
            return AdjactentNodes(position, x, y);
        }
        Node GetNode(Vector2 position)
        {
            
            foreach (var option in GetAdjacentNodes(position))//AdjactentNodes(position, x, y))
                if (roundedPoints.ContainsKey(option))
                    return points[roundedPoints[option]];
            return null;

        }
        #region A Star
        [Header("Testing properties")]

        [SerializeField]
        Vector2 startPoint;
        [SerializeField]
        Vector2 endPoint;
        
        public List<Vector2> A_StarSearch(Vector2 start, Vector2 end, int maxSearches = 300)
        {
            startPoint= start; endPoint= end;
            List<Vector2> Points(Dictionary<Vector2, Node> cameFrom, Vector2 end)
            {
                Vector2 point = end;
                List<Vector2> waypoints = new();
                waypoints.Add(end);
                while (cameFrom.ContainsKey(point) && cameFrom[point] != null)
                {
                    point = cameFrom[point].position;
                    waypoints.Add(point);
                }
                return waypoints;
            }
            if (GetNode(start) == null || GetNode(end) == null) return null;
            start = GetNode(start).position;end = GetNode(end).position;
            MinHeap<WeightedNode<Node>> unexploredNodes = new(points.Count);
            Dictionary<Vector2, Node> cameFrom = new();
            WeightedNode<Node> newNode = new();
            newNode.state = GetNode(start); newNode.weight = 0;
            cameFrom.Add(newNode.state.position, null);
            unexploredNodes.Insert(newNode, 0);
            while (unexploredNodes.size > 0 && maxSearches > cameFrom.Count)
            {
                WeightedNode<Node> currentWieghtedNode = unexploredNodes.ExtractMin();
                Node _node = points[currentWieghtedNode.state.position];
                //Found goal
                if (currentWieghtedNode.state.position == end) return Points(cameFrom, end);
                foreach (var neigbhor in _node.adjacentNodes)
                {
                    if (cameFrom.ContainsKey(neigbhor.position)) continue;
                    WeightedNode<Node> childNode = new();
                    childNode.state = neigbhor;
                    childNode.weight = currentWieghtedNode.weight + Vector2.Distance(_node.position, neigbhor.position);
                    cameFrom.Add(neigbhor.position, currentWieghtedNode.state);
                    unexploredNodes.Insert(childNode, childNode.weight + Vector2.Distance(neigbhor.position, end));
                }
            }
            Gizmos.color = Color.red;
            foreach (var point in cameFrom.Keys)
            {
                Gizmos.DrawCube(point, Vector3.one/2);
            }
            return null;

        }
        void TestPathfindingWithGizmo()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(startPoint, 0.4f);
            Gizmos.DrawSphere(endPoint, 0.4f);
            if (GetNode(startPoint) != null) GetNode(startPoint).DrawGridGizmo(true);
            if (GetNode(endPoint) != null) GetNode(endPoint).DrawGridGizmo(true);
            var path = A_StarSearch(startPoint, endPoint);
            if (path != null)
            {
                Vector2 nextPoint = endPoint;
                Gizmos.color = Color.blue;
                foreach (var point in path)
                {
                    Gizmos.DrawLine(nextPoint, point);
                    nextPoint = point;
                }
                Gizmos.DrawLine(nextPoint, startPoint);
            }
        }
        #endregion
        private void OnDrawGizmosSelected()
        {
            if (points == null) return;
            foreach (var node in points.Values)
            {
                node.DrawGridGizmo();
            }
            TestPathfindingWithGizmo();
        }
    }
}