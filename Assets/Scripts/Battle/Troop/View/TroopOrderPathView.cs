using System.Collections.Generic;
using UnityEngine;

// A troop's current order, drawn over the battle in IMGUI for every selected troop (all the time) and for the troop
// under the cursor (any team): the path it is taking (Troop.CollectOrderPath,
// the navmesh waypoints still ahead) with chevrons marching along it, and an arrowhead at the waypoint.
//   Move    path in the move colour, a dot at the destination.
//   Line    path in the move colour, the ordered line's footprint at the destination.
//   Attack  path in the attack colour, ending at the pursued troop, whose footprint is outlined.
// A troop with no order (idle, holding a line, engaged without an order, retreating or routed) shows nothing.
// Owned and drawn by TroopPlayerOrders.
public sealed class TroopOrderPathView
{
    const float LinePixels = 2.5f;
    const float ShadowPixels = 2f;
    const float ChevronSpacingPixels = 44f;
    const float ChevronPixels = 7f;
    const float ChevronSpeedPixels = 30f;     // chevrons march toward the waypoint
    const float ArrowHeadPixels = 15f;
    const float ArrowHeadAngleDeg = 28f;
    const float WingAngleDeg = 40f;
    static readonly Color Shadow = new(0f, 0f, 0f, 0.45f);

    public struct Style
    {
        public Color Move;     // Move and Line orders
        public Color Attack;   // Pursue (attack) orders
    }

    readonly List<Vector2> _world = new();
    readonly List<Vector2> _gui = new();

    public void Draw(Camera camera, Troop troop, Style style)
    {
        if (camera == null || troop == null || Event.current == null || Event.current.type != EventType.Repaint) return;
        TroopOrderKind kind = troop.CollectOrderPath(_world);
        if (kind == TroopOrderKind.None || _world.Count < 2) return;

        Color colour = kind == TroopOrderKind.Attack ? style.Attack : style.Move;
        _gui.Clear();
        foreach (Vector2 point in _world) _gui.Add(TroopScreenSpace.WorldToGui(camera, point));

        if (kind == TroopOrderKind.Attack && troop.PursuitTarget != null)
            TroopOrderOverlay.DrawFootprint(camera, troop.PursuitTarget.Footprint, colour);
        else if (kind == TroopOrderKind.Line && troop.TryGetLineOrder(out Vector2 centre, out Vector2 facing, out float length))
            TroopOrderOverlay.DrawFootprint(camera, LineGhost(troop, centre, facing, length), colour);
        else
            TroopOrderOverlay.DrawMarker(_gui[_gui.Count - 1], colour);

        DrawArrowPath(_gui, colour);
    }

    static TroopFootprint LineGhost(Troop troop, Vector2 centre, Vector2 facing, float length)
    {
        Vector2 axis = new Vector2(-facing.y, facing.x) * (length * 0.5f);
        float halfWidth = LineGeometry.RankHalfWidth(length, troop.LivingSoldierCount) + TroopConstants.SoldierRadius;
        return TroopFootprint.Rect(centre - axis, centre + axis, facing, halfWidth);
    }

    // A polyline in GUI space with a shadow, chevrons along it and an arrowhead at its last point.
    static void DrawArrowPath(List<Vector2> gui, Color colour)
    {
        for (int i = 1; i < gui.Count; i++)
            TroopOrderOverlay.DrawGuiLine(gui[i - 1], gui[i], Shadow, LinePixels + ShadowPixels);
        for (int i = 1; i < gui.Count; i++)
            TroopOrderOverlay.DrawGuiLine(gui[i - 1], gui[i], colour, LinePixels);

        float total = 0f;
        for (int i = 1; i < gui.Count; i++) total += Vector2.Distance(gui[i - 1], gui[i]);
        if (total < 1f) return;

        // Chevrons stop short of the arrowhead.
        float offset = Mathf.Repeat(Time.unscaledTime * ChevronSpeedPixels, ChevronSpacingPixels);
        for (float distance = offset + ChevronSpacingPixels * 0.5f; distance < total - ArrowHeadPixels * 1.5f;
             distance += ChevronSpacingPixels)
        {
            PointAt(gui, distance, out Vector2 point, out Vector2 direction);
            DrawHead(point, direction, ChevronPixels, WingAngleDeg, colour, LinePixels);
        }

        PointAt(gui, total, out Vector2 tip, out Vector2 end);
        DrawHead(tip, end, ArrowHeadPixels + 1.5f, ArrowHeadAngleDeg, Shadow, LinePixels + 1f + ShadowPixels);
        DrawHead(tip, end, ArrowHeadPixels, ArrowHeadAngleDeg, colour, LinePixels + 1f);
    }

    // Two wings swept back from tip, against direction.
    static void DrawHead(Vector2 tip, Vector2 direction, float size, float angleDeg, Color colour, float thickness)
    {
        Vector2 back = -direction * size;
        TroopOrderOverlay.DrawGuiLine(tip, tip + Rotate(back, angleDeg), colour, thickness);
        TroopOrderOverlay.DrawGuiLine(tip, tip + Rotate(back, -angleDeg), colour, thickness);
    }

    // The point at a distance along the polyline and the direction of the segment it is on.
    static void PointAt(List<Vector2> points, float distance, out Vector2 point, out Vector2 direction)
    {
        direction = Vector2.right;
        point = points[points.Count - 1];
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 segment = points[i] - points[i - 1];
            float length = segment.magnitude;
            if (length < 1e-4f) continue;
            direction = segment / length;
            if (distance <= length)
            {
                point = points[i - 1] + direction * distance;
                return;
            }
            distance -= length;
        }
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
