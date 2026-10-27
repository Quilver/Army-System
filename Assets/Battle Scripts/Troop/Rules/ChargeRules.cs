using System;

// Charge rate (spec §4.3). Pure C#. Each frame a soldier that has an enemy in range charges with probability
// 1 - exp(-rate x dt), where rate = EffectiveAggression / ActionWindowSeconds. Over a full window this gives an
// expected Aggression charges per soldier.
public static class ChargeRules
{
    public static float EffectiveAggression(int aggression, bool assault, int assaultAggressionBonus)
    {
        return aggression + (assault ? assaultAggressionBonus : 0);
    }

    public static float ChargeProbability(float effectiveAggression, float actionWindowSeconds, float deltaTime)
    {
        if (effectiveAggression <= 0f || deltaTime <= 0f || actionWindowSeconds <= 0f) return 0f;
        float rate = effectiveAggression / actionWindowSeconds;
        return 1f - (float)Math.Exp(-rate * deltaTime);
    }
}
