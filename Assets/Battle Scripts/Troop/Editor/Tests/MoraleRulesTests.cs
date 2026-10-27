using NUnit.Framework;

// Spec §5: Effective Morale, Shock decay and the break cases, with the Franz worked example.
public sealed class MoraleRulesTests
{
    const int RetreatPenalty = 5;
    const int SurroundedPenalty = 20;
    const float DecayFraction = 0.2f;
    const float Window = 30f;
    const float RoutFraction = 0.5f;

    [Test]
    public void Franz_StartsAt60_AndHas50AfterLosingTwoSoldiers()
    {
        Assert.AreEqual(60f, MoraleRules.InitialEffectiveMorale(5, 12));
        Assert.AreEqual(60f, MoraleRules.EffectiveMorale(5, 12, 0, 0, RetreatPenalty, SurroundedPenalty));
        Assert.AreEqual(50f, MoraleRules.EffectiveMorale(5, 10, 0, 0, RetreatPenalty, SurroundedPenalty));
    }

    [Test]
    public void Penalties_ReduceEffectiveMorale()
    {
        Assert.AreEqual(45f, MoraleRules.EffectiveMorale(5, 10, 1, 0, RetreatPenalty, SurroundedPenalty));
        Assert.AreEqual(25f, MoraleRules.EffectiveMorale(5, 10, 1, 1, RetreatPenalty, SurroundedPenalty));
    }

    [Test]
    public void Franz_With50_DecaysAThirdOfAPointPerSecond()
    {
        Assert.AreEqual(0.3333f, MoraleRules.DecayPerSecond(50f, DecayFraction, Window), 1e-3f);
    }

    [Test]
    public void ShockDropsBy10_After30SecondsWithoutHits()
    {
        float shock = 40f;
        for (int frame = 0; frame < 30 * 60; frame++)
            shock = MoraleRules.DecayShock(shock, 50f, DecayFraction, Window, 1f / 60f);
        Assert.AreEqual(30f, shock, 0.01f);
    }

    [Test]
    public void ShockDecay_ClampsAtZero_AndNeverRises()
    {
        Assert.AreEqual(0f, MoraleRules.DecayShock(0.1f, 50f, DecayFraction, Window, 10f));
        Assert.AreEqual(5f, MoraleRules.DecayShock(5f, -10f, DecayFraction, Window, 1f));
    }

    [Test]
    public void NoBreak_WhileShockIsNotAboveEffectiveMorale()
    {
        Assert.AreEqual(BreakCase.None, MoraleRules.Evaluate(40f, 40f, 60f, false, RoutFraction));
    }

    [Test]
    public void CaseA_Rout_BelowHalfOfInitial_EvenWhenSurrounded()
    {
        Assert.AreEqual(BreakCase.Rout, MoraleRules.Evaluate(26f, 25f, 60f, false, RoutFraction));
        Assert.AreEqual(BreakCase.Rout, MoraleRules.Evaluate(26f, 25f, 60f, true, RoutFraction));
    }

    [Test]
    public void CaseB_Retreat_CostsFive()
    {
        Assert.AreEqual(BreakCase.Retreat, MoraleRules.Evaluate(41f, 40f, 60f, false, RoutFraction));
        Assert.AreEqual(35f, MoraleRules.EffectiveMorale(5, 8, 1, 0, RetreatPenalty, SurroundedPenalty));
    }

    [Test]
    public void CaseC_SurroundedBreak_CostsTwenty()
    {
        Assert.AreEqual(BreakCase.SurroundedBreak, MoraleRules.Evaluate(41f, 40f, 60f, true, RoutFraction));
        Assert.AreEqual(20f, MoraleRules.EffectiveMorale(5, 8, 0, 1, RetreatPenalty, SurroundedPenalty));
    }

    [Test]
    public void ExactlyHalf_IsNotARout()
    {
        Assert.AreEqual(BreakCase.Retreat, MoraleRules.Evaluate(31f, 30f, 60f, false, RoutFraction));
    }
}
