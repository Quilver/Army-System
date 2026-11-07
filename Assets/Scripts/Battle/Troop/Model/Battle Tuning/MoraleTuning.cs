using UnityEngine;

// Battle Tuning: morale, retreat, rout and faces (spec §4.8, §5, §7.2).
// Asset: Assets/Resources/Battle Tuning/Morale Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Morale Tuning")]
public sealed class MoraleTuning : ScriptableObject
{
    public const string AssetName = "Morale Tuning";

    [Header("Morale and retreat (spec §5)")]
    [SerializeField, Min(0), Tooltip("EffectiveMorale lost per normal retreat.")]
    int _retreatMoralePenalty = 5;
    [SerializeField, Min(0), Tooltip("EffectiveMorale lost per surrounded break.")]
    int _surroundedMoralePenalty = 20;
    [SerializeField, Range(0f, 1f), Tooltip("Shock decay per ActionWindowSeconds, as a fraction of EffectiveMorale.")]
    float _shockDecayFractionPerWindow = 0.20f;
    [SerializeField, Range(0f, 1f), Tooltip("A break below this fraction of initial EffectiveMorale routs (case A).")]
    float _routMoraleThresholdFraction = 0.50f;
    [SerializeField, Min(0f), Tooltip("Seconds after a rout before the troop is destroyed.")]
    float _routDespawnSeconds = 60f;
    [SerializeField, Min(0f), Tooltip("Retreat distance in world units, the same for every troop. Provisional (Q-34).")]
    float _retreatDistance = 5f;
    [SerializeField, Min(0f), Tooltip("Retreat wave: delay between the rearmost soldier and the frontmost soldier starting to fall back.")]
    float _retreatWaveDelaySeconds = 0.6f;
    [SerializeField, Min(0.05f), Tooltip("Time each soldier takes to fall back to its new place in a retreat.")]
    float _retreatSoldierMoveSeconds = 1.2f;
    [SerializeField, Min(1f), Tooltip("Shock multiplier on flanked hits (spec §4.8).")]
    float _flankShockMultiplier = 3f;
    [SerializeField, Min(0f), Tooltip("A troop that breaks while not engaged in melee is disordered instead of retreating: " +
        "it cannot move for this many seconds (SU-104).")]
    float _disorderSeconds = 10f;

    [Header("Faces (spec §4.8, Q-31)")]
    [SerializeField, Range(0f, 90f), Tooltip("Attacks within this angle of the facing hit the Front face.")]
    float _frontFaceHalfAngleDeg = 45f;
    [SerializeField, Range(90f, 180f), Tooltip("Attacks beyond this angle from the facing hit the Rear face.")]
    float _rearFaceAngleDeg = 135f;

    public int RetreatMoralePenalty => _retreatMoralePenalty;
    public int SurroundedMoralePenalty => _surroundedMoralePenalty;
    public float ShockDecayFractionPerWindow => _shockDecayFractionPerWindow;
    public float RoutMoraleThresholdFraction => _routMoraleThresholdFraction;
    public float RoutDespawnSeconds => _routDespawnSeconds;
    public float RetreatDistance => _retreatDistance;
    public float RetreatWaveDelaySeconds => _retreatWaveDelaySeconds;
    public float RetreatSoldierMoveSeconds => _retreatSoldierMoveSeconds;
    public float FlankShockMultiplier => _flankShockMultiplier;
    public float DisorderSeconds => _disorderSeconds;

    public float FrontFaceHalfAngleDeg => _frontFaceHalfAngleDeg;
    public float RearFaceAngleDeg => _rearFaceAngleDeg;
}
