using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct Path<State>
{
    public State state;
    public float weight;
};
public abstract class Action<State>{
    Dictionary<State, aStar<State>> plans = new Dictionary<State, aStar<State>>();
    protected void setGoal(State goal)
    {
        if (plans.ContainsKey(goal))
        {
            return;
        }
        aStar<State> plan = new aStar<State>(this);
        plan.createRoute(goal);
        plans.Add(goal, plan);
    }
    abstract public int maxSearchSize();
    abstract public Path<State>[] getActions(State source, float prevCost);
    abstract public float getScore(Path<State> currentState, State endState, float currectCost);
    abstract public State getInitialState();
    abstract public bool reachedGoal(State node, State goal);
}
