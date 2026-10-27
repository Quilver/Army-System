using System.Collections.Generic;
using UnityEngine;

// Turns a right-button release into orders for the selected troops (spec §6.2, §6.3):
//   Pursue  click on an enemy troop: every selected troop pursues it (Assault applies on engagement).
//   Move    click on ground: the troops keep their relative offsets and translate to the target; they arrive Idle.
//   Line    drag: a battle line along the drag. One troop takes the whole drag; several troops split the span in order
//           along the drag, adjacent line points meet at joints, and the order creates their links explicitly (Q-19).
// Chain rules for a troop linked to a troop that is not part of the order: inside the envelope (stretch <= 3.0 x L_nat,
// inner angle >= 65 degrees) the link stays; outside it, or with Alt held, the link is severed when the order is issued
// and the troop is ChainSuppressed. Alt on an unlinked troop suppresses auto-linking until its order completes (Q-23).
public sealed class TroopOrderPlanner
{
    public struct Placement
    {
        public Troop Troop;
        public Vector2 Center;
        public Vector2 Facing;
        public float Length;
    }

    // A link from an ordered troop to a troop outside the order, previewed at the order's destination.
    public struct Tether
    {
        public Vector2 From;      // the ordered troop's line point at its destination
        public Vector2 To;        // the neighbour's line point
        public bool StaysLinked;
    }

    readonly TroopSelection _selection;
    readonly List<Placement> _placements = new();
    readonly List<Tether> _tethers = new();

    public TroopOrderPlanner(TroopSelection selection)
    {
        _selection = selection;
    }

    public IReadOnlyList<Placement> Placements => _placements;
    public IReadOnlyList<Tether> Tethers => _tethers;

    static TroopTuning Tuning => TroopTuning.Active;

    // --- Issuing ---

    public void FinishOrder(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, bool altHeld)
    {
        var selected = LiveSelection();
        if (selected.Count == 0) return;
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, releaseScreen);
        float pickRadius = TroopScreenSpace.PixelsToWorld(camera, releaseScreen, Tuning.ClickPickRadiusPixels);
        Troop enemy = _selection.TroopAtPoint(worldEnd, pickRadius, _selection.PlayerTeamId, sameTeam: false);

        switch (OrderClassifier.Classify(pressScreen, releaseScreen, Tuning.OrderDragMinPixels, enemy != null))
        {
            case OrderKind.Pursue:
                foreach (Troop troop in selected)
                {
                    ApplyChainRules(troop, null, selected, altHeld, forceUnlinkOutside: true);
                    troop.Pursue(enemy);
                }
                break;
            case OrderKind.Move:
                PlanMove(selected, worldEnd);
                foreach (var placement in _placements)
                {
                    ApplyChainRules(placement.Troop, placement, selected, altHeld, forceUnlinkOutside: false);
                    placement.Troop.MoveTo(placement.Center);
                }
                break;
            case OrderKind.Line:
                PlanLine(selected, worldStart, worldEnd);
                if (_placements.Count > 1) BreakLinksWithin(selected);
                foreach (var placement in _placements)
                {
                    ApplyChainRules(placement.Troop, placement, selected, altHeld, forceUnlinkOutside: false);
                    placement.Troop.OrderLine(placement.Center, placement.Facing, placement.Length);
                }
                LinkAdjacent(worldEnd - worldStart);
                break;
        }
    }

    // --- Planning (shared with the ghost preview) ---

    public OrderKind Preview(Vector2 pressScreen, Vector2 currentScreen, Camera camera, bool altHeld)
    {
        var selected = LiveSelection();
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, currentScreen);
        OrderKind kind = OrderClassifier.Classify(pressScreen, currentScreen, Tuning.OrderDragMinPixels, false);
        if (kind == OrderKind.Line) PlanLine(selected, worldStart, worldEnd);
        else PlanMove(selected, worldEnd);

        _tethers.Clear();
        foreach (var placement in _placements)
            CollectTethers(placement, selected, altHeld);
        return kind;
    }

    // Keeps the troops' relative offsets around the selection's centre.
    public void PlanMove(List<Troop> selected, Vector2 destination)
    {
        _placements.Clear();
        if (selected.Count == 0) return;
        Vector2 centroid = Vector2.zero;
        foreach (var troop in selected) centroid += (Vector2)troop.transform.position;
        centroid /= selected.Count;
        foreach (var troop in selected)
        {
            Vector2 offset = (Vector2)troop.transform.position - centroid;
            Vector2 facing = troop.NaturalFormation != null ? troop.NaturalFormation.Facing : troop.Facing;
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = destination + offset,
                Facing = facing,
                Length = troop.LineLength
            });
        }
    }

    // One troop takes the whole drag (clamped to [MinimumLineLength, L_nat], spec §6.2). Several troops split the span
    // in order along the drag (Q-18); each length is its share, clamped to [0.7, 3.0] x L_nat.
    public void PlanLine(List<Troop> selected, Vector2 start, Vector2 end)
    {
        _placements.Clear();
        if (selected.Count == 0) return;
        Vector2 drag = end - start;
        float span = drag.magnitude;
        Vector2 tangent = span > 1e-4f ? drag / span : Vector2.up;
        Vector2 facing = OrderClassifier.LineFacing(start, end, NearestEnemyPosition((start + end) * 0.5f));

        selected.Sort((a, b) =>
        {
            float aPosition = Vector2.Dot((Vector2)a.transform.position - start, tangent);
            float bPosition = Vector2.Dot((Vector2)b.transform.position - start, tangent);
            int alongLine = aPosition.CompareTo(bPosition);
            return alongLine != 0 ? alongLine : a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        if (selected.Count == 1)
        {
            Troop troop = selected[0];
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = (start + end) * 0.5f,
                Facing = facing,
                Length = Mathf.Clamp(span, troop.MinimumLineLength, troop.NaturalLineLength)
            });
            return;
        }

        float share = span / selected.Count;
        for (int i = 0; i < selected.Count; i++)
        {
            Troop troop = selected[i];
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = start + tangent * (share * (i + 0.5f)),
                Facing = facing,
                Length = Mathf.Clamp(share, troop.MinimumLineLength, troop.MaximumLineLength)
            });
        }
    }

    // Line point of a planned line: end 1 sits on the left of the facing (TroopConstants).
    public static Vector2 PlannedLinePoint(Placement placement, int end)
    {
        Vector2 left = new(-placement.Facing.y, placement.Facing.x);
        float half = placement.Length * 0.5f;
        return placement.Center + left * (end == TroopConstants.LeftEnd ? half : -half);
    }

    // The line points of adjacent placements that meet at a joint, for a drag along dragDirection.
    public static void AdjacentEnds(Placement placement, Vector2 dragDirection, out int towardNext, out int towardPrevious)
    {
        Vector2 left = new(-placement.Facing.y, placement.Facing.x);
        towardNext = Vector2.Dot(left, dragDirection) >= 0f ? TroopConstants.LeftEnd : TroopConstants.RightEnd;
        towardPrevious = 1 - towardNext;
    }

    // --- Chain rules ---

    void LinkAdjacent(Vector2 dragDirection)
    {
        for (int i = 0; i + 1 < _placements.Count; i++)
        {
            AdjacentEnds(_placements[i], dragDirection, out int endA, out _);
            AdjacentEnds(_placements[i + 1], dragDirection, out _, out int endB);
            ChainLinker.LinkFromOrder(_placements[i].Troop, endA, _placements[i + 1].Troop, endB);
        }
    }

    // A multi-troop line order rebuilds the chain among the selected troops from scratch.
    static void BreakLinksWithin(List<Troop> selected)
    {
        foreach (var troop in selected)
            for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
            {
                TroopEndpointLink link = troop.GetLineLink(end);
                if (link != null && selected.Contains(link.Other)) ChainLinker.BreakLink(troop, end, ChainBreakReason.PlayerOrder);
            }
    }

    void ApplyChainRules(Troop troop, Placement? placement, List<Troop> selected, bool altHeld, bool forceUnlinkOutside)
    {
        bool linked = false;
        for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null || link.Other == null) continue;
            linked = true;
            // Links inside the selection move with it (multi-select chain move).
            if (selected.Contains(link.Other) && !altHeld) continue;
            bool stays = !forceUnlinkOutside && placement.HasValue
                && StaysLinked(troop, end, placement.Value, link, altHeld, out _, out _);
            if (stays) continue;
            ChainLinker.BreakLink(troop, end, ChainBreakReason.PlayerOrder);
            troop.SetChainSuppressed(true);
        }
        if (!linked && altHeld) troop.SetChainSuppressed(true);
    }

    void CollectTethers(Placement placement, List<Troop> selected, bool altHeld)
    {
        Troop troop = placement.Troop;
        for (int end = 0; end < TroopConstants.MaxChainLinks; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null || link.Other == null || selected.Contains(link.Other)) continue;
            bool stays = StaysLinked(troop, end, placement, link, altHeld, out Vector2 from, out Vector2 to);
            _tethers.Add(new Tether { From = from, To = to, StaysLinked = stays });
        }
    }

    // Envelope check of a link with the ordered troop's line moved to the placement (spec §6.3).
    static bool StaysLinked(Troop troop, int end, Placement placement, TroopEndpointLink link, bool altHeld,
        out Vector2 movedPoint, out Vector2 neighbourPoint)
    {
        movedPoint = PlannedLinePoint(placement, end);
        Vector2 movedFar = PlannedLinePoint(placement, 1 - end);
        Troop neighbour = link.Other;
        TroopFormationData theirs = neighbour.NaturalFormation ?? neighbour.Formation;
        if (theirs == null)
        {
            neighbourPoint = neighbour.transform.position;
            return false;
        }
        neighbourPoint = link.OtherEnd == 0 ? theirs.Start : theirs.End;
        Vector2 neighbourFar = link.OtherEnd == 0 ? theirs.End : theirs.Start;
        return ChainOrderRules.StaysLinked(movedPoint, movedFar, troop.NaturalLineLength,
            neighbourPoint, neighbourFar, neighbour.NaturalLineLength,
            Tuning.ChainMaxStretchScale, Tuning.ChainMinInnerAngleDeg, altHeld);
    }

    // --- Helpers ---

    List<Troop> LiveSelection()
    {
        var selected = _selection.Selected;
        selected.RemoveAll(troop => troop == null || troop.IsRouted);
        return selected;
    }

    Vector2? NearestEnemyPosition(Vector2 from)
    {
        Vector2? nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (var troop in _selection.Troops)
        {
            if (troop == null || troop.TeamId == _selection.PlayerTeamId || troop.IsRouted) continue;
            float distance = Vector2.SqrMagnitude((Vector2)troop.transform.position - from);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = troop.transform.position;
        }
        return nearest;
    }
}
