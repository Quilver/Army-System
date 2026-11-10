using System;

// Result of one duel roll (spec §4.5).
public readonly struct DuelOutcome
{
    public readonly bool AWins;
    public readonly int Domination;

    public DuelOutcome(bool aWins, int domination)
    {
        AWins = aWins;
        Domination = domination;
    }
}

// The duel between two soldiers at contact (spec §4.5). Pure C#.
//   P_A = CP_A / (CP_A + CP_B). Roll R in [0, 100]. A wins if R <= P_A x 100.
//   Domination = round(|R - P_A x 100| / DominationDivisor), rounded half up (1.5 gives 2, 2.5 gives 3).
//   The winner adds Domination to its raw damage, whichever side wins.
public static class DuelRules
{
    public static float ChargerWinChance(float powerA, float powerB)
    {
        float total = powerA + powerB;
        return total <= 0f ? 0.5f : powerA / total;
    }

    // P_A as a percentage, computed in double and rounded to 1e-6 so 60 / 100 is exactly 60 (no float drift at the
    // rounding boundaries of Domination).
    public static double ChargerWinPercent(float powerA, float powerB)
    {
        double total = (double)powerA + powerB;
        double percent = total <= 0.0 ? 50.0 : powerA * 100.0 / total;
        return Math.Round(percent, 6);
    }

    public static DuelOutcome Resolve(float powerA, float powerB, float roll, float dominationDivisor)
    {
        double percentA = ChargerWinPercent(powerA, powerB);
        double r = Math.Round((double)roll, 6);
        bool aWins = r <= percentA;
        double divisor = dominationDivisor > 0f ? dominationDivisor : 10.0;
        int domination = (int)Math.Floor(Math.Abs(r - percentA) / divisor + 0.5 + 1e-9);
        return new DuelOutcome(aWins, domination);
    }

    // Labels: A is the charger. If both charge, the earlier charge is A. If neither charges, the lower soldier ID is A.
    // Returns true when the first soldier is A.
    public static bool FirstIsA(bool firstCharging, float firstChargeStart, int firstId,
        bool secondCharging, float secondChargeStart, int secondId)
    {
        if (firstCharging != secondCharging) return firstCharging;
        if (firstCharging) return firstChargeStart <= secondChargeStart;
        return firstId <= secondId;
    }
}
