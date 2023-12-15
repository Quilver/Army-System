/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
public class SegmentGraph{
    Map map;
    public static SegmentGraph Instance { get; protected set; }
    public Dictionary<Tile, Node<Tile>> nodes;
    public SegmentGraph()
    {
        if (Instance != null)
        {
            Debug.Log("Error: there should only be one graph instance");
        }
        Instance = this;
        map = Map.Instance;
        generateMap();
    }
    void generateMap()
    {
        nodes = new Dictionary<Tile, Node<Tile>>();
        //nodes.Clear();
        Debug.Log(map.getTile(0, 0));
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.hieght; y++)
            {
                if (map.getTile(x, y).tileIsWalkable())
                {
                    generateNode(x ,y);
                }
            }
        }
        Debug.Log("number of node created "+nodes.Count);
        foreach (Node<Tile> node in nodes.Values)
        {
            generatePaths(node);
        }
    }
    void generateNode(int x, int y)
    {
        Node<Tile> node = new Node<Tile>();
        node.x = x;
        node.y = y;
        node.data = Map.Instance.getTile(x, y);
        getNodeArea(node);
        if (node == null || Map.Instance.getTile(x, y) == null) { Debug.Log(node + " node, tile " + map.getTile(x, y)); }
        nodes.Add(Map.Instance.getTile(x, y), node);
    }
    void getNodeArea(Node<Tile> node)
    {

    }
    void generatePaths(Node<Tile> node)
    {
        int paths = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (inMap(node.x + x, node.y + y) && Map.Instance.getTile(node.x + x, node.y + y).tileIsWalkable() && (x !=0 || y!=0))
                {
                    paths++;
                }
            }
        }
        node.paths = new Edge<Tile>[paths];
        paths = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (inMap(node.x + x, node.y + y) && Map.Instance.getTile(node.x + x, node.y + y).tileIsWalkable() && (x != 0 || y != 0))
                {
                    Edge<Tile> path = new Edge<Tile>();
                    path.node = nodes[ Map.Instance.getTile(node.x + x, node.y + y) ];
                    path.direction = new Vector2(x, y);
                    path.weight = Map.Instance.getTile(node.x + x, node.y + y).tileSpeedCost();
                    node.paths[paths] = path;
                    paths++;
                }
            }
        }
    }
    public static bool inMap(float x, float y) {
        return (x >= 0 && x < Map.Instance.width) && (y >= 0 && y < Map.Instance.hieght);
    }
    public Stack<Tile> aStarPath(Tile start, Tile end, Unit unit)
    {
        List<Node<Tile>> closedSet = new List<Node<Tile>>();
        SimplePriorityQueue<Node<Tile>> openSet = new SimplePriorityQueue<Node<Tile>>();
        openSet.Enqueue(nodes[start], 0);
        Dictionary<Node<Tile>, Node<Tile>> cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();
        Dictionary<Node<Tile>, float> gScore = new Dictionary<Node<Tile>, float>();
        foreach(Node<Tile> node in nodes.Values)
        {
            gScore.Add(node, Mathf.Infinity);
        }
        gScore[nodes[start]] = 0;
        Dictionary<Node<Tile>, float> fScore = new Dictionary<Node<Tile>, float>();
        foreach (Node<Tile> node in nodes.Values)
        {
            fScore.Add(node, getScoreF(node, nodes[end]));
        }
        while (openSet.Count > 0)
        {
            Node<Tile> node = openSet.Dequeue();
            //Ending search when destination is reached
            if (node.x == nodes[end].x && node.y == nodes[end].y)
            {
                Stack<Tile> waypoints = new Stack<Tile>();
                while (node != nodes[start])
                {
                    waypoints.Push(map.getTile(node.x, node.y));
                    node = cameFrom[node];
                }
                waypoints.Push(map.getTile(node.x, node.y));
                //Debug.Log("Path found");
                return waypoints;
            }
            //Checking if node is valid for the unit
            if (unit.canMove(node) == false) continue;
            closedSet.Add(node);
            //adding new nodes to see if they are valid
            foreach (Edge<Tile> path in node.paths)
            {
                if (unit.canMove(path) == false) { continue; }
                if (closedSet.Contains(path.node)) { continue; }
                else if (openSet.Contains(path.node)) { continue; }
                else {
                    gScore[path.node] = gScore[node] + path.weight;
                    float cost = fScore[path.node] + gScore[path.node];
                    openSet.Enqueue(path.node, cost);
                    cameFrom.Add(path.node, node);
                }
            }
        }
        Debug.Log("Path not found");
        return null;
    }
    public Stack<Node<Tile>> aStartPath(Tile end, Unit unit)
    {
        Node<Tile> start = new Node<Tile>() {
            data = map.getTile(unit.position),
            direction = unit.direction
        };
        List<Node<Tile>> closedSet = new List<Node<Tile>>();
        SimplePriorityQueue<Node<Tile>> openSet = new SimplePriorityQueue<Node<Tile>>();
        openSet.Enqueue(start, 0);
        Dictionary<Node<Tile>, Node<Tile>> cameFrom = new Dictionary<Node<Tile>, Node<Tile>>();
        Dictionary<Node<Tile>, float> gScore = new Dictionary<Node<Tile>, float>();
        foreach (Node<Tile> node in nodes.Values)
        {
            //gScore.Add(node, Mathf.Infinity);
        }
        gScore.Add(start, 0);
        Dictionary<Node<Tile>, float> fScore = new Dictionary<Node<Tile>, float>();
        foreach (Node<Tile> node in nodes.Values)
        {
            //fScore.Add(node, getScoreF(node, nodes[end]));
        }
        fScore.Add(start, getScoreF(start, nodes[end]));
        while (openSet.Count > 0)
        {
            Node<Tile> node = openSet.Dequeue();
            //Ending search when destination is reached
            if (node.x == nodes[end].x && node.y == nodes[end].y)
            {
                Stack<Node<Tile>> waypoints = new Stack<Node<Tile>>();
                while (node != start)
                {
                    //waypoints.Push(map.getTile(node.x, node.y));
                    node = cameFrom[node];
                }
                //waypoints.Push(map.getTile(node.x, node.y));
                //Debug.Log("Path found");
                return waypoints;
            }
            //Checking if node is valid for the unit
            if (unit.canMove(node) == false) continue;
            closedSet.Add(node);
            //adding new nodes to see if they are valid
            foreach (Edge<Tile> path in node.paths)
            {
                if (unit.canMove(path) == false) { continue; }
                if (closedSet.Contains(path.node)) { continue; }
                else if (openSet.Contains(path.node)) { continue; }
                else
                {
                    gScore[path.node] = gScore[node] + path.weight;
                    float cost = fScore[path.node] + gScore[path.node];
                    openSet.Enqueue(path.node, cost);
                    cameFrom.Add(path.node, node);
                }
            }
        }
        Debug.Log("Path not found");
        return null;
    }
    Node<Tile> makeNode(Unit unit, Vector2 position, Vector2 facing)
    {
        Node<Tile> node = new Node<Tile>()
        {
            data = Map.Instance.getTile(position),
            direction = facing
        };
        return node;
    }
    void makeEdges(Unit unit, Node<Tile> node)
    {
        //List<Edge<Tile>> paths;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (inMap(node.data.position.x + x, node.data.position.y + y) && (x != 0 || y != 0))
                {
                    Edge<Tile> path = new Edge<Tile>()
                    {
                        direction = new Vector2(x, y),
                        node = makeNode(unit, new Vector2(node.data.position.x + x, node.data.position.y + y), new Vector2(x, y))
                    };

                }
            }
        }

    }
    float getScoreF(Node<Tile> node, Node<Tile> goal)
    {
        return Mathf.Sqrt(Mathf.Pow(node.x - goal.x, 2) + Mathf.Pow(node.y - goal.y, 2));
    }
}*/
