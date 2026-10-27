using UnityEngine;

// A weapon's stats (spec §1.6). All values are integers, except Range, which is in world units. For melee weapons,
// Damage is the damage bonus added to Strength. For ranged weapons, Damage is the base damage per hit. Weapon values
// are not capped at 20 (only soldier stats are).
[CreateAssetMenu(menuName = "Troops/Weapon Profile")]
public sealed class WeaponProfile : ScriptableObject
{
    [SerializeField] string _displayName;
    [SerializeField] WeaponClass _weaponClass = WeaponClass.Sword;
    [SerializeField, Min(0), Tooltip("Melee: damage bonus added to Strength. Ranged: base damage per hit.")] int _damage;
    [SerializeField, Min(0), Tooltip("Melee only. Added to Charge while the bearer is charging.")] int _chargeBonus;
    [SerializeField, Min(0f), Tooltip("Ranged only. Units.")] float _range;
    [SerializeField, Min(0), Tooltip("Ranged only. Added to the bearer's skill in the hit formula.")] int _accuracy;
    [SerializeField, Min(0), Tooltip("Ranged only. Shots per TroopTuning.ActionWindowSeconds.")] int _fireRate;
    [SerializeField] WeaponSpecialRule _specialRule = WeaponSpecialRule.None;
    [SerializeField, Tooltip("Values are not confirmed by the designer. Replace them before tuning.")] bool _provisional;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public WeaponClass Class => _weaponClass;
    public bool IsRanged => _weaponClass.IsRanged();
    public int Damage => _damage;
    public int ChargeBonus => _chargeBonus;
    public float Range => _range;
    public int Accuracy => _accuracy;
    public int FireRate => _fireRate;
    public WeaponSpecialRule SpecialRule => _specialRule;
    public bool Provisional => _provisional;

    public static WeaponProfile CreateRuntimeProfile(
        string displayName,
        WeaponClass weaponClass,
        int damage,
        int chargeBonus,
        float range,
        int accuracy,
        int fireRate,
        WeaponSpecialRule specialRule,
        bool provisional)
    {
        var profile = CreateInstance<WeaponProfile>();
        profile._displayName = displayName;
        profile._weaponClass = weaponClass;
        profile._damage = Mathf.Max(0, damage);
        profile._chargeBonus = Mathf.Max(0, chargeBonus);
        profile._range = Mathf.Max(0f, range);
        profile._accuracy = Mathf.Max(0, accuracy);
        profile._fireRate = Mathf.Max(0, fireRate);
        profile._specialRule = specialRule;
        profile._provisional = provisional;
        return profile;
    }
}
