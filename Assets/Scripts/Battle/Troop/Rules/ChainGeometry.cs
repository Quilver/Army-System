using System;
using UnityEngine;

// Hinge geometry of two linked battle lines (spec §3.4). Pure maths.
// Each linked troop's line runs from its far end (the other line point, or its other joint) to the shared joint J.
//   Inner hinge angle: the angle at J between the two lines. A straight chain is 180 degrees.
//   Envelope: each line's length is at most ChainMaxStretchScale x L_nat and the inner angle is at least the minimum.
public static class ChainGeometry
{
    // Angle at the joint between the lines toward farA and farB, in degrees (0-180). Degenerate lines count as straight.
    public static float InnerHingeAngle(Vector2 joint, Vector2 farA, Vector2 farB)
    {
        Vector2 toA = farA - joint;
        Vector2 toB = farB - joint;
        if (toA.sqrMagnitude < 1e-8f || toB.sqrMagnitude < 1e-8f) return 180f;
        return Vector2.Angle(toA, toB);
    }

    // Bend away from straight (180 - inner angle).
    public static float HingeBend(Vector2 joint, Vector2 farA, Vector2 farB) => 180f - InnerHingeAngle(joint, farA, farB);

    // Shared joint: the weighted average of the two natural line points. weightA is A's share (0.5 = midpoint; 1 = A's point).
    public static Vector2 Joint(Vector2 naturalA, Vector2 naturalB, float weightA)
    {
        float w = Mathf.Clamp01(weightA);
        return naturalA * w + naturalB * (1f - w);
    }

    // Length a troop's line needs to reach the joint from its far end.
    public static float RequiredLength(Vector2 farEnd, Vector2 joint) => Vector2.Distance(farEnd, joint);

    // Moves the joint so neither line is shorter than minLength. Lines that are too long are not shortened here: that
    // is the stretch break (BreaksOnStretch).
    public static Vector2 ClampJoint(Vector2 joint, Vector2 farA, float minLengthA, Vector2 farB, float minLengthB)
    {
        joint = PushOut(joint, farA, minLengthA);
        joint = PushOut(joint, farB, minLengthB);
        return joint;
    }

    static Vector2 PushOut(Vector2 joint, Vector2 far, float minLength)
    {
        Vector2 offset = joint - far;
        float length = offset.magnitude;
        if (length >= minLength) return joint;
        Vector2 direction = length > 1e-5f ? offset / length : Vector2.up;
        return far + direction * minLength;
    }

    public static bool BreaksOnStretch(float requiredLength, float naturalLength, float maxStretchScale) =>
        requiredLength > naturalLength * maxStretchScale;

    public static bool BreaksOnHinge(float innerAngleDeg, float minInnerAngleDeg) => innerAngleDeg < minInnerAngleDeg;

    public static bool WithinEnvelope(Vector2 joint, Vector2 farA, float naturalLengthA, Vector2 farB, float naturalLengthB,
        float maxStretchScale, float minInnerAngleDeg)
    {
        if (BreaksOnStretch(RequiredLength(farA, joint), naturalLengthA, maxStretchScale)) return false;
        if (BreaksOnStretch(RequiredLength(farB, joint), naturalLengthB, maxStretchScale)) return false;
        return !BreaksOnHinge(InnerHingeAngle(joint, farA, farB), minInnerAngleDeg);
    }
}
