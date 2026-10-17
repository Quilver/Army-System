using System.Collections.Generic;
using UnityEngine;

// Works out, for each troop, which direction and distance its battle line should face from nearby enemy troops.
public static class ThreatFormationRules
{
    struct Threat
    {
        public Vector2 Direction;
        public float PivotDistance;
        public float EdgeDistance;
        public float Facing;
    }

    // Troops stay engaged until their lines are this many times the action distance apart, so contact does not flicker.
    const float DisengageDistanceScale = 1.5f;

    struct Engagement
    {
        public Troop Enemy;
        public Vector2 EnemyFacing;
    }

    public static void Update(IReadOnlyList<Troop> troops, float sDistance, float actionDistance, float deltaTime)
    {
        var engagements = FindEngagements(troops, actionDistance);
        var threats = new List<Threat>();
        foreach (var troop in troops)
        {
            engagements.TryGetValue(troop, out Engagement engagement);
            threats.Clear();
            Vector2 heading = troop.PivotVelocity.sqrMagnitude > 0.0025f
                ? troop.PivotVelocity.normalized
                : troop.transform.right;
            Threat nearestThreat = CollectThreats(troop, troops, heading, sDistance, threats, out float nearestEdge);

            troop.ThreatEdgeDistance = nearestEdge;
            if (threats.Count == 0)
            {
                troop.UpdateThreatFormation(null, 0f, deltaTime);
                continue;
            }

            Vector2 weightedDirection = Vector2.zero;
            float totalWeight = 0f;
            float weightedDistance = 0f;
            foreach (var threat in threats)
            {
                float weight = ThreatWeight(threat, nearestThreat.PivotDistance, sDistance);
                totalWeight += weight;
                weightedDirection += threat.Direction * weight;
                weightedDistance += threat.PivotDistance * weight;
            }

            if (weightedDirection.sqrMagnitude < 0.000001f || totalWeight <= 0f)
            {
                troop.UpdateThreatFormation(nearestThreat.Direction, nearestThreat.PivotDistance, deltaTime,
                    engagement.Enemy, engagement.EnemyFacing);
                continue;
            }
            troop.UpdateThreatFormation(weightedDirection.normalized, weightedDistance / totalWeight, deltaTime,
                engagement.Enemy, engagement.EnemyFacing);
        }
    }

    // Finds, for each troop, the nearest enemy whose battle line is within the action distance (melee range). Lines and
    // facings are read before any troop updates so both sides of a fight see the same state.
    static Dictionary<Troop, Engagement> FindEngagements(IReadOnlyList<Troop> troops, float actionDistance)
    {
        var engagements = new Dictionary<Troop, Engagement>();
        foreach (var troop in troops)
        {
            if (troop == null || troop.Formation == null || troop.IsFaultSlipping) continue;
            float nearestDistance = float.PositiveInfinity;
            Engagement nearest = default;
            foreach (var other in troops)
            {
                if (other == null || other == troop || other.TeamId == troop.TeamId) continue;
                if (other.Formation == null || other.IsFaultSlipping) continue;
                SegmentGeometry.ClosestSegmentPoints(troop.Formation, other.Formation, out _, out _, out float distance);
                float limit = troop.EngagedEnemy == other ? actionDistance * DisengageDistanceScale : actionDistance;
                if (distance >= limit || distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = new Engagement { Enemy = other, EnemyFacing = other.Formation.Facing };
            }
            if (nearest.Enemy != null) engagements[troop] = nearest;
        }
        // Diagnostics only: reads the previous frame's engaged enemy and never changes gameplay state.
        foreach (var troop in troops)
            if (troop != null) TroopDiagnostics.RecordEngagement(troop, engagements.TryGetValue(troop, out var found) ? found.Enemy : null);
        return engagements;
    }

    // Fills threats with every enemy whose edge is within sDistance and returns the one with the nearest pivot.
    static Threat CollectThreats(Troop troop, IReadOnlyList<Troop> troops, Vector2 heading, float sDistance,
        List<Threat> threats, out float nearestEdge)
    {
        float nearestDistance = float.PositiveInfinity;
        Threat nearestThreat = default;
        nearestEdge = float.PositiveInfinity;

        foreach (var other in troops)
        {
            if (other == null || other == troop || other.TeamId == troop.TeamId) continue;
            Vector2 offset = (Vector2)other.transform.position - (Vector2)troop.transform.position;
            float pivotDistance = offset.magnitude;
            if (pivotDistance < 0.0001f) offset = heading * 0.0001f;
            Vector2 direction = offset.normalized;
            float edgeDistance = pivotDistance - troop.Radius - other.Radius;
            if (edgeDistance > sDistance) continue;
            nearestEdge = Mathf.Min(nearestEdge, edgeDistance);

            threats.Add(new Threat
            {
                Direction = direction,
                PivotDistance = pivotDistance,
                EdgeDistance = edgeDistance,
                Facing = Vector2.Dot(direction, heading)
            });
            if (pivotDistance < nearestDistance)
            {
                nearestDistance = pivotDistance;
                nearestThreat = threats[threats.Count - 1];
            }
        }
        return nearestThreat;
    }

    static float ThreatWeight(Threat threat, float nearestPivotDistance, float sDistance)
    {
        float proximity;
        if (sDistance <= 0f)
            proximity = threat.EdgeDistance <= 0f ? 1f : 0f;
        else
            proximity = 1f / Mathf.Pow(1f + Mathf.Max(0f, threat.EdgeDistance) / sDistance, 2f);
        float nearestBias = threat.PivotDistance <= nearestPivotDistance + 0.0001f ? 4f : 0.35f;
        return proximity * (1f + 0.5f * threat.Facing) * nearestBias;
    }
}
