using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Pathfinder : MonoBehaviour
    {
        public static Stack<PositionR> Search(UnitPositionR unit, PositionR start, Vector2Int goal, int searchLimit = 250)
        {
            MinHeap<WeightedNode<PositionR>> openSet = new MinHeap<WeightedNode<PositionR>>(searchLimit * 10);
            Dictionary<PositionR, PositionR> CameFrom = new Dictionary<PositionR, PositionR>();
            List<PositionR> visited = new List<PositionR>();
            openSet.insert(MakePath(start, 0), 0);
            CameFrom.Add(start, start);
            WeightedNode<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                WeightedNode<PositionR> node = openSet.extractMin();
                if (!unit.CanMoveOn(node.state)) continue;
                if (ReachedGoal(node.state, goal))
                {
                    return GenerateRoute(node.state, CameFrom, start);
                }//reached goal
                if (ExpectedDistanceFromGoal(bestPath.state, goal) > ExpectedDistanceFromGoal(node.state, goal)) bestPath = node;
                foreach (var move in PositionR.GetMoves(node))
                {
                    if (move.Equals(start) || CameFrom.ContainsKey(move.state))continue;
                    //visited.Add(move.state);
                    //if(!unit.CanMoveOn(move.state))continue;
                    int cost = ExpectedDistanceFromGoal(move.state, goal);
                    openSet.insert(move, (int)move.weight + cost);
                    CameFrom.Add(move.state, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }

        public static Stack<PositionR> Search(UnitPositionR unit, PositionR start, UnitR goal, int searchLimit = 250)
        {
            MinHeap<WeightedNode<PositionR>> openSet = new MinHeap<WeightedNode<PositionR>>(searchLimit * 10);
            Dictionary<PositionR, PositionR> CameFrom = new Dictionary<PositionR, PositionR>();
            List<PositionR> visited = new List<PositionR>();
            openSet.insert(MakePath(start, 0), 0);
            CameFrom.Add(start, start);
            WeightedNode<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                WeightedNode<PositionR> node = openSet.extractMin();
                if (!unit.CanMoveOn(node.state, 1, goal)) continue;
                if (ReachedGoal(node.state, goal))
                {
                    return GenerateRoute(node.state, CameFrom, start);
                }//reached goal
                if (ExpectedDistanceFromGoal(bestPath.state, goal) > ExpectedDistanceFromGoal(node.state, goal)) bestPath = node;
                foreach (var move in PositionR.GetMoves(node))
                {
                    if (CameFrom.ContainsKey(move.state)) continue;
                    //visited.Add(move.state);
                    int cost = ExpectedDistanceFromGoal(move.state, goal);
                    openSet.insert(move, (int)move.weight + cost);
                    CameFrom.Add(move.state, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }

        static bool ReachedGoal(PositionR node, Vector2Int goal)
        {
            return node.Location == goal;
        }
        static bool ReachedGoal(PositionR node, UnitR target)
        {
            return Map.Instance.getTile(node.Location).unit == target;
        }
        static WeightedNode<PositionR> MakePath(PositionR node, int cost)
        {
            return new WeightedNode<PositionR>() { state = node, weight = cost };
        }
        static int ExpectedDistanceFromGoal(PositionR node, Vector2Int goal)
        {
            var delta = node.Location - goal;
            return Math.Max(Math.Abs(delta.x), Math.Abs(delta.y));
        }
        static int ExpectedDistanceFromGoal(PositionR node, UnitR goal)
        {
            var delta = node.Location - goal.Movement.position.Location;
            return Math.Max(Math.Abs(delta.x), Math.Abs(delta.y));
        }
        static Stack<PositionR> GenerateRoute(PositionR node, Dictionary<PositionR, PositionR> CameFrom, PositionR start)
        {
            Stack<PositionR> waypoints = new Stack<PositionR>();
            string route = start.ToString() + " ->Route: ";
            List<string> nodes = new List<string>();
            while (CameFrom.ContainsKey(node))
            {
                waypoints.Push(node);
                nodes.Add(node.ToString());
                Debug.DrawLine(new Vector3(node.Location.x,node.Location.y), 
                    new Vector3(CameFrom[node].Location.x, CameFrom[node].Location.y), Color.red, 2, false);
                node = CameFrom[node];
                if (node.Equals(CameFrom[node]))
                    break;
            }
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                route += nodes[i];
                if (i == 0) route += ".";
                else route += ", ";
            }
            //Debug.Log(route);
            waypoints.Pop();
            return waypoints;
        }
    }
}

