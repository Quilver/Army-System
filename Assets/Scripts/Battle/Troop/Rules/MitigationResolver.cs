using System;

// Rolls incoming damage against armour (spec §4.6, SU-89, SU-90). Pure C#, no Unity types, so it can be checked outside
// Unity.
//   Each point up to TotalDefence rolls once: Deflect, Bruise or Bleed, weighted by the armour's Deflect : Bruise : Bleed
//   ratio. The attacking weapon's Piercing is added to the Bleed part and its Bludgeoning to the Bruise part first.
//   Points above TotalDefence become Overflow (bleed, no shock).
//   Without armour every point rolls (no TotalDefence cap, no Overflow, decision 7) against the unarmoured ratio
//   0 : 3 : 7, so Piercing and Bludgeoning still apply (SU-90).
public static class MitigationResolver
{
    // The Deflect : Bruise : Bleed ratio of a soldier with no armour (SU-90).
    public const int UnarmouredDeflect = 0;
    public const int UnarmouredBruise = 3;
    public const int UnarmouredBleed = 7;
    public static string UnarmouredRatioText => UnarmouredDeflect + " : " + UnarmouredBruise + " : " + UnarmouredBleed;

    // Damage against a soldier with no armour: every point rolls against 0 : 3 : 7 plus the weapon's abilities.
    public static MitigationResult Unarmoured(int rawDamage, ITroopRandom random, int piercing = 0, int bludgeoning = 0)
    {
        if (rawDamage <= 0) return new MitigationResult(0, 0, 0, 0);
        Roll(rawDamage, UnarmouredDeflect, UnarmouredBruise, UnarmouredBleed, random, piercing, bludgeoning,
            out int deflected, out int bruised, out int bled);
        return new MitigationResult(deflected, bruised, bled, 0);
    }

    // deflect, bruise and bleed are the armour ratio parts. piercing is added to bleed and bludgeoning to bruise (the
    // attacking weapon's abilities, 0 when it has none). Negative values count as 0. If every part is 0, every rolled
    // point is Bleed.
    public static MitigationResult Resolve(int rawDamage, int totalDefence, int deflect, int bruise, int bleed,
        ITroopRandom random, int piercing = 0, int bludgeoning = 0)
    {
        if (rawDamage <= 0) return new MitigationResult(0, 0, 0, 0);

        int rolls = Math.Min(rawDamage, Math.Max(0, totalDefence));
        int overflow = rawDamage - rolls;
        Roll(rolls, deflect, bruise, bleed, random, piercing, bludgeoning, out int deflected, out int bruised, out int bled);
        return new MitigationResult(deflected, bruised, bled, overflow);
    }

    static void Roll(int rolls, int deflect, int bruise, int bleed, ITroopRandom random, int piercing, int bludgeoning,
        out int deflected, out int bruised, out int bled)
    {
        deflected = 0;
        bruised = 0;
        bled = 0;
        int deflectWeight = Math.Max(0, deflect);
        int bruiseWeight = Math.Max(0, bruise) + Math.Max(0, bludgeoning);
        int bleedWeight = Math.Max(0, bleed) + Math.Max(0, piercing);
        int total = deflectWeight + bruiseWeight + bleedWeight;
        if (total <= 0)
        {
            bled = Math.Max(0, rolls);
            return;
        }

        float deflectShare = deflectWeight / (float)total;
        float bruiseShare = (deflectWeight + bruiseWeight) / (float)total;
        for (int i = 0; i < rolls; i++)
        {
            float roll = random.Value();
            if (roll < deflectShare) deflected++;
            else if (roll < bruiseShare) bruised++;
            else bled++;
        }
    }
}
