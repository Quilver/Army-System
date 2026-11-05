using System.Collections.Generic;
using UnityEngine;

// A ranged weapon: Bow, Crossbow or Javelin (spec §1.6, Decisions D-52). Damage is the base damage per hit. A
// character may carry one ranged weapon in addition to its melee weapon.
[CreateAssetMenu(menuName = "Troops/Ranged Weapon")]
public sealed class RangedWeaponProfile : WeaponProfile
{
    [SerializeField] RangedWeaponClass _weaponClass = RangedWeaponClass.Bow;
    [SerializeField, Min(0f), Tooltip("Units.")] float _range;
    [SerializeField, Min(0), Tooltip("Added to the bearer's skill in the hit formula.")] int _accuracy;
    [SerializeField, Min(0), Tooltip("Shots per TroopTuning.ActionWindowSeconds.")] int _fireRate;

    public RangedWeaponClass RangedClass => _weaponClass;
    public override WeaponClass Class => _weaponClass.ToWeaponClass();
    public override bool IsRanged => true;
    public float Range => _range;
    public int Accuracy => _accuracy;
    public int FireRate => _fireRate;

    public static RangedWeaponProfile CreateRuntimeProfile(
        string displayName,
        RangedWeaponClass weaponClass,
        int damage,
        float range,
        int accuracy,
        int fireRate,
        bool provisional,
        IEnumerable<WeaponSpecialRuleEntry> specialRules = null)
    {
        var profile = CreateInstance<RangedWeaponProfile>();
        profile.SetBaseValues(displayName, damage, provisional);
        profile._weaponClass = weaponClass;
        profile._range = Mathf.Max(0f, range);
        profile._accuracy = Mathf.Max(0, accuracy);
        profile._fireRate = Mathf.Max(0, fireRate);
        profile.SetSpecialRules(specialRules);
        return profile;
    }
}
