using UnityEngine;

// Battle Tuning: soldier stats, actions and steering (spec §1, §2, §4.6, §7.2).
// Asset: Assets/Resources/Battle Tuning/Soldier Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Soldier Tuning")]
public sealed class SoldierTuning : ScriptableObject
{
    public const string AssetName = "Soldier Tuning";
    public const float DefaultSoldierSeparation = 1.8f;

    [Header("Stats and actions")]
    [SerializeField, Min(0.01f), Tooltip("Speed stat point to units/s. Speed 5 gives 3.0 units/s.")]
    float _speedUnitsPerStatPoint = 0.6f;
    [SerializeField, Min(1f), Tooltip("Window over which Aggression, FireRate and Shock decay are measured.")]
    float _actionWindowSeconds = 30f;
    [SerializeField, Min(0), Tooltip("Maximum weapons a soldier can carry (spec §1.6).")]
    int _maxWeaponsPerSoldier = 2;
    [SerializeField, Min(0), Tooltip("Maximum weapon classes a soldier can have skill in (spec §1.6).")]
    int _maxWeaponSkillsPerSoldier = 2;
    [SerializeField, Min(0f), Tooltip("Bruise healed per soldier per second (spec §4.6).")]
    float _bruiseHealPerSecond = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("A soldier at or below this fraction of MaxHP is fatigued and cannot support.")]
    float _fatigueFraction = 0.5f;
    [SerializeField, Tooltip("Stunned soldiers (SU-107): a soldier brought to 0 HP with at least half of it Bruise is stunned " +
        "instead of killed. It cannot charge, and dies if its troop retreats from melee. Off: 0 HP always kills.")]
    bool _stunAtZeroHp = true;
    [SerializeField, Min(1), Tooltip("A stunned soldier recovers once its bruises have healed to this many HP (SU-107).")]
    int _stunRecoverHp = 5;

    [Header("Soldier steering (shared by every character)")]
    [SerializeField, Min(0.01f), Tooltip("Soldier acceleration (units/s²) per Speed point. Speed 5 gives 8, the old fixed value.")]
    float _accelerationPerSpeedPoint = 1.6f;
    [SerializeField, Min(0f), Tooltip("How strongly soldiers match their neighbours' velocity.")]
    float _soldierAlignment = 0.5f;
    [SerializeField, Min(0f), Tooltip("How strongly soldiers push apart from close neighbours, and from other troops' soldiers.")]
    float _soldierSeparation = DefaultSoldierSeparation;
    [SerializeField, Range(0f, 1f), Tooltip("Random jitter added to soldier steering.")]
    float _soldierBrownianMotion = 0.04f;

    [Header("Soldier wave (SU-166)")]
    [SerializeField, Tooltip("When the troop's march sets off, stops or turns, the soldiers nearest the direction of travel " +
        "respond first and each rank behind a moment later, so the troop stretches as it starts and bunches as it stops. " +
        "Off: every soldier is carried with the pivot, as before.")]
    bool _soldierWave = true;
    [SerializeField, Min(0.1f), Tooltip("How fast the wave runs back through the troop (units/s): a soldier responds after its " +
        "depth behind the troop's leading edge divided by this. Ranks are 0.9 apart, so 4.5 ripples a four-rank troop " +
        "over about 0.6 s.")]
    float _soldierWaveSpeed = 4.5f;
    [SerializeField, Min(0.1f), Tooltip("Furthest a soldier may lag its place in the troop (units). It also caps the wave's " +
        "delay at this over the pivot speed, so a long column does not string out without limit.")]
    float _soldierWaveMaxLag = 2.4f;
    [SerializeField, Range(0f, 1f), Tooltip("When the troop stops, a soldier may run on past its place by at most this " +
        "fraction of its depth behind the leading edge (and SoldierWaveMaxLag), so the troop closes up by at most this " +
        "fraction of its depth and the ranks behind stop one after another. Soldiers keep a rank spacing apart, so " +
        "much above 0.1 only shoves the front rank on.")]
    float _soldierWaveBunchFraction = 0.1f;
    [SerializeField, Min(1f), Tooltip("A lagging soldier catches up by walking faster: at most this times its top speed " +
        "(and its acceleration).")]
    float _soldierWaveCatchUpSpeedScale = 1.5f;

    [Header("Soldier facing in melee (display only, SU-158)")]
    [SerializeField, Min(1f), Tooltip("How fast a soldier of an engaged troop, or a charging soldier, turns to or from the " +
        "enemy it faces, in degrees per second.")]
    float _engagedFacingTurnRateDeg = 240f;
    [SerializeField, Min(0f), Tooltip("A soldier of an engaged troop picks a new enemy to face at most this often (seconds).")]
    float _facingTargetRepickSeconds = 0.3f;
    [SerializeField, Min(1f), Tooltip("A soldier keeps facing its enemy while that enemy lives and is within this many times " +
        "its direct reach, so it does not flicker between two equally near enemies.")]
    float _facingTargetKeepScale = 1.2f;
    [SerializeField, Min(0f), Tooltip("Direct reach for facing an enemy: A_Distance between soldier edges plus this slack " +
        "(units), as for the engagement heat band (its Reach Slack on the player orders, 0.45).")]
    float _facingReachSlack = 0.45f;

    public float SpeedUnitsPerStatPoint => _speedUnitsPerStatPoint;
    public float ActionWindowSeconds => _actionWindowSeconds;
    public int MaxWeaponsPerSoldier => _maxWeaponsPerSoldier;
    public int MaxWeaponSkillsPerSoldier => _maxWeaponSkillsPerSoldier;
    public float BruiseHealPerSecond => _bruiseHealPerSecond;
    public float FatigueFraction => _fatigueFraction;
    public bool StunAtZeroHp => _stunAtZeroHp;
    public int StunRecoverHp => _stunRecoverHp;

    public float AccelerationPerSpeedPoint => _accelerationPerSpeedPoint;
    public float SoldierAlignment => _soldierAlignment;
    public float SoldierSeparation => _soldierSeparation;
    public float SoldierBrownianMotion => _soldierBrownianMotion;

    public bool SoldierWave => _soldierWave;
    public float SoldierWaveSpeed => _soldierWaveSpeed;
    public float SoldierWaveMaxLag => _soldierWaveMaxLag;
    public float SoldierWaveBunchFraction => _soldierWaveBunchFraction;
    public float SoldierWaveCatchUpSpeedScale => _soldierWaveCatchUpSpeedScale;

    public float EngagedFacingTurnRateDeg => _engagedFacingTurnRateDeg;
    public float FacingTargetRepickSeconds => _facingTargetRepickSeconds;
    public float FacingTargetKeepScale => _facingTargetKeepScale;
    public float FacingReachSlack => _facingReachSlack;
}
