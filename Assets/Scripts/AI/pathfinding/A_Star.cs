using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Pathfinding
{
    public class A_StarLimitedSearch
    {
        public UnitMovement controller;
        public A_StarLimitedSearch(UnitMovement master) {
            controller = master;
        }
        public Stack<Node<Tile>> Search(Node<Tile> start, Node<Tile> goal, Unit target = null, int searchLimit = 550)
        {
            MinHeap<Path<Node<Tile>>> openSet = new MinHeap<Path<Node<Tile>>>(controller.maxSearchSize());
            Dictionary<Node<Tile>, Node<Tile>> CameFrom = new Dictionary<Node<Tile>, Node<Tile>>();
            List<Tuple<Vector2, Vector2>> visited = new List<Tuple<Vector2, Vector2>>();
            openSet.insert(MakePath(start, 0), 0);
            CameFrom.Add(start, start);
            Path<Node<Tile>> bestPath = MakePath(start, int.MaxValue);
            while(openSet.size > 0 && searchLimit > 0)
            {
                searchLimit--;
                Path<Node<Tile>> node = openSet.extractMin();
                if(controller.reachedGoal(node.state, goal)) {
                    return GenerateRoute(node.state, CameFrom, start);
                }//reached goal
                if(GetCost(bestPath.state, goal, target) > GetCost(node.state, goal, target)) bestPath = node; 
                foreach (var move in controller.getActions(node.state, node.weight, target))
                {
                    if (CameFrom.ContainsKey(move.state) || move.state == start)
                        continue;
                    if(visited.Contains(move.state.GetData())) continue;
                    int cost = GetCost(move.state, goal, target);
                    openSet.insert(move, (int)move.weight+cost);
                    CameFrom.Add(move.state, node.state);
                    visited.Add(move.state.GetData());
                }
            }
            return GenerateRoute(bestPath.state, CameFrom, start);
        }
        Path<Node<Tile>> MakePath(Node<Tile> node, int cost)
        {
            return new Path<Node<Tile>>() { state = node, weight = cost };
        }
        public int GetCost(Node<Tile> node, Node<Tile> goal, Unit Target)
        {
            if(Target== null)
                return (int)Vector2.Distance(node.position, goal.position);
            else
            {
                int cost = int.MaxValue;
                foreach (var model in Target.models)
                {
                    var distance = (int)Vector2.Distance(node.position, model.position);
                    if(cost > distance)
                        cost = distance;
                }
                return cost;
            }
        }
        Stack<Node<Tile>> GenerateRoute(Node<Tile> node, Dictionary<Node<Tile>, Node<Tile>> CameFrom, Node<Tile> start)
        {
            Stack<Node<Tile>>  waypoints = new Stack<Node<Tile>>();
            string route = start.position +"|"+start.direction + " ->Route: ";
            List<string> nodes = new List<string>();
            while (CameFrom.ContainsKey(node))
            {
                waypoints.Push(node);
                nodes.Add(node.position.ToString() +"|" +node.direction);
                Debug.DrawLine(node.position, CameFrom[node].position, Color.red, 2, false);
                node = CameFrom[node];
                if (node == CameFrom[node])
                    break;
            }
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                route += nodes[i];
                if (i == 0) route += ".";
                else route+= ", ";
            }
            Debug.Log(route);
            waypoints.Pop();
            return waypoints;
        }
    }
}

