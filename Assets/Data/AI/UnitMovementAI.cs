using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovementAI:Action<Node<Tile>>{
    Unit unit, targetUnit;
    Tile target;
    int updateCount = 0;
    Dictionary<Node<Tile>, bool> canMove = new Dictionary<Node<Tile>, bool>();
    public aStar<Node<Tile>> route;
    //setup
    public UnitMovementAI(Unit unit)
    {
        this.unit = unit;
        initialiseGraph();
    }
    void initialiseGraph()
    {
        for (int x = 0; x < Map.Instance.Width; x++)
        {
            for (int y = 0; y < Map.Instance.Height; y++)
            {
                for (int a = -1; a <= 1; a++)
                {
                    for (int b = -1; b <= 1; b++)
                    {
                        if (Map.Instance.getTile(x+a, y+b).Walkable)//.inMap(x + a, y + b) && (a != 0 || b != 0))
                        {
                            Node<Tile> endNode = makeNode(x, y, a, b);
                            if (unit.canMove(endNode))
                            {
                                canMove.Add(endNode, true);
                            }
                            else
                            {
                                canMove.Add(endNode, false);
                            }
                        }
                    }
                }
            }
        }
    }
    //route making methods
    public void getRoute(Tile goal)
    {
        //Debug.Log("Trying to get route");
        route = new aStar<Node<Tile>>(this);
        target = goal;
        targetUnit = null;
        Node<Tile> end = new Node<Tile>
        {
            data = goal,
            position = goal.position,
            direction = new Vector2(1, 0)
        };
        for (int startX = -1; startX <= 1; startX++)
        {
            for (int startY = -1; startY <= 1; startY++)
            {
                end.direction.x = startX;
                end.direction.y = startY;
                if (canMove.ContainsKey(end) && canMove[end])
                {
                    route.createRoute(end);
                    return;
                }
            }
        }

        //setGoal(end);
    }
    public void getRoute(Unit unit)
    {
        //Debug.Log("Pathfinding towards enemy unit");
        target = null;
        targetUnit = unit;
        route = new aStar<Node<Tile>>(this);
        Node<Tile> end = new Node<Tile>
        {
            data = Map.Instance.getTile(unit.getCenterPoint()),
            position = unit.getCenterPoint(),
            direction = new Vector2(1, 0)
        };
        route.createRoute(end);
    }
    public void validateRoute()
    {
        if(updateCount < 10)
        {
            updateCount++;
        }
        else
        {
            updateCount = 0;
            if (targetUnit != null) { getRoute(targetUnit); }
            else if(target != null) {
                foreach (Node<Tile> node in route.waypoints)
                {
                    if (!unit.canMove(node))
                    {
                        //route is invalid, so generate a new one and exit
                        getRoute(target);
                        return;
                    }
                }
            }
        }
    }
    //utility methods
    override public Node<Tile> getInitialState()
    {
        Node<Tile> startNode = new Node<Tile>
        {
            position = unit.position,
            direction = unit.direction,
            data = Map.Instance.getTile(unit.position)
        };
        return startNode;
    }
    Node<Tile> makeNode(int x, int y, int a, int b)
    {
        Node<Tile> endNode = new Node<Tile>
        {
            position = new Vector2(a + x, b + y),
            direction = new Vector2(a, b),
            data = Map.Instance.getTile(new Vector2(a + x, b + y))
        };
        return endNode;
    }
    public Path<Node<Tile>> followPath()
    {
        Path<Node<Tile>> waypoint = new Path<Node<Tile>>
        {
            state = route.waypoints.Pop()
        };
        return waypoint;
    }
    public override int maxSearchSize()
    {
        return Map.Instance.Width * Map.Instance.Width * 9;
    }
    public override Path<Node<Tile>>[] getActions(Node<Tile>  node, float currentCost)
    {
        List<Path<Node<Tile>>> edges = new List<Path<Node<Tile>>>();
        for (int startX = -1; startX <= 1; startX++)
        {
            for (int startY = -1; startY <= 1; startY++)
            {
                int multiplier = 0;
                bool flag = true;
                while (flag)
                {
                    multiplier++;
                    int x = multiplier * startX;
                    int y = multiplier * startY;
                    if (Map.Instance.getTile(node.data.position.x + x, node.data.position.y + y).Walkable && (x != 0 || y != 0)) 
                        //inMap(node.data.position.x + x, node.data.position.y + y) && (x != 0 || y != 0))
                    {
                        Path<Node<Tile>> path = makeEdge(node, x, y, multiplier, currentCost);
                        if (path.state.data != null)
                        {
                            edges.Add(path);
                        }
                        else { flag = false; }
                    }
                    else { flag = false; }
                
                }
            }
        }
        Path<Node<Tile>>[] paths = new Path<Node<Tile>>[edges.Count];
        for(int i = edges.Count - 1; i >= 0; i--)
        {
            paths[i] = edges[i];
        }
        return paths;
    }
    Path<Node<Tile>> makeEdge(Node<Tile> node, int x, int y, int multiplier, float currentCost)
    {
        Node<Tile> endNode = new Node<Tile>
        {
            position = new Vector2(node.data.position.x + x * multiplier, node.data.position.y + y * multiplier),
            direction = new Vector2(x, y)
        };
        endNode.data = Map.Instance.getTile(endNode.position);
        Path<Node<Tile>> path = new Path<Node<Tile>>()
        {
            state = endNode,
            weight = 1 + currentCost
        };
        float delta = (Mathf.Abs(node.direction.x - x) + Mathf.Abs(node.direction.y - y));
        //advance
        if (delta == 0)
        {
            //do nothing
        }
        //retreat
        else if (node.direction.x == -x && node.direction.y == -y)
        {
            //Debug.Log("Retreat to " + path.state.position + " from " + node.position);
            path.state.direction = node.direction;
            path.weight += 5;
        }
        //rotate
        else if(1 == delta && multiplier==1){
            path.state.position = node.position;
            path.state.data = node.data;
            path.weight += unit.rotationCost();
        }
        //strafe
        else if (2 == delta){
            path.state.direction = node.direction;
            path.weight += 10;
        }
        //do nothing
        else
        {
            path.state.data = null;
        }
        //if strafe doesn't work, returns null
        if (!canMove.ContainsKey(path.state) || !canMove[path.state] || !unit.canMove(path.state))
        {
            path.state.data = null;
        }
            return path;
    }
    //get states
    public override float getScore(Path<Node<Tile>> currentState, Node<Tile> endState, float currentCost)
    {
        float fScore = getScoreF(currentState.state.data, endState.data);
        return fScore * 2 + (currentCost + currentState.weight);
    }
    float getScoreF(Tile node, Tile goal)
    {
        return (Mathf.Abs(node.position.x - goal.position.x) + Mathf.Abs(node.position.y - goal.position.y)) * 2;
    }
    
    //end checker functions
    override public bool reachedGoal(Node<Tile> node, Node<Tile> goal)
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
        if (targetUnit.adjacentTile(node.position))
        {
            return true;
        }
        return false;
    }
}
