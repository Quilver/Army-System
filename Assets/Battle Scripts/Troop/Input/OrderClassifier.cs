using UnityEngine;

public enum OrderKind
{
    Move,
    Pursue,
    Line
}

// Classifies a right-button release (spec §6.2). Pure. The drag is measured in screen pixels, so the threshold does not
// depend on zoom (Q-47).
//   below OrderDragMinPixels on an enemy troop  -> Pursue
//   below OrderDragMinPixels on empty ground    -> Move
//   at or above OrderDragMinPixels              -> Line (battle line)
public static class OrderClassifier
{
    public static OrderKind Classify(Vector2 pressScreen, Vector2 releaseScreen, float orderDragMinPixels, bool enemyUnderCursor)
    {
        float drag = Vector2.Distance(pressScreen, releaseScreen);
        if (drag >= orderDragMinPixels) return OrderKind.Line;
        return enemyUnderCursor ? OrderKind.Pursue : OrderKind.Move;
    }

    // Box select starts at BoxSelectMinDragPixels (spec §6.1).
    public static bool IsBoxSelect(Vector2 pressScreen, Vector2 releaseScreen, float boxSelectMinDragPixels) =>
        Vector2.Distance(pressScreen, releaseScreen) >= boxSelectMinDragPixels;

    // Line facing (spec §6.2, Q-17): the normal of the drag on the side of the nearest enemy, or the right-hand normal of
    // the drag when there is no enemy. The player cannot choose the front.
    public static Vector2 LineFacing(Vector2 dragStart, Vector2 dragEnd, Vector2? nearestEnemy)
    {
        Vector2 tangent = dragEnd - dragStart;
        if (tangent.sqrMagnitude < 1e-8f) tangent = Vector2.up;
        tangent.Normalize();
        Vector2 rightNormal = new(tangent.y, -tangent.x);
        if (!nearestEnemy.HasValue) return rightNormal;
        Vector2 midpoint = (dragStart + dragEnd) * 0.5f;
        return Vector2.Dot(nearestEnemy.Value - midpoint, rightNormal) >= 0f ? rightNormal : -rightNormal;
    }
}
