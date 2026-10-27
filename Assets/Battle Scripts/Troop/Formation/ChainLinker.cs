using System.Collections.Generic;
using UnityEngine;

// Chains of linked battle lines (spec §3.4, §3.4.1). Runs once per frame after every troop has built its natural line:
//   1. Drops links that are no longer valid, activates pending links whose troops have reached their places.
//   2. Solves each shared joint J (weighted average of the two natural line points, the engaged side pinned) and breaks
//      links that are over-stretched (> ChainMaxStretchScale x L_nat), over-bent (inner angle < 65 degrees), or
//      obstructed by impassable terrain. A broken line point relaxes back to its natural place at 2 units/s.
//   3. Attaches free line points: complementary wings, facing the same way, within R_AttachScale x L_nat, with a clear
//      line of sight, never closing a loop, never taking an already linked point, ties broken by distance then lower
//      instance id.
//   4. Publishes each troop's final line and its formation state (Idle, Column, Line, Chain, Rout).
// Links exist only between troops of the same team.
public static class ChainLinker
{
    const float MinimumSympatheticStep = 0.05f;
    const float JointObstacleRadius = 0.1f;

    static readonly ChainTopology Topology = new();
    static readonly List<ChainLinkCandidate> Candidates = new();
    static readonly Dictionary<int, Troop> ById = new();
    static readonly Dictionary<Troop, Vector2[]> Endpoints = new();
    static readonly Queue<(Troop troop, float distance)> RetreatQueue = new();
    static readonly HashSet<Troop> Visited = new();

    static TroopTuning Tuning => TroopTuning.Active;

    public static void Update(IReadOnlyList<Troop> troops, float deltaTime)
    {
        ById.Clear();
        foreach (var troop in troops)
            if (troop != null) ById[troop.GetInstanceID()] = troop;

        ValidateLinks(troops);
        SolveJoints(troops);
        AttachFreeEnds(troops);
        Publish(troops);
        UpdateSuppression(troops, deltaTime);
        ResolveModes(troops);
    }

    // --- Public API (orders, rout, retreat) ---

    // Creates a pending link from a multi-troop line order (spec §6.2, Q-19). Any link already on either line point is
    // replaced. Rejected if it would close a loop.
    public static bool LinkFromOrder(Troop a, int endA, Troop b, int endB)
    {
        if (a == null || b == null || a == b || a.TeamId != b.TeamId) return false;
        if (!ChainLinkRules.AreComplementary(endA, endB)) return false;
        BreakLink(a, endA, ChainBreakReason.PlayerOrder);
        BreakLink(b, endB, ChainBreakReason.PlayerOrder);
        RebuildTopology(TroopCombatWorld.Current != null ? TroopCombatWorld.Current.Troops : null);
        if (!Topology.CanLink(a.GetInstanceID(), b.GetInstanceID())) return false;
        a.LineLinkState.Set(endA, b, endB, pending: true);
        b.LineLinkState.Set(endB, a, endA, pending: true);
        return true;
    }

    public static void BreakAllLinks(Troop troop, ChainBreakReason reason)
    {
        if (troop == null) return;
        for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            BreakLink(troop, end, reason);
    }

    // Severs one link and starts the relaxation of both line points from the last joint.
    public static void BreakLink(Troop troop, int end, ChainBreakReason reason)
    {
        if (troop == null) return;
        TroopEndpointLink link = troop.GetLineLink(end);
        if (link == null) return;
        Troop other = link.Other;
        int otherEnd = link.OtherEnd;
        bool active = !link.Pending;
        Vector2 joint = link.Joint;
        troop.LineLinkState.Clear(end);
        if (active) StartRelax(troop, end, joint);
        if (other == null) return;
        TroopEndpointLink reciprocal = other.GetLineLink(otherEnd);
        if (reciprocal == null || reciprocal.Other != troop || reciprocal.OtherEnd != end) return;
        other.LineLinkState.Clear(otherEnd);
        if (active) StartRelax(other, otherEnd, joint);
    }

    // Shared retreat (spec §3.4): when a troop retreats by distance, each unengaged neighbour whose link would be
    // stretched past ChainRetreatStretchThreshold x L_nat or bent past ChainRetreatHingeBendDeg retreats too, passing on
    // (1 - falloff) of the distance per hop. Engaged neighbours hold (Q-20). It stops at broken links.
    public static void OnTroopRetreat(Troop source, Vector2 direction, float distance)
    {
        if (source == null || distance <= 0f) return;
        Visited.Clear();
        RetreatQueue.Clear();
        Visited.Add(source);
        RetreatQueue.Enqueue((source, distance));
        while (RetreatQueue.Count > 0)
        {
            var (troop, moved) = RetreatQueue.Dequeue();
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                if (!troop.IsLinkActive(end)) continue;
                TroopEndpointLink link = troop.GetLineLink(end);
                Troop neighbour = link.Other;
                if (neighbour == null || Visited.Contains(neighbour)) continue;
                Visited.Add(neighbour);

                float next = Mathf.Min(moved, ChainLinkRules.SympatheticDistance(moved, Tuning.ChainRetreatFalloff));
                if (next < MinimumSympatheticStep) continue;

                // The joint is pulled back with the retreating troop.
                Vector2 joint = link.Joint + direction.normalized * moved;
                Vector2 neighbourFar = neighbour.SegmentEndpoint(1 - link.OtherEnd);
                Vector2 troopFar = troop.SegmentEndpoint(1 - end) + direction.normalized * moved;
                float stretch = Vector2.Distance(neighbourFar, joint) / Mathf.Max(0.01f, neighbour.NaturalLineLength);
                float bend = ChainGeometry.HingeBend(joint, neighbourFar, troopFar);
                if (!ChainLinkRules.ShouldRetreatSympathetically(neighbour.IsEngaged, stretch, bend,
                        Tuning.ChainRetreatStretchThreshold, Tuning.ChainRetreatHingeBendDeg))
                    continue;
                if (neighbour.BeginSympatheticRetreat(direction, next))
                    RetreatQueue.Enqueue((neighbour, next));
            }
        }
    }

    // --- 1. Validation ---

    static void ValidateLinks(IReadOnlyList<Troop> troops)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                TroopEndpointLink link = troop.GetLineLink(end);
                if (link == null) continue;
                Troop other = link.Other;
                TroopEndpointLink reciprocal = other != null ? other.GetLineLink(link.OtherEnd) : null;
                bool valid = other != null
                    && ById.ContainsKey(other.GetInstanceID())
                    && other.TeamId == troop.TeamId
                    && reciprocal != null && reciprocal.Other == troop && reciprocal.OtherEnd == end
                    && !troop.IsRouted && !other.IsRouted;
                if (!valid)
                {
                    BreakLink(troop, end, troop.IsRouted || (other != null && other.IsRouted) ? ChainBreakReason.Rout : ChainBreakReason.Invalid);
                    continue;
                }

                if (link.Pending)
                {
                    // A pending link lasts while both troops keep the line order that created it.
                    if (!troop.HasManualLineOrder || !other.HasManualLineOrder)
                    {
                        BreakLink(troop, end, ChainBreakReason.PlayerOrder);
                        continue;
                    }
                    bool settled = !troop.IsMoving && !other.IsMoving
                        && troop.NaturalFormation != null && other.NaturalFormation != null;
                    if (settled)
                    {
                        link.Pending = false;
                        reciprocal.Pending = false;
                    }
                    continue;
                }

                // An active link needs both troops in Line (spec §3.1 Chain).
                if (troop.NaturalFormation == null || other.NaturalFormation == null)
                    BreakLink(troop, end, ChainBreakReason.Invalid);
            }
        }
    }

    // --- 2. Joints and breaks ---

    static void SolveJoints(IReadOnlyList<Troop> troops)
    {
        // Natural line points (with any relaxation offset) for every troop with a line.
        Endpoints.Clear();
        foreach (var troop in troops)
        {
            if (troop == null || troop.NaturalFormation == null) continue;
            TroopFormationData natural = troop.NaturalFormation;
            Endpoints[troop] = new[]
            {
                natural.Start + troop.LineLinkState.RelaxOffset(0),
                natural.End + troop.LineLinkState.RelaxOffset(1)
            };
        }

        // Joints from the natural points, the engaged side pinned (spec §3.4.1 pinned anchor).
        foreach (var troop in troops)
        {
            if (troop == null || !Endpoints.ContainsKey(troop)) continue;
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                if (!troop.IsLinkActive(end)) continue;
                TroopEndpointLink link = troop.GetLineLink(end);
                Troop other = link.Other;
                if (troop.GetInstanceID() > other.GetInstanceID() || !Endpoints.ContainsKey(other)) continue;
                Vector2 own = Endpoints[troop][end];
                Vector2 theirs = Endpoints[other][link.OtherEnd];
                float weight = troop.IsEngaged == other.IsEngaged ? 0.5f : troop.IsEngaged ? 1f : 0f;
                Vector2 joint = ChainGeometry.Joint(own, theirs, weight);
                link.Joint = joint;
                other.GetLineLink(link.OtherEnd).Joint = joint;
                link.Tension = theirs - own;
                other.GetLineLink(link.OtherEnd).Tension = own - theirs;
            }
        }

        // Break checks use each line's far end: its other joint when linked at both ends, otherwise its natural point.
        foreach (var troop in troops)
        {
            if (troop == null || !Endpoints.ContainsKey(troop)) continue;
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                if (!troop.IsLinkActive(end)) continue;
                TroopEndpointLink link = troop.GetLineLink(end);
                Troop other = link.Other;
                if (troop.GetInstanceID() > other.GetInstanceID() || !Endpoints.ContainsKey(other)) continue;

                Vector2 joint = link.Joint;
                Vector2 farA = FarEnd(troop, end);
                Vector2 farB = FarEnd(other, link.OtherEnd);
                float lengthA = ChainGeometry.RequiredLength(farA, joint);
                float lengthB = ChainGeometry.RequiredLength(farB, joint);
                float angle = ChainGeometry.InnerHingeAngle(joint, farA, farB);
                ChainBreakReason reason = ChainLinkRules.Evaluate(lengthA, troop.NaturalLineLength, lengthB,
                    other.NaturalLineLength, angle, Tuning.ChainMaxStretchScale, Tuning.ChainMinInnerAngleDeg);
                if (reason == ChainBreakReason.None && IsObstructed(troop, other, joint))
                    reason = ChainBreakReason.Obstruction;
                if (reason != ChainBreakReason.None)
                {
                    BreakLink(troop, end, reason);
                    continue;
                }

                // Keep each line at least ChainMinStretchScale x L_nat long.
                Vector2 clamped = ChainGeometry.ClampJoint(joint,
                    farA, troop.NaturalLineLength * Tuning.ChainMinStretchScale,
                    farB, other.NaturalLineLength * Tuning.ChainMinStretchScale);
                link.Joint = clamped;
                other.GetLineLink(link.OtherEnd).Joint = clamped;
            }
        }
    }

    static Vector2 FarEnd(Troop troop, int linkedEnd)
    {
        int farEnd = 1 - linkedEnd;
        if (troop.IsLinkActive(farEnd)) return troop.GetLineLink(farEnd).Joint;
        return Endpoints[troop][farEnd];
    }

    // Impassable terrain between the two pivots through the joint, or at the joint, severs the link (spec §3.4.1).
    static bool IsObstructed(Troop a, Troop b, Vector2 joint)
    {
        LayerMask mask = a.ObstacleMask;
        if (mask.value == 0) return false;
        Vector2 pivotA = a.transform.position;
        Vector2 pivotB = b.transform.position;
        return Physics2D.Linecast(pivotA, joint, mask).collider != null
            || Physics2D.Linecast(joint, pivotB, mask).collider != null
            || Physics2D.OverlapCircle(joint, JointObstacleRadius, mask) != null;
    }

    static void StartRelax(Troop troop, int end, Vector2 joint)
    {
        TroopFormationData natural = troop.NaturalFormation;
        if (natural == null) return;
        Vector2 naturalPoint = end == 0 ? natural.Start : natural.End;
        troop.LineLinkState.StartRelax(end, joint - naturalPoint);
    }

    // --- 3. Attach ---

    static void AttachFreeEnds(IReadOnlyList<Troop> troops)
    {
        RebuildTopology(troops);
        Candidates.Clear();
        for (int i = 0; i < troops.Count; i++)
        {
            Troop a = troops[i];
            if (!CanAutoLink(a)) continue;
            for (int j = i + 1; j < troops.Count; j++)
            {
                Troop b = troops[j];
                if (!CanAutoLink(b) || a.TeamId != b.TeamId) continue;
                float attach = ChainLinkRules.AttachDistance(a.NaturalLineLength, b.NaturalLineLength, Tuning.AttachScale);
                for (int endA = 0; endA < TroopConstants.MaxChainLinks; endA++)
                {
                    if (a.GetLineLink(endA) != null) continue;
                    int endB = 1 - endA;
                    if (b.GetLineLink(endB) != null) continue;
                    Vector2 pointA = Endpoints[a][endA];
                    Vector2 pointB = Endpoints[b][endB];
                    if (!ChainLinkRules.CanAttach(pointA, endA, a.NaturalFormation.Facing, pointB, endB,
                            b.NaturalFormation.Facing, attach))
                        continue;
                    Candidates.Add(new ChainLinkCandidate(a.GetInstanceID(), endA, b.GetInstanceID(), endB,
                        Vector2.Distance(pointA, pointB)));
                }
            }
        }

        Candidates.Sort();
        foreach (var candidate in Candidates)
        {
            Troop a = ById[candidate.TroopA];
            Troop b = ById[candidate.TroopB];
            if (a.GetLineLink(candidate.EndA) != null || b.GetLineLink(candidate.EndB) != null) continue;
            if (!Topology.CanLink(candidate.TroopA, candidate.TroopB)) continue;
            Vector2 midpoint = (Endpoints[a][candidate.EndA] + Endpoints[b][candidate.EndB]) * 0.5f;
            // Line of sight: never attach through impassable terrain (the same test that breaks a link).
            if (IsObstructed(a, b, midpoint)) continue;
            a.LineLinkState.Set(candidate.EndA, b, candidate.EndB, pending: false);
            b.LineLinkState.Set(candidate.EndB, a, candidate.EndA, pending: false);
            Topology.Union(candidate.TroopA, candidate.TroopB);
            Vector2 joint = (Endpoints[a][candidate.EndA] + Endpoints[b][candidate.EndB]) * 0.5f;
            a.GetLineLink(candidate.EndA).Joint = joint;
            b.GetLineLink(candidate.EndB).Joint = joint;
        }
    }

    static bool CanAutoLink(Troop troop) =>
        troop != null && !troop.IsRouted && !troop.ChainSuppressed && !troop.IsRetreating
        && troop.NaturalFormation != null && Endpoints.ContainsKey(troop) && troop.HasFreeLineEnd;

    static void RebuildTopology(IReadOnlyList<Troop> troops)
    {
        Topology.Clear();
        if (troops == null) return;
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                TroopEndpointLink link = troop.GetLineLink(end);
                if (link != null && link.Other != null) Topology.Union(troop.GetInstanceID(), link.Other.GetInstanceID());
            }
        }
    }

    // --- 4. Publish ---

    static void Publish(IReadOnlyList<Troop> troops)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            TroopFormationData natural = troop.NaturalFormation;
            if (natural == null || !Endpoints.TryGetValue(troop, out Vector2[] points))
            {
                troop.Formation = null;
                continue;
            }

            bool startLinked = troop.IsLinkActive(0);
            bool endLinked = troop.IsLinkActive(1);
            Vector2 start = startLinked ? troop.GetLineLink(0).Joint : points[0];
            Vector2 end = endLinked ? troop.GetLineLink(1).Joint : points[1];
            Vector2 line = end - start;
            Vector2 facing = natural.Facing;
            if (line.sqrMagnitude > 1e-6f)
            {
                Vector2 tangent = line.normalized;
                // Facing is the tangent turned clockwise, so end 1 stays on the left wing.
                facing = new Vector2(tangent.y, -tangent.x);
                if (Vector2.Dot(facing, natural.Facing) < 0f)
                {
                    // The bent line flipped past the troop's front: keep the troop's front and accept a folded line.
                    facing = natural.Facing;
                }
            }

            troop.Formation = new TroopFormationData
            {
                Start = start,
                End = end,
                Facing = facing,
                IsThreatFacing = natural.IsThreatFacing,
                StartLinked = startLinked,
                EndLinked = endLinked
            };
        }
    }

    // --- 5. ChainSuppressed (Q-22) ---

    // Suppression ends when the troop's order completes (Troop.OnOrderComplete) or once its line points have been outside
    // every attach radius for ChainSuppressCooldownSeconds.
    static void UpdateSuppression(IReadOnlyList<Troop> troops, float deltaTime)
    {
        foreach (var troop in troops)
        {
            if (troop == null || !troop.ChainSuppressed) continue;
            if (!troop.IsMoving)
            {
                troop.SetChainSuppressed(false);
                continue;
            }
            if (InsideAnyAttachRadius(troop, troops))
            {
                troop.ChainSuppressOutsideTime = 0f;
                continue;
            }
            troop.ChainSuppressOutsideTime += deltaTime;
            if (troop.ChainSuppressOutsideTime >= Tuning.ChainSuppressCooldownSeconds)
                troop.SetChainSuppressed(false);
        }
    }

    static bool InsideAnyAttachRadius(Troop troop, IReadOnlyList<Troop> troops)
    {
        if (!Endpoints.TryGetValue(troop, out Vector2[] own))
        {
            // Without a line, use the pivot against the other troops' line points.
            own = new[] { (Vector2)troop.transform.position, (Vector2)troop.transform.position };
        }
        foreach (var other in troops)
        {
            if (other == null || other == troop || other.TeamId != troop.TeamId) continue;
            if (!Endpoints.TryGetValue(other, out Vector2[] theirs)) continue;
            float attach = ChainLinkRules.AttachDistance(troop.NaturalLineLength, other.NaturalLineLength, Tuning.AttachScale);
            for (int a = 0; a < 2; a++)
            for (int b = 0; b < 2; b++)
                if (Vector2.Distance(own[a], theirs[b]) <= attach) return true;
        }
        return false;
    }

    // --- 6. Formation states (spec §3.1) ---

    static void ResolveModes(IReadOnlyList<Troop> troops)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            bool holdingLine = troop.HasManualLineOrder && !troop.IsMoving;
            troop.SetFormationMode(FormationModeResolver.Resolve(
                troop.IsRouted,
                troop.IsChained && troop.Formation != null,
                troop.HasThreatWithinSDistance,
                holdingLine,
                troop.IsMoving));
        }
    }
}
