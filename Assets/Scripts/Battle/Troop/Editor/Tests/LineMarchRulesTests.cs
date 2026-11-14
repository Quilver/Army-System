using NUnit.Framework;
using UnityEngine;

// Marching in line (SU-150): 20% slower in Line or Chain, 40% moving back, each with its own toggle.
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
}
