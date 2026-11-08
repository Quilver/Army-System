using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// A character (spec §1.1): a campaign commander and the soldiers they bring into battle. Pre-battle data only: a
// troop (Troop) lists one or more characters, and the troop spawns Numbers soldiers for each of them at deployment.
// Every spawned soldier points back at its character for its soldier stats and equipment (SoldierAgentState.Character).
// Morale and Discipline are this character's values; a troop of several characters resolves them with
// CompositeTroopResolver (spec §1.2). Acceleration follows Speed; the other steering values are shared (TroopTuning).
//
// This script keeps the GUID of the old SoldierProfile script, so old soldier assets load as characters.
// Characters in the player's roster are CampaignCharacter assets (a subclass that adds growths); battle code only ever
// needs this base class.
[CreateAssetMenu(menuName = "Troops/Character")]
public class CharacterProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, Tooltip("The character's name. Falls back to the asset name.")] string _characterName;
    [SerializeField, Tooltip("The character's class, e.g. Legionnaire: its stat caps and, later, its special rules.")]
    CharacterClass _class;
    // Legacy class name from before CharacterClass. Moved into _class by CampaignCharacterMigration (Editor).
    [SerializeField, HideInInspector] string _className;
    [SerializeField, Tooltip("Shown on the selected-troop card.")] Sprite _portrait;
    [SerializeField, FormerlySerializedAs("_sprite"), Tooltip("Sprite drawn for each soldier in battle.")] Sprite _soldierSprite;

    [Header("Soldiers")]
    [SerializeField, Min(0), Tooltip("Soldiers this character brings into battle. Not a stat, so it has no range.")]
    int _numbers = 12;
    [SerializeField, Range(1, 20), Tooltip("Morale per soldier (BaseMorale, spec §5.1).")]
    int _morale = CombatStatLimits.Default;
    [SerializeField, Range(1, 20), Tooltip("Number of nearest friendly soldiers that may follow a charger (spec §4.4).")]
    int _discipline = CombatStatLimits.Default;

    [Header("Equipment (1 melee weapon, optional ranged weapon)")]
    [SerializeField] CharacterEquipment _equipment = new();

    [Header("Soldier stats (integer 1-20, HP 10-40)")]
    [SerializeField] SoldierStats _soldierStats = new();

    // --- Pre-battle data ---

    public string CharacterName => string.IsNullOrEmpty(_characterName) ? name : _characterName;
    public CharacterClass Class => _class;
    // The class's name. Falls back to an unmigrated legacy class name, then to the character's name.
    public string ClassName => _class != null ? _class.DisplayName
        : string.IsNullOrEmpty(_className) ? CharacterName : _className;
    // The class's icon, or null (show ClassName instead).
    public Sprite ClassIcon => _class != null ? _class.Icon : null;
    public Sprite Portrait => _portrait;
    public Sprite SoldierSprite => _soldierSprite;
    public int Numbers => _numbers;
    public int Morale => _morale;
    public int Discipline => _discipline;
    public CharacterEquipment Equipment => _equipment;
    public SoldierStats SoldierStats => _soldierStats;

    // --- Per-soldier values read during battle (shortcuts into SoldierStats and Equipment) ---

    public int Speed => _soldierStats.Speed;
    public int Defence => _soldierStats.Defence;
    public int Strength => _soldierStats.Strength;
    public int Support => _soldierStats.Support;
    public int Charge => _soldierStats.Charge;
    public int Aggression => _soldierStats.Aggression;
    public int MaxHealth => _soldierStats.MaxHealth;
    public IReadOnlyList<WeaponSkill> WeaponSkills => _soldierStats.WeaponSkills;
    public int SkillFor(WeaponClass weaponClass) => _soldierStats.SkillFor(weaponClass);

    // The weapons this character carries without skill in their class (SU-140, Q-2): the melee weapon, then the ranged
    // weapon. This is a warning, not an error: the soldiers still deploy and fight with skill 0 (no WeaponSkill term in
    // Combat Power or the hit chance). Used by OnValidate, Troops > Check Weapon Skills and the troop's spawn warning.
    public List<WeaponProfile> UnskilledWeapons()
    {
        var weapons = new List<WeaponProfile>(2);
        WeaponProfile melee = _equipment?.MeleeWeapon;
        WeaponProfile ranged = _equipment?.RangedWeapon;
        if (melee != null && IsUnskilledWith(melee)) weapons.Add(melee);
        if (ranged != null && IsUnskilledWith(ranged)) weapons.Add(ranged);
        return weapons;
    }

    bool IsUnskilledWith(WeaponProfile weapon) => _soldierStats == null || _soldierStats.SkillFor(weapon.Class) <= 0;

    // The warning for one unskilled weapon (SU-140), naming the character, the weapon and its class.
    public string UnskilledWeaponWarning(WeaponProfile weapon) =>
        $"{CharacterName}: carries {weapon.DisplayName} ({weapon.Class}) but has no {weapon.Class} skill, so its " +
        "soldiers fight with skill 0 (Q-2, SU-140).";

    public ArmourProfile Armour => _equipment.Armour;
    // The character's melee weapon. Every character has one (spec §1.6, Decisions D-52): throws if it is missing.
    public MeleeWeaponProfile MeleeWeapon
    {
        get
        {
            EnsureHasMeleeWeapon();
            return _equipment.MeleeWeapon;
        }
    }
    // The character's ranged weapon, or null.
    public RangedWeaponProfile RangedWeapon => _equipment?.RangedWeapon;
    public bool HasMeleeWeapon => _equipment != null && _equipment.HasMeleeWeapon;

    // Throws MissingMeleeWeaponException if the character has no melee weapon. Called when a troop spawns its soldiers.
    public void EnsureHasMeleeWeapon()
    {
        if (!HasMeleeWeapon) throw new MissingMeleeWeaponException(this);
    }

    // Speed converted to units/s (spec §1.3). Used by steering and the pivot.
    public float MaxSpeed => Speed * TroopTuning.Active.SpeedUnitsPerStatPoint;
    // Acceleration follows Speed: Speed x AccelerationPerSpeedPoint (units/s²). Speed 5 gives 8, the old fixed value.
    public float Acceleration => Speed * TroopTuning.Active.AccelerationPerSpeedPoint;

    // Skill with the melee weapon (0 without skill in its class).
    public int MeleeWeaponSkill => SkillFor(MeleeWeapon.Class);
    // Melee damage bonus of the weapon (spec §4.6 WeaponDamageBonus).
    public int MeleeWeaponDamage => MeleeWeapon.Damage;
    // Weapon charge bonus, added to Charge only while charging (spec §4.5, Q-4).
    public int MeleeWeaponChargeBonus => MeleeWeapon.ChargeBonus;
    // Special rules of the melee weapon (spec §1.6, SU-90).
    public bool HasSpearSupport => MeleeWeapon.HasRule(WeaponSpecialRule.SpearSupport);
    public bool HasCouchedLance => MeleeWeapon.HasRule(WeaponSpecialRule.CouchedLance);

    // This character's line for composite troop resolution (spec §1.2).
    public TroopStatLine StatLine => new(Speed, _morale, _discipline, _numbers);

    protected virtual void OnValidate()
    {
        _equipment ??= new CharacterEquipment();
        _soldierStats ??= new SoldierStats();
        _equipment.Validate(this);
        _soldierStats.Validate(this);
        // SU-140: a weapon outside the character's weapon skills is allowed but warned about.
        foreach (WeaponProfile weapon in UnskilledWeapons())
            Debug.LogWarning(UnskilledWeaponWarning(weapon), this);
    }

    // Throws MissingMeleeWeaponException if equipment is null or has no melee weapon.
    public static CharacterProfile CreateRuntimeProfile(
        string characterName,
        CharacterClass characterClass,
        Sprite portrait,
        Sprite soldierSprite,
        int numbers,
        int morale,
        int discipline,
        CharacterEquipment equipment,
        SoldierStats soldierStats)
    {
        var profile = CreateInstance<CharacterProfile>();
        profile._characterName = characterName;
        profile._class = characterClass;
        profile._portrait = portrait;
        profile._soldierSprite = soldierSprite;
        profile._numbers = Mathf.Max(0, numbers);
        profile._morale = CombatStatLimits.Clamp(morale);
        profile._discipline = CombatStatLimits.Clamp(discipline);
        profile._equipment = equipment ?? new CharacterEquipment();
        profile._soldierStats = soldierStats ?? new SoldierStats();
        if (!profile.HasMeleeWeapon)
        {
            var exception = new MissingMeleeWeaponException(profile);
            DestroyImmediate(profile);
            throw exception;
        }
        return profile;
    }

    // A default character with only a soldier sprite and a soldier count (test scenes). It carries a placeholder melee
    // weapon with no damage or charge bonus, so it fights as the old weaponless default did.
    public static CharacterProfile CreateRuntimeProfile(Sprite soldierSprite, int numbers)
    {
        MeleeWeaponProfile placeholder = MeleeWeaponProfile.CreateRuntimeProfile(
            "Placeholder Sword", MeleeWeaponClass.Sword, 0, 0, null);
        placeholder.hideFlags = HideFlags.DontSave;
        return CreateRuntimeProfile(null, null, null, soldierSprite, numbers, CombatStatLimits.Default,
            CombatStatLimits.Default, new CharacterEquipment(placeholder, null, null), null);
    }

    // The asset a runtime copy (CreateWithGains) was made from; the profile itself for an asset. Code that matches a
    // troop's characters to the campaign's (achievements, battle records) compares Source.
    [System.NonSerialized] CharacterProfile _source;
    public CharacterProfile Source => _source != null ? _source : this;

    // A copy of this character with points added to its stats and weapon skills, never saved (SU-132): how a roster
    // character fights after its level-ups. The copy keeps its type (a CampaignCharacter stays one), name, class,
    // sprites and equipment. Stats are kept in range (CombatStatLimits); a gain for an unlisted weapon class is ignored.
    public CharacterProfile CreateWithGains(IReadOnlyDictionary<CharacterStat, int> statGains,
        IReadOnlyDictionary<WeaponClass, int> skillGains)
    {
        int Gain(CharacterStat stat) => statGains != null && statGains.TryGetValue(stat, out int gain) ? gain : 0;
        int SkillGain(WeaponClass weaponClass) =>
            skillGains != null && skillGains.TryGetValue(weaponClass, out int gain) ? gain : 0;

        CharacterProfile copy = Instantiate(this);
        copy.name = name;
        copy.hideFlags = HideFlags.DontSave;
        copy._source = Source;
        copy._numbers = Mathf.Max(0, _numbers + Gain(CharacterStat.Numbers));
        copy._morale = CombatStatLimits.Clamp(_morale + Gain(CharacterStat.Morale));
        copy._discipline = CombatStatLimits.Clamp(_discipline + Gain(CharacterStat.Discipline));
        var skills = new List<WeaponSkill>();
        foreach (WeaponSkill skill in WeaponSkills)
            skills.Add(new WeaponSkill(skill.Class, skill.Level + SkillGain(skill.Class)));
        copy._soldierStats = new SoldierStats(
            Speed + Gain(CharacterStat.Speed),
            Defence + Gain(CharacterStat.Defence),
            Strength + Gain(CharacterStat.Strength),
            Support + Gain(CharacterStat.Support),
            Charge + Gain(CharacterStat.Charge),
            Aggression + Gain(CharacterStat.Aggression),
            MaxHealth + Gain(CharacterStat.MaxHealth),
            skills);
        return copy;
    }

    public void SetSpriteIfMissing(Sprite sprite)
    {
        if (_soldierSprite == null) _soldierSprite = sprite;
    }
}

// Thrown when a character profile without a melee weapon is used. Every character carries one (spec §1.6, D-52).
public sealed class MissingMeleeWeaponException : System.InvalidOperationException
{
    public CharacterProfile Character { get; }

    public MissingMeleeWeaponException(CharacterProfile character)
        : base($"Character '{(character != null ? character.CharacterName : "null")}' has no melee weapon. " +
               "Every character must carry a melee weapon (spec §1.6).")
    {
        Character = character;
    }
}
