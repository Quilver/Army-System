using UnityEngine;

// Battle Tuning: soldier stats, actions and steering (spec §1, §2, §4.6, §7.2).
// Asset: Assets/Resources/Battle Tuning/Soldier Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Soldier Tuning")]
public sealed class SoldierTuning : ScriptableObject
{
    public const string AssetName = "Soldier Tuning";
    public const float DefaultSoldierSeparation = 1.8f;

    [Header("Stats and actions")]
    [SerializeField, Min(0.01f), Tooltip("Speed stat point to units/s. Speed 5 gives 3.0 units/s. Provisional (Q-1).")]
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

    [Header("Soldier steering (shared by every character)")]
    [SerializeField, Min(0.01f), Tooltip("Soldier acceleration (units/s²) per Speed point. Speed 5 gives 8, the old fixed value.")]
    float _accelerationPerSpeedPoint = 1.6f;
    [SerializeField, Min(0f), Tooltip("How strongly soldiers match their neighbours' velocity.")]
    float _soldierAlignment = 0.5f;
    [SerializeField, Min(0f), Tooltip("How strongly soldiers push apart from close neighbours, and from other troops' soldiers.")]
    float _soldierSeparation = DefaultSoldierSeparation;
    [SerializeField, Range(0f, 1f), Tooltip("Random jitter added to soldier steering.")]
    float _soldierBrownianMotion = 0.04f;

    public float SpeedUnitsPerStatPoint => _speedUnitsPerStatPoint;
    public float ActionWindowSeconds => _actionWindowSeconds;
    public int MaxWeaponsPerSoldier => _maxWeaponsPerSoldier;
    public int MaxWeaponSkillsPerSoldier => _maxWeaponSkillsPerSoldier;
    public float BruiseHealPerSecond => _bruiseHealPerSecond;
    public float FatigueFraction => _fatigueFraction;

    public float AccelerationPerSpeedPoint => _accelerationPerSpeedPoint;
    public float SoldierAlignment => _soldierAlignment;
    public float SoldierSeparation => _soldierSeparation;
    public float SoldierBrownianMotion => _soldierBrownianMotion;
}
