using System.Collections.Generic;
using UnityEngine;

// Allied troops may close up to link lines and to surround an enemy from different sides, but their centres may not
// stack. A soft force cannot hold them apart against pursuit/line drive, so overlap is resolved by moving the pivots
// directly. Only the separation along the line between the two centres is corrected, so flanking is not obstructed.
public static class FriendlyOverlapResolver
{
    // Minimum distance between allied troop centres, in units of the (average) troop radius. Shared with line orders.
    public const float MinimumFriendlySpacingRadii = 0.5f;
    const int FriendlyOverlapPasses = 3;

    public static void Resolve(IReadOnlyList<Troop> troops)
    {
        for (int pass = 0; pass < FriendlyOverlapPasses; pass++)
        {
            bool moved = false;
            for (int i = 0; i < troops.Count; i++)
            for (int j = i + 1; j < troops.Count; j++)
            {
                Troop first = troops[i];
                Troop second = troops[j];
                if (first == null || second == null || first.TeamId != second.TeamId) continue;

                float minimumDistance = (first.Radius + second.Radius) * 0.5f * MinimumFriendlySpacingRadii;
                Vector2 offset = (Vector2)first.transform.position - (Vector2)second.transform.position;
                float distance = offset.magnitude;
                if (distance >= minimumDistance) continue;

                Vector2 direction = distance > 0.0001f
                    ? offset / distance
                    : (first.GetInstanceID() < second.GetInstanceID() ? Vector2.right : Vector2.left);
                Vector2 correction = direction * ((minimumDistance - distance) * 0.5f);
                first.ShiftPivot(correction);
                second.ShiftPivot(-correction);
                moved = true;
            }
            if (!moved) break;
        }
    }
}
