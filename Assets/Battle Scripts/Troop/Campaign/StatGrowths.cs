using System;
using UnityEngine;

// A campaign character's growth chances: the % chance that each stat goes up by 1 on a level-up (default 20 %).
// Integer 0-100. Read with For(CharacterStat).
[Serializable]
public sealed class StatGrowths
{
    public const int MinChance = 0;
    public const int MaxChance = 100;
    public const int DefaultChance = 20;

    [SerializeField, Range(0, 100), Tooltip("Chance that Numbers (soldiers) goes up on a level-up.")]
    int _numbers = DefaultChance;
    [SerializeField, Range(0, 100)] int _speed = DefaultChance;
    [SerializeField, Range(0, 100)] int _defence = DefaultChance;
    [SerializeField, Range(0, 100)] int _strength = DefaultChance;
    [SerializeField, Range(0, 100)] int _support = DefaultChance;
    [SerializeField, Range(0, 100)] int _charge = DefaultChance;
    [SerializeField, Range(0, 100)] int _aggression = DefaultChance;
    [SerializeField, Range(0, 100), Tooltip("Chance that MaxHP goes up on a level-up.")] int _maxHealth = DefaultChance;
    [SerializeField, Range(0, 100)] int _morale = DefaultChance;
    [SerializeField, Range(0, 100)] int _discipline = DefaultChance;
    [SerializeField, Range(0, 100), Tooltip("Rolled separately for each listed weapon skill.")]
    int _weaponSkill = DefaultChance;

    // Growth chance (%) of a stat.
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
        CharacterStat.WeaponSkill => _weaponSkill,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    };

    public static int ClampChance(int value) => Math.Max(MinChance, Math.Min(MaxChance, value));

    // Called from CampaignCharacter.OnValidate. Keeps every chance within 0-100.
    internal void Validate()
    {
        _numbers = ClampChance(_numbers);
        _speed = ClampChance(_speed);
        _defence = ClampChance(_defence);
        _strength = ClampChance(_strength);
        _support = ClampChance(_support);
        _charge = ClampChance(_charge);
        _aggression = ClampChance(_aggression);
        _maxHealth = ClampChance(_maxHealth);
        _morale = ClampChance(_morale);
        _discipline = ClampChance(_discipline);
        _weaponSkill = ClampChance(_weaponSkill);
    }
}
