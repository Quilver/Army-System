using UnityEngine;

// Player rules for moving a chained troop (spec §6.3). Pure.
//   Inside the envelope (stretch <= 3.0 x L_nat and inner angle >= 65 degrees) the troop stays chained.
//   Outside the envelope, or with Alt held, the link is severed when the order is issued (mouse release).
public static class ChainOrderRules
{
    // A link to a stationary neighbour, previewed with the ordered troop's line point at movedPoint and its far end at
    // movedFar. The joint is the midpoint of the two line points.
    public static bool StaysLinked(Vector2 movedPoint, Vector2 movedFar, float movedNaturalLength,
        Vector2 neighbourPoint, Vector2 neighbourFar, float neighbourNaturalLength,
        float maxStretchScale, float minInnerAngleDeg, bool altHeld)
    {
        if (altHeld) return false;
        Vector2 joint = (movedPoint + neighbourPoint) * 0.5f;
        return ChainGeometry.WithinEnvelope(joint, movedFar, movedNaturalLength, neighbourFar, neighbourNaturalLength,
            maxStretchScale, minInnerAngleDeg);
    }
}
