using System.Collections.Generic;
using UnityEngine;

// Named, configurable tuning values (spec §7.2), split by section into ScriptableObject assets in
// Assets/Resources/Battle Tuning/:
//   Soldier Tuning     stats, actions and soldier steering          (SoldierTuning)
//   Formation Tuning   S_Distance, A_Distance, formation, movement  (FormationTuning)
//   Morale Tuning      morale, retreat, rout and faces              (MoraleTuning)
//   Melee Tuning       charges, clashes, support, duels, Assault    (MeleeTuning)
//   Chain Tuning       chains and link springs                      (ChainTuning)
//   Ranged Tuning      ranged fire and composite ranks              (RangedTuning)
//   Input Tuning       player input thresholds                      (InputTuning)
// This class gathers the sections so code reads every value from one place (TroopTuning.Active.X). It holds only values
// that are not combat stats: combat stat limits are in CombatStatLimits, and values that never change (soldier radius,
// line spacing, contact distance) are constants in TroopConstants.
// A missing section asset falls back to its C# defaults. New fields take their C# default when an older asset does not
// contain them.
public sealed class TroopTuning
{
    public const string ResourceFolder = "Battle Tuning";
    public const string AssetFolder = "Assets/Resources/" + ResourceFolder;

    static TroopTuning _active;

    public SoldierTuning Soldier { get; }
    public FormationTuning Formation { get; }
    public MoraleTuning Morale { get; }
    public MeleeTuning Melee { get; }
    public ChainTuning Chain { get; }
    public RangedTuning Ranged { get; }
    public InputTuning Input { get; }

    TroopTuning(SoldierTuning soldier, FormationTuning formation, MoraleTuning morale, MeleeTuning melee, ChainTuning chain,
        RangedTuning ranged, InputTuning input)
    {
        Soldier = soldier;
        Formation = formation;
        Morale = morale;
        Melee = melee;
        Chain = chain;
        Ranged = ranged;
        Input = input;
    }

<<<<<<< HEAD
    // Every section, in Inspector order. Tests use it to find the section that owns a serialized field.
    public IReadOnlyList<ScriptableObject> Sections => new ScriptableObject[]
        { Soldier, Formation, Morale, Melee, Chain, Ranged, Input };
=======
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
>>>>>>> 3d6160f0ae6d113d825a20452525e7614893f92e

    public static TroopTuning Active
    {
        get
        {
            if (_active == null || !_active.IsAlive) _active = LoadFromResources();
            return _active;
        }
    }

    // A tuning made of fresh section instances with the C# (spec) defaults, not the project's assets. Tests use it.
    // Destroy it with DestroySections when done.
    public static TroopTuning CreateDefaults() => new(
        Fresh<SoldierTuning>(), Fresh<FormationTuning>(), Fresh<MoraleTuning>(), Fresh<MeleeTuning>(),
        Fresh<ChainTuning>(), Fresh<RangedTuning>(), Fresh<InputTuning>());

    // Tests swap in their own instance. Pass null to go back to the assets.
    public static void OverrideForTests(TroopTuning tuning) => _active = tuning;

    // Destroys section instances made by CreateDefaults. Never call it on the loaded assets.
    public void DestroySections()
    {
        foreach (var section in Sections)
        {
            if (section == null) continue;
            if (Application.isPlaying) Object.Destroy(section);
            else Object.DestroyImmediate(section);
        }
    }

    static TroopTuning LoadFromResources() => new(
        Load<SoldierTuning>(SoldierTuning.AssetName), Load<FormationTuning>(FormationTuning.AssetName),
        Load<MoraleTuning>(MoraleTuning.AssetName), Load<MeleeTuning>(MeleeTuning.AssetName),
        Load<ChainTuning>(ChainTuning.AssetName), Load<RangedTuning>(RangedTuning.AssetName),
        Load<InputTuning>(InputTuning.AssetName));

    static T Load<T>(string assetName) where T : ScriptableObject
    {
        T asset = Resources.Load<T>(ResourceFolder + "/" + assetName);
        return asset != null ? asset : Fresh<T>();
    }

<<<<<<< HEAD
    static T Fresh<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        instance.hideFlags = HideFlags.DontSave;
        return instance;
    }
=======
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
>>>>>>> 3d6160f0ae6d113d825a20452525e7614893f92e

    // A section destroyed under us (a test tearing down, or an asset deleted) makes the cached tuning stale.
    bool IsAlive => Soldier != null && Formation != null && Morale != null && Melee != null && Chain != null
        && Ranged != null && Input != null;

    // --- Soldier Tuning ---

    public const float DefaultSoldierSeparation = SoldierTuning.DefaultSoldierSeparation;
    public float SpeedUnitsPerStatPoint => Soldier.SpeedUnitsPerStatPoint;
    public float ActionWindowSeconds => Soldier.ActionWindowSeconds;
    public int MaxWeaponsPerSoldier => Soldier.MaxWeaponsPerSoldier;
    public int MaxWeaponSkillsPerSoldier => Soldier.MaxWeaponSkillsPerSoldier;
    public float BruiseHealPerSecond => Soldier.BruiseHealPerSecond;
    public float FatigueFraction => Soldier.FatigueFraction;
    // Acceleration itself is CharacterProfile.Acceleration (Speed x AccelerationPerSpeedPoint).
    public float AccelerationPerSpeedPoint => Soldier.AccelerationPerSpeedPoint;
    public float SoldierAlignment => Soldier.SoldierAlignment;
    public float SoldierSeparation => Soldier.SoldierSeparation;
    public float SoldierBrownianMotion => Soldier.SoldierBrownianMotion;

    // --- Formation Tuning ---

    public float SDistance => Formation.SDistance;
    public float SDistanceExitHysteresis => Formation.SDistanceExitHysteresis;
    public float ADistance => Formation.ADistance;
    public float EngagementGap => Formation.EngagementGap;
    public float NaturalLineRadiusScale => Formation.NaturalLineRadiusScale;
    public float BlobRadiusScale => Formation.BlobRadiusScale;
    public float PivotSeparationScale => Formation.PivotSeparationScale;
    public float PathRebuildIntervalSeconds => Formation.PathRebuildIntervalSeconds;
    public float JointClearance => Formation.JointClearance;
    public float JointClearanceFraction => Formation.JointClearanceFraction;
    public float RoutScatterRadiusScale => Formation.RoutScatterRadiusScale;

    // --- Morale Tuning ---

    public int RetreatMoralePenalty => Morale.RetreatMoralePenalty;
    public int SurroundedMoralePenalty => Morale.SurroundedMoralePenalty;
    public float ShockDecayFractionPerWindow => Morale.ShockDecayFractionPerWindow;
    public float RoutMoraleThresholdFraction => Morale.RoutMoraleThresholdFraction;
    public float RoutDespawnSeconds => Morale.RoutDespawnSeconds;
    public float RetreatDistance => Morale.RetreatDistance;
    public float RetreatWaveDelaySeconds => Morale.RetreatWaveDelaySeconds;
    public float RetreatSoldierMoveSeconds => Morale.RetreatSoldierMoveSeconds;
    public float FlankShockMultiplier => Morale.FlankShockMultiplier;
    public float FrontFaceHalfAngleDeg => Morale.FrontFaceHalfAngleDeg;
    public float RearFaceAngleDeg => Morale.RearFaceAngleDeg;

    // --- Melee Tuning ---

    public float ChargeRangeScale => Melee.ChargeRangeScale;
    public int ChargeCandidateCount => Melee.ChargeCandidateCount;
    public float ChargeSpeedScale => Melee.ChargeSpeedScale;
    public float ChargeTimeoutSeconds => Melee.ChargeTimeoutSeconds;
    public float ChargeFollowProbability => Melee.ChargeFollowProbability;
    public float ClashCooldownSeconds => Melee.ClashCooldownSeconds;
    public float SupportRadius => Melee.SupportRadius;
    public float BoarSpearSupportRangeMultiplier => Melee.BoarSpearSupportRangeMultiplier;
    public int SupportBonusPerAlly => Melee.SupportBonusPerAlly;
    public float DominationDivisor => Melee.DominationDivisor;
    public float AssaultDurationSeconds => Melee.AssaultDurationSeconds;
    public int AssaultAggressionBonus => Melee.AssaultAggressionBonus;
    public int AssaultChargeBonus => Melee.AssaultChargeBonus;

    // --- Chain Tuning ---

    public float ChainMinStretchScale => Chain.ChainMinStretchScale;
    public float ChainMaxStretchScale => Chain.ChainMaxStretchScale;
    public float ChainMinInnerAngleDeg => Chain.ChainMinInnerAngleDeg;
    public float AttachScale => Chain.AttachScale;
    public float ChainSuppressCooldownSeconds => Chain.ChainSuppressCooldownSeconds;
    public float ChainRetreatStretchThreshold => Chain.ChainRetreatStretchThreshold;
    public float ChainRetreatHingeBendDeg => Chain.ChainRetreatHingeBendDeg;
    public float ChainRetreatFalloff => Chain.ChainRetreatFalloff;
    public float ChainEndpointRelaxSpeed => Chain.ChainEndpointRelaxSpeed;
    public float LinkSlack => Chain.LinkSlack;
    public float LinkStiffness => Chain.LinkStiffness;
    public float LinkMaxForce => Chain.LinkMaxForce;

    // --- Ranged Tuning ---

    public int BaseShotDC => Ranged.BaseShotDC;
    public float FireRateJitterFraction => Ranged.FireRateJitterFraction;
    public float ProjectileSpeed => Ranged.ProjectileSpeed;
    public float RangedFrontArcDeg => Ranged.RangedFrontArcDeg;
    public float CompositeRankGain => Ranged.CompositeRankGain;
    public float CompositeRankDepthFraction => Ranged.CompositeRankDepthFraction;

    // --- Input Tuning ---

    public float BoxSelectMinDragPixels => Input.BoxSelectMinDragPixels;
    public float OrderDragMinPixels => Input.OrderDragMinPixels;
    public float ClickPickRadiusPixels => Input.ClickPickRadiusPixels;

    // --- Derived values used in several places ---

    public float ChargeRange => ChargeRangeScale * ADistance;
    public float NaturalLineLength(float troopRadius) => LineGeometry.NaturalLength(troopRadius, NaturalLineRadiusScale);
}
