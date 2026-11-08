using System;

// Battle line sizes (spec §3.3). Pure C#.
//   L_nat = Frontage x LineSpacing (SU-103): a troop of frontage 5 stands 5 soldiers wide. It does not depend on the
//   living soldier count (Q-12).
//   Soldiers per rank = floor(length / LineSpacing). Ranks = ceil(soldiers / per rank).
//   Troop radius (world units, used for the blob, the soldier clamp and spacing) = L_nat / NaturalLineRadiusScale, but at
//   least the radius of a blob that holds the troop's soldiers, times TroopSizeScale (1.5, SU-131) so they have room.
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
    // sizeScale (Formation Tuning TroopSizeScale, SU-131) multiplies the result.
    public static float TroopRadius(int frontage, int soldiers, float naturalLineRadiusScale, float blobRadiusScale,
        float sizeScale = 1f)
    {
        float fromFrontage = NaturalLength(frontage) / Math.Max(0.1f, naturalLineRadiusScale);
        float packedBlob = (float)Math.Sqrt(Math.Max(0, soldiers) * PackedAreaPerSoldier / Math.PI);
        return Math.Max(fromFrontage, packedBlob / Math.Max(0.1f, blobRadiusScale)) * Math.Max(0.1f, sizeScale);
    }

    public static int SoldiersPerRank(float length, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(1, (int)Math.Floor(length / Math.Max(0.0001f, spacing) + 0.0001f));

    public static int Ranks(int soldiers, float length, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(1, (int)Math.Ceiling((double)Math.Max(0, soldiers) / SoldiersPerRank(length, spacing)));

    // Half the depth of the soldier field around the centre line: rank spacing for every rank after the first.
    public static float RankHalfWidth(float length, int soldiers, float spacing = TroopConstants.LineSpacing) =>
        (Ranks(soldiers, length, spacing) - 1) * spacing * 0.5f + RankThickness;

    // --- Marching column (spec §3.2, SU-120) ---
    // A marching troop forms a column that is longer than it is wide at every size. Shared by the soldier field
    // (SoldierFieldPlanner) and the outline (TroopOutline), so the outline hugs the soldiers:
    //   lanes      = clamp(ceil(frontage / FrontagePerColumnLane), 1, MaxColumnLanes), less one while the column would
    //                have no more rows than lanes (a 4-soldier troop marches in single file, not 2 x 2)
    //   rows       = ceil(living / lanes)
    //   length     = max(LineSpacing, (rows - 1) x LineSpacing): the core, from the first row's soldiers to the last's
    //   half-width = (lanes - 1) x LineSpacing / 2 + RankThickness
    // The core starts ColumnTrailFraction x TroopRadius behind the pivot. The length no longer depends on the troop
    // radius (it was TroopRadius x 0.8, which held only 1-2 soldiers per lane in small troops), so a long column reaches
    // past the troop radius; SoldierMotionConstraints widens the soldier clamp to it while marching.
    public const int MaxColumnLanes = 3;
    public const int FrontagePerColumnLane = 2;
    public const float ColumnTrailFraction = 0.05f;

    public static int ColumnLanes(int frontage, int soldiers)
    {
        int living = Math.Max(1, soldiers);
        int lanes = Math.Min(MaxColumnLanes, CeilDiv(Math.Max(1, frontage), FrontagePerColumnLane));
        while (lanes > 1 && CeilDiv(living, lanes) <= lanes) lanes--;
        return Math.Max(1, lanes);
    }

    public static int ColumnRows(int frontage, int soldiers) =>
        CeilDiv(Math.Max(1, soldiers), ColumnLanes(frontage, soldiers));

    // Length of the column's core (soldier centre to soldier centre, front row to rear row).
    public static float ColumnLength(int frontage, int soldiers, float spacing = TroopConstants.LineSpacing) =>
        Math.Max(spacing, (ColumnRows(frontage, soldiers) - 1) * spacing);

    // Half the width of the column around its core: lane spacing for every lane after the first.
    public static float ColumnHalfWidth(int frontage, int soldiers, float spacing = TroopConstants.LineSpacing) =>
        (ColumnLanes(frontage, soldiers) - 1) * spacing * 0.5f + RankThickness;

    // How far behind the pivot the column's core starts.
    public static float ColumnTrail(float troopRadius) => Math.Max(0f, troopRadius) * ColumnTrailFraction;

    // Farthest a column soldier's centre stands from the pivot: the core's far end plus the half-width.
    public static float ColumnReach(float troopRadius, int frontage, int soldiers) =>
        ColumnTrail(troopRadius) + ColumnLength(frontage, soldiers) + ColumnHalfWidth(frontage, soldiers);

    static int CeilDiv(int value, int divisor) => (value + divisor - 1) / Math.Max(1, divisor);

    // Line length clamped to the chain stretch range [min, max] x L_nat (spec §3.4, §6.2).
    public static float ClampToStretch(float length, float naturalLength, float minScale, float maxScale) =>
        Math.Max(naturalLength * minScale, Math.Min(naturalLength * maxScale, length));

    // Clearance that keeps soldiers off a linked joint: min(clearance, fraction x length).
    public static float JointClearance(float length, float clearance, float fraction) =>
        Math.Min(clearance, Math.Max(0f, length) * fraction);
}
