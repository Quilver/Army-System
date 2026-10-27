using System;
using NUnit.Framework;

// Spec §4.3-§4.6: duel, Domination, support, Combat Power, damage and the charge rate.
public sealed class DuelRulesTests
{
    [Test]
    public void SixtyPercent_Roll40_ChargerWins_Domination2()
    {
        DuelOutcome outcome = DuelRules.Resolve(60f, 40f, 40f, 10f);
        Assert.IsTrue(outcome.AWins);
        Assert.AreEqual(2, outcome.Domination);
    }

    [Test]
    public void SixtyPercent_Roll70_DefenderWins_Domination1()
    {
        DuelOutcome outcome = DuelRules.Resolve(60f, 40f, 70f, 10f);
        Assert.IsFalse(outcome.AWins);
        Assert.AreEqual(1, outcome.Domination);
    }

    [Test]
    public void RollEqualToChance_ChargerWins()
    {
        Assert.IsTrue(DuelRules.Resolve(60f, 40f, 60f, 10f).AWins);
    }

    [TestCase(75f, 2)]   // |75 - 60| / 10 = 1.5 -> 2
    [TestCase(85f, 3)]   // 2.5 -> 3
    [TestCase(64f, 0)]   // 0.4 -> 0
    public void Domination_RoundsToNearest_HalfUp(float roll, int expected)
    {
        Assert.AreEqual(expected, DuelRules.Resolve(60f, 40f, roll, 10f).Domination);
    }

    [Test]
    public void Labels_ChargerIsA_EarlierChargeIsA_LowerIdIsA()
    {
        Assert.IsTrue(DuelRules.FirstIsA(true, 5f, 9, false, 0f, 1));
        Assert.IsFalse(DuelRules.FirstIsA(false, 0f, 1, true, 5f, 9));
        Assert.IsTrue(DuelRules.FirstIsA(true, 1f, 9, true, 2f, 1));
        Assert.IsFalse(DuelRules.FirstIsA(true, 3f, 1, true, 2f, 9));
        Assert.IsTrue(DuelRules.FirstIsA(false, 0f, 1, false, 0f, 2));
    }
}

public sealed class SupportRulesTests
{
    [Test]
    public void SixEligible_SupportFive_CountsFive_BonusTen()
    {
        Assert.AreEqual(10, SupportRules.SupportBonus(6, 5, 2, false));
    }

    [Test]
    public void Flanked_GetsNoSupport()
    {
        Assert.AreEqual(0, SupportRules.SupportBonus(6, 5, 2, true));
    }

    [Test]
    public void BoarSpear_DoublesTheSupportersRange()
    {
        Assert.AreEqual(3f, SupportRules.SupporterRange(3f, 2f, false));
        Assert.AreEqual(6f, SupportRules.SupporterRange(3f, 2f, true));
        Assert.IsTrue(SupportRules.InSupportRange(5f, SupportRules.SupporterRange(3f, 2f, true)));
        Assert.IsFalse(SupportRules.InSupportRange(5f, SupportRules.SupporterRange(3f, 2f, false)));
    }

    [Test]
    public void FatiguedSoldier_CannotSupport()
    {
        var soldier = new SoldierAgentState(1, null, UnityEngine.Vector2.zero); // MaxHP 15
        Assert.IsTrue(soldier.CanSupport);
        soldier.ApplyMitigation(new MitigationResult(0, 0, 7, 0));       // 8 HP left: not fatigued (threshold 7.5)
        Assert.IsTrue(soldier.CanSupport);
        soldier.ApplyMitigation(new MitigationResult(0, 0, 1, 0));       // 7 HP left: fatigued
        Assert.IsFalse(soldier.CanSupport);
        Assert.AreEqual(SoldierLifeState.Fatigued, soldier.LifeState);
    }
}

public sealed class CombatPowerTests
{
    [Test]
    public void BothCharging_BothGetTheChargeBonus()
    {
        int a = CombatPower.Compute(5, true, 5, 3, false, 5, 4);
        int b = CombatPower.Compute(6, true, 4, 0, false, 5, 0);
        Assert.AreEqual(5 + 5 + 3 + 4, a);
        Assert.AreEqual(6 + 4, b);
    }

    [Test]
    public void NotCharging_NoChargeBonus_AssaultOnlyWhileCharging()
    {
        Assert.AreEqual(9, CombatPower.Compute(5, false, 5, 3, true, 5, 4));
        Assert.AreEqual(5 + 5 + 3 + 5 + 4, CombatPower.Compute(5, true, 5, 3, true, 5, 4));
    }
}

public sealed class DamageRulesTests
{
    [Test]
    public void Damage_IsStrengthPlusWeaponPlusDomination()
    {
        Assert.AreEqual(5 + 3 + 2, DamageRules.RawDamage(5, 3, 2, false));
    }

    [Test]
    public void Lance_DoublesTotalDamage_OnlyWhileCharging()
    {
        Assert.AreEqual(20, DamageRules.RawDamage(5, 3, 2, true));
    }
}

public sealed class ChargeRulesTests
{
    [Test]
    public void ProbabilityPerFrame_MatchesExponentialRate()
    {
        float p = ChargeRules.ChargeProbability(5f, 30f, 1f / 60f);
        Assert.AreEqual(1f - (float)Math.Exp(-5.0 / 30.0 / 60.0), p, 1e-6f);
        Assert.AreEqual(0f, ChargeRules.ChargeProbability(0f, 30f, 0.1f));
    }

    [Test]
    public void AssaultAddsFiveAggression()
    {
        Assert.AreEqual(10f, ChargeRules.EffectiveAggression(5, true, 5));
        Assert.AreEqual(5f, ChargeRules.EffectiveAggression(5, false, 5));
    }

    // 200 engaged soldiers over 30 s: charges per soldier is about Aggression (within 10%).
    [TestCase(5)]
    [TestCase(10)]
    public void ChargeRate_Over30Seconds_IsAboutAggression(int aggression)
    {
        var random = new SeededTroopRandom(1234);
        const float dt = 1f / 60f;
        float p = ChargeRules.ChargeProbability(aggression, 30f, dt);
        int charges = 0;
        for (int soldier = 0; soldier < 200; soldier++)
            for (int frame = 0; frame < 30 * 60; frame++)
                if (random.Value() < p) charges++;
        Assert.AreEqual(aggression, charges / 200f, aggression * 0.1f);
    }
}

public sealed class ClashRulesTests
{
    [Test]
    public void ChargerInContact_Clashes_EvenOnCooldown()
    {
        Assert.AreEqual(ClashKind.Charge, ClashRules.Classify(true, false, false, false, 0.5f, 0.5f));
    }

    [Test]
    public void Followers_NeverStartAClash()
    {
        Assert.AreEqual(ClashKind.None, ClashRules.Classify(false, false, true, false, 0f, 0f));
        Assert.AreEqual(ClashKind.Charge, ClashRules.Classify(false, true, true, false, 0f, 0f));
    }

    [Test]
    public void Contact_NeedsBothOffCooldown()
    {
        Assert.AreEqual(ClashKind.Contact, ClashRules.Classify(false, false, false, false, 0f, 0f));
        Assert.AreEqual(ClashKind.None, ClashRules.Classify(false, false, false, false, 0.2f, 0f));
    }

    // One soldier against one soldier in contact without a charge: at most one clash per ClashCooldownSeconds.
    [Test]
    public void OneOnOneContact_AtMostOneClashPerSecond()
    {
        var a = new SoldierAgentState(1, null, UnityEngine.Vector2.zero);
        var b = new SoldierAgentState(2, null, UnityEngine.Vector2.zero);
        const float dt = 1f / 60f;
        int clashes = 0;
        for (int frame = 0; frame < 10 * 60; frame++)
        {
            a.TickCooldowns(dt);
            b.TickCooldowns(dt);
            if (ClashRules.Classify(a.IsCharging, b.IsCharging, a.IsFollowing, b.IsFollowing,
                    a.ClashCooldownRemaining, b.ClashCooldownRemaining) == ClashKind.None) continue;
            clashes++;
            a.StartClashCooldown(1f);
            b.StartClashCooldown(1f);
        }
        Assert.LessOrEqual(clashes, 10);
        Assert.GreaterOrEqual(clashes, 9);
    }
}
