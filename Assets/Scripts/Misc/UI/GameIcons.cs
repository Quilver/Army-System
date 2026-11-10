using UnityEngine;

// The parts of an armour line on the troop card and ledger (SU-91): the armour itself, then the three parts of its
// Deflect : Bruise : Bleed ratio.
public enum ArmourIcon
{
    Armour,
    Deflect,
    Bruise,
    Bleed
}

// Icons shown in place of stat, weapon class and armour names (SU-91). One asset, loaded from Resources/UI/Game Icons.
// The UI shows the name when the cursor is over an icon, and falls back to the name as text when an icon is empty.
// Class icons live on each CharacterClass instead.
[CreateAssetMenu(menuName = "UI/Game Icons")]
public sealed class GameIcons : ScriptableObject
{
    const string ResourcePath = "UI/Game Icons";

    [Header("Stats")]
    [SerializeField] Sprite _numbers;
    [SerializeField] Sprite _speed;
    [SerializeField] Sprite _defence;
    [SerializeField] Sprite _strength;
    [SerializeField] Sprite _support;
    [SerializeField] Sprite _charge;
    [SerializeField] Sprite _aggression;
    [SerializeField] Sprite _maxHealth;
    [SerializeField] Sprite _morale;
    [SerializeField] Sprite _discipline;

    [Header("Armour")]
    [SerializeField] Sprite _armour;
    [SerializeField] Sprite _deflect;
    [SerializeField] Sprite _bruise;
    [SerializeField] Sprite _bleed;

    [Header("Weapon classes (weapon skills)")]
    [SerializeField] Sprite _spear;
    [SerializeField] Sprite _sword;
    [SerializeField] Sprite _axe;
    [SerializeField] Sprite _bow;
    [SerializeField] Sprite _javelin;
    [SerializeField] Sprite _mace;
    [SerializeField] Sprite _crossbow;

    static GameIcons _instance;
    static bool _loaded;

    // The icon set, or null when the asset is missing (every icon then falls back to its name).
    public static GameIcons Instance
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                _instance = Resources.Load<GameIcons>(ResourcePath);
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache()
    {
        _loaded = false;
        _instance = null;
    }

    public static Sprite For(CharacterStat stat) => Instance != null ? Instance.Stat(stat) : null;
    public static Sprite For(WeaponClass weaponClass) => Instance != null ? Instance.Weapon(weaponClass) : null;
    public static Sprite For(ArmourIcon part) => Instance != null ? Instance.Armour(part) : null;

    public static string NameOf(CharacterStat stat) => stat switch
    {
        CharacterStat.Numbers => "Soldiers",
        CharacterStat.MaxHealth => "HP",
        CharacterStat.WeaponSkill => "Weapon skill",
        _ => stat.ToString()
    };

    public static string NameOf(WeaponClass weaponClass) => weaponClass + " skill";

    public static string NameOf(ArmourIcon part) => part switch
    {
        ArmourIcon.Armour => "Armour",
        _ => part.ToString()
    };

    Sprite Stat(CharacterStat stat) => stat switch
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
        _ => null
    };

    Sprite Weapon(WeaponClass weaponClass) => weaponClass switch
    {
        WeaponClass.Spear => _spear,
        WeaponClass.Sword => _sword,
        WeaponClass.Axe => _axe,
        WeaponClass.Bow => _bow,
        WeaponClass.Javelin => _javelin,
        WeaponClass.Mace => _mace,
        WeaponClass.Crossbow => _crossbow,
        _ => null
    };

    Sprite Armour(ArmourIcon part) => part switch
    {
        ArmourIcon.Armour => _armour,
        ArmourIcon.Deflect => _deflect,
        ArmourIcon.Bruise => _bruise,
        ArmourIcon.Bleed => _bleed,
        _ => null
    };
}
