using NUnit.Framework;
using UnityEngine;

// Marching in line (SU-150): 20% slower in Line or Chain, 40% moving back, each with its own toggle. Turning (SU-155):
// marching more than the turn angle off the facing is as slow as moving back, and a line wheels at a limited rate.
public sealed class LineMarchRulesTests
{
    [Test]
    public void OutsideALine_TheMarchIsNotSlowed()
    {
        Assert.AreEqual(1f, LineMarchRules.SpeedScale(false, Vector2.left, Vector2.right, true, 0.8f, true, 0.6f, 90f));
        Assert.IsFalse(LineMarchRules.IsInLine(FormationMode.Column));
        Assert.IsFalse(LineMarchRules.IsInLine(FormationMode.Idle));
        Assert.IsTrue(LineMarchRules.IsInLine(FormationMode.Line));
        Assert.IsTrue(LineMarchRules.IsInLine(FormationMode.Chain));
    }

    [Test]
    public void InALine_ForwardIsTwentyPercentSlower_BackIsFortyPercentSlower()
    {
        Assert.AreEqual(0.8f, LineMarchRules.SpeedScale(true, Vector2.right, Vector2.right, true, 0.8f, true, 0.6f, 90f));
        Assert.AreEqual(0.8f, LineMarchRules.SpeedScale(true, Vector2.up, Vector2.right, true, 0.8f, true, 0.6f, 90f),
            "Sideways (90°) is not moving back");
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, new Vector2(-1f, 1f), Vector2.right, true, 0.8f, true, 0.6f, 90f));
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, Vector2.left, Vector2.right, true, 0.8f, true, 0.6f, 90f));
    }

    [Test]
    public void EachSlowdown_HasItsOwnToggle()
    {
        Assert.AreEqual(1f, LineMarchRules.SpeedScale(true, Vector2.right, Vector2.right, false, 0.8f, true, 0.6f, 90f));
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, Vector2.left, Vector2.right, false, 0.8f, true, 0.6f, 90f));
        Assert.AreEqual(0.8f, LineMarchRules.SpeedScale(true, Vector2.left, Vector2.right, true, 0.8f, false, 0.6f, 90f));
        Assert.AreEqual(1f, LineMarchRules.SpeedScale(true, Vector2.left, Vector2.right, false, 0.8f, false, 0.6f, 90f));
    }

    [Test]
    public void Turning_IsAsSlowAsMovingBack_WhenItsToggleIsOn()
    {
        Vector2 oblique = new(1f, 1.2f); // about 50° off the facing
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, Vector2.up, Vector2.right, true, 0.8f, true, 0.6f, 90f, true, 45f),
            "sideways is turning");
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, oblique, Vector2.right, true, 0.8f, true, 0.6f, 90f, true, 45f));
        Assert.AreEqual(0.8f, LineMarchRules.SpeedScale(true, new Vector2(1f, 0.5f), Vector2.right, true, 0.8f, true, 0.6f, 90f,
            true, 45f), "within the turn angle is forward");
        Assert.AreEqual(0.8f, LineMarchRules.SpeedScale(true, oblique, Vector2.right, true, 0.8f, true, 0.6f, 90f, false, 45f),
            "toggle off");
        Assert.AreEqual(0.6f, LineMarchRules.SpeedScale(true, oblique, Vector2.right, true, 0.8f, false, 0.6f, 90f, true, 45f),
            "the turn slowdown does not need the backward toggle");
        Assert.AreEqual(1f, LineMarchRules.SpeedScale(false, Vector2.up, Vector2.right, true, 0.8f, true, 0.6f, 90f, true, 45f),
            "not in line");
    }

    [Test]
    public void Wheel_TurnsAtMostTheStep_TowardTheTarget()
    {
        Vector2 step = LineMarchRules.Wheel(Vector2.right, Vector2.up, 30f);
        Assert.AreEqual(30f, Vector2.SignedAngle(Vector2.right, step), 1e-3f);
        Vector2 back = LineMarchRules.Wheel(Vector2.right, Vector2.down, 30f);
        Assert.AreEqual(-30f, Vector2.SignedAngle(Vector2.right, back), 1e-3f, "turns the short way");
        Vector2 done = LineMarchRules.Wheel(Vector2.right, new Vector2(1f, 0.1f), 30f);
        Assert.AreEqual(0f, Vector2.Angle(new Vector2(1f, 0.1f), done), 1e-3f, "within reach: reaches it");
        Assert.AreEqual(1f, step.magnitude, 1e-5f);
    }

    // SU-173: steering-circle paths start on the start facing, run continuously with the line facing along them, and
    // end on the target (facing the end facing when there is one).
    [TestCase(0f, 12f, false, 0f, 0f)]
    [TestCase(-10f, 0f, false, 0f, 0f)]
    [TestCase(0f, 15f, true, 1f, 0f)]
    [TestCase(20f, -5f, true, 0f, 1f)]
    [TestCase(-15f, 3f, true, 1f, 0f)]
    [TestCase(10f, 1f, true, 1f, 0f)]
    public void SteeringCircles_PathIsContinuous_AndFacesAlongItself(float x, float y, bool hasEnd, float ex, float ey)
    {
        var samples = new System.Collections.Generic.List<SteeringCircles.Sample>();
        Vector2 target = new(x, y);
        Vector2? end = hasEnd ? new Vector2(ex, ey) : (Vector2?)null;
        Assert.IsTrue(SteeringCircles.Build(Vector2.zero, Vector2.right, target, end, 4.5f, 0.15f, samples));
        Assert.Less(Vector2.Distance(samples[0].Position, Vector2.zero), 1e-3f);
        Assert.Greater(Vector2.Dot(samples[0].Facing, Vector2.right), 0.999f, "starts on its facing");
        Assert.Less(Vector2.Distance(samples[samples.Count - 1].Position, target), 1e-3f, "ends on the target");
        if (hasEnd) Assert.Greater(Vector2.Dot(samples[samples.Count - 1].Facing, end.Value), 0.999f, "arrives on its front");
        for (int i = 1; i < samples.Count; i++)
        {
            Vector2 step = samples[i].Position - samples[i - 1].Position;
            Assert.Less(step.magnitude, 0.2f, "continuous");
            if (step.magnitude > 1e-4f) Assert.Less(Vector2.Angle(step, samples[i].Facing), 3f, "faces along the path");
        }
        float length = SteeringCircles.Length(samples);
        Assert.Less(length, target.magnitude * 1.6f + 4.5f * Mathf.PI * 2f, "no needless loops");
    }

    // SU-173: a quarter wheel at r = half the line: the inner end of the front rank stands.
    [Test]
    public void SteeringCircles_DrillWheel_KeepsTheInnerEndStill()
    {
        var samples = new System.Collections.Generic.List<SteeringCircles.Sample>();
        Assert.IsTrue(SteeringCircles.Build(Vector2.zero, Vector2.right, new Vector2(4.5f, 30f), null, 4.5f, 0.1f, samples));
        Vector2 inner = new(0f, 4.5f);
        foreach (var sample in samples)
        {
            if (!sample.OnArc) continue;
            Vector2 left = new(-sample.Facing.y, sample.Facing.x);
            Assert.Less(Vector2.Distance(sample.Position + left * 4.5f, inner), 0.02f);
        }
    }
}
