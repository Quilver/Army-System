// Weapon classes and special rules (spec §1.6). Kept free of Unity types so the classification can be tested directly.
// Spec v0.9 lists Spear, Sword, Axe, Bow and Javelin. Mace and Crossbow are added for the Phase 01 character sheets
// (see Docs/Battle/Decisions.md, D-03). Crossbow is classed as ranged.
//
// WeaponClass is the full list, used for weapon skills (a soldier's skill is per class, melee or ranged).
// MeleeWeaponClass and RangedWeaponClass are the classes each weapon type may use (Decisions D-52). Their values match
// WeaponClass, so a cast converts between them and the classes saved on weapon assets keep their meaning.
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

public enum MeleeWeaponClass
{
    Spear = WeaponClass.Spear,
    Sword = WeaponClass.Sword,
    Axe = WeaponClass.Axe,
    Mace = WeaponClass.Mace
}

public enum RangedWeaponClass
{
    Bow = WeaponClass.Bow,
    Javelin = WeaponClass.Javelin,
    Crossbow = WeaponClass.Crossbow
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

    public static bool IsMelee(this WeaponClass weaponClass) => !weaponClass.IsRanged();

    public static WeaponClass ToWeaponClass(this MeleeWeaponClass weaponClass) => (WeaponClass)weaponClass;
    public static WeaponClass ToWeaponClass(this RangedWeaponClass weaponClass) => (WeaponClass)weaponClass;

    public static MeleeWeaponClass ToMeleeClass(this WeaponClass weaponClass)
    {
        if (weaponClass.IsRanged())
            throw new System.ArgumentException($"{weaponClass} is a ranged weapon class, not a melee one.", nameof(weaponClass));
        return (MeleeWeaponClass)weaponClass;
    }

    public static RangedWeaponClass ToRangedClass(this WeaponClass weaponClass)
    {
        if (!weaponClass.IsRanged())
            throw new System.ArgumentException($"{weaponClass} is a melee weapon class, not a ranged one.", nameof(weaponClass));
        return (RangedWeaponClass)weaponClass;
    }
}
