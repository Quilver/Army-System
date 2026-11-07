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

    // The direction to move b to separate it from a (unit length, pointing from a toward b). Apart inner boxes separate
    // along the line between their closest points; overlapping ones along the axis of least overlap.
    public static Vector2 SeparationDirection(in TroopFootprint a, in TroopFootprint b)
    {
        Vector2 between = b.Center - a.Center;
        Vector2 fallback = between.sqrMagnitude > 1e-10f ? between.normalized : a.Axis;

        float bestSeparation = float.NegativeInfinity;
        Vector2 bestAxis = fallback;
        ConsiderAxis(a, b, a.Axis, ref bestSeparation, ref bestAxis);
        ConsiderAxis(a, b, a.Facing, ref bestSeparation, ref bestAxis);
        ConsiderAxis(a, b, b.Axis, ref bestSeparation, ref bestAxis);
        ConsiderAxis(a, b, b.Facing, ref bestSeparation, ref bestAxis);

        if (bestSeparation > 0f)
        {
            // Apart: corner of one against the inner box of the other, as in BoxGap.
            float best = float.PositiveInfinity;
            Vector2 direction = fallback;
            for (int i = 0; i < 4; i++)
            {
                Vector2 cornerA = a.Corner(i);
                Vector2 onB = b.ClosestInnerPoint(cornerA);
                float distanceA = (onB - cornerA).sqrMagnitude;
                if (distanceA < best && distanceA > 1e-12f)
                {
                    best = distanceA;
                    direction = (onB - cornerA).normalized;
                }
                Vector2 cornerB = b.Corner(i);
                Vector2 onA = a.ClosestInnerPoint(cornerB);
                float distanceB = (cornerB - onA).sqrMagnitude;
                if (distanceB < best && distanceB > 1e-12f)
                {
                    best = distanceB;
                    direction = (cornerB - onA).normalized;
                }
            }
            return direction;
        }

        // Overlapping: the axis with the smallest overlap, turned to point from a toward b.
        float side = Vector2.Dot(between, bestAxis);
        return side < 0f ? -bestAxis : bestAxis;
    }

    static void ConsiderAxis(in TroopFootprint a, in TroopFootprint b, Vector2 axis, ref float bestSeparation,
        ref Vector2 bestAxis)
    {
        if (axis.sqrMagnitude < 1e-10f) return;
        float separation = Separation(a, b, axis);
        if (separation <= bestSeparation) return;
        bestSeparation = separation;
        bestAxis = axis;
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
