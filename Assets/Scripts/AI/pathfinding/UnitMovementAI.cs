using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Pathfinding
{
    public class UnitMovement
    {
        Unit unit, targetUnit;
        UnitMovementHandler unitMovement;
        Tile target;
        int updateCount = 0;
        Dictionary<Node<Tile>, bool> _canMove = new Dictionary<Node<Tile>, bool>();

        A_StarLimitedSearch a_Star;
        Stack<Node<Tile>> waypoints;
        public bool PathIsEmpty()
        {
            return waypoints == null || waypoints.Count == 0;
        }
        //setup
        public UnitMovement(Unit unit, UnitMovementHandler unitMovement)
        {
            this.unit = unit;
            _canMove = new Dictionary<Node<Tile>, bool>();
            this.unitMovement = unitMovement;
        }
        float rotationCost { get { return 4; } }
        float strafeCost { get { return 20; } }
        int AvoidOtherUnitsBy { get { return 16; } }
        public bool CanMove(Node<Tile> node)
        {
            if (_canMove.ContainsKey(node)) return _canMove[node];
            bool walkable = Map.Instance.getTile((int)node.position.x, (int)node.position.y).Walkable(unit);
            var pos = node.position + node.direction;
            walkable = walkable && Map.Instance.getTile((int)pos.x, (int)pos.y).Walkable(unit);
            _canMove.Add(node, walkable);
            return _canMove[node];
        }
        //route making methods
        public void getRoute(Tile goal)
        {
            //Debug.Log("Trying to get route");
            a_Star = new A_StarLimitedSearch(this);
            target = goal;
            targetUnit = null;
            Node<Tile> end = new Node<Tile>
            {
                data = goal,
                position = goal.position,
                direction = new Vector2(1, 0)
            };
            foreach (var direction in TileAndDirection.Directions)
            {
                end.direction = direction;
                if (CanMove(end))
                {
                    waypoints = a_Star.Search(getInitialState(), end);
                    Debug.Log("Path length: " + waypoints.Count);
                    return;
                }
            }

            //setGoal(end);
        }
        public void getRoute(Unit unit)
        {
            //Debug.Log("Pathfinding towards enemy unit");
            target = null;
            targetUnit = unit;
            a_Star = new A_StarLimitedSearch(this);
            Node<Tile> end = new Node<Tile>
            {
                data = Map.Instance.getTile(unit.unitMovementHandler.CenterPoint),
                position = unit.unitMovementHandler.CenterPoint,
                direction = new Vector2(1, 0)
            };
            Debug.Log(end.position);
            
            waypoints = a_Star.Search(getInitialState(), end);
        }
        public void validateRoute()
        {
            
            
            if (Vector2.Equals(waypoints.Peek().position, (Vector2)unit.transform.position)) return;
            Debug.Log((Vector2)unit.transform.position + "==" + waypoints.Peek().position);
            if (targetUnit != null) { getRoute(targetUnit); }
            else if (target != null) getRoute(target);
        }
        //utility methods
        public Node<Tile> getInitialState()
        {
            Node<Tile> startNode = new Node<Tile>
            {
                position = unit.transform.position,
                direction = unitMovement.CardinalDirection,
                data = Map.Instance.getTile(unit.transform.position)
            };
            return startNode;
        }
        public Path<Node<Tile>> followPath()
        {
            Path<Node<Tile>> waypoint = new Path<Node<Tile>>
            {
                state = waypoints.Pop()
            };
            return waypoint;
        }
        public int maxSearchSize()
        {
            return Map.Instance.Width * Map.Instance.Width * 9;
        }
        public Path<Node<Tile>>[] getActions(Node<Tile> node, float currentCost, Unit target = null)
        {
            List<Path<Node<Tile>>> edges = new List<Path<Node<Tile>>>();
            foreach (var direction in TileAndDirection.Directions)
            {
                Vector2Int position = node.data.position + direction;
                bool walkable = Map.Instance.getTile(position).Walkable(unit, target);
                bool safe = Map.Instance.NearestUnitDistance(position, unit, target) > AvoidOtherUnitsBy;
                if (walkable && safe)
                {
                    Path<Node<Tile>> path = makeEdge(node, direction, currentCost);
                    if (path.state.data != null)
                    {
                        edges.Add(path);
                    }
                }
            }
            Path<Node<Tile>>[] paths = new Path<Node<Tile>>[edges.Count];
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                paths[i] = edges[i];
            }
            return paths;
        }
        Path<Node<Tile>> makeEdge(Node<Tile> node, Vector2Int direction, float currentCost)
        {
            Node<Tile> endNode = new Node<Tile>
            {
                position = node.data.position + direction,
                direction = direction,
                data = Map.Instance.getTile(node.data.position + direction)
            };
            //endNode.data = 
            Path<Node<Tile>> path = new Path<Node<Tile>>()
            {
                state = endNode,
                weight = 1 + currentCost
            };
            var rotation = node.direction - direction;
            float delta = Mathf.Abs(rotation.x) + Mathf.Abs(rotation.y);
            if (node.direction == direction) { }
            //rotate
            else if (1 == delta)
            {
                path.state.position = node.position;
                path.state.data = node.data;
                path.weight += rotationCost;
            }
            //strafe
            else
            {
                path.state.direction = node.direction;
                path.weight += strafeCost;
            }
            if (!CanMove(path.state) || !unitMovement.canMove(path.state))
            {
                path.state.data = null;
            }
            return path;
        }
        //get states
        public float getScore(Path<Node<Tile>> currentState, Node<Tile> endState, float currentCost)
        {
            float fScore = getScoreF(currentState.state.data, endState.data);
            return fScore * 2 + (currentCost + currentState.weight);
        }
        float getScoreF(Tile node, Tile goal)
        {
            return (Mathf.Abs(node.position.x - goal.position.x) + Mathf.Abs(node.position.y - goal.position.y)) * 2;
        }

        //end checker functions
        public bool reachedGoal(Node<Tile> node, Node<Tile> goal)
        {
            if (target != null)
            {
                return reachedTile(node);
            }
            else
            {
                return reachedUnit(node);
            }
        }
        bool reachedTile(Node<Tile> node)
        {
            if (node.position == target.position)
            {
                return true;
            }
            return false;
        }
        bool reachedUnit(Node<Tile> node)
        {
            return Map.Instance.getTile(node.position).unit == targetUnit;
        }
    }
}

