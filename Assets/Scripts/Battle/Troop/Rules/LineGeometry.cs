using System;

// Battle line sizes (spec §3.3). Pure C#.
//   L_nat = Frontage x LineSpacing (SU-103): a troop of frontage 5 stands 5 soldiers wide. It does not depend on the
//   living soldier count (Q-12).
//   Soldiers per rank = floor(length / LineSpacing). Ranks = ceil(soldiers / per rank).
//   Troop radius (world units, used for the blob, the soldier clamp and spacing) = L_nat / NaturalLineRadiusScale, but at
//   least the radius of a blob that holds the troop's soldiers.
public static class LineGeometry
{
    // Half the thickness of a single rank. Soldiers stay in their rank but can spread along it.
    public const float RankThickness = TroopConstants.SoldierRadius * 0.5f;

    // Floor area one soldier takes in a packed blob: hexagonal packing at LineSpacing.
    public const float PackedAreaPerSoldier = 0.8660254f * TroopConstants.LineSpacing * TroopConstants.LineSpacing;

    public static float NaturalLength(int frontage, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(1, frontage) * spacing;

    // The troop's radius in world units (SU-103): L_nat / naturalLineRadiusScale (the old radius-to-line ratio, so a
    // frontage of 10 is the old radius 5), but never smaller than the radius whose blob (blobRadiusScale x radius) holds
    // the soldiers packed at LineSpacing.
    public static float TroopRadius(int frontage, int soldiers, float naturalLineRadiusScale, float blobRadiusScale)
    {
        float fromFrontage = NaturalLength(frontage) / Math.Max(0.1f, naturalLineRadiusScale);
        float packedBlob = (float)Math.Sqrt(Math.Max(0, soldiers) * PackedAreaPerSoldier / Math.PI);
        return Math.Max(fromFrontage, packedBlob / Math.Max(0.1f, blobRadiusScale));
    }

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
