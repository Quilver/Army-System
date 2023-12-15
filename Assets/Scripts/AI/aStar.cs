using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Priority_Queue;
public class aStar<State>{
    //the path of edges the unit will follow
    public Stack<State> waypoints = new Stack<State>();
    Action<State> controller;
    int nodesAdded;
    //
    Dictionary<State, int> nodes = new Dictionary<State, int>();
    Dictionary<State, int> closedSet = new Dictionary<State, int>();
    minHeap<Path<State>> openSet;
    //SimplePriorityQueue<Path<State>> open_set = new SimplePriorityQueue<Path<State>>();
    Dictionary<Path<State>, State> source = new Dictionary<Path<State>, State>();
    Dictionary<State, State> cameFrom = new Dictionary<State, State>();
    public aStar(Action<State> user)
    {
        controller = user;
    }
    public void createRoute(State goal)
    {
        //Debug.Log("Creating a loop.");
        //
        closedSet.Clear();
        openSet = new minHeap<Path<State>>(controller.maxSearchSize());
        waypoints.Clear();
        source.Clear();
        cameFrom.Clear();

        //open_set.Clear();
        nodesAdded = 0;
        //
        UnityEngine.Profiling.Profiler.BeginSample("A* script");
        State n = controller.getInitialState();
        Path<State> node = new Path<State>
        {
            state = n,
            weight = 0
        };
        openSet.insert(node, 0);
        //open_set.Enqueue(node, 0);
        while (openSet.size > 0)
        //while (open_set.Count > 0)
        {
            //openSet.displayScores();
            if(openSet.size>0)
                node = openSet.extractMin();
            //node = open_set.Dequeue();
            //checks if node has already been added
            if (closedSet.ContainsKey(node.state)) { continue; }
            closedSet.Add(node.state, 1);
            if (source.ContainsKey(node))
            {
                cameFrom.Add(node.state, source[node]);
            }
            //reached goal
            if (controller.reachedGoal(node.state, goal))
            {
                //Debug.Log("Number of nodes added: " + nodesAdded);
                //Debug.Log("Number of nodes considered: " + closedSet.Count);
                //Debug.Log("Found route");
                goalReached(node.state);
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }
            //if not goal state, explores what other states can be taken
            expandNode(node.state, goal, node.weight);
        }
        UnityEngine.Profiling.Profiler.EndSample();
        Debug.Log("Path not found");
    }
    void goalReached(State node)
    {
        waypoints = new Stack<State>();
        while (cameFrom.ContainsKey(node))
        {
            waypoints.Push(node);
            node = cameFrom[node];
        }
        //Debug.Log("Number of nodes in the path: " + waypoints.Count);
    }
    void expandNode(State node, State goal, float currentCost)
    {

        foreach (Path<State> path in controller.getActions(node, currentCost))
        {
            if (closedSet.ContainsKey(path.state)) { continue; }
            else if (source.ContainsKey(path)) { continue; }
            else
            {
                float cost = controller.getScore(path, goal, path.weight);
                if (nodes.ContainsKey(path.state) && nodes[path.state] > cost) { nodes[path.state] = Mathf.RoundToInt(cost); }
                else if (nodes.ContainsKey(path.state) && nodes[path.state] <= cost) { continue; }
                else { nodes.Add(path.state, Mathf.RoundToInt(cost)); }
                nodesAdded++;
                openSet.insert(path, Mathf.RoundToInt(cost));
                //open_set.Enqueue(path, Mathf.RoundToInt(cost));
                source.Add(path, node);
            }
        }
    }
}
