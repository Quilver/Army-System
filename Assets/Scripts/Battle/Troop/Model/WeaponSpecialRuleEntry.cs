using System;
using UnityEngine;

// One special rule on a weapon (spec §1.6, SU-90). Value is X for the rules that take one (Piercing X, Bludgeoning X)
// and is ignored by the others.
[Serializable]
public struct WeaponSpecialRuleEntry
{
    [SerializeField] WeaponSpecialRule _rule;
    [SerializeField, Min(0), Tooltip("X for Piercing X and Bludgeoning X. Ignored by the other rules.")] int _value;

    public WeaponSpecialRuleEntry(WeaponSpecialRule rule, int value = 0)
    {
        _rule = rule;
        _value = Math.Max(0, value);
    }

    public WeaponSpecialRule Rule => _rule;
    public int Value => _value;

    public override string ToString() => _rule.TakesValue() ? _rule.DisplayName() + " " + _value : _rule.DisplayName();
}
