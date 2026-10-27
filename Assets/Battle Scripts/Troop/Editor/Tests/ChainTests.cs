using NUnit.Framework;
using UnityEngine;

// Spec §3.4, §3.4.1, §6.3: hinge geometry, attach and break rules, topology, shared retreat falloff, order envelope.
public sealed class ChainGeometryTests
{
    [Test]
    public void StraightChain_Is180()
    {
        Assert.AreEqual(180f, ChainGeometry.InnerHingeAngle(Vector2.zero, Vector2.left * 5f, Vector2.right * 5f), 1e-3f);
    }

    [Test]
    public void RightAngleChain_Is90()
    {
        Assert.AreEqual(90f, ChainGeometry.InnerHingeAngle(Vector2.zero, Vector2.left * 5f, Vector2.up * 5f), 1e-3f);
    }

    [Test]
    public void FoldTo60_FailsTheEnvelope()
    {
        Vector2 farB = new(Mathf.Cos(60f * Mathf.Deg2Rad), Mathf.Sin(60f * Mathf.Deg2Rad));
        Assert.IsFalse(ChainGeometry.WithinEnvelope(Vector2.zero, Vector2.right * 5f, 5f, farB * 5f, 5f, 3f, 65f));
        Assert.IsTrue(ChainGeometry.WithinEnvelope(Vector2.zero, Vector2.right * 5f, 5f, Vector2.left * 5f, 5f, 3f, 65f));
    }

    [Test]
    public void Stretch_BreaksAboveThreeTimesNatural()
    {
        Assert.IsFalse(ChainGeometry.BreaksOnStretch(30f, 10f, 3f));
        Assert.IsTrue(ChainGeometry.BreaksOnStretch(30.1f, 10f, 3f));
        Assert.AreEqual(ChainBreakReason.Stretch, ChainLinkRules.Evaluate(31f, 10f, 10f, 10f, 180f, 3f, 65f));
        Assert.AreEqual(ChainBreakReason.Hinge, ChainLinkRules.Evaluate(10f, 10f, 10f, 10f, 64f, 3f, 65f));
        Assert.AreEqual(ChainBreakReason.None, ChainLinkRules.Evaluate(10f, 10f, 10f, 10f, 65f, 3f, 65f));
    }

    [Test]
    public void Joint_IsTheWeightedAverage_AndPinsTheEngagedSide()
    {
        Assert.AreEqual(new Vector2(1f, 0f), ChainGeometry.Joint(Vector2.zero, new Vector2(2f, 0f), 0.5f));
        Assert.AreEqual(Vector2.zero, ChainGeometry.Joint(Vector2.zero, new Vector2(2f, 0f), 1f));
    }

    [Test]
    public void ClampJoint_KeepsAtLeastTheMinimumLength()
    {
        Vector2 joint = ChainGeometry.ClampJoint(new Vector2(1f, 0f), Vector2.zero, 7f, new Vector2(20f, 0f), 7f);
        Assert.AreEqual(7f, joint.x, 1e-4f);
    }
}

public sealed class ChainLinkRulesTests
{
    const float NaturalLength = 10f;

    static bool Attaches(float distanceInNatural, int endA = 1, int endB = 0, Vector2? facingB = null) =>
        ChainLinkRules.CanAttach(Vector2.zero, endA, Vector2.right, new Vector2(0f, distanceInNatural * NaturalLength), endB,
            facingB ?? Vector2.right, ChainLinkRules.AttachDistance(NaturalLength, NaturalLength, 0.25f));

    [Test] public void At0_24_Attaches() => Assert.IsTrue(Attaches(0.24f));
    [Test] public void At0_26_DoesNotAttach() => Assert.IsFalse(Attaches(0.26f));
    [Test] public void SameWing_DoesNotAttach() => Assert.IsFalse(Attaches(0.1f, 1, 1));
    [Test] public void PerpendicularFacing_DoesNotAttach() => Assert.IsFalse(Attaches(0.1f, 1, 0, Vector2.up));
    [Test] public void OppositeFacing_DoesNotAttach() => Assert.IsFalse(Attaches(0.1f, 1, 0, Vector2.left));

    [Test]
    public void TieBreak_ShortestDistance_ThenLowerInstanceId()
    {
        var near = new ChainLinkCandidate(50, 1, 60, 0, 1f);
        var far = new ChainLinkCandidate(10, 1, 20, 0, 2f);
        var tieLow = new ChainLinkCandidate(10, 1, 70, 0, 1f);
        Assert.Less(near.CompareTo(far), 0);
        Assert.Less(tieLow.CompareTo(near), 0);
    }

    [Test]
    public void SharedRetreat_PassesOn70PercentPerHop()
    {
        float first = ChainLinkRules.SympatheticDistance(10f, 0.3f);
        float second = ChainLinkRules.SympatheticDistance(first, 0.3f);
        Assert.AreEqual(7f, first, 1e-4f);
        Assert.AreEqual(4.9f, second, 1e-4f);
    }

    [Test]
    public void SympatheticRetreat_OnlyForUnengagedOverStretchedOrBent()
    {
        Assert.IsTrue(ChainLinkRules.ShouldRetreatSympathetically(false, 1.5f, 0f, 1.4f, 30f));
        Assert.IsTrue(ChainLinkRules.ShouldRetreatSympathetically(false, 1f, 31f, 1.4f, 30f));
        Assert.IsFalse(ChainLinkRules.ShouldRetreatSympathetically(false, 1.3f, 29f, 1.4f, 30f));
        Assert.IsFalse(ChainLinkRules.ShouldRetreatSympathetically(true, 2f, 60f, 1.4f, 30f));
    }
}

public sealed class ChainTopologyTests
{
    [Test]
    public void ClosingALoop_IsRejected()
    {
        var topology = new ChainTopology();
        Assert.IsTrue(topology.CanLink(1, 2));
        topology.Union(1, 2);
        Assert.IsTrue(topology.CanLink(2, 3));
        topology.Union(2, 3);
        Assert.IsFalse(topology.CanLink(3, 1));
        Assert.IsTrue(topology.CanLink(3, 4));
    }
}

public sealed class ChainOrderRulesTests
{
    [Test]
    public void InsideTheEnvelope_StaysLinked()
    {
        // Neighbour line from (-10, 0) to (0, 0); ordered troop moved a little: line point at (1, 0), far end (11, 1).
        Assert.IsTrue(ChainOrderRules.StaysLinked(new Vector2(1f, 0f), new Vector2(11f, 1f), 10f,
            Vector2.zero, new Vector2(-10f, 0f), 10f, 3f, 65f, false));
    }

    [Test]
    public void OutsideTheEnvelope_Unlinks()
    {
        Assert.IsFalse(ChainOrderRules.StaysLinked(new Vector2(60f, 0f), new Vector2(70f, 0f), 10f,
            Vector2.zero, new Vector2(-10f, 0f), 10f, 3f, 65f, false));
    }

    [Test]
    public void AltHeld_AlwaysUnlinks()
    {
        Assert.IsFalse(ChainOrderRules.StaysLinked(new Vector2(1f, 0f), new Vector2(11f, 0f), 10f,
            Vector2.zero, new Vector2(-10f, 0f), 10f, 3f, 65f, true));
    }
}
