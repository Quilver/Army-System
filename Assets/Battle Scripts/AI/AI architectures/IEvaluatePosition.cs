using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.MapEvaluator
{
    [System.Serializable]
    public abstract class IEvaluate
    {
        protected ISquad squad;
        public void Setup(ISquad squad) => this.squad = squad;
        public static float roughUnitRadius = 2.5f;
        public static bool CanSeeTarget(Vector2 position, Transform target)
        {
            Vector2 dir = ((Vector2)target.position - position);
            RaycastHit2D ray = Physics2D.CircleCast(position, roughUnitRadius, dir, dir.magnitude, 1 << 6 | 1 << 8);
            if (ray && ray.transform != target) return false;
            return true;
        }
        public static bool CanWalkToTarget(Vector2 position, Transform target)
        {
            Vector2 dir = ((Vector2)target.position - position);
            RaycastHit2D ray = Physics2D.CircleCast(position, roughUnitRadius, dir, dir.magnitude, 1 << 3 | 1 << 6 | 1 << 8);
            if (ray && ray.transform != target) return false;
            return true;
        }
        public static bool CanWalkToTarget(Vector2 position, Vector2 target)
        {
            Vector2 dir = target - position;
            RaycastHit2D ray = Physics2D.CircleCast(position, roughUnitRadius, dir, dir.magnitude, 1 << 3 | 1 << 6 | 1 << 8);
            if (ray) return false;
            return true;
        }
        public abstract void EvaluateOrders(InfluenceMap map);
    }
    [Serializable]
    public abstract class IEvaluatePosition : IEvaluate
    {
        protected virtual List<float> ScoreOrders(List<Vector2> orders, InfluenceMap map)
        {
            List<float> results = new();
            foreach(Vector2 order in orders) 
                results.Add(EvaluateOrder(order, map));
            return results;
        }
        public abstract float EvaluateOrder(Vector2 order, InfluenceMap map);

    }
    [Serializable]
    public abstract class IEvaluatePosTarget : IEvaluate
    {
        protected virtual List<ScoreAndUnit> ScoreOrders(List<Vector2> orders, InfluenceMap map)
        {
            List<ScoreAndUnit> results = new();
            foreach (Vector2 order in orders)
                results.Add(EvaluateOrder(order, map));
            return results;
        }
        public abstract ScoreAndUnit EvaluateOrder(Vector2 order, InfluenceMap map);
    }
}