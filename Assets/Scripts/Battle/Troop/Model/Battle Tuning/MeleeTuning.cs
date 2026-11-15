using UnityEngine;
using UnityEngine.Serialization;

// Battle Tuning: charges, clashes, support, duels and Assault (spec §4.2-§4.5, §7.2).
// Asset: Assets/Resources/Battle Tuning/Melee Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Melee Tuning")]
public sealed class MeleeTuning : ScriptableObject
{
    public const string AssetName = "Melee Tuning";

    [Header("Charges and clashes (spec §4.3-§4.5)")]
    [SerializeField, Min(1f), Tooltip("Charge window (SU-112): a soldier makes about EffectiveAggression charges per this " +
        "many seconds while it may charge (Aggression 3: one charge every 15 s). Ranged fire and Shock decay keep " +
        "ActionWindowSeconds (Soldier Tuning).")]
    float _chargeWindowSeconds = 45f;
    [SerializeField, Min(0f), Tooltip("Soldiers charge only while their troop's footprint gap to an enemy troop is within " +
        "EngagementGap plus this (units). It absorbs the jitter of lines held at EngagementGap by the barrier (SU-95). " +
        "A soldier's charge reach is A_Distance (Formation Tuning).")]
    float _chargeGapTolerance = 0.25f;
    [SerializeField, Tooltip("Only the front rank charges (SU-106): of the soldiers of a troop, only as many as stand in one " +
        "rank, those furthest toward the enemy, may start a charge. Off: any soldier within reach may.")]
    bool _frontRankChargesOnly = true;
    [SerializeField, Min(1), Tooltip("Nearest enemy soldiers a charger picks its target from.")]
    int _chargeCandidateCount = 3;
    [SerializeField, Min(0.1f), Tooltip("Charge speed multiplier (max speed and acceleration).")]
    float _chargeSpeedScale = 2f;
    [SerializeField, Min(0.1f), Tooltip("Charge cancel timeout.")]
    float _chargeTimeoutSeconds = 3f;
    [SerializeField, Range(0f, 1f), Tooltip("Chance each Discipline-nearest friend follows a charger.")]
    float _chargeFollowProbability = 0.5f;
    [SerializeField, Min(0f), Tooltip("Per-soldier clash rate limit for contacts that are not charges.")]
    float _clashCooldownSeconds = 1f;

    [Header("Feints (SU-164)")]
    [SerializeField, Min(0f), Tooltip("Feints per real charge (SU-164): besides its charge roll, every soldier that may charge " +
        "rolls for a feint at this many times its charge rate, on a random stream of its own. A feint runs at its target " +
        "like a charge; on contact both soldiers recoil and flash, and it ends: no duel, damage, Shock, stun or clash " +
        "cooldown, no followers. 0: no feints (the battle is as before).")]
    float _feintsPerCharge = 3f;
    [SerializeField, Range(0f, 1f), Tooltip("A feint's knock-back as a share of a clash's (SU-164).")]
    float _feintKnockBackScale = 0.5f;
    [SerializeField, Min(0.1f), Tooltip("A feint that has not reached an enemy soldier after this many seconds ends (SU-164). " +
        "Short, so a feinting soldier, which cannot start a real charge, is soon free to charge again.")]
    float _feintTimeoutSeconds = 1f;

    [Header("Support and duel (spec §4.5)")]
    [SerializeField, Min(0f), Tooltip("Support range (units).")]
    float _supportRadius = 3f;
    [SerializeField, Min(1f), Tooltip("Support range multiplier for a Spear Support supporter (Q-7, SU-90).")]
    [FormerlySerializedAs("_boarSpearSupportRangeMultiplier")]
    float _spearSupportRangeMultiplier = 2f;
    [SerializeField, Min(0f), Tooltip("Support gives (SU-149): each supporter adds its own Support stat x this to the " +
        "clashing soldier's Combat Power (Support 8 at 0.25 adds 2). The total is rounded half up.")]
    float _supportBonusPerSupportPoint = 0.25f;
    [SerializeField, Min(0), Tooltip("Most supporters counted in one clash (SU-149): the eligible supporters with the " +
        "highest Support.")]
    int _maxSupporters = 8;
    [SerializeField, Min(0.1f), Tooltip("Divisor of the Domination formula.")]
    float _dominationDivisor = 10f;
    [SerializeField, Tooltip("Elo duel (SU-153): on, the charger's chance is 1 / (1 + 10^((CP_B - CP_A) / Elo Scale)), from " +
        "the Combat Power difference. Off: CP_A / (CP_A + CP_B).")]
    bool _eloDuel;
    [SerializeField, Min(1f), Tooltip("Elo duel: a Combat Power lead of this many points wins 10 to 1 (91%). Half of it wins " +
        "about 3 to 1 (76%).")]
    float _eloScale = 30f;

    [Header("Assault (spec §4.2)")]
    [SerializeField, Min(0f), Tooltip("Assault length after an ordered troop engages.")]
    float _assaultDurationSeconds = 10f;
    [SerializeField, Min(0), Tooltip("Aggression bonus during Assault.")]
    int _assaultAggressionBonus = 5;
    [SerializeField, Min(0), Tooltip("Charge bonus during Assault, while charging.")]
    int _assaultChargeBonus = 5;

    public float ChargeWindowSeconds => _chargeWindowSeconds;
    public float ChargeGapTolerance => _chargeGapTolerance;
    public bool FrontRankChargesOnly => _frontRankChargesOnly;
    public int ChargeCandidateCount => _chargeCandidateCount;
    public float ChargeSpeedScale => _chargeSpeedScale;
    public float ChargeTimeoutSeconds => _chargeTimeoutSeconds;
    public float ChargeFollowProbability => _chargeFollowProbability;
    public float ClashCooldownSeconds => _clashCooldownSeconds;
    public float FeintsPerCharge => _feintsPerCharge;
    public float FeintKnockBackScale => _feintKnockBackScale;
    public float FeintTimeoutSeconds => _feintTimeoutSeconds;

    public float SupportRadius => _supportRadius;
    public float SpearSupportRangeMultiplier => _spearSupportRangeMultiplier;
    public float SupportBonusPerSupportPoint => _supportBonusPerSupportPoint;
    public int MaxSupporters => _maxSupporters;
    public float DominationDivisor => _dominationDivisor;
    public bool EloDuel => _eloDuel;
    public float EloScale => _eloScale;

    public float AssaultDurationSeconds => _assaultDurationSeconds;
    public int AssaultAggressionBonus => _assaultAggressionBonus;
    public int AssaultChargeBonus => _assaultChargeBonus;
}
