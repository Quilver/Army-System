using System.Collections.Generic;

// Groups touching allied troops and runs the line-building steps on each group that has a threat-facing anchor.
public static class AlliedLineConnector
{
    public static void Connect(IReadOnlyList<Troop> troops)
    {
        LineLinkAssigner.Prune(troops);
        var remaining = new HashSet<Troop>(troops);
        while (remaining.Count > 0)
        {
            List<Troop> group = TakeConnectedGroup(remaining);
            if (!HasThreatFacingAnchor(group))
            {
                LineLinkAssigner.ClearGroupLinks(group);
                continue;
            }

            var segments = LineSegmentBuilder.Build(group);
            LineLinkAssigner.LinkAvailableEnds(group, segments);
            LineEndAimer.AimEndsAtMiddleTroops(group, segments);
            LineEndAimer.SnapLinkedEnds(group, segments);
        }
    }

    // Removes one connected allied group (touching or already linked) from remaining and returns it.
    static List<Troop> TakeConnectedGroup(HashSet<Troop> remaining)
    {
        Troop seed = null;
        foreach (var candidate in remaining)
        {
            seed = candidate;
            break;
        }
        remaining.Remove(seed);
        var group = new List<Troop> { seed };
        for (int i = 0; i < group.Count; i++)
        {
            Troop member = group[i];
            var candidates = new List<Troop>(remaining);
            foreach (var candidate in candidates)
            {
                if (candidate.TeamId != seed.TeamId) continue;
                if (TroopPivots.Distance(member, candidate) >= member.Radius + candidate.Radius
                    && !LineLinkAssigner.LinkedTo(member, candidate)) continue;
                remaining.Remove(candidate);
                group.Add(candidate);
            }
        }
        return group;
    }

    static bool HasThreatFacingAnchor(List<Troop> group)
    {
        foreach (var troop in group)
            if (troop.HasThreatFacingFormation) return true;
        return false;
    }
}
