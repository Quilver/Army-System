using NUnit.Framework;

// Spec §4.6 (Phase 02 acceptance): mitigation split, the combined HP pool, bruise healing and fatigue.
public sealed class MitigationResolverTests
{
    [Test]
    public void DamageBelowDefence_AllPointsRoll_NoOverflow()
    {
        MitigationResult result = MitigationResolver.Resolve(5, 8, 30, 50, new SeededTroopRandom(1));
        Assert.AreEqual(5, result.Deflected + result.Bruised + result.Bled);
        Assert.AreEqual(0, result.Overflow);
    }

    [Test]
    public void DamageAboveDefence_ExtraBecomesOverflowBleed()
    {
        MitigationResult result = MitigationResolver.Resolve(12, 8, 30, 50, new SeededTroopRandom(1));
        Assert.AreEqual(8, result.Deflected + result.Bruised + result.Bled);
        Assert.AreEqual(4, result.Overflow);
        Assert.AreEqual(result.Bruised + result.Bled, result.ShockPoints);
    }

    [Test]
    public void HeavyArmour_DeflectShareIsAbout60Percent()
    {
        var random = new SeededTroopRandom(42);
        int deflected = 0;
        int rolled = 0;
        for (int i = 0; i < 1000; i++)
        {
            MitigationResult result = MitigationResolver.Resolve(10, 10, 60, 30, random);
            deflected += result.Deflected;
            rolled += 10;
        }
        Assert.AreEqual(0.6f, deflected / (float)rolled, 0.01f);
    }
}

public sealed class SoldierHealthTests
{
    [Test]
    public void Bruise_HealsOnePointPerSecond()
    {
        var health = new SoldierHealth(15);
        health.ApplyBruise(5);
        for (int frame = 0; frame < 3 * 60 + 3; frame++) health.TickBruiseHealing(1f / 60f, 1f);
        Assert.AreEqual(2, health.Bruise, "3 points heal in 3 s");
    }

    [Test]
    public void Bleed_IsPermanent()
    {
        var health = new SoldierHealth(15);
        health.ApplyBleed(4);
        health.TickBruiseHealing(10f, 1f);
        Assert.AreEqual(4, health.Bleed);
        Assert.AreEqual(11, health.CurrentHp);
    }

    [TestCase(15, 8, false)]
    [TestCase(15, 7, true)]
    [TestCase(10, 5, true)]
    [TestCase(10, 6, false)]
    [TestCase(40, 20, true)]
    [TestCase(40, 21, false)]
    public void FatigueThreshold_IsHalfMaxHp(int maxHp, int hpLeft, bool fatigued)
    {
        var health = new SoldierHealth(maxHp);
        health.ApplyBleed(maxHp - hpLeft);
        Assert.AreEqual(fatigued, health.IsFatigued(0.5f));
    }

    [Test]
    public void OverflowPastZero_IsNotReturnedAsApplied()
    {
        var health = new SoldierHealth(10);
        health.ApplyBleed(8);
        Assert.AreEqual(2, health.ApplyBleed(5));
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void Shock_IsAppliedBruisePlusBleed_WithoutOverflow()
    {
        var soldier = new SoldierAgentState(1, null, UnityEngine.Vector2.zero); // 15 HP
        AppliedDamage applied = soldier.ApplyMitigation(new MitigationResult(2, 3, 4, 6));
        Assert.AreEqual(7, applied.Shock);
        Assert.AreEqual(13, applied.Total);
    }
}

public sealed class CompositeTroopResolverTests
{
    [Test]
    public void Composite_MinMove_MaxMoraleAndDiscipline_SumSoldiers()
    {
        var resolved = CompositeTroopResolver.Resolve(new[]
        {
            new TroopStatLine(5, 6, 3, 10),
            new TroopStatLine(7, 4, 8, 12)
        });
        Assert.AreEqual(5, resolved.Move);
        Assert.AreEqual(6, resolved.BaseMorale);
        Assert.AreEqual(8, resolved.Discipline);
        Assert.AreEqual(22, resolved.InitialSoldierCount);
    }
}
