using System.Collections.Generic;
using UnityEngine;

// Orients line segments around their links: aims the ends of neighbours at middle troops, then snaps linked ends to a shared
// joint and publishes the final formation to each troop.
public static class LineEndAimer
{
    // A troop linked at both ends is the middle of a line. The adjacent troops' ends that connect to it are
    // re-aimed so they point at the middle troop's centre. Neighbours that are themselves linked at both ends
    // keep their own orientation so chains of middle troops do not fight over each other.
    public static void AimEndsAtMiddleTroops(List<Troop> group, Dictionary<Troop, LineSegment> segments)
    {
        var aimed = new HashSet<Troop>();
        foreach (Troop middle in group)
        {
            TroopEndpointLink startLink = middle.GetLineLink(0);
            TroopEndpointLink endLink = middle.GetLineLink(1);
            if (startLink == null || endLink == null || startLink.Other == null || endLink.Other == null) continue;
            if (startLink.Other == endLink.Other) continue;

            Vector2 middleCenter = middle.transform.position;
            AimEnd(startLink, middleCenter, segments, aimed);
            AimEnd(endLink, middleCenter, segments, aimed);
        }
    }

    static void AimEnd(TroopEndpointLink link, Vector2 middleCenter, Dictionary<Troop, LineSegment> segments, HashSet<Troop> aimed)
    {
        Troop neighbor = link.Other;
        if (neighbor.GetLineLink(0) != null && neighbor.GetLineLink(1) != null) return;
        if (aimed.Contains(neighbor) || !segments.TryGetValue(neighbor, out LineSegment segment)) return;

        Vector2 center = (segment.Start + segment.End) * 0.5f;
        Vector2 toMiddle = middleCenter - center;
        if (toMiddle.sqrMagnitude < 0.0001f) return;

        // End 0 sits at -tangent, end 1 at +tangent; orient the tangent so the linked end faces the middle troop.
        Vector2 aimDirection = toMiddle.normalized;
        Vector2 tangent = link.OtherEnd == 0 ? -aimDirection : aimDirection;
        float halfLength = (segment.End - segment.Start).magnitude * 0.5f;

        Vector2 facing = new(tangent.y, -tangent.x);
        if (Vector2.Dot(facing, segment.Facing) < 0f) facing = -facing;

        segment.Start = center - tangent * halfLength;
        segment.End = center + tangent * halfLength;
        segment.Facing = facing;
        aimed.Add(neighbor);
    }

    public static void SnapLinkedEnds(List<Troop> group, Dictionary<Troop, LineSegment> segments)
    {
        var joints = new Dictionary<Troop, Vector2[]>();
        foreach (Troop troop in group)
            joints[troop] = new[] { segments[troop].Endpoint(0), segments[troop].Endpoint(1) };

        foreach (Troop troop in group)
        for (int end = 0; end < 2; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null || link.Other == null || troop.GetInstanceID() >= link.Other.GetInstanceID()) continue;
            if (!segments.TryGetValue(link.Other, out var otherSegment)) continue;

            Vector2 localPoint = segments[troop].Endpoint(end);
            Vector2 otherPoint = otherSegment.Endpoint(link.OtherEnd);
            Vector2 tension = otherPoint - localPoint;
            troop.SetLineLinkTension(end, tension);
            link.Other.SetLineLinkTension(link.OtherEnd, -tension);
            Vector2 joint = (localPoint + otherPoint) * 0.5f;
            joints[troop][end] = joint;
            joints[link.Other][link.OtherEnd] = joint;
        }

        foreach (Troop troop in group)
        {
            if (!segments.TryGetValue(troop, out var segment)) continue;
            segment.Start = joints[troop][0];
            segment.End = joints[troop][1];
            troop.Formation = new TroopFormationData
            {
                Start = segment.Start,
                End = segment.End,
                Facing = segment.Facing,
                IsThreatFacing = segment.IsThreatFacing,
                StartLinked = troop.GetLineLink(0) != null,
                EndLinked = troop.GetLineLink(1) != null
            };
        }
    }
}
