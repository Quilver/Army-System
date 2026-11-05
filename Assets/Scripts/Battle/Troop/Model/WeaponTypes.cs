using UnityEngine;

// Weapon classes and special rules (spec §1.6). Unity is used only for the Inspector names of the special rules.
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

// Weapon special rules (spec §1.6, SU-90). A weapon carries a list of them (WeaponProfile.SpecialRules). The values are
// saved on weapon assets, so keep each rule's number when renaming or adding rules.
public enum WeaponSpecialRule
{
    // Placeholder for a new, unset list entry. Has no effect.
    None = 0,
    // Melee. Doubles the bearer's support range when the bearer supports an ally (spec §1.6; was Boar Spear Support).
    [InspectorName("Spear Support")] SpearSupport = 1,
    // Melee. Doubles total melee damage while the bearer is charging (spec §1.6; was Lance Charge Double).
    [InspectorName("Couched Lance")] CouchedLance = 2,
    // Melee or ranged. Piercing X: adds X to the Bleed part of the target's armour ratio when this weapon's damage is
    // rolled (SU-89).
    [InspectorName("Piercing")] Piercing = 3,
    // Melee or ranged. Bludgeoning X: adds X to the Bruise part of the target's armour ratio when this weapon's damage
    // is rolled (SU-89).
    [InspectorName("Bludgeoning")] Bludgeoning = 4,
    // Ranged. The bearer shoots once when its troop enters an engagement and once when it retreats from that
    // engagement, even though troops do not normally fire while engaged or retreating (SU-90).
    [InspectorName("Fire & Engage")] FireAndEngage = 5
}

public static class WeaponSpecialRuleExtensions
{
    public static string DisplayName(this WeaponSpecialRule rule) => rule switch
    {
        WeaponSpecialRule.SpearSupport => "Spear Support",
        WeaponSpecialRule.CouchedLance => "Couched Lance",
        WeaponSpecialRule.Piercing => "Piercing",
        WeaponSpecialRule.Bludgeoning => "Bludgeoning",
        WeaponSpecialRule.FireAndEngage => "Fire & Engage",
        _ => "None"
    };

    // Rules with a value X (Piercing X, Bludgeoning X).
    public static bool TakesValue(this WeaponSpecialRule rule) =>
        rule == WeaponSpecialRule.Piercing || rule == WeaponSpecialRule.Bludgeoning;

    // Whether the rule has an effect on a melee (ranged = false) or ranged (ranged = true) weapon.
    public static bool AppliesTo(this WeaponSpecialRule rule, bool ranged) => rule switch
    {
        WeaponSpecialRule.SpearSupport => !ranged,
        WeaponSpecialRule.CouchedLance => !ranged,
        WeaponSpecialRule.Piercing => true,
        WeaponSpecialRule.Bludgeoning => true,
        WeaponSpecialRule.FireAndEngage => ranged,
        _ => false
    };
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
