using System;
using UnityEngine;

// The gap between two footprints (spec §3.3, §4.1, Q-16): positive when apart, negative when they overlap. Used for
// S_Distance, A_Distance and line pressure, so every engagement rule measures the same thing. Pure maths.
public static class FootprintGap
{
    public static float Gap(in TroopFootprint a, in TroopFootprint b) =>
        BoxGap(a, b) - a.Rounding - b.Rounding;

    // Distance between the two inner boxes, or minus the smallest overlap along a separating-axis test when they overlap.
    static float BoxGap(in TroopFootprint a, in TroopFootprint b)
    {
        float largestSeparation = float.NegativeInfinity;
        largestSeparation = Math.Max(largestSeparation, Separation(a, b, a.Axis));
        largestSeparation = Math.Max(largestSeparation, Separation(a, b, a.Facing));
        largestSeparation = Math.Max(largestSeparation, Separation(a, b, b.Axis));
        largestSeparation = Math.Max(largestSeparation, Separation(a, b, b.Facing));
        if (largestSeparation <= 0f) return largestSeparation;

        // Separated convex boxes: the closest pair is a corner of one against an edge of the other.
        float best = float.PositiveInfinity;
        for (int i = 0; i < 4; i++)
        {
            best = Math.Min(best, PointToBox(a.Corner(i), b));
            best = Math.Min(best, PointToBox(b.Corner(i), a));
        }
        return best;
    }

    static float PointToBox(Vector2 point, in TroopFootprint box) =>
        Vector2.Distance(point, box.ClosestInnerPoint(point));

    // Gap between the boxes' projections on an axis (negative when the projections overlap).
    static float Separation(in TroopFootprint a, in TroopFootprint b, Vector2 axis)
    {
        Project(a, axis, out float minA, out float maxA);
        Project(b, axis, out float minB, out float maxB);
        return Math.Max(minB - maxA, minA - maxB);
    }

    static void Project(in TroopFootprint box, Vector2 axis, out float min, out float max)
    {
        float centre = Vector2.Dot(box.Center, axis);
        float extent = Math.Abs(Vector2.Dot(box.Axis, axis)) * box.HalfLength
            + Math.Abs(Vector2.Dot(box.Facing, axis)) * box.HalfWidth;
        min = centre - extent;
        max = centre + extent;
    }
}
