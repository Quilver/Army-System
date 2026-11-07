using System;
using System.Collections.Generic;
using UnityEngine;

// Soldier-level stats of one character's soldier type (spec §1.5). Every soldier the character brings into battle uses
// these values. All combat stats are integers 1-20 (default 5), and HP is an integer 10-40 (default 15). See
// CombatStatLimits. The [Range] values below repeat CombatStatLimits; keep them in step.
[Serializable]
public sealed class SoldierStats
{
    [SerializeField, Range(1, 20), Tooltip("Individual speed along the troop's vector field. Troop Move is the lowest soldier Speed. Acceleration is Speed x TroopTuning.AccelerationPerSpeedPoint.")]
    int _speed = CombatStatLimits.Default;
    [SerializeField, Range(1, 20)] int _defence = CombatStatLimits.Default;
    [SerializeField, Range(1, 20)] int _strength = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Maximum number of nearby friendly soldiers that can support this soldier.")]
    int _support = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Bonus to Combat Power while charging.")]
    int _charge = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Expected charges per ChargeWindowSeconds (Melee Tuning, 45 s) while it may charge.")]
    int _aggression = CombatStatLimits.Default;
    [SerializeField, Range(10, 40), Tooltip("Maximum HP (MaxHP), integer 10-40.")]
    int _maxHealth = CombatStatLimits.HpDefault;
    [Tooltip("Skill levels for listed weapon classes (integer 1-20, default 5). A class that is not listed is unskilled (0). At most 2.")]
    [SerializeField] List<WeaponSkill> _weaponSkills = new();

    public int Speed => _speed;
    public int Defence => _defence;
    public int Strength => _strength;
    public int Support => _support;
    public int Charge => _charge;
    public int Aggression => _aggression;
    public int MaxHealth => _maxHealth;
    public IReadOnlyList<WeaponSkill> WeaponSkills => _weaponSkills;

    public SoldierStats() { }

    public SoldierStats(int speed, int defence, int strength, int support, int charge, int aggression, int maxHealth,
        IEnumerable<WeaponSkill> weaponSkills = null)
    {
        _speed = CombatStatLimits.Clamp(speed);
        _defence = CombatStatLimits.Clamp(defence);
        _strength = CombatStatLimits.Clamp(strength);
        _support = CombatStatLimits.Clamp(support);
        _charge = CombatStatLimits.Clamp(charge);
        _aggression = CombatStatLimits.Clamp(aggression);
        _maxHealth = CombatStatLimits.ClampHp(maxHealth);
        _weaponSkills = weaponSkills != null ? new List<WeaponSkill>(weaponSkills) : new List<WeaponSkill>();
    }

    // Skill level with a weapon class. A class the soldier has no skill in is unskilled (0, Questions.md Q-2).
    public int SkillFor(WeaponClass weaponClass)
    {
        foreach (var skill in _weaponSkills)
            if (skill.Class == weaponClass) return skill.Level;
        return 0;
    }

    // Called from CharacterProfile.OnValidate. A listed skill cannot be 0: 0 means unlisted. A listed 0 (or a value
    // outside 1-20) is set to the default or clamped.
    internal void Validate(UnityEngine.Object context)
    {
        int maxSkills = TroopTuning.Active.MaxWeaponSkillsPerSoldier;
        if (_weaponSkills.Count > maxSkills)
            Debug.LogWarning($"{context.name}: a soldier can have skill in at most {maxSkills} weapon classes.", context);

        for (int i = 0; i < _weaponSkills.Count; i++)
        {
            WeaponSkill skill = _weaponSkills[i];
            int level = skill.Level < CombatStatLimits.Min ? CombatStatLimits.Default : CombatStatLimits.Clamp(skill.Level);
            if (level != skill.Level)
            {
                skill.Level = level;
                _weaponSkills[i] = skill;
            }
        }
    }
}
