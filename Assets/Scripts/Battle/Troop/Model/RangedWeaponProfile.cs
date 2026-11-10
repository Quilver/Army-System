using System.Collections.Generic;
using UnityEngine;

// A ranged weapon: Bow, Crossbow or Javelin (spec §1.6, Decisions D-52). Damage is the base damage per hit. A
// character may carry one ranged weapon in addition to its melee weapon.
// ArcedShot (SU-136): an arced weapon shoots over troops and terrain; a direct one (the default) needs a clear line of
// sight to the soldier it shoots at (RangedFireController).
[CreateAssetMenu(menuName = "Troops/Ranged Weapon")]
public sealed class RangedWeaponProfile : WeaponProfile
{
    [SerializeField] RangedWeaponClass _weaponClass = RangedWeaponClass.Bow;
    [SerializeField, Min(0f), Tooltip("Units.")] float _range;
    [SerializeField, Range(0, MaxValue), Tooltip("Added to the bearer's skill in the hit formula. 0 to 20 (SU-137).")]
    int _accuracy;
    [SerializeField, Range(0, MaxValue), Tooltip("Shots per TroopTuning.ActionWindowSeconds. 0 to 20 (SU-137).")]
    int _fireRate;
    [SerializeField, Tooltip("Volley fire (SU-105): the troop's soldiers with this weapon shoot in sync, all at once. Off: " +
        "each soldier shoots on its own timing.")] bool _volley;
    [SerializeField, Tooltip("Arced shot (SU-136): on, the weapon shoots over troops and terrain. Off (direct fire), a " +
        "shot needs a straight line to its target with no other troop's soldier and no sight-blocking terrain " +
        "(Ranged Tuning, Sight Blocking Layers) in the way.")] bool _arcedShot;

    public RangedWeaponClass RangedClass => _weaponClass;
    public override WeaponClass Class => _weaponClass.ToWeaponClass();
    public override bool IsRanged => true;
    public float Range => _range;
    public int Accuracy => ClampValue(_accuracy);
    public int FireRate => ClampValue(_fireRate);
    // Volley fire (SU-105): soldiers with this weapon shoot together on the troop's clock instead of each on its own.
    public bool Volley => _volley;
    // Arced shot (SU-136): ignores obstructions. Direct fire (false) needs a clear line of sight.
    public bool ArcedShot => _arcedShot;

    protected override void OnValidate()
    {
        base.OnValidate();
        _accuracy = ClampValue(_accuracy);
        _fireRate = ClampValue(_fireRate);
    }

    public static RangedWeaponProfile CreateRuntimeProfile(
        string displayName,
        RangedWeaponClass weaponClass,
        int damage,
        float range,
        int accuracy,
        int fireRate,
        IEnumerable<WeaponSpecialRuleEntry> specialRules = null,
        bool volley = false,
        bool arcedShot = false)
    {
        var profile = CreateInstance<RangedWeaponProfile>();
        profile.SetBaseValues(displayName, damage);
        profile._weaponClass = weaponClass;
        profile._range = Mathf.Max(0f, range);
        profile._accuracy = ClampValue(accuracy);
        profile._fireRate = ClampValue(fireRate);
        profile._volley = volley;
        profile._arcedShot = arcedShot;
        profile.SetSpecialRules(specialRules);
        return profile;
    }
}
