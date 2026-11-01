using System;
using UnityEngine;

// The four faces of a footprint (spec §3.3, §4.8).
public enum TroopFace
{
    Front,
    LeftFlank,
    RightFlank,
    Rear
}

public static class TroopFaceExtensions
{
    // Clashes against an enemy on a flank or the rear are flanked: 0 support and x3 Shock (spec §4.8).
    public static bool IsFlanked(this TroopFace face) => face != TroopFace.Front;
}

public enum FootprintKind
{
    Rect,     // Line and Chain troops: the thick line
    Circle,   // Idle (blob) and Rout troops
    Capsule   // Column troops
}

// A troop's footprint (spec §3.3): an oriented box around a core, with rounded corners. Pure maths, no Unity lifecycle.
//   Rect     box of HalfLength along the line and HalfWidth across it, no rounding.
//   Circle   a point with Rounding as its radius.
//   Capsule  a segment of HalfLength with Rounding as its radius.
// Axis runs along the line toward the left wing (end 1). Facing points out of the Front face.
public readonly struct TroopFootprint
{
    public readonly FootprintKind Kind;
    public readonly Vector2 Center;
    public readonly Vector2 Axis;
    public readonly Vector2 Facing;
    public readonly float HalfLength;
    public readonly float HalfWidth;
    public readonly float Rounding;

    public TroopFootprint(FootprintKind kind, Vector2 center, Vector2 facing, float halfLength, float halfWidth, float rounding)
    {
        Kind = kind;
        Center = center;
        Facing = facing.sqrMagnitude > 1e-10f ? facing.normalized : Vector2.right;
        Axis = new Vector2(-Facing.y, Facing.x);
        HalfLength = Math.Max(0f, halfLength);
        HalfWidth = Math.Max(0f, halfWidth);
        Rounding = Math.Max(0f, rounding);
    }

    // Thick line between two line points. The facing is made perpendicular to the line, on the side of facingHint.
    public static TroopFootprint Rect(Vector2 start, Vector2 end, Vector2 facingHint, float halfWidth)
    {
        Vector2 line = end - start;
        float length = line.magnitude;
        Vector2 facing = facingHint;
        if (length > 1e-5f)
        {
            Vector2 tangent = line / length;
            facing = new Vector2(tangent.y, -tangent.x);
            if (Vector2.Dot(facing, facingHint) < 0f) facing = -facing;
        }
        return new TroopFootprint(FootprintKind.Rect, (start + end) * 0.5f, facing, length * 0.5f, halfWidth, 0f);
    }

    public static TroopFootprint Circle(Vector2 center, float radius, Vector2 facing) =>
        new(FootprintKind.Circle, center, facing, 0f, 0f, radius);

    public static TroopFootprint Capsule(Vector2 start, Vector2 end, float radius, Vector2 facing)
    {
        Vector2 line = end - start;
        float length = line.magnitude;
        // The capsule's axis runs along its core; its facing is the side of facing nearest the core's normal.
        Vector2 normal = length > 1e-5f ? new Vector2(line.y, -line.x) / length : facing;
        if (Vector2.Dot(normal, facing) < 0f) normal = -normal;
        return new TroopFootprint(FootprintKind.Capsule, (start + end) * 0.5f, normal, length * 0.5f, 0f, radius);
    }

    // Overall extents including the rounding.
    public float OuterHalfLength => HalfLength + Rounding;
    public float OuterHalfWidth => HalfWidth + Rounding;

    // Line points (spec §3.4): end 0 is the right wing (-Axis), end 1 the left wing (+Axis).
    public Vector2 LinePoint(int end) => Center + Axis * (end == TroopConstants.LeftEnd ? HalfLength : -HalfLength);

    // Corner i of the inner box, anticlockwise starting at front-left.
    public Vector2 Corner(int index)
    {
        switch (index & 3)
        {
            case 0: return Center + Axis * HalfLength + Facing * HalfWidth;
            case 1: return Center - Axis * HalfLength + Facing * HalfWidth;
            case 2: return Center - Axis * HalfLength - Facing * HalfWidth;
            default: return Center + Axis * HalfLength - Facing * HalfWidth;
        }
    }

    // Outer corner i, including rounding, for drawing.
    public Vector2 OuterCorner(int index)
    {
        float l = OuterHalfLength;
        float w = OuterHalfWidth;
        switch (index & 3)
        {
            case 0: return Center + Axis * l + Facing * w;
            case 1: return Center - Axis * l + Facing * w;
            case 2: return Center - Axis * l - Facing * w;
            default: return Center + Axis * l - Facing * w;
        }
    }

    // Middle of a face's outer edge.
    public Vector2 FaceCenter(TroopFace face)
    {
        switch (face)
        {
            case TroopFace.Front: return Center + Facing * OuterHalfWidth;
            case TroopFace.Rear: return Center - Facing * OuterHalfWidth;
            case TroopFace.LeftFlank: return Center + Axis * OuterHalfLength;
            default: return Center - Axis * OuterHalfLength;
        }
    }

    // Nearest point of the inner box (the point itself when inside).
    public Vector2 ClosestInnerPoint(Vector2 point)
    {
        Vector2 offset = point - Center;
        float along = Mathf.Clamp(Vector2.Dot(offset, Axis), -HalfLength, HalfLength);
        float across = Mathf.Clamp(Vector2.Dot(offset, Facing), -HalfWidth, HalfWidth);
        return Center + Axis * along + Facing * across;
    }

    // Nearest point of the whole footprint (the point itself when inside).
    public Vector2 ClosestPoint(Vector2 point)
    {
        Vector2 inner = ClosestInnerPoint(point);
        Vector2 offset = point - inner;
        float distance = offset.magnitude;
        if (distance <= Rounding || distance < 1e-6f) return point;
        return inner + offset / distance * Rounding;
    }

    // Distance from the footprint's edge; negative inside (only the rounded part measures depth exactly).
    public float SignedDistance(Vector2 point)
    {
        Vector2 offset = point - Center;
        float along = Math.Abs(Vector2.Dot(offset, Axis)) - HalfLength;
        float across = Math.Abs(Vector2.Dot(offset, Facing)) - HalfWidth;
        float outside = new Vector2(Math.Max(along, 0f), Math.Max(across, 0f)).magnitude;
        float inside = Math.Min(Math.Max(along, across), 0f);
        return outside + inside - Rounding;
    }

    public bool Contains(Vector2 point) => SignedDistance(point) <= 0f;
}
