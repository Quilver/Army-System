using System;

// Rolls incoming damage against armour (spec §4.6). Pure C#, no Unity types, so it can be checked outside Unity.
//   Each point up to TotalDefence rolls once: Deflect, Bruise or Bleed, using the armour split.
//   Points above TotalDefence become Overflow (bleed, no shock).
//   Without armour there is no split and no roll: every point is Bleed (decision 7).
public static class MitigationResolver
{
    // Damage against a soldier with no armour. Nothing is deflected or bruised.
    public static MitigationResult Unarmoured(int rawDamage) =>
        new MitigationResult(0, 0, Math.Max(0, rawDamage), 0);

    // deflectPercent and bruisePercent are the armour split; bleed is the remainder (100 - deflect - bruise).
    public static MitigationResult Resolve(int rawDamage, int totalDefence, int deflectPercent, int bruisePercent, ITroopRandom random)
    {
        if (rawDamage <= 0) return new MitigationResult(0, 0, 0, 0);

        int rolls = Math.Min(rawDamage, Math.Max(0, totalDefence));
        int overflow = rawDamage - rolls;
        float deflectShare = Clamp01(deflectPercent / 100f);
        float bruiseShare = Clamp01((Clamp(deflectPercent, 0, 100) + Clamp(bruisePercent, 0, 100)) / 100f);

        int deflected = 0;
        int bruised = 0;
        int bled = 0;
        for (int i = 0; i < rolls; i++)
        {
            float roll = random.Value();
            if (roll < deflectShare) deflected++;
            else if (roll < bruiseShare) bruised++;
            else bled++;
        }
        return new MitigationResult(deflected, bruised, bled, overflow);
    }

    static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
