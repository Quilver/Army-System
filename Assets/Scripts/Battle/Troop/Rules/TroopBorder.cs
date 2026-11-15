using System.Collections.Generic;
using UnityEngine;

// The troop border (spec §6.1, SU-162): the faint shape drawn under a hovered or selected troop (TroopVisualiser) and the
// shape the player clicks to pick it (TroopSelection). Display and picking only: the rules use Troop.Footprint, and
// friendly separation the formation outline (TroopOutline).
//
// One shape for every formation: a rounded box (centre, frame angle, outer half sizes, corner rounding). A circle (Idle,
// Rout) is a box whose rounding is its half size, a capsule (Column) a box rounded on its short side, a line a box with
// small rounded corners. Changing formation eases the sizes, the frame and the rounding, so the shape never switches.
// The box looks the same turned by 90 degrees (with its sizes swapped), so the frame turns at most 45 degrees toward the
// formation's and its facing is not the troop's front.
//
// Each frame the target is the formation's shape (OutlinePadding on every side) fitted to the troop's soldiers:
//   - measured in the formation's frame, each side lies OutlinePadding beyond the outermost counted soldier centre, and
//     reaches on toward the formation's side by at most FormationReach (so a settled formation shows its own shape, and
//     the column a line is turning into is not shown before its soldiers get there);
//   - the rounding is the formation's, cut down where a soldier near a corner would otherwise be closer to the edge than
//     OutlinePadding.
//   Counted soldiers are the living ones not charging, following a charge or returning (as TroopFootprintBuilder).
// The border grows toward the target over GrowTime and shrinks over ShrinkTime, so it does not pulse. However it eases,
// it always holds every counted soldier centre at least one soldier radius inside its edge. With no time step (paused,
// deploying) it snaps. The shape is kept relative to the pivot, so a marching troop's border moves exactly with it.
public sealed class TroopBorder
{
    // Seconds for a side moving outward to close most of the gap to the target.
    public const float GrowTime = 0.1f;
    // Seconds for a side moving inward to close most of the gap to the target.
    public const float ShrinkTime = 0.5f;
    // Seconds for the frame to turn toward the formation's.
    public const float TurnTime = 0.2f;
    // Seconds for the corner rounding to follow the formation's.
    public const float RoundingTime = 0.25f;
    // How far beyond its soldiers (plus padding) a side may reach toward the formation's side.
    public const float FormationReach = TroopConstants.LineSpacing;
    // However the border eases, each counted soldier centre stays this far inside it (its sprite stays inside).
    public const float MinimumMargin = TroopConstants.SoldierRadius;

    // Counted soldier centres relative to the pivot, refilled each frame (no allocation once grown).
    readonly List<Vector2> _soldiers = new();

    bool _seeded;
    Vector2 _offset;      // centre relative to the pivot
    float _angle;         // degrees; the frame's facing
    float _halfAlong;     // outer half size along the frame's axis
    float _halfAcross;    // outer half size along the frame's facing
    float _rounding;

    public bool IsSeeded => _seeded;

    public void Reset() => _seeded = false;

    // Whether a soldier is held by the border (and measured for it).
    public static bool Holds(SoldierAgentState soldier) =>
        soldier != null && soldier.IsAlive && !soldier.IsCommitted && !soldier.IsReturning;

    // The border around a pivot position.
    public TroopFootprint At(Vector2 pivot)
    {
        float radians = _angle * Mathf.Deg2Rad;
        Vector2 facing = new(Mathf.Cos(radians), Mathf.Sin(radians));
        float r = Mathf.Max(0f, Mathf.Min(_rounding, Mathf.Min(_halfAlong, _halfAcross)));
        float coreAlong = Mathf.Max(0f, _halfAlong - r);
        float coreAcross = Mathf.Max(0f, _halfAcross - r);
        const float flat = 1e-3f;
        FootprintKind kind = coreAlong < flat && coreAcross < flat ? FootprintKind.Circle
            : coreAlong < flat || coreAcross < flat ? FootprintKind.Capsule
            : FootprintKind.Rect;
        return new TroopFootprint(kind, pivot + _offset, facing, coreAlong, coreAcross, r);
    }

    public void Update(Troop troop, TroopOutline outline, float deltaTime)
    {
        Vector2 pivot = troop.transform.position;
        _soldiers.Clear();
        IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
        for (int i = 0; i < soldiers.Count; i++)
            if (Holds(soldiers[i])) _soldiers.Add(troop.WorldPosition(soldiers[i]) - pivot);
        float padding = TroopTuning.Active.OutlinePadding;
        Step(Formation(troop, outline, pivot, padding), pivot, _soldiers, padding, deltaTime);
    }

    // The formation's shape this frame, padded, before fitting it to the soldiers.
    static TroopFootprint Formation(Troop troop, TroopOutline outline, Vector2 pivot, float padding)
    {
        TroopTuning tuning = TroopTuning.Active;
        int living = Mathf.Max(1, troop.LivingSoldierCount);
        switch (troop.FormationMode)
        {
            case FormationMode.Rout:
                return Blob(pivot, troop.Radius * tuning.RoutScatterRadiusScale, troop.Facing, padding);
            case FormationMode.Line:
            case FormationMode.Chain:
                TroopFormationData formation = troop.Formation;
                if (formation != null) return Line(formation.Start, formation.End, formation.Facing, living, padding);
                break;
            case FormationMode.Column:
                if (outline != null && outline.IsMarching)
                    return Column(pivot, outline.Heading, troop.Radius, troop.Frontage, living, padding);
                break;
        }
        return Blob(pivot, troop.Radius * tuning.BlobRadiusScale, troop.Facing, padding);
    }

    // --- Formation shapes (pure) ---

    // The battle line padded on every side: the line's ranks (RankThickness ahead of the line, the rank depth plus
    // RankThickness behind it, SU-161) with corners rounded to the padding. Also the ghost of a planned line.
    public static TroopFootprint Line(Vector2 start, Vector2 end, Vector2 facing, int livingSoldiers) =>
        Line(start, end, facing, livingSoldiers, TroopTuning.Active.OutlinePadding);

    public static TroopFootprint Line(Vector2 start, Vector2 end, Vector2 facing, int livingSoldiers, float padding)
    {
        float length = Vector2.Distance(start, end);
        TroopFootprint ranks = TroopFootprint.RectAcross(start, end, facing, LineGeometry.RankThickness,
            LineGeometry.RankDepth(length, livingSoldiers) + LineGeometry.RankThickness);
        return new TroopFootprint(FootprintKind.Rect, ranks.Center, ranks.Facing, ranks.HalfLength, ranks.HalfWidth,
            Mathf.Max(0f, padding));
    }

    // The marching column (TroopOutline.Column) with the padding around its lanes.
    public static TroopFootprint Column(Vector2 pivot, Vector2 heading, float troopRadius, int frontage, int livingSoldiers,
        float padding)
    {
        Vector2 forward = heading.sqrMagnitude > 1e-8f ? heading.normalized : Vector2.right;
        float length = LineGeometry.ColumnLength(frontage, livingSoldiers);
        Vector2 center = pivot - forward * (LineGeometry.ColumnTrail(troopRadius) + length * 0.5f);
        return new TroopFootprint(FootprintKind.Capsule, center, forward, 0f, length * 0.5f,
            LineGeometry.ColumnHalfWidth(frontage, livingSoldiers) + Mathf.Max(0f, padding));
    }

    public static TroopFootprint Blob(Vector2 center, float radius, Vector2 facing, float padding) =>
        TroopFootprint.Circle(center, Mathf.Max(0f, radius) + Mathf.Max(0f, padding), facing);

    // --- Fitting and easing ---

    // One frame: fits the formation to the soldiers (centres relative to the pivot) and eases toward it. The first call,
    // and any call with no time step, snaps.
    public TroopFootprint Step(in TroopFootprint formation, Vector2 pivot, List<Vector2> soldierOffsets, float padding,
        float deltaTime)
    {
        padding = Mathf.Max(0f, padding);
        Target(formation, pivot, soldierOffsets, padding, out float targetAngle, out Vector2 targetCenter,
            out float targetAlong, out float targetAcross, out float targetRounding);

        if (!_seeded || deltaTime <= 0f)
        {
            _angle = targetAngle;
            _offset = targetCenter;
            _halfAlong = targetAlong;
            _halfAcross = targetAcross;
            _rounding = targetRounding;
            _seeded = true;
            return At(pivot);
        }

        // Turn the frame toward the formation's, at most 45 degrees (the box is the same a quarter turn round).
        float delta = Mathf.DeltaAngle(_angle, targetAngle);
        float quarter = delta - 90f * Mathf.Round(delta / 90f);
        _angle = Mathf.Repeat(_angle + quarter * Blend(deltaTime, TurnTime) + 180f, 360f) - 180f;
        Frame(_angle, out Vector2 axis, out Vector2 facing);

        // The target as seen in the turned frame: the bounds of its rounded box.
        float relative = (targetAngle - _angle) * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(relative));
        float sin = Mathf.Abs(Mathf.Sin(relative));
        float coreAlong = Mathf.Max(0f, targetAlong - targetRounding);
        float coreAcross = Mathf.Max(0f, targetAcross - targetRounding);
        float seenAlong = cos * coreAlong + sin * coreAcross + targetRounding;
        float seenAcross = sin * coreAlong + cos * coreAcross + targetRounding;
        float targetMidAlong = Vector2.Dot(targetCenter, axis);
        float targetMidAcross = Vector2.Dot(targetCenter, facing);

        float midAlong = Vector2.Dot(_offset, axis);
        float midAcross = Vector2.Dot(_offset, facing);
        float grow = Blend(deltaTime, GrowTime);
        float shrink = Blend(deltaTime, ShrinkTime);
        float alongMax = EaseOut(midAlong + _halfAlong, targetMidAlong + seenAlong, grow, shrink);
        float alongMin = -EaseOut(-(midAlong - _halfAlong), -(targetMidAlong - seenAlong), grow, shrink);
        float acrossMax = EaseOut(midAcross + _halfAcross, targetMidAcross + seenAcross, grow, shrink);
        float acrossMin = -EaseOut(-(midAcross - _halfAcross), -(targetMidAcross - seenAcross), grow, shrink);
        float rounding = Mathf.Lerp(_rounding, targetRounding, Blend(deltaTime, RoundingTime));

        // Never let a soldier out, however the border eases.
        float margin = Mathf.Min(MinimumMargin, padding);
        Hold(soldierOffsets, axis, facing, margin, ref alongMin, ref alongMax, ref acrossMin, ref acrossMax);
        rounding = FitRounding(soldierOffsets, axis, facing, margin, alongMin, alongMax, acrossMin, acrossMax, rounding);

        _offset = axis * ((alongMin + alongMax) * 0.5f) + facing * ((acrossMin + acrossMax) * 0.5f);
        _halfAlong = Mathf.Max(0f, (alongMax - alongMin) * 0.5f);
        _halfAcross = Mathf.Max(0f, (acrossMax - acrossMin) * 0.5f);
        _rounding = rounding;
        return At(pivot);
    }

    // The formation fitted to the soldiers, in the formation's frame: its angle, centre (relative to the pivot), outer
    // half sizes and rounding.
    static void Target(in TroopFootprint formation, Vector2 pivot, List<Vector2> soldiers, float padding,
        out float angle, out Vector2 center, out float halfAlong, out float halfAcross, out float rounding)
    {
        Vector2 axis = formation.Axis;
        Vector2 facing = formation.Facing;
        angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        Vector2 middle = formation.Center - pivot;
        float midAlong = Vector2.Dot(middle, axis);
        float midAcross = Vector2.Dot(middle, facing);
        float alongMax = midAlong + formation.OuterHalfLength;
        float alongMin = midAlong - formation.OuterHalfLength;
        float acrossMax = midAcross + formation.OuterHalfWidth;
        float acrossMin = midAcross - formation.OuterHalfWidth;

        if (soldiers != null && soldiers.Count > 0)
        {
            Extents(soldiers, axis, facing, out float sAlongMin, out float sAlongMax, out float sAcrossMin,
                out float sAcrossMax);
            alongMax = Reach(sAlongMax + padding, alongMax);
            alongMin = -Reach(-(sAlongMin - padding), -alongMin);
            acrossMax = Reach(sAcrossMax + padding, acrossMax);
            acrossMin = -Reach(-(sAcrossMin - padding), -acrossMin);
        }

        center = axis * ((alongMin + alongMax) * 0.5f) + facing * ((acrossMin + acrossMax) * 0.5f);
        halfAlong = (alongMax - alongMin) * 0.5f;
        halfAcross = (acrossMax - acrossMin) * 0.5f;
        rounding = FitRounding(soldiers, axis, facing, padding, alongMin, alongMax, acrossMin, acrossMax,
            formation.Rounding);
    }

    // The soldiers' side, reaching on toward the formation's by at most FormationReach.
    static float Reach(float soldierSide, float formationSide) =>
        Mathf.Max(soldierSide, Mathf.Min(formationSide, soldierSide + FormationReach));

    // Pushes the sides out so every soldier centre is at least margin inside each of them.
    static void Hold(List<Vector2> soldiers, Vector2 axis, Vector2 facing, float margin, ref float alongMin,
        ref float alongMax, ref float acrossMin, ref float acrossMax)
    {
        if (soldiers == null || soldiers.Count == 0) return;
        Extents(soldiers, axis, facing, out float sAlongMin, out float sAlongMax, out float sAcrossMin, out float sAcrossMax);
        alongMax = Mathf.Max(alongMax, sAlongMax + margin);
        alongMin = Mathf.Min(alongMin, sAlongMin - margin);
        acrossMax = Mathf.Max(acrossMax, sAcrossMax + margin);
        acrossMin = Mathf.Min(acrossMin, sAcrossMin - margin);
    }

    // The rounding, no more than half the box's smaller size, and cut so that no soldier near a corner is closer than
    // margin to the edge. Every soldier must already be margin inside each side (see Hold).
    static float FitRounding(List<Vector2> soldiers, Vector2 axis, Vector2 facing, float margin, float alongMin,
        float alongMax, float acrossMin, float acrossMax, float rounding)
    {
        float half = Mathf.Min(alongMax - alongMin, acrossMax - acrossMin) * 0.5f;
        rounding = Mathf.Clamp(rounding, 0f, Mathf.Max(0f, half));
        if (soldiers == null) return rounding;
        for (int i = 0; i < soldiers.Count; i++)
        {
            float along = Vector2.Dot(soldiers[i], axis);
            float across = Vector2.Dot(soldiers[i], facing);
            // Distances to the nearest side in each direction.
            float x = Mathf.Min(alongMax - along, along - alongMin) - margin;
            float y = Mathf.Min(acrossMax - across, across - acrossMin) - margin;
            if (x < 0f || y < 0f) continue;
            // The largest rounding that keeps the point margin inside the rounded corner:
            // (r - x')^2 + (r - y')^2 <= (r - margin)^2 with x' = x + margin, y' = y + margin.
            float largest = x + y + margin + Mathf.Sqrt(2f * x * y);
            if (largest < rounding) rounding = largest;
        }
        return rounding;
    }

    static void Extents(List<Vector2> soldiers, Vector2 axis, Vector2 facing, out float alongMin, out float alongMax,
        out float acrossMin, out float acrossMax)
    {
        alongMin = acrossMin = float.PositiveInfinity;
        alongMax = acrossMax = float.NegativeInfinity;
        for (int i = 0; i < soldiers.Count; i++)
        {
            float along = Vector2.Dot(soldiers[i], axis);
            float across = Vector2.Dot(soldiers[i], facing);
            if (along < alongMin) alongMin = along;
            if (along > alongMax) alongMax = along;
            if (across < acrossMin) acrossMin = across;
            if (across > acrossMax) acrossMax = across;
        }
    }

    // Eases an outer side (larger is further out) toward its target: quickly outward, slowly inward.
    static float EaseOut(float current, float target, float grow, float shrink) =>
        current + (target - current) * (target > current ? grow : shrink);

    static float Blend(float deltaTime, float smoothTime) =>
        smoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / smoothTime);

    static void Frame(float angle, out Vector2 axis, out Vector2 facing)
    {
        float radians = angle * Mathf.Deg2Rad;
        facing = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        axis = new Vector2(-facing.y, facing.x);
    }
}
