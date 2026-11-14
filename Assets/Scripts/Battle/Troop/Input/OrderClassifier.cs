using UnityEngine;

public enum OrderKind
{
    Move,
    Pursue,
    Line,
    Face
}

// Classifies a right-button release (spec §6.2). Pure. The drag is measured in screen pixels, so the threshold does not
// depend on zoom (Q-47).
//   below OrderDragMinPixels with Ctrl held           -> Face: turn in place toward the point (SU-156)
//   below OrderDragMinPixels on an enemy troop        -> Pursue
//   below OrderDragMinPixels on empty ground          -> Move
//   at or above OrderDragMinPixels                    -> Line: a battle line along the drag, for one troop or several. The
//                                                        drag's direction sets the front (SU-156).
public static class OrderClassifier
{
    public static OrderKind Classify(Vector2 pressScreen, Vector2 releaseScreen, float orderDragMinPixels,
        bool enemyUnderCursor, bool faceHeld = false)
    {
        float drag = Vector2.Distance(pressScreen, releaseScreen);
        if (drag >= orderDragMinPixels) return OrderKind.Line;
        if (faceHeld) return OrderKind.Face;
        return enemyUnderCursor ? OrderKind.Pursue : OrderKind.Move;
    }

    // Box select starts at BoxSelectMinDragPixels (spec §6.1).
    public static bool IsBoxSelect(Vector2 pressScreen, Vector2 releaseScreen, float boxSelectMinDragPixels) =>
        Vector2.Distance(pressScreen, releaseScreen) >= boxSelectMinDragPixels;

    // Line facing of a battle line (spec §6.2, SU-156): the player chooses the front by the drag's direction. The front is
    // the drag turned a quarter anticlockwise (its left-hand normal), so a drag from left to right faces up the screen and
    // a drag from right to left faces down it. Enemies play no part.
    public static Vector2 LineFacing(Vector2 dragStart, Vector2 dragEnd)
    {
        Vector2 tangent = dragEnd - dragStart;
        if (tangent.sqrMagnitude < 1e-8f) tangent = Vector2.right;
        tangent.Normalize();
        return new Vector2(-tangent.y, tangent.x);
    }

    // Facing of a Face order (SU-156): from the selection's centre toward the point (null when they coincide).
    public static Vector2? FaceToward(Vector2 selectionCentre, Vector2 point)
    {
        Vector2 direction = point - selectionCentre;
        return direction.sqrMagnitude > 1e-6f ? direction.normalized : (Vector2?)null;
    }
}
