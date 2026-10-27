using UnityEngine;

// The surround test (spec §4.9, §5.3, Q-33). A troop is Surrounded when the path its retreat would take is blocked.
// The path is a box behind the rear face: as wide as the footprint and RetreatDistance long. Pure geometry; the terrain
// check is done by the caller with a physics query on the same box (see Troop.IsSurrounded).
public static class SurroundTest
{
    // Box behind the rear face of the footprint, facing the same way as the troop.
    public static TroopFootprint RetreatPath(in TroopFootprint footprint, float retreatDistance)
    {
        float halfLength = Mathf.Max(0f, retreatDistance) * 0.5f;
        Vector2 rear = footprint.Center - footprint.Facing * footprint.OuterHalfWidth;
        Vector2 centre = rear - footprint.Facing * halfLength;
        // The box's HalfWidth runs along the facing (the retreat direction) and its HalfLength across the troop.
        return new TroopFootprint(FootprintKind.Rect, centre, footprint.Facing, footprint.OuterHalfLength, halfLength, 0f);
    }

    // Blocked by an enemy footprint that overlaps the path. Friendly troops never block (Q-33).
    public static bool BlockedBy(in TroopFootprint path, in TroopFootprint enemy) => FootprintGap.Gap(path, enemy) < 0f;

    // Size and rotation of the path box for Physics2D.OverlapBox.
    public static Vector2 BoxSize(in TroopFootprint path) => new(path.HalfLength * 2f, path.HalfWidth * 2f);

    public static float BoxAngleDegrees(in TroopFootprint path) => Mathf.Atan2(path.Axis.y, path.Axis.x) * Mathf.Rad2Deg;
}
