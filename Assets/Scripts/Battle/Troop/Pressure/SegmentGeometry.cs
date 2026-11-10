using UnityEngine;

// Pure 2D segment maths used by the line pressure rules.
public static class SegmentGeometry
{
    public static void ClosestSegmentPoints(TroopFormationData a, TroopFormationData b, out Vector2 pointA, out Vector2 pointB, out float distance)
    {
        Vector2 r = a.End - a.Start;
        Vector2 s = b.End - b.Start;
        Vector2 q = b.Start - a.Start;
        float denominator = Cross(r, s);
        if (Mathf.Abs(denominator) > 0.000001f)
        {
            float t = Cross(q, s) / denominator;
            float u = Cross(q, r) / denominator;
            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                pointA = a.Start + r * t;
                pointB = pointA;
                distance = 0f;
                return;
            }
        }

        pointA = a.Start;
        pointB = ClosestPoint(a.Start, b.Start, b.End);
        distance = Vector2.Distance(pointA, pointB);
        TryPair(a.End, ClosestPoint(a.End, b.Start, b.End), ref pointA, ref pointB, ref distance);
        TryPair(ClosestPoint(b.Start, a.Start, a.End), b.Start, ref pointA, ref pointB, ref distance);
        TryPair(ClosestPoint(b.End, a.Start, a.End), b.End, ref pointA, ref pointB, ref distance);
    }

    public static Vector2 ClosestPoint(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
        return start + segment * t;
    }

    public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    static void TryPair(Vector2 candidateA, Vector2 candidateB, ref Vector2 bestA, ref Vector2 bestB, ref float bestDistance)
    {
        float candidateDistance = Vector2.Distance(candidateA, candidateB);
        if (candidateDistance >= bestDistance) return;
        bestA = candidateA;
        bestB = candidateB;
        bestDistance = candidateDistance;
    }
}
