using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Pathfinder : MonoBehaviour
    {
        
        public static Stack<PositionR> Search(UnitPositionR unit, PositionR start, Vector2 goal, int searchLimit = 250)
        {
            MinHeap<WeightedNode<PositionR>> openSet = new(searchLimit * 10);
            Dictionary<PositionR, PositionR> CameFrom = new();
            Startup(start, openSet, CameFrom);
            CameFrom.Add(start, start);
            WeightedNode<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                WeightedNode<PositionR> node = openSet.ExtractMin();
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
                    float cost = ExpectedDistanceFromGoal(move.state, goal);
                    openSet.Insert(move, move.weight + cost);
                    CameFrom.Add(move.state, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }

        public static Stack<PositionR> Search(UnitPositionR unit, PositionR start, UnitBase goal, int searchLimit = 250)
        {
            MinHeap<WeightedNode<PositionR>> openSet = new(searchLimit * 10);
            Dictionary<PositionR, PositionR> CameFrom = new();
            Startup(start, openSet, CameFrom);
            CameFrom.Add(start, start);
            WeightedNode<PositionR> bestPath = MakePath(start, int.MaxValue);
            while (openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                WeightedNode<PositionR> node = openSet.ExtractMin();
                if (ReachedGoal(unit, node.state, goal))
                {
                    if (unit.CanMoveOn(node.state, -0.05f, null))
                        return GenerateRoute(node.state, CameFrom, start);
                    else
                        continue;
                }//reached goal
                if (!unit.CanMoveOn(node.state, 1, goal)) continue;
                if (ExpectedDistanceFromGoal(bestPath.state, goal) > ExpectedDistanceFromGoal(node.state, goal)) bestPath = node;
                foreach (var move in PositionR.GetMoves(node))
                {
                    if (CameFrom.ContainsKey(move.state)) continue;
                    float cost = ExpectedDistanceFromGoal(move.state, goal);
                    openSet.Insert(move, move.weight + cost);
                    CameFrom.Add(move.state, node.state);
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }
        
        static void Startup(PositionR start, MinHeap<WeightedNode<PositionR>> openSet, Dictionary<PositionR, PositionR> CameFrom)
        {
            var node = MakePath(start, 0);
            foreach (var move in PositionR.GetMoves(node))
            {
                if (CameFrom.ContainsKey(move.state)) continue;
                openSet.Insert(move, (int)move.weight);
                CameFrom.Add(move.state, start);
            }
        }
        #region Reached goal
        static bool ReachedGoal(PositionR node, Vector2 goal)
        {
            return node.Location == goal;
        }
        static bool ReachedGoal(UnitPositionR unit, PositionR node, UnitBase target)
        {
            return unit.InCombatWith(node.Location, node.Rotation, target);//Map.Instance.GetTile(node.Location).unit == target;
        }
        #endregion
        static WeightedNode<PositionR> MakePath(PositionR node, int cost)
        {
            return new WeightedNode<PositionR>() { state = node, weight = cost };
        }
        #region Heuristic
        static float ExpectedDistanceFromGoal(PositionR node, Vector2 goal)
        {
            var delta = node.Location - goal;
            float angleOff = Vector2.Distance(delta.normalized, node.Direction.normalized);
            if (delta.magnitude == 0) return 0;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) * (angleOff + 0.3f);
        }
        static float ExpectedDistanceFromGoal(PositionR node, UnitBase goal)
        {
            var delta = node.Location - goal.Movement.Location;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        }
        #endregion
        static Stack<PositionR> GenerateRoute(PositionR node, Dictionary<PositionR, PositionR> CameFrom, PositionR start)
        {
            Stack<PositionR> waypoints = new();
            string route = start.ToString() + " ->Route: ";
            List<string> nodes = new();
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

