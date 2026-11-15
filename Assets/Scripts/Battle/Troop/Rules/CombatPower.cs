// Combat Power at contact (spec §4.5). Pure C#.
//   CombatPower = WeaponSkill + ChargeBonus (only while charging) + SupportBonus
//   ChargeBonus = Charge + weapon ChargeBonus (the Assault adds nothing since SU-165)
// Both soldiers of a clash receive their own charge bonus when both are charging (Q-4).
public static class CombatPower
{
    public static int Compute(int weaponSkill, bool charging, int charge, int weaponChargeBonus, int supportBonus)
    {
        int power = weaponSkill + supportBonus;
        if (charging) power += charge + weaponChargeBonus;
        return power;
    }
}
