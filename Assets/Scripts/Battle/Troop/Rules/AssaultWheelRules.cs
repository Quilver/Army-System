using UnityEngine;

// The assault wheel (SU-163): a troop with a Pursue order that is engaged with its target wheels until its front is
// parallel to the face of the target it attacks, turning about the end of its front rank nearest the enemy so the far
// end swings in and the frontage in contact grows. Pure maths; TroopAssaultWheel keeps the state and
// TroopFormationState turns the heading.
//   Face: chosen with FaceClassifier's boundaries (FrontFaceHalfAngleDeg 45, RearFaceAngleDeg 135) when the troops
//   engage, then kept until the attacker's bearing from the target is hysteresis past a boundary (KeepFace). Only the
//   wheel uses it; the rules' flank test still classifies every frame.
//   Engaged end: the end of the front rank furthest along the heading the troop wheels to (LeadingEnd), which is the
//   end nearest the face it squares to.
public static class AssaultWheelRules
{
    // The attacker's bearing from the target's pivot, in degrees from the target's facing (anticlockwise positive),
    // measured to the attacker's nearest footprint point as FaceClassifier does.
    public static float Bearing(in TroopFootprint target, Vector2 targetPivot, in TroopFootprint attacker)
    {
        Vector2 toAttacker = attacker.ClosestPoint(targetPivot) - targetPivot;
        if (toAttacker.sqrMagnitude < 1e-8f) return 0f;
        return Vector2.SignedAngle(target.Facing, toAttacker);
    }

    // The face of a bearing with FaceClassifier's boundaries.
    public static TroopFace FaceAt(float bearingDeg, float frontHalfAngleDeg, float rearAngleDeg)
    {
        float magnitude = Mathf.Abs(bearingDeg);
        if (magnitude <= frontHalfAngleDeg + 1e-3f) return TroopFace.Front;
        if (magnitude > rearAngleDeg + 1e-3f) return TroopFace.Rear;
        return bearingDeg > 0f ? TroopFace.LeftFlank : TroopFace.RightFlank;
    }

    // Keeps current while the bearing is within its sector widened by hysteresisDeg on each side; otherwise the face
    // the bearing is in.
    public static TroopFace KeepFace(TroopFace current, float bearingDeg, float frontHalfAngleDeg, float rearAngleDeg,
        float hysteresisDeg)
    {
        TroopFace raw = FaceAt(bearingDeg, frontHalfAngleDeg, rearAngleDeg);
        if (raw == current) return current;
        return WithinSector(current, bearingDeg, frontHalfAngleDeg, rearAngleDeg, Mathf.Max(0f, hysteresisDeg))
            ? current
            : raw;
    }

    static bool WithinSector(TroopFace face, float bearingDeg, float front, float rear, float hysteresis)
    {
        float magnitude = Mathf.Abs(bearingDeg);
        return face switch
        {
            TroopFace.Front => magnitude <= front + hysteresis,
            TroopFace.Rear => magnitude >= rear - hysteresis,
            TroopFace.LeftFlank => bearingDeg > 0f && magnitude >= front - hysteresis && magnitude <= rear + hysteresis,
            TroopFace.RightFlank => bearingDeg < 0f && magnitude >= front - hysteresis && magnitude <= rear + hysteresis,
            _ => false
        };
    }

    // The heading that puts the attacker's front parallel to a face of a line target: against its outward normal.
    public static Vector2 SquareTo(in TroopFootprint target, TroopFace face) => -FaceClassifier.OutwardNormal(target, face);

    // A front rank's end (SU-161): +1 is End (the left wing), -1 is Start (the right wing).
    public static Vector2 EndPoint(Vector2 pivot, Vector2 heading, float length, float frontOffset, int side)
    {
        Vector2 tangent = new(-heading.y, heading.x);
        return pivot + heading * Mathf.Max(0f, frontOffset) + tangent * (side * length * 0.5f);
    }

    // The end of the front rank (+1 End, -1 Start) further along toward: the one nearest the face being squared to.
    public static int LeadingEnd(Vector2 pivot, Vector2 heading, float length, float frontOffset, Vector2 toward)
    {
        float end = Vector2.Dot(EndPoint(pivot, heading, length, frontOffset, 1), toward);
        float start = Vector2.Dot(EndPoint(pivot, heading, length, frontOffset, -1), toward);
        return end >= start ? 1 : -1;
    }

    // The pivot step that keeps the given end of the front rank where it is while the heading turns from oldHeading to
    // newHeading.
    public static Vector2 PivotStepKeepingEnd(Vector2 oldHeading, Vector2 newHeading, float length, float frontOffset,
        int side) =>
        EndPoint(Vector2.zero, oldHeading, length, frontOffset, side)
        - EndPoint(Vector2.zero, newHeading, length, frontOffset, side);
}
