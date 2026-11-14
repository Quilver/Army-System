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
}
