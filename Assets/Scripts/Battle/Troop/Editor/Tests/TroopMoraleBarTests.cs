using NUnit.Framework;

// The floating morale bar (TroopMoraleBar.Layout): its length follows Effective Morale against the initial value, and
// Shock fills it from the left on the same scale, reaching the right end at the break point.
public sealed class TroopMoraleBarTests
{
    const float Full = 64f;

    [Test]
    public void Length_FollowsEffectiveMorale()
    {
        TroopMoraleBar.Layout(Full, 60f, 60f, 0f, out float full, out _);
        Assert.AreEqual(Full, full, 1e-4f, "full morale: full length");
        TroopMoraleBar.Layout(Full, 30f, 60f, 0f, out float half, out _);
        Assert.AreEqual(Full * 0.5f, half, 1e-4f, "half morale: half length");
        TroopMoraleBar.Layout(Full, -5f, 60f, 0f, out float none, out _);
        Assert.AreEqual(0f, none, 1e-4f, "negative morale: no length");
        TroopMoraleBar.Layout(Full, 90f, 60f, 0f, out float capped, out _);
        Assert.AreEqual(Full, capped, 1e-4f, "never longer than full");
    }

    [Test]
    public void Shock_FillsFromTheLeft_OnTheMoraleScale()
    {
        TroopMoraleBar.Layout(Full, 30f, 60f, 15f, out float bar, out float shock);
        Assert.AreEqual(bar * 0.5f, shock, 1e-4f, "half of Effective Morale in Shock fills half the bar");
        TroopMoraleBar.Layout(Full, 30f, 60f, 0f, out _, out float noShock);
        Assert.AreEqual(0f, noShock, 1e-4f);
        TroopMoraleBar.Layout(Full, 30f, 60f, 45f, out bar, out shock);
        Assert.AreEqual(bar, shock, 1e-4f, "Shock past the break point fills the whole bar");
    }
}
