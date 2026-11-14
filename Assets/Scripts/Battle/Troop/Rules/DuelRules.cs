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
//   Elo duel (SU-153, Melee Tuning toggle): P_A = 1 / (1 + 10^((CP_B - CP_A) / EloScale)), so a Combat Power difference
//   gives the same odds at any level (EloScale 30: 30 points ahead wins 10 to 1).
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

    // Elo duel (SU-153): P_A from the Combat Power difference, as a percentage rounded to 1e-6.
    public static double EloWinPercent(float powerA, float powerB, float eloScale)
    {
        double scale = eloScale > 0f ? eloScale : 30.0;
        double percent = 100.0 / (1.0 + Math.Pow(10.0, (powerB - (double)powerA) / scale));
        return Math.Round(percent, 6);
    }

    // P_A in percent by the active rule: Elo when elo is on, the ratio otherwise.
    public static double WinPercent(float powerA, float powerB, bool elo, float eloScale) =>
        elo ? EloWinPercent(powerA, powerB, eloScale) : ChargerWinPercent(powerA, powerB);

    public static DuelOutcome Resolve(float powerA, float powerB, float roll, float dominationDivisor, bool elo = false,
        float eloScale = 30f)
    {
        double percentA = WinPercent(powerA, powerB, elo, eloScale);
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
