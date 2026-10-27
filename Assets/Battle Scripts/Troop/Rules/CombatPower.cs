// Combat Power at contact (spec §4.5). Pure C#.
//   CombatPower = WeaponSkill + ChargeBonus (only while charging) + SupportBonus
//   ChargeBonus = Charge + weapon ChargeBonus + AssaultChargeBonus (only while Assault)
// Both soldiers of a clash receive their own charge bonus when both are charging (Q-4).
public static class CombatPower
{
    public static int Compute(int weaponSkill, bool charging, int charge, int weaponChargeBonus, bool assault,
        int assaultChargeBonus, int supportBonus)
    {
        int power = weaponSkill + supportBonus;
        if (charging)
            power += charge + weaponChargeBonus + (assault ? assaultChargeBonus : 0);
        return power;
    }
}
