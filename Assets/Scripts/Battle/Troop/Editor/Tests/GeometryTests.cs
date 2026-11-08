using NUnit.Framework;
using UnityEngine;

// Spec §3.1, §3.3, §4.8: footprints, footprint gap, faces, formation priority and line geometry.
public sealed class FootprintGapTests
{
    static TroopFootprint Line(Vector2 center, Vector2 facing, float length, float halfWidth)
    {
        Vector2 left = new(-facing.y, facing.x);
        return TroopFootprint.Rect(center - left * length * 0.5f, center + left * length * 0.5f, facing, halfWidth);
    }

    [Test]
    public void FacingLines_OneUnitApart_GapIsOne()
    {
        TroopFootprint a = Line(Vector2.zero, Vector2.right, 10f, 1f);
        TroopFootprint b = Line(new Vector2(3f, 0f), Vector2.left, 10f, 1f);
        Assert.AreEqual(1f, FootprintGap.Gap(a, b), 1e-4f);
    }

    // SU-94: the way to move one footprint off another.
    [Test]
    public void SeparationDirection_OverlappingLines_AlongTheLeastOverlap()
    {
        TroopFootprint a = Line(Vector2.zero, Vector2.up, 10f, 1f);                  // x in [-5, 5], y in [-1, 1]
        TroopFootprint b = Line(new Vector2(1f, 1.5f), Vector2.up, 10f, 1f);         // overlaps 0.5 in y, 9 in x
        Vector2 direction = FootprintGap.SeparationDirection(a, b);
        Assert.AreEqual(0f, direction.x, 1e-4f);
        Assert.AreEqual(1f, direction.y, 1e-4f, "b moves up, off a");
        Assert.AreEqual(-1f, FootprintGap.SeparationDirection(b, a).y, 1e-4f, "and a moves down, off b");
    }

    [Test]
    public void SeparationDirection_Circles_AlongTheCentres()
    {
        TroopFootprint a = TroopFootprint.Circle(Vector2.zero, 2f, Vector2.right);
        TroopFootprint b = TroopFootprint.Circle(new Vector2(3f, 4f), 4f, Vector2.right);
        Vector2 direction = FootprintGap.SeparationDirection(a, b);
        Assert.AreEqual(0.6f, direction.x, 1e-4f);
        Assert.AreEqual(0.8f, direction.y, 1e-4f);
    }

    [Test]
    public void Overlapping_GapIsNegative()
    {
        TroopFootprint a = Line(Vector2.zero, Vector2.right, 10f, 1f);
        TroopFootprint b = Line(new Vector2(1.5f, 0f), Vector2.left, 10f, 1f);
        Assert.AreEqual(-0.5f, FootprintGap.Gap(a, b), 1e-4f);
    }

    [Test]
    public void CircleToRect_MeasuresToTheEdge()
    {
        TroopFootprint line = Line(Vector2.zero, Vector2.right, 10f, 1f);
        TroopFootprint blob = TroopFootprint.Circle(new Vector2(5f, 0f), 2f, Vector2.left);
        Assert.AreEqual(2f, FootprintGap.Gap(line, blob), 1e-4f);
    }

    [Test]
    public void CornerToCorner_UsesEuclideanDistance()
    {
        TroopFootprint a = Line(Vector2.zero, Vector2.right, 2f, 1f);           // x in [-1, 1], y in [-1, 1]
        TroopFootprint b = Line(new Vector2(5f, 5f), Vector2.right, 2f, 1f);    // x in [4, 6], y in [4, 6]
        Assert.AreEqual(Mathf.Sqrt(18f), FootprintGap.Gap(a, b), 1e-3f);
    }

    [Test]
    public void LinePoints_EndOneIsTheLeftWing()
    {
        TroopFootprint line = Line(Vector2.zero, Vector2.right, 10f, 1f);
        Assert.AreEqual(5f, line.LinePoint(TroopConstants.LeftEnd).y, 1e-4f);
        Assert.AreEqual(-5f, line.LinePoint(TroopConstants.RightEnd).y, 1e-4f);
    }
}

public sealed class FaceClassifierTests
{
    static TroopFace Face(float degrees) =>
        FaceClassifier.Classify(Vector2.zero, Vector2.right,
            new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * 10f);

    [Test] public void StraightAhead_IsFront() => Assert.AreEqual(TroopFace.Front, Face(0f));
    [Test] public void NinetyLeft_IsLeftFlank() => Assert.AreEqual(TroopFace.LeftFlank, Face(90f));
    [Test] public void NinetyRight_IsRightFlank() => Assert.AreEqual(TroopFace.RightFlank, Face(-90f));
    [Test] public void Behind_IsRear() => Assert.AreEqual(TroopFace.Rear, Face(180f));
    [Test] public void FortyFive_IsStillFront() => Assert.AreEqual(TroopFace.Front, Face(45f));
    [Test] public void JustPastFortyFive_IsFlank() => Assert.AreEqual(TroopFace.LeftFlank, Face(46f));
    [Test] public void OneThirtyFive_IsStillFlank() => Assert.AreEqual(TroopFace.LeftFlank, Face(135f));
    [Test] public void JustPastOneThirtyFive_IsRear() => Assert.AreEqual(TroopFace.Rear, Face(136f));

    [Test]
    public void FlankAndRear_AreFlanked()
    {
        Assert.IsFalse(TroopFace.Front.IsFlanked());
        Assert.IsTrue(TroopFace.LeftFlank.IsFlanked());
        Assert.IsTrue(TroopFace.Rear.IsFlanked());
    }

    [Test]
    public void UsesTheAttackersNearestFootprintPoint()
    {
        // Defender at the origin facing +x. A long enemy line north of it, offset so its pivot is ahead but its nearest
        // point is straight north: the attack is on the left flank.
        TroopFootprint defender = TroopFootprint.Circle(Vector2.zero, 1f, Vector2.right);
        TroopFootprint attacker = TroopFootprint.Rect(new Vector2(-10f, 5f), new Vector2(30f, 5f), Vector2.down, 0.5f);
        Assert.AreEqual(TroopFace.LeftFlank, FaceClassifier.Classify(defender, Vector2.zero, attacker));
    }
}

public sealed class SurroundTestTests
{
    [Test]
    public void RetreatPath_SitsBehindTheRearFace()
    {
        TroopFootprint line = TroopFootprint.Rect(new Vector2(0f, -5f), new Vector2(0f, 5f), Vector2.right, 1f);
        TroopFootprint path = SurroundTest.RetreatPath(line, 4f);
        Assert.AreEqual(-3f, path.Center.x, 1e-4f);
        TroopFootprint enemyBehind = TroopFootprint.Circle(new Vector2(-3f, 0f), 1f, Vector2.right);
        TroopFootprint enemyAhead = TroopFootprint.Circle(new Vector2(4f, 0f), 1f, Vector2.left);
        Assert.IsTrue(SurroundTest.BlockedBy(path, enemyBehind));
        Assert.IsFalse(SurroundTest.BlockedBy(path, enemyAhead));
    }
}

public sealed class FormationModeResolverTests
{
    [Test] public void RoutBeatsChain() => Assert.AreEqual(FormationMode.Rout, FormationModeResolver.Resolve(true, true, true, true, true));
    [Test] public void ChainBeatsLine() => Assert.AreEqual(FormationMode.Chain, FormationModeResolver.Resolve(false, true, true, false, false));
    [Test] public void LineBeatsColumn() => Assert.AreEqual(FormationMode.Line, FormationModeResolver.Resolve(false, false, true, false, true));
    [Test] public void ColumnBeatsIdle() => Assert.AreEqual(FormationMode.Column, FormationModeResolver.Resolve(false, false, false, false, true));
    [Test] public void IdleByDefault() => Assert.AreEqual(FormationMode.Idle, FormationModeResolver.Resolve(false, false, false, false, false));
    [Test] public void HeldLineOrder_IsLine() => Assert.AreEqual(FormationMode.Line, FormationModeResolver.Resolve(false, false, false, true, false));
    [Test] public void MarchingChainStaysChain() => Assert.AreEqual(FormationMode.Chain, FormationModeResolver.Resolve(false, true, true, false, true));
    [Test] public void LinkWithoutLine_IsNotChain() => Assert.AreEqual(FormationMode.Column, FormationModeResolver.Resolve(false, true, false, false, true));
}

public sealed class LineGeometryTests
{
    // SU-103: the line is Frontage x LineSpacing long, so a frontage of 5 stands 5 soldiers wide.
    [Test]
    public void NaturalLength_ForFrontage5_Is4_5_AndFiveSoldiersWide()
    {
        Assert.AreEqual(4.5f, LineGeometry.NaturalLength(5), 1e-4f);
        Assert.AreEqual(5, LineGeometry.SoldiersPerRank(LineGeometry.NaturalLength(5)));
        Assert.AreEqual(29.7f, LineGeometry.NaturalLength(33), 1e-4f);
    }

    // SU-103: the radius is L_nat / 1.8 (frontage 10 is the old radius 5), unless the blob needs more room.
    [Test]
    public void TroopRadius_FromFrontage_OrLargerForACrowdedBlob()
    {
        Assert.AreEqual(5f, LineGeometry.TroopRadius(10, 16, 1.8f, 0.85f), 1e-4f);
        Assert.AreEqual(2.5f, LineGeometry.TroopRadius(5, 16, 1.8f, 0.85f), 1e-4f);
        float crowded = LineGeometry.TroopRadius(3, 40, 1.8f, 0.85f);
        float blobArea = Mathf.PI * Mathf.Pow(crowded * 0.85f, 2f);
        Assert.AreEqual(40f * LineGeometry.PackedAreaPerSoldier, blobArea, 1e-2f);
    }

    // SU-131: TroopSizeScale multiplies the radius (Formation Tuning default 1.5).
    [Test]
    public void TroopRadius_IsScaledByTroopSizeScale()
    {
        Assert.AreEqual(3.75f, LineGeometry.TroopRadius(5, 16, 1.8f, 0.85f, 1.5f), 1e-4f);
        TroopTuning tuning = TroopTuning.CreateDefaults();
        try
        {
            Assert.AreEqual(1.5f, tuning.TroopSizeScale, 1e-6f);
            Assert.AreEqual(3.75f, tuning.TroopRadius(5, 16), 1e-4f);
        }
        finally
        {
            tuning.DestroySections();
        }
    }

    [Test]
    public void SoldiersPerRank_Is33_For29_7()
    {
        Assert.AreEqual(33, LineGeometry.SoldiersPerRank(29.7f));
    }

    [Test]
    public void Ranks_AreAddedWhenTheRankIsFull()
    {
        Assert.AreEqual(1, LineGeometry.Ranks(33, 29.7f));
        Assert.AreEqual(2, LineGeometry.Ranks(34, 29.7f));
    }

    [Test]
    public void StretchClamp_IsPointSevenToThreeTimesNatural()
    {
        Assert.AreEqual(7f, LineGeometry.ClampToStretch(1f, 10f, 0.7f, 3f), 1e-4f);
        Assert.AreEqual(30f, LineGeometry.ClampToStretch(50f, 10f, 0.7f, 3f), 1e-4f);
    }

    [Test]
    public void LineSpacing_Is0_9()
    {
        Assert.AreEqual(0.9f, TroopConstants.LineSpacing);
        Assert.AreEqual(0.9f, TroopConstants.SeparationRadius, 1e-6f);
    }
}
