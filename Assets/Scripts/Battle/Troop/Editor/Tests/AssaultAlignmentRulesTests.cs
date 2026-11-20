using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The predicted Assault line (SU-183, AssaultAlignmentRules): pure maths.
public sealed class AssaultAlignmentRulesTests
{
    const float Spacing = TroopConstants.LineSpacing;

    // A 3 x 2 troop facing +y whose front centre soldier sags 0.3 behind its rank (as Julian's did in Movie_025), with
    // 0.4 of gap still to close: the front rank stands level on the closed front line and the rear rank one spacing back.
    [Test]
    public void HeadOn_ASaggingSoldierKeepsItsRank_AndTheLineClosesForward()
    {
        float length = LineGeometry.NaturalLength(3);
        float frontOffset = LineGeometry.FrontRankOffset(length, 6);
        Vector2 pivot = Vector2.zero;
        Vector2 front = pivot + Vector2.up * frontOffset;
        var positions = new List<Vector2>
        {
            front + new Vector2(-0.9f, 0f), front + new Vector2(0.9f, 0f),
            front + new Vector2(-0.9f, -0.9f), front + new Vector2(0.9f, -0.9f),
            front + new Vector2(0f, -1.2f), front + new Vector2(0f, -0.3f)
        };
        var predicted = new List<Vector2>();
        var ranks = new List<int>();
        AssaultAlignmentRules.Predict(pivot, Vector2.up, length, frontOffset, Vector2.up, true, 0.4f, 3, Spacing,
            positions, predicted, ranks);

        CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 1, 0 }, ranks, "The sagging centre soldier is still front rank");
        float frontY = front.y + 0.4f;
        foreach (int i in new[] { 0, 1, 5 }) Assert.AreEqual(frontY, predicted[i].y, 1e-4f, "Front rank level, closed up");
        foreach (int i in new[] { 2, 3, 4 }) Assert.AreEqual(frontY - Spacing, predicted[i].y, 1e-4f, "Rear rank behind");
        for (int i = 0; i < positions.Count; i++)
            Assert.AreEqual(positions[i].x, predicted[i].x, 1e-4f, "Each keeps its place along the line");
    }

    // A line facing +x wheels 90 degrees to face +y about its leading end: that end stays put and the front rank lies
    // along the new front, behind which the second rank stands.
    [Test]
    public void Wheel_AboutTheLeadingEnd_KeepsThatEnd()
    {
        float length = LineGeometry.NaturalLength(4);
        float frontOffset = LineGeometry.FrontRankOffset(length, 8);
        Vector2 pivot = new(2f, 1f);
        Vector2 heading = Vector2.right;
        Vector2 aligned = Vector2.up;
        int side = AssaultWheelRules.LeadingEnd(pivot, heading, length, frontOffset, aligned);
        Vector2 leadingEnd = AssaultWheelRules.EndPoint(pivot, heading, length, frontOffset, side);
        Vector2 center = pivot + heading * frontOffset;
        var positions = new List<Vector2>();
        for (int file = 0; file < 4; file++)
        {
            float along = -length * 0.5f + (file + 0.5f) * Spacing;
            positions.Add(center + new Vector2(0f, along));
            positions.Add(center + new Vector2(-Spacing, along));
        }
        var predicted = new List<Vector2>();
        var ranks = new List<int>();
        AssaultAlignmentRules.Predict(pivot, heading, length, frontOffset, aligned, true, 0f, 4, Spacing, positions,
            predicted, ranks);

        Vector2 alignedPivot = pivot + AssaultWheelRules.PivotStepKeepingEnd(heading, aligned, length, frontOffset, side);
        Assert.AreEqual(leadingEnd.x, AssaultWheelRules.EndPoint(alignedPivot, aligned, length, frontOffset, side).x, 1e-4f);
        float frontY = leadingEnd.y;
        for (int i = 0; i < positions.Count; i++)
        {
            int rank = i % 2;
            Assert.AreEqual(rank, ranks[i]);
            Assert.AreEqual(frontY - rank * Spacing, predicted[i].y, 1e-4f, "Square to the new front");
            Assert.LessOrEqual(Mathf.Abs(predicted[i].x - (alignedPivot.x)), length * 0.5f + 1e-4f, "Within the line");
        }
    }
}
