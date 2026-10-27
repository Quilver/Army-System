using UnityEngine;

// Armour (spec §1.6). Resistance is the Deflect / Bruise / Bleed split used when rolling each point of damage up to the
// defender's TotalDefence (spec §4.6). The three values must sum to 100. Unity cannot reject a save, so an invalid split
// logs a warning and IsValid returns false.
[CreateAssetMenu(menuName = "Troops/Armour Profile")]
public sealed class ArmourProfile : ScriptableObject
{
    [SerializeField] string _displayName;
    [SerializeField, Range(0, 100)] int _deflect = 30;
    [SerializeField, Range(0, 100)] int _bruise = 50;
    [SerializeField, Range(0, 100)] int _bleed = 20;
    [SerializeField, Min(0), Tooltip("Added to the soldier's Defence to give TotalDefence.")] int _defenceBonus = 4;
    [SerializeField, Tooltip("Values are not confirmed by the designer. Replace them before tuning.")] bool _provisional;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public int Deflect => _deflect;
    public int Bruise => _bruise;
    public int Bleed => _bleed;
    public int DefenceBonus => _defenceBonus;
    public bool Provisional => _provisional;
    public bool IsValid => _deflect + _bruise + _bleed == 100;

    void OnValidate()
    {
        if (!IsValid)
            Debug.LogWarning($"{name}: Deflect + Bruise + Bleed must sum to 100 (currently {_deflect + _bruise + _bleed}).", this);
    }

    public static ArmourProfile CreateRuntimeProfile(
        string displayName,
        int deflect,
        int bruise,
        int bleed,
        int defenceBonus,
        bool provisional)
    {
        var profile = CreateInstance<ArmourProfile>();
        profile._displayName = displayName;
        profile._deflect = deflect;
        profile._bruise = bruise;
        profile._bleed = bleed;
        profile._defenceBonus = Mathf.Max(0, defenceBonus);
        profile._provisional = provisional;
        return profile;
    }
}
