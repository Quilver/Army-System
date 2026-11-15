using NUnit.Framework;

// The troop card's Shock bar label (TroopCardHud.LabelAtRightEnd): it stays at the bar's left end unless the white
// morale tick would fall on it, then it moves to the right end, so low morale no longer hides the Shock value.
// The Morale bar's plus (SU-167): shown while the chain's Morale bonus lifts Effective Morale.
public sealed class TroopCardHudTests
{
    const float LabelWidth = 90f;
    const float Clearance = 6f;

    [Test]
    public void ShockLabel_StaysLeft_WhenTheTickIsPastIt()
    {
        Assert.IsFalse(TroopCardHud.LabelAtRightEnd(300f, LabelWidth, Clearance), "full morale: tick at the right end");
        Assert.IsFalse(TroopCardHud.LabelAtRightEnd(LabelWidth + Clearance + 1f, LabelWidth, Clearance));
    }

    [Test]
    public void ShockLabel_MovesRight_WhenTheTickWouldCoverIt()
    {
        Assert.IsTrue(TroopCardHud.LabelAtRightEnd(0f, LabelWidth, Clearance), "no morale left: tick at the left end");
        Assert.IsTrue(TroopCardHud.LabelAtRightEnd(LabelWidth * 0.5f, LabelWidth, Clearance), "tick inside the label");
        Assert.IsTrue(TroopCardHud.LabelAtRightEnd(LabelWidth + Clearance, LabelWidth, Clearance), "tick just past it");
    }

    // SU-167: the plus beside the morale figure shows while the chain's Morale bonus adds to Effective Morale.
    [Test]
    public void MoraleBonusLift_IsTheBonusTimesLivingSoldiers()
    {
        Assert.AreEqual(20, TroopCardHud.MoraleBonusLift(1, 20), "one link: 120 base over 20 soldiers reads 140 / 120");
        Assert.AreEqual(12, TroopCardHud.MoraleBonusLift(2, 6), "two links over 6 soldiers");
    }

    [Test]
    public void MoraleBonusLift_IsZero_WithoutABonusOrSoldiers()
    {
        Assert.AreEqual(0, TroopCardHud.MoraleBonusLift(0, 20), "not chained: no plus");
        Assert.AreEqual(0, TroopCardHud.MoraleBonusLift(1, 0), "no living soldiers: nothing is lifted");
        Assert.AreEqual(0, TroopCardHud.MoraleBonusLift(-1, 20), "a negative bonus is not a bonus");
    }

    [Test]
    public void ChainMoraleTip_NamesTheBonusAndWhatItAdds()
    {
        Assert.AreEqual("Chain +20", TroopCardHud.ChainMoraleTip(20));
    }
}
