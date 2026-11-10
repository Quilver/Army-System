using NUnit.Framework;

// The troop card's Shock bar label (TroopCardHud.LabelAtRightEnd): it stays at the bar's left end unless the white
// morale tick would fall on it, then it moves to the right end, so low morale no longer hides the Shock value.
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
}
