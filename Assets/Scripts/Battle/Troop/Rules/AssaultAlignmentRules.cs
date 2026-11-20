using System.Collections.Generic;
using UnityEngine;

// The predicted Assault line (SU-183): where each soldier of a troop in line will stand once the troop has closed to the
// hold gap and finished its assault wheel square to the enemy. Pure maths; ChargeCoordinator sends the one wave from
// these places, so the soldiers' eligibility (front rank, an enemy within the charge reach) and their targets are judged
// from where they will be, not from where the approach left them.
//   Ranks: the soldiers in order of how far forward they stand along the troop's current facing, perRank to a rank, the
//   first rank in front. A soldier sagging or lagging a little behind its rank's line keeps its rank.
//   Along: each keeps its place along the line, clamped to the line's length.
//   Wheel: the line turns to the aligned heading about its leading end (AssaultWheelRules.LeadingEnd) when aboutEnd, else
//   about its middle, as TroopFormationState.AssaultWheel turns it.
//   Close: the turned line then steps forward along the aligned heading by closeBy (the gap still to close).
//   Place: the turned front rank's centre + along x its tangent, rank x rankSpacing behind it.
public static class AssaultAlignmentRules
{
    // pivot, heading, length and frontOffset describe the troop's line now (TroopFormationState.Build: the front rank's
    // centre is frontOffset ahead of the pivot). positions are the soldiers' world positions; predicted and ranks are
    // filled in the same order.
    public static void Predict(Vector2 pivot, Vector2 heading, float length, float frontOffset, Vector2 alignedHeading,
        bool aboutEnd, float closeBy, int perRank, float rankSpacing, IReadOnlyList<Vector2> positions,
        List<Vector2> predicted, List<int> ranks)
    {
        predicted.Clear();
        ranks.Clear();
        int count = positions.Count;
        if (count == 0) return;
        heading = heading.sqrMagnitude > 1e-8f ? heading.normalized : Vector2.right;
        alignedHeading = alignedHeading.sqrMagnitude > 1e-8f ? alignedHeading.normalized : heading;
        length = Mathf.Max(0f, length);
        frontOffset = Mathf.Max(0f, frontOffset);
        perRank = Mathf.Max(1, perRank);

        Vector2 tangent = new(-heading.y, heading.x);
        Vector2 center = pivot + heading * frontOffset;

        Vector2 alignedPivot = pivot;
        if (aboutEnd)
        {
            int side = AssaultWheelRules.LeadingEnd(pivot, heading, length, frontOffset, alignedHeading);
            alignedPivot += AssaultWheelRules.PivotStepKeepingEnd(heading, alignedHeading, length, frontOffset, side);
        }
        alignedPivot += alignedHeading * Mathf.Max(0f, closeBy);
        Vector2 alignedTangent = new(-alignedHeading.y, alignedHeading.x);
        Vector2 alignedCenter = alignedPivot + alignedHeading * frontOffset;

        var order = new int[count];
        var forward = new float[count];
        for (int i = 0; i < count; i++)
        {
            order[i] = i;
            forward[i] = Vector2.Dot(positions[i] - center, heading);
            predicted.Add(Vector2.zero);
            ranks.Add(0);
        }
        // Furthest forward first; ties keep their order, so the result does not depend on the sort.
        System.Array.Sort(order, (a, b) => forward[a] != forward[b] ? forward[b].CompareTo(forward[a]) : a.CompareTo(b));

        float half = length * 0.5f;
        for (int k = 0; k < count; k++)
        {
            int i = order[k];
            int rank = k / perRank;
            float along = Mathf.Clamp(Vector2.Dot(positions[i] - center, tangent), -half, half);
            ranks[i] = rank;
            predicted[i] = alignedCenter + alignedTangent * along - alignedHeading * (rank * rankSpacing);
        }
    }
}
