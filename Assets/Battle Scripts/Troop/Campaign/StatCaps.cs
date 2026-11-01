using System;
using System.Collections.Generic;
using UnityEngine;

// The highest value each stat can grow to on level-ups. Held by a CharacterClass.
// - Combat stats default to the stat maximum (20) and are kept within 1-20.
// - MaxHealth uses the HP scale (10-40), so its cap defaults to the HP maximum (40); a cap of 20 there would sit below
//   several existing characters' HP.
// - Numbers has no upper limit of its own; its cap defaults to 20.
// - Weapon skills are capped per weapon class by a list, which starts with one entry (Sword, 20). A weapon class the
//   list does not name uses the default cap (20). Name each class at most once.
[Serializable]
public sealed class StatCaps
{
    public const int DefaultCap = CombatStatLimits.Max;
    public const int DefaultHpCap = CombatStatLimits.HpMax;
    public const int DefaultNumbersCap = 20;

    [SerializeField, Min(0), Tooltip("Cap on Numbers (soldiers brought into battle).")] int _numbers = DefaultNumbersCap;
    [SerializeField, Range(1, 20)] int _speed = DefaultCap;
    [SerializeField, Range(1, 20)] int _defence = DefaultCap;
    [SerializeField, Range(1, 20)] int _strength = DefaultCap;
    [SerializeField, Range(1, 20)] int _support = DefaultCap;
    [SerializeField, Range(1, 20)] int _charge = DefaultCap;
    [SerializeField, Range(1, 20)] int _aggression = DefaultCap;
    [SerializeField, Range(10, 40), Tooltip("Cap on MaxHP (HP scale, 10-40).")] int _maxHealth = DefaultHpCap;
    [SerializeField, Range(1, 20)] int _morale = DefaultCap;
    [SerializeField, Range(1, 20)] int _discipline = DefaultCap;
    [SerializeField, Tooltip("Cap per weapon class. A class not listed uses the default cap (20).")]
    List<WeaponSkillCap> _weaponSkills = new() { new WeaponSkillCap(WeaponClass.Sword, DefaultCap) };

    public IReadOnlyList<WeaponSkillCap> WeaponSkills => _weaponSkills;

    // Cap of a stat. Weapon skills are capped per class: use ForWeapon.
    public int For(CharacterStat stat) => stat switch
    {
        CharacterStat.Numbers => _numbers,
        CharacterStat.Speed => _speed,
        CharacterStat.Defence => _defence,
        CharacterStat.Strength => _strength,
        CharacterStat.Support => _support,
        CharacterStat.Charge => _charge,
        CharacterStat.Aggression => _aggression,
        CharacterStat.MaxHealth => _maxHealth,
        CharacterStat.Morale => _morale,
        CharacterStat.Discipline => _discipline,
        CharacterStat.WeaponSkill => throw new ArgumentException(
            "Weapon skills are capped per weapon class. Use ForWeapon(WeaponClass).", nameof(stat)),
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    // Cap of the skill with a weapon class: the listed cap, or the default cap when the class is not listed.
    public int ForWeapon(WeaponClass weaponClass)
    {
        foreach (var cap in _weaponSkills)
            if (cap.Class == weaponClass) return cap.Cap;
        return DefaultCap;
    }

    // The cap used when a character has no class.
    public static int DefaultFor(CharacterStat stat) => stat switch
    {
        CharacterStat.MaxHealth => DefaultHpCap,
        CharacterStat.Numbers => DefaultNumbersCap,
        _ => DefaultCap,
    };

    // Called from CharacterClass.OnValidate. Keeps every cap within its stat's range and warns about a weapon class
    // listed twice (only the first entry is used).
    internal void Validate(UnityEngine.Object context)
    {
        _numbers = Math.Max(0, _numbers);
        _speed = CombatStatLimits.Clamp(_speed);
        _defence = CombatStatLimits.Clamp(_defence);
        _strength = CombatStatLimits.Clamp(_strength);
        _support = CombatStatLimits.Clamp(_support);
        _charge = CombatStatLimits.Clamp(_charge);
        _aggression = CombatStatLimits.Clamp(_aggression);
        _maxHealth = CombatStatLimits.ClampHp(_maxHealth);
        _morale = CombatStatLimits.Clamp(_morale);
        _discipline = CombatStatLimits.Clamp(_discipline);

        _weaponSkills ??= new List<WeaponSkillCap>();
        var seen = new HashSet<WeaponClass>();
        for (int i = 0; i < _weaponSkills.Count; i++)
        {
            WeaponSkillCap cap = _weaponSkills[i];
            int clamped = CombatStatLimits.Clamp(cap.Cap);
            if (clamped != cap.Cap)
            {
                cap.Cap = clamped;
                _weaponSkills[i] = cap;
            }
            if (!seen.Add(cap.Class))
                Debug.LogWarning($"{context.name}: {cap.Class} is listed more than once in the weapon skill caps; " +
                                 "only the first is used.", context);
        }
    }
}

// The cap on a character's skill with one weapon class (integer 1-20).
[Serializable]
public struct WeaponSkillCap
{
    public WeaponClass Class;
    [Range(1, 20)] public int Cap;

    public WeaponSkillCap(WeaponClass weaponClass, int cap)
    {
        Class = weaponClass;
        Cap = CombatStatLimits.Clamp(cap);
    }
}
