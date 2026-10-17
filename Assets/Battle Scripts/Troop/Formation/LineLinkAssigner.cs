using System.Collections.Generic;
using UnityEngine;

// Creates, validates and clears the endpoint links that join allied troops into a single line.
public static class LineLinkAssigner
{
    const float LinkBreakDistanceScale = 1.25f;

    public static void Prune(IReadOnlyList<Troop> troops)
    {
        foreach (Troop troop in troops)
        for (int end = 0; end < 2; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null) continue;
            Troop other = link.Other;
            TroopEndpointLink reciprocal = other != null ? other.GetLineLink(link.OtherEnd) : null;
            // Established links tolerate extra stretch, and are never broken by distance while either troop retreats.
            bool retreating = troop.IsFaultSlipping || (other != null && other.IsFaultSlipping);
            bool valid = other != null
                && other.TeamId == troop.TeamId
                && (retreating || TroopPivots.Distance(troop, other) < (troop.Radius + other.Radius) * LinkBreakDistanceScale)
                && reciprocal != null
                && reciprocal.Other == troop
                && reciprocal.OtherEnd == end;
            if (valid) continue;
            TroopDiagnostics.RecordLinkCleared(troop, end, other, link.OtherEnd, PruneReason(troop, other, reciprocal, end));
            troop.ClearLineLink(end);
            if (reciprocal != null && reciprocal.Other == troop && reciprocal.OtherEnd == end)
                other.ClearLineLink(link.OtherEnd);
        }
    }

    public static void LinkAvailableEnds(List<Troop> group, Dictionary<Troop, LineSegment> segments)
    {
        var pairs = new List<(Troop a, Troop b, float distance)>();
        for (int i = 0; i < group.Count; i++)
        for (int j = i + 1; j < group.Count; j++)
        {
            Troop a = group[i];
            Troop b = group[j];
            if (!segments.ContainsKey(a) || !segments.ContainsKey(b) || a.TeamId != b.TeamId) continue;
            float distance = TroopPivots.Distance(a, b);
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
                Vector2 pointA = segments[pair.a].Endpoint(endA);
                Vector2 pointB = segments[pair.b].Endpoint(endB);
                float distance = Vector2.Distance(pointA, pointB);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestA = endA;
                bestB = endB;
            }
            if (bestA < 0) continue;
            pair.a.SetLineLink(bestA, pair.b, bestB);
            pair.b.SetLineLink(bestB, pair.a, bestA);
            TroopDiagnostics.RecordLinkCreated(pair.a, bestA, pair.b, bestB, bestDistance);
        }
    }

    public static void ClearGroupLinks(List<Troop> group)
    {
        var members = new HashSet<Troop>(group);
        foreach (Troop troop in group)
        for (int end = 0; end < 2; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null || !members.Contains(link.Other)) continue;
            TroopEndpointLink reciprocal = link.Other.GetLineLink(link.OtherEnd);
            TroopDiagnostics.RecordLinkCleared(troop, end, link.Other, link.OtherEnd, "group reset");
            troop.ClearLineLink(end);
            if (reciprocal != null && reciprocal.Other == troop && reciprocal.OtherEnd == end)
                link.Other.ClearLineLink(link.OtherEnd);
        }
    }

    public static bool LinkedTo(Troop a, Troop b)
    {
        return a.GetLineLink(0)?.Other == b || a.GetLineLink(1)?.Other == b;
    }

    static string PruneReason(Troop troop, Troop other, TroopEndpointLink reciprocal, int end)
    {
        if (other == null) return "other destroyed";
        if (other.TeamId != troop.TeamId) return "team changed";
        if (reciprocal == null || reciprocal.Other != troop || reciprocal.OtherEnd != end) return "reciprocal missing";
        return "too far";
    }

    static List<int> FreeEnds(Troop troop)
    {
        var result = new List<int>(2);
        if (troop.GetLineLink(0) == null) result.Add(0);
        if (troop.GetLineLink(1) == null) result.Add(1);
        return result;
    }
}
