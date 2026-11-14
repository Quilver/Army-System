using System.Collections.Generic;
using UnityEngine;

// Turns a right-button release into orders for the selected troops (spec §6.2, §6.3):
//   Pursue  click on an enemy troop: every selected troop pursues it (Assault applies on engagement).
//   Move    click on ground: the troops keep their relative offsets and translate to the release point; they arrive
//           Idle, facing the way they marched (SU-154).
//           A left-drag that starts on one of the player's troops is also a Move of the selection (FinishMove, SU-129).
//   Line    drag with one or more troops: a battle line along the drag, its front set by the drag's direction (SU-156).
//           The troops split the span in order along the drag; adjacent troops' centres stay between 0.5 and 2 x the
//           average of their maximum line lengths apart (SU-157). Adjacent troops whose line points meet (within the
//           attach distance) are linked by the order (Q-19); troops with a gap between them are not.
//   Face    Ctrl + click: every selected troop turns in place to face the point (from the selection's centre), and holds
//           its line there (a line order at its own position, SU-156).
// Chain rules for a troop linked to a troop that is not part of the order: inside the envelope (stretch <= 3.0 x L_nat,
// inner angle >= 65 degrees) the link stays; outside it, or with Alt held, the link is severed when the order is issued
// and the troop is ChainSuppressed. Alt on an unlinked troop suppresses auto-linking the same way: until it has been outside every attach radius for
// ChainSuppressCooldownSeconds (SU-130).
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
    readonly List<bool> _joinsNext = new();

    public TroopOrderPlanner(TroopSelection selection)
    {
        _selection = selection;
    }

    public IReadOnlyList<Placement> Placements => _placements;
    public IReadOnlyList<Tether> Tethers => _tethers;

    // Whether placement i of the last planned line meets placement i + 1 at a joint, so the order links them (SU-157).
    public bool JoinsNext(int index) => index >= 0 && index < _joinsNext.Count && _joinsNext[index];

    static TroopTuning Tuning => TroopTuning.Active;

    // --- Issuing ---

    public void FinishOrder(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, bool altHeld, bool faceHeld = false)
    {
        var selected = LiveSelection();
        if (selected.Count == 0) return;
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, releaseScreen);
        float pickRadius = TroopScreenSpace.PixelsToWorld(camera, releaseScreen, Tuning.ClickPickRadiusPixels);
        Troop enemy = _selection.TroopAtPoint(worldEnd, pickRadius, _selection.PlayerTeamId, sameTeam: false);

        switch (OrderClassifier.Classify(pressScreen, releaseScreen, Tuning.OrderDragMinPixels, enemy != null, faceHeld))
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
            case OrderKind.Face:
                PlanFace(selected, worldEnd);
                foreach (var placement in _placements)
                {
                    ApplyChainRules(placement.Troop, placement, selected, altHeld, forceUnlinkOutside: false);
                    placement.Troop.OrderLine(placement.Center, placement.Facing, placement.Length);
                }
                break;
        }
    }

    // Left-drag move (SU-129): the selection moves to the release point, keeping its relative offsets, whatever the
    // drag length. Chain rules apply as for a right-click Move, so a selected whole chain moves together.
    public void FinishMove(Vector2 releaseScreen, Camera camera, bool altHeld)
    {
        var selected = LiveSelection();
        if (selected.Count == 0) return;
        PlanMove(selected, TroopScreenSpace.ScreenToWorld(camera, releaseScreen));
        foreach (var placement in _placements)
        {
            ApplyChainRules(placement.Troop, placement, selected, altHeld, forceUnlinkOutside: false);
            placement.Troop.MoveTo(placement.Center);
        }
    }

    // Left-drag move while deploying: the selection is put straight at the release point and stands Idle.
    public void FinishPlacementMove(Vector2 releaseScreen, Camera camera, ITroopPlacement placement)
    {
        var selected = LiveSelection();
        if (selected.Count == 0 || placement == null) return;
        PlanMove(selected, TroopScreenSpace.ScreenToWorld(camera, releaseScreen));
        BreakLinksWithin(selected);
        foreach (var planned in _placements)
            placement.Place(planned.Troop, planned.Center, planned.Facing, planned.Length, false);
    }

    // Ghost preview of a left-drag move: the planned spot of each selected troop and its chain tethers.
    public void PreviewMove(Vector2 currentScreen, Camera camera, bool altHeld)
    {
        var selected = LiveSelection();
        PlanMove(selected, TroopScreenSpace.ScreenToWorld(camera, currentScreen));
        _tethers.Clear();
        foreach (var placement in _placements)
            CollectTethers(placement, selected, altHeld);
    }

    // Deployment: the same Move and Line plans as FinishOrder, but each troop is put straight at its planned spot.
    // Attack orders do nothing while deploying.
    public void FinishPlacement(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, ITroopPlacement placement,
        bool faceHeld = false)
    {
        var selected = LiveSelection();
        if (selected.Count == 0 || placement == null) return;
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, releaseScreen);
        OrderKind kind = OrderClassifier.Classify(pressScreen, releaseScreen, Tuning.OrderDragMinPixels, false, faceHeld);
        if (kind == OrderKind.Line) PlanLine(selected, worldStart, worldEnd);
        else if (kind == OrderKind.Face) PlanFace(selected, worldEnd);
        else PlanMove(selected, worldEnd);
        BreakLinksWithin(selected);
        // A line (one troop's right-drag included, SU-156) or a Face stands in its line; a move stands Idle, as it arrives
        // in battle.
        bool holdLine = kind == OrderKind.Line || kind == OrderKind.Face;
        foreach (var planned in _placements)
            placement.Place(planned.Troop, planned.Center, planned.Facing, planned.Length, holdLine);
    }

    // --- Planning (shared with the ghost preview) ---

    public OrderKind Preview(Vector2 pressScreen, Vector2 currentScreen, Camera camera, bool altHeld, bool faceHeld = false)
    {
        var selected = LiveSelection();
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, currentScreen);
        OrderKind kind = OrderClassifier.Classify(pressScreen, currentScreen, Tuning.OrderDragMinPixels, false, faceHeld);
        if (kind == OrderKind.Line) PlanLine(selected, worldStart, worldEnd);
        else if (kind == OrderKind.Face) PlanFace(selected, worldEnd);
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

    // The troops split the span in order along the drag (Q-18); each length is its share, clamped to [0.7, 3.0] x L_nat.
    // One troop takes the whole span (SU-156). The drag's direction sets the front (OrderClassifier.LineFacing).
    // Several troops (SU-157): adjacent centres are the share apart, clamped to [LineOrderMinSpacingScale,
    // LineOrderMaxSpacingScale] (0.5, 2) x the average of the two troops' maximum line lengths, so a short drag spreads
    // the line out past the release point and a long one stops short of it. The line starts at the press point.
    public void PlanLine(List<Troop> selected, Vector2 start, Vector2 end)
    {
        _placements.Clear();
        _joinsNext.Clear();
        if (selected.Count == 0) return;
        Vector2 drag = end - start;
        float span = drag.magnitude;
        Vector2 tangent = span > 1e-4f ? drag / span : Vector2.up;
        Vector2 facing = OrderClassifier.LineFacing(start, end);

        selected.Sort((a, b) =>
        {
            float aPosition = Vector2.Dot((Vector2)a.transform.position - start, tangent);
            float bPosition = Vector2.Dot((Vector2)b.transform.position - start, tangent);
            int alongLine = aPosition.CompareTo(bPosition);
            return alongLine != 0 ? alongLine : a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        float share = span / selected.Count;
        float along = selected.Count > 1 ? 0.5f * PairSpacing(share, selected[0], selected[1]) : 0.5f * share;
        for (int i = 0; i < selected.Count; i++)
        {
            Troop troop = selected[i];
            if (i > 0) along += PairSpacing(share, selected[i - 1], troop);
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = start + tangent * along,
                Facing = facing,
                Length = Mathf.Clamp(share, troop.MinimumLineLength, troop.MaximumLineLength)
            });
        }

        for (int i = 0; i + 1 < _placements.Count; i++)
            _joinsNext.Add(PlacementsMeet(_placements[i], _placements[i + 1], tangent));
    }

    static float PairSpacing(float share, Troop a, Troop b) =>
        AdjacentCentreSpacing(share, a.MaximumLineLength, b.MaximumLineLength,
            Tuning.LineOrderMinSpacingScale, Tuning.LineOrderMaxSpacingScale);

    // Centre-to-centre spacing of two adjacent troops in a multi-troop line (SU-157): their share of the drag, clamped to
    // [minScale, maxScale] x the average of their maximum line lengths.
    public static float AdjacentCentreSpacing(float share, float maxLengthA, float maxLengthB, float minScale, float maxScale)
    {
        float reference = (maxLengthA + maxLengthB) * 0.5f;
        float min = reference * minScale;
        return Mathf.Clamp(share, min, Mathf.Max(min, reference * maxScale));
    }

    // Two adjacent planned lines meet when their facing line points are within the attach distance (spec §3.4).
    static bool PlacementsMeet(Placement a, Placement b, Vector2 dragDirection)
    {
        AdjacentEnds(a, dragDirection, out int endA, out _);
        AdjacentEnds(b, dragDirection, out _, out int endB);
        float gap = Vector2.Distance(PlannedLinePoint(a, endA), PlannedLinePoint(b, endB));
        float attach = ChainLinkRules.AttachDistance(a.Troop.NaturalLineLength, b.Troop.NaturalLineLength, Tuning.AttachScale);
        return gap <= attach + 1e-3f;
    }

    // Face (SU-156): each troop stays where it is and turns to face the point, all of them the same way (from the
    // selection's centre), so a selected chain stays straight. Each keeps its current line length.
    public void PlanFace(List<Troop> selected, Vector2 point)
    {
        _placements.Clear();
        if (selected.Count == 0) return;
        Vector2 centroid = Vector2.zero;
        foreach (var troop in selected) centroid += (Vector2)troop.transform.position;
        centroid /= selected.Count;
        Vector2? toward = OrderClassifier.FaceToward(centroid, point);
        foreach (var troop in selected)
        {
            Vector2 facing = toward ?? troop.Facing;
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = troop.transform.position,
                Facing = facing,
                Length = troop.LineLength
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
            if (!JoinsNext(i)) continue;
            AdjacentEnds(_placements[i], dragDirection, out int endA, out _);
            AdjacentEnds(_placements[i + 1], dragDirection, out _, out int endB);
            ChainLinker.LinkFromOrder(_placements[i].Troop, endA, _placements[i + 1].Troop, endB);
        }
    }

    // A multi-troop line order rebuilds the chain among the selected troops from scratch; only troops whose planned lines
    // meet are linked again (SU-157).
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
}
