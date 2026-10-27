using System;

// Which way a break goes (spec §5.3). Pure C#.
public enum BreakCase
{
    None,
    Rout,            // A
    Retreat,         // B
    SurroundedBreak  // C
}

// Effective Morale, Shock decay and break case selection (spec §5). Pure C#, no Unity types.
public static class MoraleRules
{
    // EffectiveMorale = BaseMorale x CurrentSoldierCount - RetreatCount x RetreatPenalty - SurroundedCount x SurroundedPenalty.
    public static float EffectiveMorale(int baseMorale, int currentSoldiers, int retreatCount, int surroundedBreakCount,
        int retreatPenalty, int surroundedPenalty)
    {
        return baseMorale * currentSoldiers - retreatCount * retreatPenalty - surroundedBreakCount * surroundedPenalty;
    }

    public static float InitialEffectiveMorale(int baseMorale, int initialSoldiers) => baseMorale * initialSoldiers;

    // Shock lost per second: fraction x max(0, EffectiveMorale) / window.
    public static float DecayPerSecond(float effectiveMorale, float decayFractionPerWindow, float windowSeconds)
    {
        return decayFractionPerWindow * Math.Max(0f, effectiveMorale) / windowSeconds;
    }

    // Continuous decay, clamped at zero. Shock is never raised by this.
    public static float DecayShock(float shock, float effectiveMorale, float decayFractionPerWindow, float windowSeconds, float deltaTime)
    {
        return Math.Max(0f, shock - DecayPerSecond(effectiveMorale, decayFractionPerWindow, windowSeconds) * deltaTime);
    }

    // No break unless Shock is greater than EffectiveMorale. Then: below the rout threshold is a rout (A), otherwise a
    // retreat (B), or a surrounded break (C) when the retreat path is blocked.
    public static BreakCase Evaluate(float shock, float effectiveMorale, float initialEffectiveMorale, bool surrounded,
        float routThresholdFraction)
    {
        if (shock <= effectiveMorale) return BreakCase.None;
        if (effectiveMorale < routThresholdFraction * initialEffectiveMorale) return BreakCase.Rout;
        return surrounded ? BreakCase.SurroundedBreak : BreakCase.Retreat;
    }
}
