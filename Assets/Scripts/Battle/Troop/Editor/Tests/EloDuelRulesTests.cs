using NUnit.Framework;

// Elo duel option (SU-153): P_A from the Combat Power difference.
public sealed class EloDuelRulesTests
{
    [Test]
    public void EqualPower_IsFiftyFifty()
    {
        Assert.AreEqual(50.0, DuelRules.EloWinPercent(21f, 21f, 30f), 1e-6);
        Assert.AreEqual(50.0, DuelRules.EloWinPercent(5f, 5f, 30f), 1e-6);
    }

    [Test]
    public void AScaleAhead_WinsTenToOne()
    {
        Assert.AreEqual(100.0 / 1.1, DuelRules.EloWinPercent(40f, 10f, 30f), 1e-4);
        Assert.AreEqual(100.0 / 11.0, DuelRules.EloWinPercent(10f, 40f, 30f), 1e-4);
    }

    // The point of the Elo duel: the same lead gives the same odds at any level, where the ratio does not.
    [Test]
    public void TheSameLead_GivesTheSameOdds_AtAnyLevel()
    {
        Assert.AreEqual(DuelRules.EloWinPercent(28f, 5f, 30f), DuelRules.EloWinPercent(38f, 15f, 30f), 1e-6);
        Assert.AreNotEqual(DuelRules.ChargerWinPercent(28f, 5f), DuelRules.ChargerWinPercent(38f, 15f));
    }

    [Test]
    public void Resolve_UsesTheEloChance_WhenOn()
    {
        // Elo: 40 vs 10 is 90.9%; the ratio gives 80%. A roll of 85 wins only with Elo.
        Assert.IsTrue(DuelRules.Resolve(40f, 10f, 85f, 10f, elo: true, eloScale: 30f).AWins);
        Assert.IsFalse(DuelRules.Resolve(40f, 10f, 85f, 10f).AWins);
        // Domination is still the roll's distance from the chance, / 10: |85 - 90.909| / 10 = 0.59, rounds to 1.
        Assert.AreEqual(1, DuelRules.Resolve(40f, 10f, 85f, 10f, elo: true, eloScale: 30f).Domination);
    }
}
