using System.Collections.Generic;
using UnityEngine;

public enum SoldierLifeState
{
    Active,
    Fatigued,
    Dead
}

// One soldier type: stats, loadout and steering. Troop-level stats (Morale, Discipline, Numbers) live on TroopProfile.
// All combat stats are integers 1-20 (default 5), and HP is an integer 10-40 (default 15). See CombatStatLimits.
// The [Range] values below repeat CombatStatLimits; keep them in step.
[CreateAssetMenu(menuName = "Troops/Soldier Profile")]
public sealed class SoldierProfile : ScriptableObject
{
    [SerializeField] Sprite _sprite;
    [SerializeField] string _className;

    [Header("Combat stats (integer 1-20, default 5)")]
    [SerializeField, Range(1, 20), Tooltip("Individual speed along the troop's vector field. Troop Move is the lowest soldier Speed.")]
    int _speed = CombatStatLimits.Default;
    [SerializeField, Range(1, 20)] int _defence = CombatStatLimits.Default;
    [SerializeField, Range(1, 20)] int _strength = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Maximum number of nearby friendly soldiers that can support this soldier.")]
    int _support = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Bonus to Combat Power while charging.")]
    int _charge = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Expected charges per TroopTuning.ActionWindowSeconds while engaged.")]
    int _aggression = CombatStatLimits.Default;

    [Header("Health (integer 10-40, default 15)")]
    [SerializeField, Range(10, 40), Tooltip("Maximum HP (MaxHP).")]
    int _maxHealth = CombatStatLimits.HpDefault;

    [Header("Loadout (max 2 weapons, max 2 skills)")]
    [SerializeField] List<WeaponProfile> _weapons = new();
    [Tooltip("Skill levels for listed weapon classes (integer 1-20, default 5). A class that is not listed is unskilled (0).")]
    [SerializeField] List<WeaponSkill> _weaponSkills = new();
    [SerializeField] ArmourProfile _armour;

    [Header("Steering (unchanged by the v0.9 stat set)")]
    [SerializeField, Min(0.1f)] float _acceleration = 8f;
    [SerializeField, Min(0f)] float _alignment = 0.5f;
    [SerializeField, Min(0f)] float _separation = 1.8f;
    [SerializeField, Range(0f, 1f)] float _brownianMotion = 0.04f;

    public Sprite Sprite => _sprite;
    public string ClassName => string.IsNullOrEmpty(_className) ? name : _className;
    public int Speed => _speed;
    public int Defence => _defence;
    public int Strength => _strength;
    public int Support => _support;
    public int Charge => _charge;
    public int Aggression => _aggression;
    public int MaxHealth => _maxHealth;
    public ArmourProfile Armour => _armour;
    public IReadOnlyList<WeaponProfile> Weapons => _weapons;
    public IReadOnlyList<WeaponSkill> WeaponSkills => _weaponSkills;

    // Speed converted to units/s (spec §1.3). Used by steering and the pivot.
    public float MaxSpeed => _speed * TroopTuning.Active.SpeedUnitsPerStatPoint;
    public float Acceleration => _acceleration;
    public float Alignment => _alignment;
    public float Separation => _separation;
    public float BrownianMotion => _brownianMotion;

    // The first listed melee weapon. Used in melee (spec §1.6; Questions.md Q-3, working assumption).
    public WeaponProfile MeleeWeapon
    {
        get
        {
            foreach (var weapon in _weapons)
                if (weapon != null && !weapon.IsRanged) return weapon;
            return null;
        }
    }

    // The first listed ranged weapon, or null.
    public WeaponProfile RangedWeapon
    {
        get
        {
            foreach (var weapon in _weapons)
                if (weapon != null && weapon.IsRanged) return weapon;
            return null;
        }
    }

    // Skill level with a weapon class. A class the soldier has no skill in is unskilled (0, Questions.md Q-2).
    public int SkillFor(WeaponClass weaponClass)
    {
        foreach (var skill in _weaponSkills)
            if (skill.Class == weaponClass) return skill.Level;
        return 0;
    }

    // Skill with the melee weapon in use (0 without a melee weapon or without skill in its class).
    public int MeleeWeaponSkill => MeleeWeapon != null ? SkillFor(MeleeWeapon.Class) : 0;

    // Melee damage bonus of the weapon in use (spec §4.6 WeaponDamageBonus).
    public int MeleeWeaponDamage => MeleeWeapon != null ? MeleeWeapon.Damage : 0;

    // Weapon charge bonus, added to Charge only while charging (spec §4.5, Q-4).
    public int MeleeWeaponChargeBonus => MeleeWeapon != null ? MeleeWeapon.ChargeBonus : 0;

    // Special rules of the melee weapon in use (spec §1.6).
    public bool HasBoarSpear => MeleeWeapon != null && MeleeWeapon.SpecialRule == WeaponSpecialRule.BoarSpearSupport;
    public bool HasLance => MeleeWeapon != null && MeleeWeapon.SpecialRule == WeaponSpecialRule.LanceChargeDouble;

    void OnValidate()
    {
        var tuning = TroopTuning.Active;
        if (_weapons.Count > tuning.MaxWeaponsPerSoldier)
            Debug.LogWarning($"{name}: a soldier can carry at most {tuning.MaxWeaponsPerSoldier} weapons.", this);
        if (_weaponSkills.Count > tuning.MaxWeaponSkillsPerSoldier)
            Debug.LogWarning($"{name}: a soldier can have skill in at most {tuning.MaxWeaponSkillsPerSoldier} weapon classes.", this);

        // A listed skill cannot be 0: 0 means unlisted. A listed 0 (or a value outside 1-20) is set to the default or clamped.
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

        bool hasRanged = RangedWeapon != null;
        if (hasRanged && MeleeWeapon == null)
            Debug.LogWarning($"{name}: a ranged soldier must also carry a melee weapon (spec §1.6).", this);
    }

    public static SoldierProfile CreateRuntimeProfile(Sprite sprite)
    {
        var profile = CreateInstance<SoldierProfile>();
        profile._sprite = sprite;
        return profile;
    }

    public static SoldierProfile CreateCharacterProfile(
        string className,
        Sprite sprite,
        int speed,
        int defence,
        int maxHealth,
        int aggression,
        int strength,
        int charge,
        int support,
        IReadOnlyList<WeaponProfile> weapons,
        IReadOnlyList<WeaponSkill> weaponSkills,
        ArmourProfile armour)
    {
        var profile = CreateInstance<SoldierProfile>();
        profile._className = className;
        profile._sprite = sprite;
        profile._speed = CombatStatLimits.Clamp(speed);
        profile._defence = CombatStatLimits.Clamp(defence);
        profile._maxHealth = CombatStatLimits.ClampHp(maxHealth);
        profile._aggression = CombatStatLimits.Clamp(aggression);
        profile._strength = CombatStatLimits.Clamp(strength);
        profile._charge = CombatStatLimits.Clamp(charge);
        profile._support = CombatStatLimits.Clamp(support);
        profile._weapons = new List<WeaponProfile>(weapons);
        profile._weaponSkills = new List<WeaponSkill>(weaponSkills);
        profile._armour = armour;
        return profile;
    }

    public void SetSpriteIfMissing(Sprite sprite)
    {
        if (_sprite == null) _sprite = sprite;
    }
}
