using System;

// Raw melee damage (spec §4.6). Pure C#.
//   Raw = Strength + WeaponDamageBonus + Domination
//   A Couched Lance bearer's total is doubled while it charges during its troop's Assault (Q-8, SU-148). Domination is
//   included in the doubled total.
// The result goes to MitigationResolver (Phase 02).
public static class DamageRules
{
    public static int RawDamage(int strength, int weaponDamageBonus, int domination, bool lanceDoubles)
    {
        int raw = Math.Max(0, strength) + Math.Max(0, weaponDamageBonus) + Math.Max(0, domination);
        return lanceDoubles ? raw * 2 : raw;
    }

    // Couched Lance doubles only on a charge made during the bearer's troop's Assault (SU-148): the first charges of an
    // ordered attack, not every charge of a long melee.
    public static bool LanceDoubles(bool hasCouchedLance, bool charging, bool assault) =>
        hasCouchedLance && charging && assault;
}
