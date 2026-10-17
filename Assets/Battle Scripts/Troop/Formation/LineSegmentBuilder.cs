using System.Collections.Generic;
using UnityEngine;

// Builds one line segment per troop of a connected allied group. Troops without a formation of their own
// borrow an orientation from the nearest anchor (a troop that already has a formation or a link).
public static class LineSegmentBuilder
{
    public static Dictionary<Troop, LineSegment> Build(List<Troop> group)
    {
        var anchors = new List<Troop>();
        foreach (var troop in group)
            if (troop.Formation != null || troop.GetLineLink(0) != null || troop.GetLineLink(1) != null)
                anchors.Add(troop);

        var segments = new Dictionary<Troop, LineSegment>();
        foreach (var troop in group)
        {
            TroopFormationData preferred = troop.Formation;
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
            }
            else
            {
                Troop neighbor = FindNearestAnchor(troop, anchors);
                if (neighbor == null) continue;
                Vector2 toNeighbor = ((Vector2)neighbor.transform.position - (Vector2)troop.transform.position).normalized;
                tangent = toNeighbor.sqrMagnitude > 0.0001f ? toNeighbor : Vector2.up;
                facing = new Vector2(tangent.y, -tangent.x);
                center = troop.transform.position;
                isThreatFacing = false;
            }

            Vector2 relative = center - (Vector2)troop.transform.position;
            float halfLength = Mathf.Min(
                troop.LineWidth * 0.5f,
                Mathf.Sqrt(Mathf.Max(0f, Mathf.Pow(troop.Radius * 0.9f, 2f) - relative.sqrMagnitude)));
            segments[troop] = new LineSegment
            {
                Start = center - tangent * halfLength,
                End = center + tangent * halfLength,
                Facing = facing,
                IsThreatFacing = isThreatFacing
            };
        }
        return segments;
    }

    static Troop FindNearestAnchor(Troop subject, List<Troop> anchors)
    {
        Troop nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Troop candidate in anchors)
        {
            if (candidate == subject) continue;
            float distance = TroopPivots.Distance(subject, candidate);
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }
        return nearest;
    }
}
