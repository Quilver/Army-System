// Limits for combat stats (spec §1.3). Every combat stat is an integer from 1 to 20 with a default of 5. HP is an
// integer from 10 to 40 with a default of 15. Inspector [Range] attributes must use the literal values, because C#
// attributes cannot read constants from another class in a readable way, so keep those in step with these constants.
// Pure C#, no Unity types.
public static class CombatStatLimits
{
    public const int Min = 1;
    public const int Max = 20;
    public const int Default = 5;

    public const int HpMin = 10;
    public const int HpMax = 40;
    public const int HpDefault = 15;

    public static int Clamp(int value) => System.Math.Max(Min, System.Math.Min(Max, value));

    public static int ClampHp(int value) => System.Math.Max(HpMin, System.Math.Min(HpMax, value));

    public static bool InRange(int value) => value >= Min && value <= Max;

    public static bool InHpRange(int value) => value >= HpMin && value <= HpMax;
}
