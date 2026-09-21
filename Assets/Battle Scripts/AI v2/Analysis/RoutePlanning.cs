using System.Collections.Generic;
using UnityEngine;

namespace ArmySystem.AI.V2
{
    public interface IRoutePlanner
    {
        PlannedRoute FindRoute(RouteRequest request, AIContext context);
    }

    public interface IRouteUtility
    {
        float Evaluate(PlannedRoute route, RouteRequest request, AIContext context);
    }

    public sealed class RouteRequest
    {
        public IAISquad Squad { get; set; }
        public Vector2 Start { get; set; }
        public Vector2 Objective { get; set; }
        public IReadOnlyList<RequiredCrossing> RequiredCrossings { get; set; }
        public IReadOnlyList<RouteBarrier> Barriers { get; set; }
        public IRouteUtility Utility { get; set; }
    }

    public sealed class PlannedRoute
    {
        public IReadOnlyList<Vector2> Waypoints { get; set; }
        public float TravelCost { get; set; }
        public float ExposureCost { get; set; }
        public float TerrainCost { get; set; }
        public float Utility { get; set; }
        public bool IsReachable { get; set; }
    }

    public readonly struct RequiredCrossing
    {
        public RequiredCrossing(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
    }

    public readonly struct RouteBarrier
    {
        public RouteBarrier(Vector2 start, Vector2 end, bool isRay)
        {
            Start = start;
            End = end;
            IsRay = isRay;
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
        public bool IsRay { get; }
    }
}
