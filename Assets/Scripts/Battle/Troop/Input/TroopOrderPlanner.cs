using System.Collections.Generic;
using UnityEngine;

// Turns a right-button release into orders for the selected troops (spec §6.2, §6.3):
//   Pursue  click on an enemy troop: every selected troop pursues it (Assault applies on engagement).
//   Move    click on ground: the troops keep their relative offsets and translate to the release point; they arrive
//           Idle, facing the way they marched (SU-154).
//           A left-drag that starts on one of the player's troops is also a Move of the selection (FinishMove, SU-129).
//   Pursue  also a release on an enemy troop that was under the press too (any drag), or a release on an enemy after a
//           drag shorter than AttackClickSlopPixels (SU-159, OrderClassifier).
//   Line    drag with one or more troops: a battle line along the drag, its front set by the drag's direction (SU-156).
//           Several troops (SU-160) stand end to end from the press point, in order along the drag, and the order links
//           every adjacent pair (Q-19). One stretch for the whole line: clamp(drag / sum of L_nat, 0.7, 2.0), each
//           troop's length that stretch x its own L_nat, so they share the drag in proportion to their frontage. A
//           drag shorter than a quarter of the minimum total width keeps the selection's facing (no spinning ghost).
//   Face    Ctrl + click: every selected troop turns in place to face the point (from the selection's centre), and holds
//           its line there (a line order at its own position, SU-156).
// Chain rules for a troop linked to a troop that is not part of the order: inside the envelope (stretch <= 2.0 x L_nat,
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
    Vector2 _lineDirection = Vector2.right;

    public TroopOrderPlanner(TroopSelection selection)
    {
        _selection = selection;
    }

    public IReadOnlyList<Placement> Placements => _placements;
    public IReadOnlyList<Tether> Tethers => _tethers;

    // Whether placement i of the last planned line meets placement i + 1 at a joint, so the order links them. In a
    // multi-troop line every adjacent pair does (SU-160).
    public bool JoinsNext(int index) => index >= 0 && index < _joinsNext.Count && _joinsNext[index];

    // The direction the last planned line runs in, from the press point (the drag's direction, or across the selection's
    // facing inside the facing dead zone, SU-160). AdjacentEnds takes it.
    public Vector2 LineDirection => _lineDirection;

    // The enemy troop the last Preview would attack on release (SU-159), or null.
    public Troop PreviewTarget { get; private set; }

    static TroopTuning Tuning => TroopTuning.Active;

    // --- Issuing ---

    public void FinishOrder(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, bool altHeld, bool faceHeld = false)
    {
        var selected = LiveSelection();
        if (selected.Count == 0) return;
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, releaseScreen);

        switch (ClassifyRelease(pressScreen, releaseScreen, camera, faceHeld, out Troop enemy))
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
                LinkAdjacent();
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

    // Deployment: the same Move and Line plans as FinishOrder, but each troop is put straight at its planned spot, and a
    // multi-troop line is linked as in battle (SU-160). Attack orders do nothing while deploying.
    public void FinishPlacement(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, ITroopPlacement placement,
        bool faceHeld = false)
    {
        var selected = LiveSelection();
        if (selected.Count == 0 || placement == null) return;
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, releaseScreen);
        // Attack orders do nothing while deploying, so the enemy troops play no part here.
        OrderKind kind = OrderClassifier.Classify(pressScreen, releaseScreen, Tuning.OrderDragMinPixels,
            Tuning.AttackClickSlopPixels, false, false, faceHeld);
        if (kind == OrderKind.Line) PlanLine(selected, worldStart, worldEnd);
        else if (kind == OrderKind.Face) PlanFace(selected, worldEnd);
        else PlanMove(selected, worldEnd);
        BreakLinksWithin(selected);
        // A line (one troop's right-drag included, SU-156) or a Face stands in its line; a move stands Idle, as it arrives
        // in battle.
        bool holdLine = kind == OrderKind.Line || kind == OrderKind.Face;
        foreach (var planned in _placements)
            placement.Place(planned.Troop, planned.Center, planned.Facing, planned.Length, holdLine);
        // A multi-troop line is linked end to end, as in battle (SU-160).
        if (kind == OrderKind.Line && _placements.Count > 1) LinkAdjacent();
    }

    // Classifies a right-button release in battle (SU-159): the enemy troop under the release point (within
    // ClickPickRadiusPixels, as TroopSelection.TroopAtPoint picks it) and whether the same troop was under the press.
    OrderKind ClassifyRelease(Vector2 pressScreen, Vector2 releaseScreen, Camera camera, bool faceHeld, out Troop enemy)
    {
        enemy = EnemyAt(releaseScreen, camera);
        bool sameUnderPress = enemy != null && EnemyAt(pressScreen, camera) == enemy;
        return OrderClassifier.Classify(pressScreen, releaseScreen, Tuning.OrderDragMinPixels, Tuning.AttackClickSlopPixels,
            enemy != null, sameUnderPress, faceHeld);
    }

    Troop EnemyAt(Vector2 screen, Camera camera)
    {
        float pickRadius = TroopScreenSpace.PixelsToWorld(camera, screen, Tuning.ClickPickRadiusPixels);
        return _selection.TroopAtPoint(TroopScreenSpace.ScreenToWorld(camera, screen), pickRadius,
            _selection.PlayerTeamId, sameTeam: false);
    }

    // --- Planning (shared with the ghost preview) ---

    // The ghost shows what a release here would do (SU-159): a Pursue (PreviewTarget set, no placements), or the planned
    // Move, Line or Face. While deploying (deploying = true) attack orders do nothing, so enemies play no part, as in
    // FinishPlacement.
    public OrderKind Preview(Vector2 pressScreen, Vector2 currentScreen, Camera camera, bool altHeld, bool faceHeld = false,
        bool deploying = false)
    {
        var selected = LiveSelection();
        Vector2 worldStart = TroopScreenSpace.ScreenToWorld(camera, pressScreen);
        Vector2 worldEnd = TroopScreenSpace.ScreenToWorld(camera, currentScreen);
        Troop enemy = null;
        OrderKind kind = deploying
            ? OrderClassifier.Classify(pressScreen, currentScreen, Tuning.OrderDragMinPixels, Tuning.AttackClickSlopPixels,
                false, false, faceHeld)
            : ClassifyRelease(pressScreen, currentScreen, camera, faceHeld, out enemy);
        PreviewTarget = kind == OrderKind.Pursue ? enemy : null;
        if (kind == OrderKind.Pursue)
        {
            _placements.Clear();
            _tethers.Clear();
            return kind;
        }
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

    // One troop (SU-156): its line spans the drag, centred on the drag's midpoint, clamped to [0.7, 2.0] x L_nat; the drag's
    // direction sets the front (OrderClassifier.LineFacing).
    // Several troops (SU-160): they stand end to end from the press point, in order along the drag (Q-18), and every
    // adjacent pair is linked. One stretch for the whole line, LineStretch(drag, sum of L_nat), and each troop's length is
    // that stretch x its own L_nat, so a short drag lays the minimum line out past the cursor and a long one stops short
    // of it. Inside the facing dead zone (InFacingDeadZone) the selection keeps its facing and the line runs across it
    // from the press point, on the cursor's side.
    public void PlanLine(List<Troop> selected, Vector2 start, Vector2 end)
    {
        _placements.Clear();
        _joinsNext.Clear();
        if (selected.Count == 0) return;
        Vector2 drag = end - start;
        float span = drag.magnitude;
        Vector2 tangent = span > 1e-4f ? drag / span : Vector2.right;
        Vector2 facing = OrderClassifier.LineFacing(start, end);

        if (selected.Count == 1)
        {
            Troop troop = selected[0];
            _lineDirection = tangent;
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = start + tangent * (0.5f * span),
                Facing = facing,
                Length = Mathf.Clamp(span, troop.MinimumLineLength, troop.MaximumLineLength)
            });
            return;
        }

        float naturalTotal = 0f;
        foreach (var troop in selected) naturalTotal += troop.NaturalLineLength;
        if (InFacingDeadZone(span, naturalTotal, Tuning.ChainMinStretchScale, Tuning.LineOrderFacingDeadZone))
        {
            facing = SelectionFacing(selected);
            tangent = DeadZoneDirection(facing, drag);
        }
        _lineDirection = tangent;

        Vector2 sortTangent = tangent;
        selected.Sort((a, b) =>
        {
            float aPosition = Vector2.Dot((Vector2)a.transform.position - start, sortTangent);
            float bPosition = Vector2.Dot((Vector2)b.transform.position - start, sortTangent);
            int alongLine = aPosition.CompareTo(bPosition);
            return alongLine != 0 ? alongLine : a.GetInstanceID().CompareTo(b.GetInstanceID());
        });

        float stretch = LineStretch(span, naturalTotal, Tuning.ChainMinStretchScale, Tuning.ChainMaxStretchScale);
        float along = 0f;
        foreach (var troop in selected)
        {
            float length = stretch * troop.NaturalLineLength;
            _placements.Add(new Placement
            {
                Troop = troop,
                Center = start + tangent * (along + 0.5f * length),
                Facing = facing,
                Length = length
            });
            along += length;
        }

        for (int i = 0; i + 1 < _placements.Count; i++) _joinsNext.Add(true);
    }

    // Stretch of a multi-troop line (SU-160): the drag over the troops' total natural length, clamped to
    // [minScale, maxScale] (ChainMinStretchScale, ChainMaxStretchScale).
    public static float LineStretch(float dragLength, float naturalTotal, float minScale, float maxScale)
    {
        if (naturalTotal <= 1e-6f) return minScale;
        return Mathf.Clamp(dragLength / naturalTotal, minScale, Mathf.Max(minScale, maxScale));
    }

    // Facing dead zone of a multi-troop line (SU-160): until the drag reaches deadZone (a quarter) of the line's minimum
    // total width, its direction does not set the front.
    public static bool InFacingDeadZone(float dragLength, float naturalTotal, float minScale, float deadZone) =>
        dragLength < naturalTotal * minScale * deadZone;

    // The line's direction inside the dead zone: across the facing (the facing is its left-hand normal, as for a drag),
    // flipped to the side the cursor is on.
    public static Vector2 DeadZoneDirection(Vector2 facing, Vector2 drag)
    {
        Vector2 across = new(facing.y, -facing.x);
        return Vector2.Dot(across, drag) < 0f ? -across : across;
    }

    // The selection's current facing: the mean of the troops' facings, or the first troop's when they cancel out.
    static Vector2 SelectionFacing(List<Troop> selected)
    {
        Vector2 sum = Vector2.zero;
        foreach (var troop in selected) sum += CurrentFacing(troop);
        if (sum.sqrMagnitude > 1e-6f) return sum.normalized;
        Vector2 first = CurrentFacing(selected[0]);
        return first.sqrMagnitude > 1e-6f ? first.normalized : Vector2.up;
    }

    static Vector2 CurrentFacing(Troop troop)
    {
        Vector2 facing = troop.NaturalFormation != null ? troop.NaturalFormation.Facing : troop.Facing;
        return facing.sqrMagnitude > 1e-6f ? facing.normalized : Vector2.zero;
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
                // The line's front rank, so the troop turns about its pivot and does not step (SU-161).
                Center = troop.LineCenterAhead(troop.transform.position, facing, troop.LineLength),
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

    void LinkAdjacent()
    {
        for (int i = 0; i + 1 < _placements.Count; i++)
        {
            if (!JoinsNext(i)) continue;
            AdjacentEnds(_placements[i], _lineDirection, out int endA, out _);
            AdjacentEnds(_placements[i + 1], _lineDirection, out _, out int endB);
            ChainLinker.LinkFromOrder(_placements[i].Troop, endA, _placements[i + 1].Troop, endB);
        }
    }

    // A multi-troop line order rebuilds the chain among the selected troops from scratch and links every adjacent pair
    // again (SU-160).
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

    readonly List<Troop> _ordered = new();

    // The selected troops that take orders now (Troop.AcceptsOrders). Routed troops leave the selection; a retreating,
    // disordered or dazed troop stays selected but has no part in the order, its ghost or its chain rules (SU-182).
    List<Troop> LiveSelection()
    {
        var selected = _selection.Selected;
        selected.RemoveAll(troop => troop == null || troop.IsRouted);
        _ordered.Clear();
        foreach (var troop in selected)
            if (troop.AcceptsOrders) _ordered.Add(troop);
        return _ordered;
    }
}
