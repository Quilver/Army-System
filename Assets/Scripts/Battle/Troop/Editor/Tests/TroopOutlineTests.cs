using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The troop outline (TroopOutline, TroopOutlinePolygon): drawn from the formation so it is steady, and a polygon that
// matches it. Also the return after a charge (SoldierChargeState).
public sealed class TroopOutlineTests
{
    const float Frame = 1f / 60f;

    static TroopFootprint Settle(TroopOutline outline, TroopFootprint target, Vector2 pivot, float seconds)
    {
        TroopFootprint result = outline.At(pivot);
        for (float t = 0f; t < seconds; t += Frame) result = outline.StepToward(target, pivot, Frame);
        return result;
    }

    [Test]
    public void LineOutline_DependsOnTheLineAndSoldierCountOnly()
    {
        TroopFootprint a = TroopOutline.Line(new Vector2(-5f, 0f), new Vector2(5f, 0f), Vector2.up, 40);
        TroopFootprint b = TroopOutline.Line(new Vector2(-5f, 0f), new Vector2(5f, 0f), Vector2.up, 40);
        Assert.AreEqual(a.HalfWidth, b.HalfWidth, 1e-6f);
        Assert.AreEqual(5f, a.HalfLength, 1e-4f);
        float expected = LineGeometry.RankHalfWidth(10f, 40) + TroopConstants.SoldierRadius;
        Assert.AreEqual(expected, a.HalfWidth, 1e-4f);
    }

    [Test]
    public void ColumnOutline_TrailsThePivot()
    {
        TroopFootprint column = TroopOutline.Column(Vector2.zero, Vector2.right, 10f, 30);
        Assert.Less(column.Center.x, 0f);
        Assert.AreEqual(0f, column.Center.y, 1e-4f);
        Assert.IsTrue(column.Contains(new Vector2(-4f, 0f)));
        Assert.IsFalse(column.Contains(new Vector2(3f, 0f)));
    }

    [Test]
    public void FirstStep_SnapsToTheTarget()
    {
        var outline = new TroopOutline();
        TroopFootprint target = TroopOutline.Line(new Vector2(-5f, 1f), new Vector2(5f, 1f), Vector2.up, 40);
        TroopFootprint result = outline.StepToward(target, Vector2.zero, Frame);
        Assert.IsTrue(outline.IsSeeded);
        Assert.AreEqual(target.HalfWidth, result.HalfWidth, 1e-5f);
        Assert.AreEqual(target.Center.y, result.Center.y, 1e-5f);
    }

    // SU-92: while time stands still (deploying or paused) nothing eases, so the outline snaps to the formation. A troop
    // placed during deployment used to keep an outline offset to where its line had been.
    [Test]
    public void NoTimeStep_SnapsToTheTarget()
    {
        var outline = new TroopOutline();
        outline.StepToward(TroopOutline.Line(new Vector2(-5f, 0f), new Vector2(5f, 0f), Vector2.up, 40), Vector2.zero, Frame);
        Vector2 placed = new(20f, 10f);
        TroopFootprint moved = TroopOutline.Line(placed + new Vector2(-5f, 0f), placed + new Vector2(5f, 0f), Vector2.right, 40);
        TroopFootprint result = outline.StepToward(moved, placed, 0f);
        Assert.AreEqual(moved.Center.x, result.Center.x, 1e-4f, "on the placed troop, not where it was");
        Assert.AreEqual(moved.Center.y, result.Center.y, 1e-4f);
        Assert.AreEqual(0f, Vector2.Angle(moved.Facing, result.Facing), 0.01f);
    }

    [Test]
    public void Outline_MovesExactlyWithThePivot()
    {
        var outline = new TroopOutline();
        TroopFootprint target = TroopOutline.Blob(Vector2.zero, 4f, Vector2.right);
        outline.StepToward(target, Vector2.zero, Frame);
        TroopFootprint moved = outline.At(new Vector2(3f, 2f));
        Assert.AreEqual(3f, moved.Center.x, 1e-5f);
        Assert.AreEqual(2f, moved.Center.y, 1e-5f);
    }

    [Test]
    public void LineToBlob_MorphsToTheCircle()
    {
        var outline = new TroopOutline();
        outline.StepToward(TroopOutline.Line(new Vector2(-5f, 0f), new Vector2(5f, 0f), Vector2.up, 40), Vector2.zero, Frame);
        TroopFootprint blob = TroopOutline.Blob(Vector2.zero, 4f, Vector2.up);
        TroopFootprint result = Settle(outline, blob, Vector2.zero, 3f);
        Assert.AreEqual(blob.Rounding, result.Rounding, 0.01f);
        Assert.AreEqual(0f, result.HalfLength, 0.01f);
    }

    [Test]
    public void JitteryMarchHeading_IsSmoothed()
    {
        var outline = new TroopOutline();
        outline.StepHeading(new Vector2(1f, 0f), Frame);
        float worst = 0f;
        for (int i = 0; i < 120; i++)
        {
            // The pivot velocity swings +/-30 degrees every frame.
            float angle = (i % 2 == 0 ? 30f : -30f) * Mathf.Deg2Rad;
            outline.StepHeading(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), Frame);
            worst = Mathf.Max(worst, Vector2.Angle(Vector2.right, outline.Heading));
        }
        Assert.Less(worst, 5f);
    }

    [Test]
    public void Column_HoldsThroughABriefStop()
    {
        var outline = new TroopOutline();
        outline.StepHeading(Vector2.right, Frame);
        outline.StepHeading(Vector2.zero, TroopOutline.StopHoldSeconds * 0.5f);
        Assert.IsTrue(outline.IsMarching);
        outline.StepHeading(Vector2.zero, TroopOutline.StopHoldSeconds);
        Assert.IsFalse(outline.IsMarching);
    }

    [Test]
    public void Polygon_OfALine_IsItsFourCorners()
    {
        var points = new List<Vector2>();
        TroopFootprint line = TroopFootprint.Rect(new Vector2(-5f, 0f), new Vector2(5f, 0f), Vector2.up, 1f);
        TroopOutlinePolygon.Build(line, points);
        Assert.AreEqual(4, points.Count);
        Assert.AreEqual(20f, TroopOutlinePolygon.SignedArea(points), 1e-3f);
    }

    [Test]
    public void Polygon_OfACircle_StaysOnTheRadius()
    {
        var points = new List<Vector2>();
        TroopFootprint blob = TroopFootprint.Circle(new Vector2(3f, 2f), 4f, Vector2.right);
        TroopOutlinePolygon.Build(blob, points);
        Assert.Greater(points.Count, 8);
        foreach (var point in points)
            Assert.AreEqual(4f, Vector2.Distance(point, blob.Center), 1e-3f);
        Assert.Greater(TroopOutlinePolygon.SignedArea(points), 0f);
    }

    [Test]
    public void Polygon_OfAColumn_SpansTheLane()
    {
        var points = new List<Vector2>();
        TroopFootprint column = TroopOutline.Column(Vector2.zero, Vector2.right, 10f, 1);
        TroopOutlinePolygon.Build(column, points);
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        foreach (var point in points)
        {
            min = Mathf.Min(min, point.x);
            max = Mathf.Max(max, point.x);
        }
        float trail = 10f * SoldierFieldPlanner.ColumnTrailFraction;
        float length = 10f * SoldierFieldPlanner.ColumnLengthFraction;
        Assert.AreEqual(-(trail + length) - column.Rounding, min, 1e-3f);
        Assert.AreEqual(-trail + column.Rounding, max, 1e-3f);
    }

    [Test]
    public void ChargeEnd_LeavesAPendingReturn_ButARoutDoesNot()
    {
        var charge = new SoldierChargeState();
        charge.BeginCharge(null, null, 3f, 0f);
        charge.End();
        Assert.IsTrue(charge.ConsumeReturnPending());
        Assert.IsFalse(charge.ConsumeReturnPending());

        charge.BeginCharge(null, null, 3f, 0f);
        charge.End(returnToTroop: false);
        Assert.IsFalse(charge.ConsumeReturnPending());
    }

    [Test]
    public void Return_EndsOnTimeoutAndOnANewCharge()
    {
        var charge = new SoldierChargeState();
        charge.BeginReturn(1f);
        charge.TickReturn(0.5f);
        Assert.IsTrue(charge.IsReturning);
        charge.TickReturn(0.6f);
        Assert.IsFalse(charge.IsReturning);

        charge.BeginReturn(1f);
        charge.BeginCharge(null, null, 3f, 0f);
        Assert.IsFalse(charge.IsReturning);
    }
}
