using NUnit.Framework;

// Spec §4.6 (Phase 02 acceptance): mitigation split, the combined HP pool, bruise healing and fatigue.
public sealed class MitigationResolverTests
{
    [Test]
    public void DamageBelowDefence_AllPointsRoll_NoOverflow()
    {
        MitigationResult result = MitigationResolver.Resolve(5, 8, 3, 5, 2, new SeededTroopRandom(1));
        Assert.AreEqual(5, result.Deflected + result.Bruised + result.Bled);
        Assert.AreEqual(0, result.Overflow);
    }

    [Test]
    public void DamageAboveDefence_ExtraBecomesOverflowBleed()
    {
        MitigationResult result = MitigationResolver.Resolve(12, 8, 3, 5, 2, new SeededTroopRandom(1));
        Assert.AreEqual(8, result.Deflected + result.Bruised + result.Bled);
        Assert.AreEqual(4, result.Overflow);
        Assert.AreEqual(result.Bruised + result.Bled + result.Overflow, result.ShockPoints, "Overflow bleed counts as shock (SU-128)");
    }

    [Test]
    public void HeavyArmour_DeflectShareIsAbout60Percent()
    {
        var random = new SeededTroopRandom(42);
        int deflected = 0;
        int rolled = 0;
        for (int i = 0; i < 1000; i++)
        {
            MitigationResult result = MitigationResolver.Resolve(10, 10, 6, 3, 1, random);
            deflected += result.Deflected;
            rolled += 10;
        }
        Assert.AreEqual(0.6f, deflected / (float)rolled, 0.01f);
    }

    // SU-89: the armour split is a ratio, so 6 : 3 : 1 and 60 : 30 : 10 roll the same.
    [Test]
    public void Ratio_ScaledPartsRollTheSame()
    {
        MitigationResult small = MitigationResolver.Resolve(10, 10, 6, 3, 1, new SeededTroopRandom(7));
        MitigationResult large = MitigationResolver.Resolve(10, 10, 60, 30, 10, new SeededTroopRandom(7));
        Assert.AreEqual(small.Deflected, large.Deflected);
        Assert.AreEqual(small.Bruised, large.Bruised);
        Assert.AreEqual(small.Bled, large.Bled);
    }

    // SU-89: Piercing 2 against 6 : 3 : 1 rolls as 6 : 3 : 3, so Bleed is 3 / 12 = 25%.
    [Test]
    public void Piercing_AddsToBleedPart()
    {
        float bleedShare = Share(r => MitigationResolver.Resolve(10, 10, 6, 3, 1, r, piercing: 2).Bled);
        Assert.AreEqual(0.25f, bleedShare, 0.015f);
    }

    // SU-89: Bludgeoning 2 against 6 : 3 : 1 rolls as 6 : 5 : 1, so Bruise is 5 / 12 (about 42%).
    [Test]
    public void Bludgeoning_AddsToBruisePart()
    {
        float bruiseShare = Share(r => MitigationResolver.Resolve(10, 10, 6, 3, 1, r, bludgeoning: 2).Bruised);
        Assert.AreEqual(5f / 12f, bruiseShare, 0.015f);
    }

    [Test]
    public void Abilities_DoNotChangeOverflow()
    {
        MitigationResult result = MitigationResolver.Resolve(12, 8, 6, 3, 1, new SeededTroopRandom(1), 3, 3);
        Assert.AreEqual(8, result.Deflected + result.Bruised + result.Bled);
        Assert.AreEqual(4, result.Overflow);
    }

    [Test]
    public void AllZeroRatio_EveryRolledPointBleeds()
    {
        MitigationResult result = MitigationResolver.Resolve(5, 8, 0, 0, 0, new SeededTroopRandom(1));
        Assert.AreEqual(5, result.Bled);
        Assert.AreEqual(0, result.Deflected + result.Bruised);
    }

    // SU-90: no armour rolls every point (no overflow) against 0 : 3 : 7.
    [Test]
    public void NoArmour_RollsAgainstZeroThreeSeven_WithoutOverflow()
    {
        MitigationResult one = MitigationResolver.Unarmoured(10, new SeededTroopRandom(3));
        Assert.AreEqual(0, one.Deflected);
        Assert.AreEqual(0, one.Overflow);
        Assert.AreEqual(10, one.Bruised + one.Bled);
        float bruiseShare = Share(r => MitigationResolver.Unarmoured(10, r).Bruised);
        Assert.AreEqual(0.3f, bruiseShare, 0.015f);
    }

    // SU-90: Bludgeoning 2 against no armour rolls as 0 : 5 : 7.
    [Test]
    public void NoArmour_BludgeoningAddsToBruise()
    {
        float bruiseShare = Share(r => MitigationResolver.Unarmoured(10, r, bludgeoning: 2).Bruised);
        Assert.AreEqual(5f / 12f, bruiseShare, 0.015f);
    }

    static float Share(System.Func<ITroopRandom, int> count)
    {
        var random = new SeededTroopRandom(42);
        int total = 0;
        for (int i = 0; i < 2000; i++) total += count(random);
        return total / 20000f;
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
    public void Shock_IsAppliedBruisePlusBleed_IncludingOverflow()
    {
        var soldier = new SoldierAgentState(1, null, UnityEngine.Vector2.zero); // 15 HP
        AppliedDamage applied = soldier.ApplyMitigation(new MitigationResult(2, 3, 4, 6));
        Assert.AreEqual(13, applied.Shock, "Every applied bruise and bleed point counts, overflow included (SU-128)");
        Assert.AreEqual(13, applied.Total);
    }

    [Test]
    public void Shock_ExcludesDamagePastZeroHp()
    {
        var soldier = new SoldierAgentState(1, null, UnityEngine.Vector2.zero); // 15 HP
        AppliedDamage applied = soldier.ApplyMitigation(new MitigationResult(0, 4, 6, 10));
        Assert.AreEqual(15, applied.Total, "Only the 15 HP the soldier had are applied");
        Assert.AreEqual(15, applied.Shock, "Damage past zero HP adds no shock");
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
