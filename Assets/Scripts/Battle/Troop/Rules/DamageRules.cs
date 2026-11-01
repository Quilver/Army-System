using System;

// Raw melee damage (spec §4.6). Pure C#.
//   Raw = Strength + WeaponDamageBonus + Domination
//   A Lance bearer's total is doubled while charging (Q-8). Domination is included in the doubled total.
// The result goes to MitigationResolver (Phase 02).
public static class DamageRules
{
    public static int RawDamage(int strength, int weaponDamageBonus, int domination, bool lanceDoublesWhileCharging)
    {
        int raw = Math.Max(0, strength) + Math.Max(0, weaponDamageBonus) + Math.Max(0, domination);
        return lanceDoublesWhileCharging ? raw * 2 : raw;
    }
}
