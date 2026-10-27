using System;

// Support at a clash (spec §4.5). Pure C#.
//   Eligible: same army, not fatigued, within the supporter's range of the clashing soldier.
//   SupportingSoldiers = min(eligible, the clashing soldier's Support stat)
//   SupportBonus = SupportingSoldiers x SupportBonusPerAlly
//   A clash against a flanking enemy gives 0 (face data supplied from Phase 05).
public static class SupportRules
{
    // Supporter's range. A Boar Spear multiplies the range of the soldier carrying it (Q-7).
    public static float SupporterRange(float supportRadius, float boarSpearMultiplier, bool supporterHasBoarSpear)
    {
        return supporterHasBoarSpear ? supportRadius * boarSpearMultiplier : supportRadius;
    }

    public static bool InSupportRange(float distance, float range) => distance <= range;

    public static int SupportBonus(int eligibleSupporters, int maxSupportingAllies, int bonusPerAlly, bool flanked)
    {
        if (flanked) return 0;
        int counted = Math.Min(Math.Max(0, eligibleSupporters), Math.Max(0, maxSupportingAllies));
        return counted * bonusPerAlly;
    }
}
