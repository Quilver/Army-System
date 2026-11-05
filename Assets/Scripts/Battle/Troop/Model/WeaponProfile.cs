using System.Collections.Generic;
using UnityEngine;

// Shared base for weapon stats (spec §1.6). A weapon is either a MeleeWeaponProfile (Sword, Axe, Spear, Mace) or a
// RangedWeaponProfile (Bow, Crossbow, Javelin); see Decisions D-52. All values are integers, except Range, which is in
// world units. Weapon values are not capped at 20 (only soldier stats are).
//
// Special rules (SU-90) are a list, on melee and ranged weapons alike: Spear Support and Couched Lance (melee), Piercing X
// and Bludgeoning X (both), Fire & Engage (ranged). See WeaponSpecialRule. A rule on the wrong kind of weapon has no
// effect and logs a warning.
public abstract class WeaponProfile : ScriptableObject
{
    [SerializeField] string _displayName;
    [SerializeField, Min(0), Tooltip("Melee: damage bonus added to Strength. Ranged: base damage per hit.")] int _damage;
    [SerializeField, Tooltip("Special rules. Value is X for Piercing X and Bludgeoning X; the other rules ignore it.")]
    List<WeaponSpecialRuleEntry> _specialRules = new();
    [SerializeField, Tooltip("Values are not confirmed by the designer. Replace them before tuning.")] bool _provisional;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    // The class used to look up the bearer's weapon skill.
    public abstract WeaponClass Class { get; }
    public abstract bool IsRanged { get; }
    public int Damage => _damage;
    public bool Provisional => _provisional;
    public IReadOnlyList<WeaponSpecialRuleEntry> SpecialRules => _specialRules;

    // Whether the weapon has the rule and the rule applies to this kind of weapon (melee or ranged).
    public bool HasRule(WeaponSpecialRule rule)
    {
        if (!rule.AppliesTo(IsRanged)) return false;
        foreach (var entry in _specialRules)
            if (entry.Rule == rule) return true;
        return false;
    }

    // X of a valued rule (Piercing X, Bludgeoning X). Entries of the same rule add up. 0 without the rule.
    public int RuleValue(WeaponSpecialRule rule)
    {
        if (!rule.AppliesTo(IsRanged)) return 0;
        int total = 0;
        foreach (var entry in _specialRules)
            if (entry.Rule == rule) total += entry.Value;
        return total;
    }

    public int Piercing => RuleValue(WeaponSpecialRule.Piercing);
    public int Bludgeoning => RuleValue(WeaponSpecialRule.Bludgeoning);

    protected void SetBaseValues(string displayName, int damage, bool provisional)
    {
        _displayName = displayName;
        _damage = Mathf.Max(0, damage);
        _provisional = provisional;
    }

    protected void SetSpecialRules(IEnumerable<WeaponSpecialRuleEntry> rules)
    {
        _specialRules = new List<WeaponSpecialRuleEntry>();
        if (rules == null) return;
        foreach (var entry in rules)
            if (entry.Rule != WeaponSpecialRule.None) _specialRules.Add(entry);
    }

    protected virtual void OnValidate()
    {
        if (_specialRules == null) return;
        foreach (var entry in _specialRules)
        {
            if (entry.Rule == WeaponSpecialRule.None) continue;
            if (!entry.Rule.AppliesTo(IsRanged))
                Debug.LogWarning($"{name}: {entry.Rule.DisplayName()} has no effect on a {(IsRanged ? "ranged" : "melee")} weapon.", this);
            else if (entry.Rule.TakesValue() && entry.Value <= 0)
                Debug.LogWarning($"{name}: {entry.Rule.DisplayName()} has X = 0, so it has no effect.", this);
        }
    }
}
