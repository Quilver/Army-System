using UnityEngine;
using UnityEngine.Serialization;

// Battle Tuning: charges, clashes, support, duels and Assault (spec §4.2-§4.5, §7.2).
// Asset: Assets/Resources/Battle Tuning/Melee Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Melee Tuning")]
public sealed class MeleeTuning : ScriptableObject
{
    public const string AssetName = "Melee Tuning";

    [Header("Charges and clashes (spec §4.3-§4.5)")]
    [SerializeField, Min(0f), Tooltip("Charge range as a multiple of A_Distance (7.8 units at the spec A_Distance of 5.2). Q-25.")]
    float _chargeRangeScale = 1.5f;
    [SerializeField, Min(1), Tooltip("Nearest enemy soldiers a charger picks its target from.")]
    int _chargeCandidateCount = 3;
    [SerializeField, Min(0.1f), Tooltip("Charge speed multiplier (max speed and acceleration).")]
    float _chargeSpeedScale = 2f;
    [SerializeField, Min(0.1f), Tooltip("Charge cancel timeout. Provisional (Q-27).")]
    float _chargeTimeoutSeconds = 3f;
    [SerializeField, Range(0f, 1f), Tooltip("Chance each Discipline-nearest friend follows a charger.")]
    float _chargeFollowProbability = 0.5f;
    [SerializeField, Min(0f), Tooltip("Per-soldier clash rate limit for contacts that are not charges. Provisional (Q-26).")]
    float _clashCooldownSeconds = 1f;

    [Header("Support and duel (spec §4.5)")]
    [SerializeField, Min(0f), Tooltip("Support range (units).")]
    float _supportRadius = 3f;
    [SerializeField, Min(1f), Tooltip("Support range multiplier for a Spear Support supporter (Q-7, SU-90).")]
    [FormerlySerializedAs("_boarSpearSupportRangeMultiplier")]
    float _spearSupportRangeMultiplier = 2f;
    [SerializeField, Min(0), Tooltip("Combat Power per supporting ally.")]
    int _supportBonusPerAlly = 2;
    [SerializeField, Min(0.1f), Tooltip("Divisor of the Domination formula.")]
    float _dominationDivisor = 10f;

    [Header("Assault (spec §4.2)")]
    [SerializeField, Min(0f), Tooltip("Assault length after an ordered troop engages.")]
    float _assaultDurationSeconds = 10f;
    [SerializeField, Min(0), Tooltip("Aggression bonus during Assault.")]
    int _assaultAggressionBonus = 5;
    [SerializeField, Min(0), Tooltip("Charge bonus during Assault, while charging.")]
    int _assaultChargeBonus = 5;

    public float ChargeRangeScale => _chargeRangeScale;
    public int ChargeCandidateCount => _chargeCandidateCount;
    public float ChargeSpeedScale => _chargeSpeedScale;
    public float ChargeTimeoutSeconds => _chargeTimeoutSeconds;
    public float ChargeFollowProbability => _chargeFollowProbability;
    public float ClashCooldownSeconds => _clashCooldownSeconds;

    public float SupportRadius => _supportRadius;
    public float SpearSupportRangeMultiplier => _spearSupportRangeMultiplier;
    public int SupportBonusPerAlly => _supportBonusPerAlly;
    public float DominationDivisor => _dominationDivisor;

    public float AssaultDurationSeconds => _assaultDurationSeconds;
    public int AssaultAggressionBonus => _assaultAggressionBonus;
    public int AssaultChargeBonus => _assaultChargeBonus;
}
