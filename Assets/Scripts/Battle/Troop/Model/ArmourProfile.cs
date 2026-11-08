using UnityEngine;

// Armour (spec §1.6, SU-89). Resistance is the Deflect : Bruise : Bleed ratio used when rolling each point of damage up
// to the defender's TotalDefence (spec §4.6). Each point has Deflect / Total chance to be deflected, and so on. A weapon's
// Piercing adds to the Bleed part and its Bludgeoning to the Bruise part for that weapon's hits only. The ratio parts
// must not all be 0. Unity cannot reject a save, so an invalid ratio logs a warning and IsValid returns false.
[CreateAssetMenu(menuName = "Troops/Armour Profile")]
public sealed class ArmourProfile : ScriptableObject
{
    [SerializeField] string _displayName;
    [SerializeField, Min(0), Tooltip("Deflect part of the Deflect : Bruise : Bleed ratio.")] int _deflect = 3;
    [SerializeField, Min(0), Tooltip("Bruise part of the Deflect : Bruise : Bleed ratio.")] int _bruise = 5;
    [SerializeField, Min(0), Tooltip("Bleed part of the Deflect : Bruise : Bleed ratio.")] int _bleed = 2;
    [SerializeField, Min(0), Tooltip("Added to the soldier's Defence to give TotalDefence.")] int _defenceBonus = 4;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public int Deflect => _deflect;
    public int Bruise => _bruise;
    public int Bleed => _bleed;
    public int DefenceBonus => _defenceBonus;
    // Sum of the three ratio parts.
    public int RatioTotal => _deflect + _bruise + _bleed;
    public bool IsValid => _deflect >= 0 && _bruise >= 0 && _bleed >= 0 && RatioTotal > 0;
    // "Deflect : Bruise : Bleed", e.g. "6 : 3 : 1".
    public string RatioText => _deflect + " : " + _bruise + " : " + _bleed;

    void OnValidate()
    {
        if (!IsValid)
            Debug.LogWarning($"{name}: the Deflect : Bruise : Bleed ratio needs at least one part above 0 (currently {RatioText}).", this);
    }

    public static ArmourProfile CreateRuntimeProfile(
        string displayName,
        int deflect,
        int bruise,
        int bleed,
        int defenceBonus)
    {
        var profile = CreateInstance<ArmourProfile>();
        profile._displayName = displayName;
        profile._deflect = Mathf.Max(0, deflect);
        profile._bruise = Mathf.Max(0, bruise);
        profile._bleed = Mathf.Max(0, bleed);
        profile._defenceBonus = Mathf.Max(0, defenceBonus);
        return profile;
    }
}
