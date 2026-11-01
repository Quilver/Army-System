using System.Collections.Generic;
using UnityEngine;

// The boundary of a footprint as a convex polygon in world space: the box of HalfLength x HalfWidth with its corners
// rounded to Rounding (a Line is the plain box, a Column a capsule, a Blob a circle). Anticlockwise, no repeated points.
// Pure maths, used to draw the troop outline (TroopVisualiser).
public static class TroopOutlinePolygon
{
    public const int DefaultCornerSegments = 8;
    const float MinEdge = 1e-4f;

    public static void Build(in TroopFootprint footprint, List<Vector2> points, int cornerSegments = DefaultCornerSegments)
    {
        points.Clear();
        Vector2 axis = footprint.Axis;
        Vector2 facing = footprint.Facing;
        int segments = footprint.Rounding > MinEdge ? Mathf.Max(1, cornerSegments) : 0;

        for (int corner = 0; corner < 4; corner++)
        {
            // Corners in the order of TroopFootprint.Corner: (+,+), (-,+), (-,-), (+,-) along Axis and Facing.
            float alongSign = corner == 0 || corner == 3 ? 1f : -1f;
            float acrossSign = corner < 2 ? 1f : -1f;
            Vector2 core = footprint.Center + axis * (alongSign * footprint.HalfLength) + facing * (acrossSign * footprint.HalfWidth);
            if (segments == 0)
            {
                Add(points, core);
                continue;
            }
            for (int i = 0; i <= segments; i++)
            {
                float angle = (corner + i / (float)segments) * Mathf.PI * 0.5f;
                Vector2 direction = axis * Mathf.Cos(angle) + facing * Mathf.Sin(angle);
                Add(points, core + direction * footprint.Rounding);
            }
        }

        if (points.Count > 1 && (points[0] - points[points.Count - 1]).sqrMagnitude < MinEdge * MinEdge)
            points.RemoveAt(points.Count - 1);
        if (SignedArea(points) < 0f) points.Reverse();
    }

    // Positive when the points run anticlockwise.
    public static float SignedArea(IReadOnlyList<Vector2> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    static void Add(List<Vector2> points, Vector2 point)
    {
        if (points.Count > 0 && (point - points[points.Count - 1]).sqrMagnitude < MinEdge * MinEdge) return;
        points.Add(point);
    }
}
