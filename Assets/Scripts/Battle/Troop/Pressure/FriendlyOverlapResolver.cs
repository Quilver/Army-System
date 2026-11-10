using System.Collections.Generic;
using UnityEngine;

// Friendly troops (same army) are kept apart in two ways:
//   Pivots      at least PivotSeparationScale x (r_A + r_B) apart (spec §2.4), which stops troops blobbing onto one point.
//               A soft force cannot hold them apart against pursuit or line drive, so the overlap is resolved by moving
//               the pivots directly along the line between them. Line points are never part of this, so endpoint
//               attachment is never blocked. Linked pairs are not exempt (Q-15).
//   Outlines    (SU-94) the troops' formation outlines (Troop.Outline: the shape the soldiers are steered into) keep a
//               clear FriendlyGap, so friends no longer stand in or march through each other. Troops linked in a chain
//               (or about to link from a line order) are exempt: their lines meet at the joints. The troop with right of
//               way stays put: engaged or attacking (SU-46) over marching or retreating, over standing. Equals share the
//               move. The troop giving way moves at FriendlyGiveWaySpeedScale x its march speed at most, so its soldiers
//               keep up and a marching troop slows while it passes a friend but is never held up for good. Nothing moves
//               while time stands still (paused or deploying).
public static class FriendlyOverlapResolver
{
    const int FriendlyOverlapPasses = 3;

    // Right of way between overlapping friends: the higher rank is not moved.
    public static int RightOfWay(Troop troop)
    {
        if (troop.IsCommitted) return 2;
        if (troop.IsMoving || troop.IsFaultSlipping) return 1;
        return 0;
    }

    // Whether the two troops are joined (or about to be joined) at a line point.
    public static bool AreLinked(Troop a, Troop b)
    {
        foreach (var link in a.LineLinks)
            if (link != null && link.Other == b) return true;
        foreach (var link in b.LineLinks)
            if (link != null && link.Other == a) return true;
        return false;
    }

    // Moves overlapping friendly outlines apart (see the class comment). One pass per frame: the move is rate-limited.
    public static void SeparateOutlines(IReadOnlyList<Troop> troops, float deltaTime)
    {
        if (deltaTime <= 0f) return;
        TroopTuning tuning = TroopTuning.Active;
        float wantedGap = tuning.FriendlyGap;
        float speedScale = tuning.FriendlyGiveWaySpeedScale;
        if (speedScale <= 0f) return;

        for (int i = 0; i < troops.Count; i++)
        for (int j = i + 1; j < troops.Count; j++)
        {
            Troop first = troops[i];
            Troop second = troops[j];
            if (first == null || second == null || first.TeamId != second.TeamId) continue;
            if (first.IsRouted || second.IsRouted) continue;
            if (first.LivingSoldierCount == 0 || second.LivingSoldierCount == 0) continue;
            if (AreLinked(first, second)) continue;

            TroopFootprint a = first.Outline;
            TroopFootprint b = second.Outline;
            float correction = wantedGap - FootprintGap.Gap(a, b);
            if (correction <= 0f) continue;

            Vector2 direction = FootprintGap.SeparationDirection(a, b);
            int firstRank = RightOfWay(first);
            int secondRank = RightOfWay(second);
            float firstShare = firstRank == secondRank ? 0.5f : firstRank > secondRank ? 0f : 1f;
            GiveWay(first, -direction, correction * firstShare, speedScale, deltaTime);
            GiveWay(second, direction, correction * (1f - firstShare), speedScale, deltaTime);
        }
    }

    static void GiveWay(Troop troop, Vector2 direction, float distance, float speedScale, float deltaTime)
    {
        if (distance <= 0f) return;
        float step = Mathf.Min(distance, troop.PivotSpeed * speedScale * deltaTime);
        if (step > 0f) troop.ShiftPivot(direction * step);
    }

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
                // An engaged or attacking troop is not pushed; its friend gives way (SU-46).
                bool firstCommitted = first.IsCommitted;
                bool secondCommitted = second.IsCommitted;
                float firstShare = firstCommitted == secondCommitted ? 0.5f : firstCommitted ? 0f : 1f;
                first.ShiftPivot(direction * (correction * firstShare));
                second.ShiftPivot(-direction * (correction * (1f - firstShare)));
                moved = true;
            }
            if (!moved) break;
        }
    }
}
