using System.Collections.Generic;
using UnityEngine;

public static class FlockFormationRules
{
    struct Threat
    {
        public Vector2 Direction;
        public float PivotDistance;
        public float EdgeDistance;
        public float Facing;
    }

    sealed class Segment
    {
        public Vector2 Start;
        public Vector2 End;
        public Vector2 Facing;
        public bool IsThreatFacing;
    }

    public static void UpdateThreatFormations(IReadOnlyList<Flock> flocks, float sDistance, float deltaTime)
    {
        foreach (var flock in flocks)
        {
            Vector2 heading = flock.PivotVelocity.sqrMagnitude > 0.0025f
                ? flock.PivotVelocity.normalized
                : flock.transform.right;
            var threats = new List<Threat>();
            float nearestDistance = float.PositiveInfinity;
            Threat nearestThreat = default;

            foreach (var other in flocks)
            {
                if (other == null || other == flock || other.TeamId == flock.TeamId) continue;
                Vector2 offset = (Vector2)other.transform.position - (Vector2)flock.transform.position;
                float pivotDistance = offset.magnitude;
                if (pivotDistance < 0.0001f) offset = heading * 0.0001f;
                Vector2 direction = offset.normalized;
                float edgeDistance = pivotDistance - flock.Radius - other.Radius;
                if (edgeDistance > sDistance) continue;

                float facing = Vector2.Dot(direction, heading);
                threats.Add(new Threat
                {
                    Direction = direction,
                    PivotDistance = pivotDistance,
                    EdgeDistance = edgeDistance,
                    Facing = facing
                });
                if (pivotDistance < nearestDistance)
                {
                    nearestDistance = pivotDistance;
                    nearestThreat = threats[threats.Count - 1];
                }
            }

            if (threats.Count == 0)
            {
                flock.UpdateThreatFormation(null, 0f, deltaTime);
                continue;
            }

            Vector2 weightedDirection = Vector2.zero;
            float totalWeight = 0f;
            float weightedDistance = 0f;
            foreach (var threat in threats)
            {
                float proximity;
                if (sDistance <= 0f)
                    proximity = threat.EdgeDistance <= 0f ? 1f : 0f;
                else
                    proximity = 1f / Mathf.Pow(1f + Mathf.Max(0f, threat.EdgeDistance) / sDistance, 2f);
                float nearestBias = threat.PivotDistance <= nearestDistance + 0.0001f ? 4f : 0.35f;
                float weight = proximity * (1f + 0.5f * threat.Facing) * nearestBias;
                totalWeight += weight;
                weightedDirection += threat.Direction * weight;
                weightedDistance += threat.PivotDistance * weight;
            }

            if (weightedDirection.sqrMagnitude < 0.000001f || totalWeight <= 0f)
            {
                flock.UpdateThreatFormation(nearestThreat.Direction, nearestThreat.PivotDistance, deltaTime);
                continue;
            }
            flock.UpdateThreatFormation(weightedDirection.normalized, weightedDistance / totalWeight, deltaTime);
        }
    }

    public static void ConnectAlliedLines(IReadOnlyList<Flock> flocks)
    {
        PruneLinks(flocks);
        var remaining = new HashSet<Flock>(flocks);
        while (remaining.Count > 0)
        {
            Flock seed = null;
            foreach (var candidate in remaining)
            {
                seed = candidate;
                break;
            }
            remaining.Remove(seed);
            var group = new List<Flock> { seed };
            for (int i = 0; i < group.Count; i++)
            {
                Flock member = group[i];
                var candidates = new List<Flock>(remaining);
                foreach (var candidate in candidates)
                {
                    if (candidate.TeamId != seed.TeamId) continue;
                    if (PivotDistance(member, candidate) >= member.Radius + candidate.Radius && !LinkedTo(member, candidate)) continue;
                    remaining.Remove(candidate);
                    group.Add(candidate);
                }
            }

            bool hasThreatFacingAnchor = false;
            foreach (var flock in group)
                if (flock.HasThreatFacingFormation) hasThreatFacingAnchor = true;
            if (!hasThreatFacingAnchor)
            {
                ClearGroupLinks(group);
                continue;
            }

            var segments = BuildSegments(group);
            LinkAvailableEnds(group, segments);
            SnapLinkedEnds(group, segments);
        }
    }

    static Dictionary<Flock, Segment> BuildSegments(List<Flock> group)
    {
        var anchors = new List<Flock>();
        foreach (var flock in group)
            if (flock.Formation != null || flock.GetLineLink(0) != null || flock.GetLineLink(1) != null)
                anchors.Add(flock);

        var segments = new Dictionary<Flock, Segment>();
        foreach (var flock in group)
        {
            FlockFormationData preferred = flock.Formation;
            Vector2 center;
            Vector2 facing;
            Vector2 tangent;
            bool isThreatFacing;
            if (preferred != null)
            {
                center = (preferred.Start + preferred.End) * 0.5f;
                facing = preferred.Facing;
                tangent = preferred.Tangent;
                isThreatFacing = preferred.IsThreatFacing;
                if (group.Count > 1 && isThreatFacing)
                    center -= facing * (flock.Radius * 0.25f);
            }
            else
            {
                Flock neighbor = FindNearestAnchor(flock, anchors);
                if (neighbor == null) continue;
                Vector2 toNeighbor = ((Vector2)neighbor.transform.position - (Vector2)flock.transform.position).normalized;
                tangent = toNeighbor.sqrMagnitude > 0.0001f ? toNeighbor : Vector2.up;
                facing = new Vector2(tangent.y, -tangent.x);
                center = flock.transform.position;
                isThreatFacing = false;
            }

            Vector2 relative = center - (Vector2)flock.transform.position;
            float halfLength = Mathf.Min(
                flock.LineWidth * 0.5f,
                Mathf.Sqrt(Mathf.Max(0f, Mathf.Pow(flock.Radius * 0.9f, 2f) - relative.sqrMagnitude)));
            segments[flock] = new Segment
            {
                Start = center - tangent * halfLength,
                End = center + tangent * halfLength,
                Facing = facing,
                IsThreatFacing = isThreatFacing
            };
        }
        return segments;
    }

    static void LinkAvailableEnds(List<Flock> group, Dictionary<Flock, Segment> segments)
    {
        var pairs = new List<(Flock a, Flock b, float distance)>();
        for (int i = 0; i < group.Count; i++)
        for (int j = i + 1; j < group.Count; j++)
        {
            Flock a = group[i];
            Flock b = group[j];
            if (!segments.ContainsKey(a) || !segments.ContainsKey(b) || a.TeamId != b.TeamId) continue;
            float distance = PivotDistance(a, b);
            if (distance < a.Radius + b.Radius) pairs.Add((a, b, distance));
        }
        pairs.Sort((a, b) => a.distance.CompareTo(b.distance));

        foreach (var pair in pairs)
        {
            if (LinkedTo(pair.a, pair.b)) continue;
            var freeA = FreeEnds(pair.a);
            var freeB = FreeEnds(pair.b);
            if (freeA.Count == 0 || freeB.Count == 0) continue;

            int bestA = -1;
            int bestB = -1;
            float bestDistance = float.PositiveInfinity;
            foreach (int endA in freeA)
            foreach (int endB in freeB)
            {
                Vector2 pointA = Endpoint(segments[pair.a], endA);
                Vector2 pointB = Endpoint(segments[pair.b], endB);
                float distance = Vector2.Distance(pointA, pointB);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestA = endA;
                bestB = endB;
            }
            if (bestA < 0) continue;
            pair.a.SetLineLink(bestA, pair.b, bestB);
            pair.b.SetLineLink(bestB, pair.a, bestA);
        }
    }

    static void SnapLinkedEnds(List<Flock> group, Dictionary<Flock, Segment> segments)
    {
        var joints = new Dictionary<Flock, Vector2[]>();
        foreach (Flock flock in group)
            joints[flock] = new[] { Endpoint(segments[flock], 0), Endpoint(segments[flock], 1) };

        foreach (Flock flock in group)
        for (int end = 0; end < 2; end++)
        {
            FlockEndpointLink link = flock.GetLineLink(end);
            if (link == null || link.Other == null || flock.GetInstanceID() >= link.Other.GetInstanceID()) continue;
            if (!segments.TryGetValue(link.Other, out var otherSegment)) continue;

            Vector2 localPoint = Endpoint(segments[flock], end);
            Vector2 otherPoint = Endpoint(otherSegment, link.OtherEnd);
            Vector2 tension = otherPoint - localPoint;
            flock.SetLineLinkTension(end, tension);
            link.Other.SetLineLinkTension(link.OtherEnd, -tension);
            Vector2 joint = (localPoint + otherPoint) * 0.5f;
            joints[flock][end] = joint;
            joints[link.Other][link.OtherEnd] = joint;
        }

        foreach (Flock flock in group)
        {
            if (!segments.TryGetValue(flock, out var segment)) continue;
            segment.Start = joints[flock][0];
            segment.End = joints[flock][1];
            var formation = new FlockFormationData
            {
                Start = segment.Start,
                End = segment.End,
                Facing = segment.Facing,
                IsThreatFacing = segment.IsThreatFacing,
                StartLinked = flock.GetLineLink(0) != null,
                EndLinked = flock.GetLineLink(1) != null
            };
            flock.Formation = formation;
        }
    }

    static void PruneLinks(IReadOnlyList<Flock> flocks)
    {
        foreach (Flock flock in flocks)
        for (int end = 0; end < 2; end++)
        {
            FlockEndpointLink link = flock.GetLineLink(end);
            if (link == null) continue;
            Flock other = link.Other;
            FlockEndpointLink reciprocal = other != null ? other.GetLineLink(link.OtherEnd) : null;
            bool valid = other != null
                && other.TeamId == flock.TeamId
                && PivotDistance(flock, other) < (flock.Radius + other.Radius) * 1.05f
                && reciprocal != null
                && reciprocal.Other == flock
                && reciprocal.OtherEnd == end;
            if (valid) continue;
            flock.ClearLineLink(end);
            if (reciprocal != null && reciprocal.Other == flock && reciprocal.OtherEnd == end)
                other.ClearLineLink(link.OtherEnd);
        }
    }

    static void ClearGroupLinks(List<Flock> group)
    {
        var members = new HashSet<Flock>(group);
        foreach (Flock flock in group)
        for (int end = 0; end < 2; end++)
        {
            FlockEndpointLink link = flock.GetLineLink(end);
            if (link == null || !members.Contains(link.Other)) continue;
            FlockEndpointLink reciprocal = link.Other.GetLineLink(link.OtherEnd);
            flock.ClearLineLink(end);
            if (reciprocal != null && reciprocal.Other == flock && reciprocal.OtherEnd == end)
                link.Other.ClearLineLink(link.OtherEnd);
        }
    }

    static Flock FindNearestAnchor(Flock subject, List<Flock> anchors)
    {
        Flock nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Flock candidate in anchors)
        {
            if (candidate == subject) continue;
            float distance = PivotDistance(subject, candidate);
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }
        return nearest;
    }

    static List<int> FreeEnds(Flock flock)
    {
        var result = new List<int>(2);
        if (flock.GetLineLink(0) == null) result.Add(0);
        if (flock.GetLineLink(1) == null) result.Add(1);
        return result;
    }

    static bool LinkedTo(Flock a, Flock b)
    {
        return a.GetLineLink(0)?.Other == b || a.GetLineLink(1)?.Other == b;
    }

    static Vector2 Endpoint(Segment segment, int end) => end == 0 ? segment.Start : segment.End;
    static float PivotDistance(Flock a, Flock b) => Vector2.Distance(a.transform.position, b.transform.position);
}
