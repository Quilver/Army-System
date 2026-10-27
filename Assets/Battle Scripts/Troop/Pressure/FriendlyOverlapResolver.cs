using System.Collections.Generic;
using UnityEngine;

// Pivots of friendly troops (same army) keep at least PivotSeparationScale x (r_A + r_B) apart (spec §2.4), which stops
// troops blobbing onto one point. A soft force cannot hold them apart against pursuit or line drive, so the overlap is
// resolved by moving the pivots directly along the line between them. Only pivots are checked: line points are never
// part of this, so endpoint attachment is never blocked. Linked pairs are not exempt (Q-15).
public static class FriendlyOverlapResolver
{
    const int FriendlyOverlapPasses = 3;

    public static float MinimumSpacing(Troop a, Troop b) =>
        TroopTuning.Active.PivotSeparationScale * (a.Radius + b.Radius);

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
                if (first.IsRouted || second.IsRouted) continue;

                float minimumDistance = MinimumSpacing(first, second);
                Vector2 offset = (Vector2)first.transform.position - (Vector2)second.transform.position;
                float distance = offset.magnitude;
                if (distance >= minimumDistance) continue;

                Vector2 direction = distance > 0.0001f
                    ? offset / distance
                    : (first.GetInstanceID() < second.GetInstanceID() ? Vector2.right : Vector2.left);
                float correction = minimumDistance - distance;
                // An engaged troop is not pushed; its friend gives way.
                float firstShare = first.IsEngaged == second.IsEngaged ? 0.5f : first.IsEngaged ? 0f : 1f;
                first.ShiftPivot(direction * (correction * firstShare));
                second.ShiftPivot(-direction * (correction * (1f - firstShare)));
                moved = true;
            }
            if (!moved) break;
        }
    }
}
