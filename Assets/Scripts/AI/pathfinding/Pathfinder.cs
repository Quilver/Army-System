using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Pathfinding
{
    public class Pathfinder : MonoBehaviour
    {
        public Stack<PositionR> Search(UnitPositionR unit, PositionR start, PositionR goal, int searchLimit = 600)
        {
            MinHeap<Path<PositionR>> openSet = new Pathfinding.MinHeap<Path<PositionR>>(searchLimit);
            Dictionary<PositionR, PositionR> CameFrom = new Dictionary<PositionR, PositionR>();
            List<PositionR> visited = new List<PositionR>();
            openSet.insert(MakePath(start, 0), 0);
            CameFrom.Add(start, start);
            Path<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                Path<PositionR> node = openSet.extractMin();
                if (ReachedGoal(node.state, goal))
                {
                    return GenerateRoute(node.state, CameFrom, start);
                }//reached goal
                if (ExpectedDistanceFromGoal(bestPath.state, goal) > ExpectedDistanceFromGoal(node.state, goal)) bestPath = node;
                foreach (var move in node.state.GetMoves())
                {
                    if (visited.Contains(move) || move.Equals(start))continue;
                    visited.Add(move);
                    if(!unit.CanMoveOn(move))continue;
                    Path<PositionR> weightedNode = MakePath(move, (int)node.weight + GetMoveCost(unit, node.state, move));
                    int cost = ExpectedDistanceFromGoal(move, goal);
                    openSet.insert(weightedNode, (int)weightedNode.weight + cost);
                    CameFrom.Add(move, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }
        public Stack<PositionR> Search(UnitPositionR unit, PositionR start, UnitR goal, int searchLimit = 600)
        {
            MinHeap<Path<PositionR>> openSet = new Pathfinding.MinHeap<Path<PositionR>>(searchLimit);
            Dictionary<PositionR, PositionR> CameFrom = new Dictionary<PositionR, PositionR>();
            List<PositionR> visited = new List<PositionR>();
            openSet.insert(MakePath(start, 0), 0);
            CameFrom.Add(start, start);
            Path<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                Path<PositionR> node = openSet.extractMin();
                if (ReachedGoal(node.state, goal))
                {
                    return GenerateRoute(node.state, CameFrom, start);
                }//reached goal
                if (ExpectedDistanceFromGoal(bestPath.state, goal) > ExpectedDistanceFromGoal(node.state, goal)) bestPath = node;
                foreach (var move in node.state.GetMoves())
                {
                    if (visited.Contains(move) || move.Equals(start)) continue;
                    visited.Add(move);
                    if (!unit.CanMoveOn(move)) continue;
                    Path<PositionR> weightedNode = MakePath(move, (int)node.weight + GetMoveCost(unit, node.state, move));
                    int cost = ExpectedDistanceFromGoal(move, goal);
                    openSet.insert(weightedNode, (int)weightedNode.weight + cost);
                    CameFrom.Add(move, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }
        bool ReachedGoal(PositionR node, PositionR goal)
        {
            throw new NotImplementedException();
        }
        bool ReachedGoal(PositionR node, UnitR target)
        {
            throw new NotImplementedException();
        }
        int GetMoveCost(UnitPositionR unit, PositionR from, PositionR to)
        {
            throw new NotImplementedException();
        }
        Path<PositionR> MakePath(PositionR node, int cost)
        {
            return new Path<PositionR>() { state = node, weight = cost };
        }
        int ExpectedDistanceFromGoal(PositionR node, PositionR goal)
        {
            throw new System.NotImplementedException();
        }
        int ExpectedDistanceFromGoal(PositionR node, UnitR goal)
        {
            throw new System.NotImplementedException();
        }
        Stack<PositionR> GenerateRoute(PositionR node, Dictionary<PositionR, PositionR> CameFrom, PositionR start)
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
            Debug.Log(route);
            waypoints.Pop();
            return waypoints;
        }
    }
}

