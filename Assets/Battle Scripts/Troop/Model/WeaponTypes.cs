// Weapon classes and special rules (spec §1.6). Kept free of Unity types so the classification can be tested directly.
// Spec v0.9 lists Spear, Sword, Axe, Bow and Javelin. Mace and Crossbow are added for the Phase 01 character sheets
// (see Docs/Battle/Decisions.md, D-03). Crossbow is classed as ranged.
public enum WeaponClass
{
    Spear,
    Sword,
    Axe,
    Bow,
    Javelin,
    Mace,
    Crossbow
}

public enum WeaponSpecialRule
{
    None,
    // Doubles the bearer's support range when the bearer supports an ally (spec §1.6, Boar Spear).
    BoarSpearSupport,
    // Doubles total melee damage while the bearer is charging (spec §1.6, Lance).
    LanceChargeDouble
}

public static class WeaponClassExtensions
{
    public static bool IsRanged(this WeaponClass weaponClass)
    {
        return weaponClass == WeaponClass.Bow
            || weaponClass == WeaponClass.Javelin
            || weaponClass == WeaponClass.Crossbow;
    }
}
