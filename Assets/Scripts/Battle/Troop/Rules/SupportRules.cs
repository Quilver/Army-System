using System;
using System.Collections.Generic;

// Support at a clash (spec §4.5, SU-149). Pure C#.
//   Eligible supporters: same army, not fatigued or stunned, within the supporter's range of the clashing soldier.
//   Support is a stat that gives: each supporter adds its own Support x SupportBonusPerSupportPoint. Only the
//   MaxSupporters eligible supporters with the highest Support count.
//   SupportBonus = round(sum of the counted supporters' Support x SupportBonusPerSupportPoint), half up.
//   A clash against a flanking enemy gives 0.
// Before SU-149 the clashing soldier's own Support capped the number of supporters, each worth 2.
public static class SupportRules
{
    // Supporter's range. A Spear Support weapon multiplies the range of the soldier carrying it (Q-7).
    public static float SupporterRange(float supportRadius, float spearSupportMultiplier, bool supporterHasSpearSupport)
    {
        return supporterHasSpearSupport ? supportRadius * spearSupportMultiplier : supportRadius;
    }

    public static bool InSupportRange(float distance, float range) => distance <= range;

    // supporterSupport: the Support stat of every eligible supporter, in any order. It is sorted in place (highest first).
    public static int SupportBonus(List<int> supporterSupport, int maxSupporters, float bonusPerSupportPoint, bool flanked)
    {
        if (flanked || supporterSupport == null || supporterSupport.Count == 0 || maxSupporters <= 0) return 0;
        supporterSupport.Sort(HighestFirst);
        int counted = Math.Min(maxSupporters, supporterSupport.Count);
        int total = 0;
        for (int i = 0; i < counted; i++) total += Math.Max(0, supporterSupport[i]);
        return RoundHalfUp(total * (double)Math.Max(0f, bonusPerSupportPoint));
    }

    static readonly Comparison<int> HighestFirst = (a, b) => b.CompareTo(a);

    static int RoundHalfUp(double value) => (int)Math.Floor(value + 0.5 + 1e-9);
}
