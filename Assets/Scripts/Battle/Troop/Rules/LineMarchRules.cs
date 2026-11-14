using UnityEngine;

// Marching and turning in battle line (SU-150, SU-155). Pure C#.
//   A troop in Line or Chain marches at LineMarchSpeedScale (0.8) of its pivot speed when Line March Slowdown is on.
//   Moving back, more than LineBackwardAngleDeg (90) from its facing, it marches at LineBackwardSpeedScale (0.6) when
//   Line Backward Slowdown is on (in place of the line scale, not on top of it).
//   Turning (SU-155): a line marching more than LineTurnAngleDeg (45) from its facing, because it is still wheeling to its
//   march or keeps an ordered front, marches at the same LineBackwardSpeedScale when Line Turn Slowdown is on.
//   Wheeling (SU-155): a line turns its facing toward a new heading at no more than LineWheelRateDeg (60) per second when
//   Line Wheel is on (Wheel), instead of turning at once.
//   Only the pivot's march is slowed: charges, followers and soldiers rushing back keep their own speed, and retreats,
//   routs, line pressure and chain tension are not marches.
public static class LineMarchRules
{
    public static bool IsInLine(FormationMode mode) => mode == FormationMode.Line || mode == FormationMode.Chain;

    // Whether direction points back from facing by more than backwardAngleDeg.
    public static bool IsBackward(Vector2 direction, Vector2 facing, float backwardAngleDeg) =>
        IsOffFacing(direction, facing, backwardAngleDeg);

    // Whether direction is more than angleDeg from facing (false when either is zero).
    public static bool IsOffFacing(Vector2 direction, Vector2 facing, float angleDeg)
    {
        if (direction.sqrMagnitude < 1e-10f || facing.sqrMagnitude < 1e-10f) return false;
        return Vector2.Angle(direction, facing) > angleDeg;
    }

    public static float SpeedScale(bool inLine, Vector2 direction, Vector2 facing, bool lineSlowdown, float lineScale,
        bool backwardSlowdown, float backwardScale, float backwardAngleDeg) =>
        SpeedScale(inLine, direction, facing, lineSlowdown, lineScale, backwardSlowdown, backwardScale, backwardAngleDeg,
            false, 180f);

    // As above, with the turning slowdown (SU-155): more than turnAngleDeg off the facing also marches at backwardScale.
    public static float SpeedScale(bool inLine, Vector2 direction, Vector2 facing, bool lineSlowdown, float lineScale,
        bool backwardSlowdown, float backwardScale, float backwardAngleDeg, bool turnSlowdown, float turnAngleDeg)
    {
        if (!inLine) return 1f;
        bool back = backwardSlowdown && IsOffFacing(direction, facing, backwardAngleDeg);
        bool turning = turnSlowdown && IsOffFacing(direction, facing, turnAngleDeg);
        if (back || turning) return Mathf.Clamp01(backwardScale);
        return lineSlowdown ? Mathf.Clamp01(lineScale) : 1f;
    }

    // Turns current toward target by at most maxDegrees (a wheel step). Returns a unit vector; target when either is
    // zero, or when it is within reach.
    public static Vector2 Wheel(Vector2 current, Vector2 target, float maxDegrees)
    {
        if (target.sqrMagnitude < 1e-10f) return current.sqrMagnitude < 1e-10f ? Vector2.right : current.normalized;
        target.Normalize();
        if (current.sqrMagnitude < 1e-10f) return target;
        current.Normalize();
        float angle = Vector2.SignedAngle(current, target);
        float step = Mathf.Max(0f, maxDegrees);
        if (Mathf.Abs(angle) <= step) return target;
        return ((Vector2)(Quaternion.AngleAxis(Mathf.Sign(angle) * step, Vector3.forward) * current)).normalized;
    }
}
