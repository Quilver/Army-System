using System.Collections.Generic;
using UnityEngine;

// A melee weapon: Sword, Axe, Spear or Mace (spec §1.6, Decisions D-52). Damage is the damage bonus added to Strength.
// Every character carries exactly one melee weapon.
//
// This script keeps the GUID of the old combined WeaponProfile script, so every old weapon asset loads as a melee
// weapon. Old ranged assets are converted to RangedWeaponProfile by WeaponProfileMigration (Editor). The hidden
// _range, _accuracy and _fireRate fields exist only so that migration can read the old ranged values.
[CreateAssetMenu(menuName = "Troops/Melee Weapon")]
public sealed class MeleeWeaponProfile : WeaponProfile
{
    [SerializeField] MeleeWeaponClass _weaponClass = MeleeWeaponClass.Sword;
    [SerializeField, Min(0), Tooltip("Added to Charge while the bearer is charging.")] int _chargeBonus;

    // Legacy ranged values from the combined WeaponProfile. Read by WeaponProfileMigration only.
    [SerializeField, HideInInspector] float _range;
    [SerializeField, HideInInspector] int _accuracy;
    [SerializeField, HideInInspector] int _fireRate;

    public MeleeWeaponClass MeleeClass => _weaponClass;
    public override WeaponClass Class => _weaponClass.ToWeaponClass();
    public override bool IsRanged => false;
    public int ChargeBonus => _chargeBonus;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (((WeaponClass)_weaponClass).IsRanged() || !System.Enum.IsDefined(typeof(MeleeWeaponClass), _weaponClass))
            Debug.LogWarning($"{name}: weapon class {(WeaponClass)_weaponClass} is not a melee class. Run " +
                             "Troops > Migrate > Split Weapon Profiles to convert it to a ranged weapon.", this);
    }

    public static MeleeWeaponProfile CreateRuntimeProfile(
        string displayName,
        MeleeWeaponClass weaponClass,
        int damage,
        int chargeBonus,
        IEnumerable<WeaponSpecialRuleEntry> specialRules)
    {
        var profile = CreateInstance<MeleeWeaponProfile>();
        profile.SetBaseValues(displayName, damage);
        profile._weaponClass = weaponClass;
        profile._chargeBonus = Mathf.Max(0, chargeBonus);
        profile.SetSpecialRules(specialRules);
        return profile;
    }
}
