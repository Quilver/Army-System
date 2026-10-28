using UnityEngine;

// Named, configurable tuning values (spec §7.2). Holds only values that are not combat stats. Combat stat limits
// (1-20, default 5; HP 10-40, default 15) are in CombatStatLimits, and values that never change (soldier radius, line
// spacing, contact distance) are constants in TroopConstants.
// Loaded from Assets/Resources/Boid Profiles/Troop Tuning.asset. Falls back to defaults when the asset is missing.
// New fields take their C# default when an older asset does not contain them.
[CreateAssetMenu(menuName = "Troops/Troop Tuning")]
public sealed class TroopTuning : ScriptableObject
{
    public const string ResourcePath = "Boid Profiles/Troop Tuning";

    static TroopTuning _active;

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

    [Header("Distances (spec §3, §4.1)")]
    [SerializeField, Min(0f), Tooltip("S_Distance: enemy footprint gap at which a troop forms a battle line.")]
    float _sDistance = 30f;
    [SerializeField, Min(0f), Tooltip("Extra footprint gap before a threat-facing line is dropped again. The spec has none (0).")]
    float _sDistanceExitHysteresis = 0f;
    [SerializeField, Min(0f), Tooltip("A_Distance: footprint gap at which two opposing troops are engaged.")]
    float _aDistance = 5.2f;
    [SerializeField, Min(0f), Tooltip("EngagementGap: clear footprint gap two engaged lines hold between them. Replaces LineGap (0.3).")]
    float _engagementGap = 2f;

    [Header("Formation and movement (spec §3)")]
    [SerializeField, Min(0.1f), Tooltip("L_nat = this x TroopRadius. Provisional (Q-12).")]
    float _naturalLineRadiusScale = 1.8f;
    [SerializeField, Range(0.1f, 1f), Tooltip("Idle blob disc radius as a fraction of TroopRadius.")]
    float _blobRadiusScale = 0.85f;
    [SerializeField, Min(0f), Tooltip("Minimum spacing of friendly pivots, x (r_A + r_B). Provisional.")]
    float _pivotSeparationScale = 0.25f;
    [SerializeField, Min(0.05f), Tooltip("Path rebuild interval while marching.")]
    float _pathRebuildIntervalSeconds = 0.5f;
    [SerializeField, Min(0f), Tooltip("Joint clearance at a linked end: min(this, fraction x length). Provisional.")]
    float _jointClearance = 0.45f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Joint clearance as a fraction of the line length.")]
    float _jointClearanceFraction = 0.15f;
    [SerializeField, Min(0.5f), Tooltip("A routed troop's soldiers scatter in a disc this many times its radius.")]
    float _routScatterRadiusScale = 1.6f;

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
    float _retreatWaveDelaySeconds = 1.0f;
    [SerializeField, Min(0.05f), Tooltip("Time the rearmost soldier takes to fall back to its new place in a retreat (fastest).")]
    float _retreatRearMoveSeconds = 0.8f;
    [SerializeField, Min(0.05f), Tooltip("Time the frontmost soldier takes to fall back to its new place in a retreat (slowest).")]
    float _retreatFrontMoveSeconds = 1.8f;
    [SerializeField, Min(1f), Tooltip("Shock multiplier on flanked hits (spec §4.8).")]
    float _flankShockMultiplier = 3f;

    [Header("Faces (spec §4.8, Q-31)")]
    [SerializeField, Range(0f, 90f), Tooltip("Attacks within this angle of the facing hit the Front face.")]
    float _frontFaceHalfAngleDeg = 45f;
    [SerializeField, Range(90f, 180f), Tooltip("Attacks beyond this angle from the facing hit the Rear face.")]
    float _rearFaceAngleDeg = 135f;

    [Header("Charges and clashes (spec §4.3-§4.5)")]
    [SerializeField, Min(0f), Tooltip("Charge range as a multiple of A_Distance (7.8 units). Q-25.")]
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
    [SerializeField, Min(1f), Tooltip("Support range multiplier for a Boar Spear supporter (Q-7).")]
    float _boarSpearSupportRangeMultiplier = 2f;
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

    [Header("Chains (spec §3.4)")]
    [SerializeField, Min(0.1f), Tooltip("Minimum line length of a linked troop, x L_nat.")]
    float _chainMinStretchScale = 0.7f;
    [SerializeField, Min(1f), Tooltip("Maximum line length of a linked troop, x L_nat. Longer breaks the link.")]
    float _chainMaxStretchScale = 3f;
    [SerializeField, Range(0f, 180f), Tooltip("Minimum inner hinge angle at a joint. Sharper breaks the link.")]
    float _chainMinInnerAngleDeg = 65f;
    [SerializeField, Min(0f), Tooltip("Endpoint-to-endpoint attach distance, x L_nat.")]
    float _attachScale = 0.25f;
    [SerializeField, Min(0f), Tooltip("ChainSuppressed ends this long after the troop leaves every attach radius (Q-22).")]
    float _chainSuppressCooldownSeconds = 3f;
    [SerializeField, Min(1f), Tooltip("Stretch (x L_nat) that makes an unengaged neighbour retreat with its chain.")]
    float _chainRetreatStretchThreshold = 1.4f;
    [SerializeField, Range(0f, 180f), Tooltip("Hinge bend (180 - inner angle) that makes an unengaged neighbour retreat. Q-13.")]
    float _chainRetreatHingeBendDeg = 30f;
    [SerializeField, Range(0f, 1f), Tooltip("Fraction of the retreat distance lost per hop. Provisional (Q-21).")]
    float _chainRetreatFalloff = 0.3f;
    [SerializeField, Min(0.01f), Tooltip("Speed at which a stretched endpoint relaxes to L_nat after its link breaks.")]
    float _chainEndpointRelaxSpeed = 2f;
    [SerializeField, Min(0f), Tooltip("Link spring: gap allowed before the spring pulls.")]
    float _linkSlack = 0.1f;
    [SerializeField, Min(0f), Tooltip("Link spring stiffness.")]
    float _linkStiffness = 5f;
    [SerializeField, Min(0f), Tooltip("Link spring maximum force.")]
    float _linkMaxForce = 10.4f;

    [Header("Ranged (spec §4.7)")]
    [SerializeField, Min(1), Tooltip("Shot difficulty in the hit formula.")]
    int _baseShotDC = 10;
    [SerializeField, Range(0f, 1f), Tooltip("Random variation of the fire interval.")]
    float _fireRateJitterFraction = 0.1f;
    [SerializeField, Min(0.1f), Tooltip("Projectile speed. Not in the spec (Q-42), provisional.")]
    float _projectileSpeed = 20f;
    [SerializeField, Range(0f, 180f), Tooltip("Targets must lie within this angle of the troop's facing (Q-41).")]
    float _rangedFrontArcDeg = 45f;

    [Header("Input (spec §6, screen pixels)")]
    [SerializeField, Min(1f)] float _boxSelectMinDragPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Provisional (Q-47).")] float _orderDragMinPixels = 8f;
    [SerializeField, Min(1f), Tooltip("Click pick radius around a pivot or soldier.")] float _clickPickRadiusPixels = 24f;

    public static TroopTuning Active
    {
        get
        {
            if (_active == null)
            {
                _active = Resources.Load<TroopTuning>(ResourcePath);
                if (_active == null)
                {
                    _active = CreateInstance<TroopTuning>();
                    _active.hideFlags = HideFlags.DontSave;
                }
            }
            return _active;
        }
    }

    // Tests swap in their own instance. Pass null to go back to the asset.
    public static void OverrideForTests(TroopTuning tuning) => _active = tuning;

    public float SpeedUnitsPerStatPoint => _speedUnitsPerStatPoint;
    public float ActionWindowSeconds => _actionWindowSeconds;
    public int MaxWeaponsPerSoldier => _maxWeaponsPerSoldier;
    public int MaxWeaponSkillsPerSoldier => _maxWeaponSkillsPerSoldier;
    public float BruiseHealPerSecond => _bruiseHealPerSecond;
    public float FatigueFraction => _fatigueFraction;

    public float SDistance => _sDistance;
    public float SDistanceExitHysteresis => _sDistanceExitHysteresis;
    public float ADistance => _aDistance;
    public float EngagementGap => _engagementGap;

    public float NaturalLineRadiusScale => _naturalLineRadiusScale;
    public float BlobRadiusScale => _blobRadiusScale;
    public float PivotSeparationScale => _pivotSeparationScale;
    public float PathRebuildIntervalSeconds => _pathRebuildIntervalSeconds;
    public float JointClearance => _jointClearance;
    public float JointClearanceFraction => _jointClearanceFraction;
    public float RoutScatterRadiusScale => _routScatterRadiusScale;

    public int RetreatMoralePenalty => _retreatMoralePenalty;
    public int SurroundedMoralePenalty => _surroundedMoralePenalty;
    public float ShockDecayFractionPerWindow => _shockDecayFractionPerWindow;
    public float RoutMoraleThresholdFraction => _routMoraleThresholdFraction;
    public float RoutDespawnSeconds => _routDespawnSeconds;
    public float RetreatDistance => _retreatDistance;
    public float RetreatWaveDelaySeconds => _retreatWaveDelaySeconds;
    public float RetreatRearMoveSeconds => _retreatRearMoveSeconds;
    public float RetreatFrontMoveSeconds => _retreatFrontMoveSeconds;
    // Move time for a soldier at frontness 0 (rear) to 1 (front); the slowest soldier sets the retreat's length.
    public float RetreatMoveSecondsAt(float frontness) =>
        Mathf.Lerp(_retreatRearMoveSeconds, _retreatFrontMoveSeconds, Mathf.Clamp01(frontness));
    public float RetreatSlowestMoveSeconds => Mathf.Max(_retreatRearMoveSeconds, _retreatFrontMoveSeconds);
    public float FlankShockMultiplier => _flankShockMultiplier;

    public float FrontFaceHalfAngleDeg => _frontFaceHalfAngleDeg;
    public float RearFaceAngleDeg => _rearFaceAngleDeg;

    public float ChargeRangeScale => _chargeRangeScale;
    public int ChargeCandidateCount => _chargeCandidateCount;
    public float ChargeSpeedScale => _chargeSpeedScale;
    public float ChargeTimeoutSeconds => _chargeTimeoutSeconds;
    public float ChargeFollowProbability => _chargeFollowProbability;
    public float ClashCooldownSeconds => _clashCooldownSeconds;

    public float SupportRadius => _supportRadius;
    public float BoarSpearSupportRangeMultiplier => _boarSpearSupportRangeMultiplier;
    public int SupportBonusPerAlly => _supportBonusPerAlly;
    public float DominationDivisor => _dominationDivisor;

    public float AssaultDurationSeconds => _assaultDurationSeconds;
    public int AssaultAggressionBonus => _assaultAggressionBonus;
    public int AssaultChargeBonus => _assaultChargeBonus;

    public float ChainMinStretchScale => _chainMinStretchScale;
    public float ChainMaxStretchScale => _chainMaxStretchScale;
    public float ChainMinInnerAngleDeg => _chainMinInnerAngleDeg;
    public float AttachScale => _attachScale;
    public float ChainSuppressCooldownSeconds => _chainSuppressCooldownSeconds;
    public float ChainRetreatStretchThreshold => _chainRetreatStretchThreshold;
    public float ChainRetreatHingeBendDeg => _chainRetreatHingeBendDeg;
    public float ChainRetreatFalloff => _chainRetreatFalloff;
    public float ChainEndpointRelaxSpeed => _chainEndpointRelaxSpeed;
    public float LinkSlack => _linkSlack;
    public float LinkStiffness => _linkStiffness;
    public float LinkMaxForce => _linkMaxForce;

    public int BaseShotDC => _baseShotDC;
    public float FireRateJitterFraction => _fireRateJitterFraction;
    public float ProjectileSpeed => _projectileSpeed;
    public float RangedFrontArcDeg => _rangedFrontArcDeg;

    public float BoxSelectMinDragPixels => _boxSelectMinDragPixels;
    public float OrderDragMinPixels => _orderDragMinPixels;
    public float ClickPickRadiusPixels => _clickPickRadiusPixels;

    // Derived values used in several places.
    public float ChargeRange => _chargeRangeScale * _aDistance;
    public float NaturalLineLength(float troopRadius) => LineGeometry.NaturalLength(troopRadius, _naturalLineRadiusScale);
}
