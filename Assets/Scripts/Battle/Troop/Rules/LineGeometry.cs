using System;

// Battle line sizes (spec §3.3). Pure C#.
//   L_nat = NaturalLineRadiusScale x TroopRadius. It does not depend on the living soldier count (Q-12).
//   Soldiers per rank = floor(length / LineSpacing). Ranks = ceil(soldiers / per rank).
public static class LineGeometry
{
    // Half the thickness of a single rank. Soldiers stay in their rank but can spread along it.
    public const float RankThickness = TroopConstants.SoldierRadius * 0.5f;

    public static float NaturalLength(float troopRadius, float naturalLineRadiusScale) =>
        Math.Max(0f, troopRadius) * Math.Max(0f, naturalLineRadiusScale);

    public static int SoldiersPerRank(float length, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(1, (int)Math.Floor(length / Math.Max(0.0001f, spacing) + 0.0001f));

    public static int Ranks(int soldiers, float length, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(1, (int)Math.Ceiling((double)Math.Max(0, soldiers) / SoldiersPerRank(length, spacing)));

    // Half the depth of the soldier field around the centre line: rank spacing for every rank after the first.
    public static float RankHalfWidth(float length, int soldiers, float spacing = TroopConstants.LineSpacing) =>
        (Ranks(soldiers, length, spacing) - 1) * spacing * 0.5f + RankThickness;

    // Line length clamped to the chain stretch range [min, max] x L_nat (spec §3.4, §6.2).
    public static float ClampToStretch(float length, float naturalLength, float minScale, float maxScale) =>
        Math.Max(naturalLength * minScale, Math.Min(naturalLength * maxScale, length));

    // Clearance that keeps soldiers off a linked joint: min(clearance, fraction x length).
    public static float JointClearance(float length, float clearance, float fraction) =>
        Math.Min(clearance, Math.Max(0f, length) * fraction);
}
