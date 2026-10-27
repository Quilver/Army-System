using System;
using UnityEngine;

// Why a chain link broke (spec §3.4 Breaking).
public enum ChainBreakReason
{
    None,
    Stretch,
    Hinge,
    Obstruction,
    Rout,
    PlayerOrder,
    Invalid
}

// One possible new link between two free line points. Sorted by distance, then by the lower troop instance id, so a
// race for the same line point resolves the same way every time (spec §3.4.1 tie-break).
public readonly struct ChainLinkCandidate : IComparable<ChainLinkCandidate>
{
    public readonly int TroopA;
    public readonly int EndA;
    public readonly int TroopB;
    public readonly int EndB;
    public readonly float Distance;

    public ChainLinkCandidate(int troopA, int endA, int troopB, int endB, float distance)
    {
        TroopA = troopA;
        EndA = endA;
        TroopB = troopB;
        EndB = endB;
        Distance = distance;
    }

    public int LowerId => Math.Min(TroopA, TroopB);
    public int HigherId => Math.Max(TroopA, TroopB);

    public int CompareTo(ChainLinkCandidate other)
    {
        int byDistance = Distance.CompareTo(other.Distance);
        if (byDistance != 0) return byDistance;
        int byLower = LowerId.CompareTo(other.LowerId);
        if (byLower != 0) return byLower;
        int byHigher = HigherId.CompareTo(other.HigherId);
        return byHigher != 0 ? byHigher : (EndA * 2 + EndB).CompareTo(other.EndA * 2 + other.EndB);
    }
}

// Attach and break rules for chain links (spec §3.4). Pure C#.
public static class ChainLinkRules
{
    // Complementary wings meet: one troop's right line point with the other's left line point.
    public static bool AreComplementary(int endA, int endB) => endA != endB;

    // Attach distance: R_AttachScale x L_nat (the two troops' L_nat averaged).
    public static float AttachDistance(float naturalLengthA, float naturalLengthB, float attachScale) =>
        attachScale * (naturalLengthA + naturalLengthB) * 0.5f;

    public static bool CanAttach(Vector2 pointA, int endA, Vector2 facingA, Vector2 pointB, int endB, Vector2 facingB,
        float attachDistance)
    {
        if (!AreComplementary(endA, endB)) return false;
        if (Vector2.Dot(facingA, facingB) <= 0f) return false;
        return Vector2.Distance(pointA, pointB) <= attachDistance;
    }

    public static ChainBreakReason Evaluate(float requiredLengthA, float naturalLengthA, float requiredLengthB,
        float naturalLengthB, float innerAngleDeg, float maxStretchScale, float minInnerAngleDeg)
    {
        if (ChainGeometry.BreaksOnStretch(requiredLengthA, naturalLengthA, maxStretchScale)
            || ChainGeometry.BreaksOnStretch(requiredLengthB, naturalLengthB, maxStretchScale))
            return ChainBreakReason.Stretch;
        if (ChainGeometry.BreaksOnHinge(innerAngleDeg, minInnerAngleDeg)) return ChainBreakReason.Hinge;
        return ChainBreakReason.None;
    }

    // Distance each hop of a shared retreat passes on (spec §3.4, Q-21): 10 -> 7 -> 4.9 with a 0.3 falloff.
    public static float SympatheticDistance(float distance, float falloff) => distance * (1f - Mathf.Clamp01(falloff));

    // An unengaged neighbour retreats with its chain when the retreat would over-stretch or over-bend its link.
    public static bool ShouldRetreatSympathetically(bool engaged, float stretchScale, float hingeBendDeg,
        float stretchThreshold, float bendThresholdDeg) =>
        !engaged && (stretchScale > stretchThreshold || hingeBendDeg > bendThresholdDeg);
}
