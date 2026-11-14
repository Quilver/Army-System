using UnityEngine;

// Marching in battle line (SU-150). Pure C#.
//   A troop in Line or Chain marches at LineMarchSpeedScale (0.8) of its pivot speed when Line March Slowdown is on.
//   Moving back, more than LineBackwardAngleDeg (90) from its facing, it marches at LineBackwardSpeedScale (0.6) when
//   Line Backward Slowdown is on (in place of the line scale, not on top of it).
//   Only the pivot's march is slowed: charges, followers and soldiers rushing back keep their own speed, and retreats,
//   routs, line pressure and chain tension are not marches.
public static class LineMarchRules
{
    public static bool IsInLine(FormationMode mode) => mode == FormationMode.Line || mode == FormationMode.Chain;

    // Whether direction points back from facing by more than backwardAngleDeg.
    public static bool IsBackward(Vector2 direction, Vector2 facing, float backwardAngleDeg)
    {
        if (direction.sqrMagnitude < 1e-10f || facing.sqrMagnitude < 1e-10f) return false;
        return Vector2.Angle(direction, facing) > backwardAngleDeg;
    }

    public static float SpeedScale(bool inLine, Vector2 direction, Vector2 facing, bool lineSlowdown, float lineScale,
        bool backwardSlowdown, float backwardScale, float backwardAngleDeg)
    {
        if (!inLine) return 1f;
        if (backwardSlowdown && IsBackward(direction, facing, backwardAngleDeg)) return Mathf.Clamp01(backwardScale);
        return lineSlowdown ? Mathf.Clamp01(lineScale) : 1f;
    }
}
