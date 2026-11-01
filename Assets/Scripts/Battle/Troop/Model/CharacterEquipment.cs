using System;
using System.Collections.Generic;
using UnityEngine;

// What every soldier of a character carries (spec §1.6, Decisions D-52): one melee weapon (required), at most one
// ranged weapon, and one armour.
[Serializable]
public sealed class CharacterEquipment
{
    [SerializeField, Tooltip("Required. Sword, Axe, Spear or Mace.")] MeleeWeaponProfile _meleeWeapon;
    [SerializeField, Tooltip("Optional. Bow, Crossbow or Javelin.")] RangedWeaponProfile _rangedWeapon;
    [SerializeField] ArmourProfile _armour;

    // Legacy list from before the melee/ranged split. Emptied by WeaponProfileMigration (Editor).
    [SerializeField, HideInInspector] List<WeaponProfile> _weapons = new();

    public MeleeWeaponProfile MeleeWeapon => _meleeWeapon;
    public RangedWeaponProfile RangedWeapon => _rangedWeapon;
    public ArmourProfile Armour => _armour;
    public bool HasMeleeWeapon => _meleeWeapon != null;

    public CharacterEquipment() { }

    public CharacterEquipment(MeleeWeaponProfile meleeWeapon, RangedWeaponProfile rangedWeapon, ArmourProfile armour)
    {
        _meleeWeapon = meleeWeapon != null
            ? meleeWeapon
            : throw new ArgumentNullException(nameof(meleeWeapon), "Every character must carry a melee weapon (spec §1.6).");
        _rangedWeapon = rangedWeapon;
        _armour = armour;
    }

    // Called from CharacterProfile.OnValidate.
    internal void Validate(UnityEngine.Object context)
    {
        if (_weapons != null && _weapons.Count > 0)
            Debug.LogWarning($"{context.name}: still uses the old weapon list. Run Troops > Migrate > Split Weapon Profiles.",
                context);
        else if (_meleeWeapon == null)
            Debug.LogError($"{context.name}: has no melee weapon. Every character must carry one (spec §1.6).", context);
    }
}
