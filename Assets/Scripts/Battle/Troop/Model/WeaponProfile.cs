using UnityEngine;

// Shared base for weapon stats (spec §1.6). A weapon is either a MeleeWeaponProfile (Sword, Axe, Spear, Mace) or a
// RangedWeaponProfile (Bow, Crossbow, Javelin); see Decisions D-52. All values are integers, except Range, which is in
// world units. Weapon values are not capped at 20 (only soldier stats are).
public abstract class WeaponProfile : ScriptableObject
{
    [SerializeField] string _displayName;
    [SerializeField, Min(0), Tooltip("Melee: damage bonus added to Strength. Ranged: base damage per hit.")] int _damage;
    [SerializeField, Tooltip("Values are not confirmed by the designer. Replace them before tuning.")] bool _provisional;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    // The class used to look up the bearer's weapon skill.
    public abstract WeaponClass Class { get; }
    public abstract bool IsRanged { get; }
    public int Damage => _damage;
    public bool Provisional => _provisional;

    protected void SetBaseValues(string displayName, int damage, bool provisional)
    {
        _displayName = displayName;
        _damage = Mathf.Max(0, damage);
        _provisional = provisional;
    }
}
