using UnityEngine;

// A character's class, e.g. Legionnaire or Logger. Shared by every character of that class. Holds the stat caps that
// campaign characters cannot grow past (StatCaps). Class special rules will be added here later.
// The battle system reads only DisplayName (the troop card); caps are used by the campaign (CampaignCharacter).
[CreateAssetMenu(menuName = "Troops/Character Class")]
public sealed class CharacterClass : ScriptableObject
{
    [SerializeField, Tooltip("Shown on the troop card. Falls back to the asset name.")] string _displayName;
    [SerializeField, TextArea] string _description;

    [Header("Stat caps (level-ups stop here)")]
    [SerializeField] StatCaps _statCaps = new();

    // Special rules (later).

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public string Description => _description;
    public StatCaps StatCaps => _statCaps;
    public int CapFor(CharacterStat stat) => _statCaps.For(stat);
    public int CapFor(WeaponClass weaponClass) => _statCaps.ForWeapon(weaponClass);

    void OnValidate()
    {
        _statCaps ??= new StatCaps();
        _statCaps.Validate(this);
    }

    public static CharacterClass CreateRuntimeClass(string displayName, StatCaps statCaps = null)
    {
        var characterClass = CreateInstance<CharacterClass>();
        characterClass._displayName = displayName;
        characterClass._statCaps = statCaps ?? new StatCaps();
        characterClass.name = displayName;
        return characterClass;
    }
}
