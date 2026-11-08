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

    // Every section, in Inspector order. Tests use it to find the section that owns a serialized field.
    public IReadOnlyList<ScriptableObject> Sections => new ScriptableObject[]
        { Soldier, Formation, Morale, Melee, Chain, Ranged, Input };

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

    static T Fresh<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        instance.hideFlags = HideFlags.DontSave;
        return instance;
    }

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
    public bool StunAtZeroHp => Soldier.StunAtZeroHp;
    public int StunRecoverHp => Soldier.StunRecoverHp;
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
    public float EngagementHoldMargin => Formation.EngagementHoldMargin;
    public float NaturalLineRadiusScale => Formation.NaturalLineRadiusScale;
    public float TroopSizeScale => Formation.TroopSizeScale;
    public float BlobRadiusScale => Formation.BlobRadiusScale;
    public float PivotSeparationScale => Formation.PivotSeparationScale;
    public float FriendlyGap => Formation.FriendlyGap;
    public float FriendlyGiveWaySpeedScale => Formation.FriendlyGiveWaySpeedScale;
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
    public float DisorderSeconds => Morale.DisorderSeconds;
    public float FrontFaceHalfAngleDeg => Morale.FrontFaceHalfAngleDeg;
    public float RearFaceAngleDeg => Morale.RearFaceAngleDeg;

    // --- Melee Tuning ---

    public float ChargeGapTolerance => Melee.ChargeGapTolerance;
    public int ChargeCandidateCount => Melee.ChargeCandidateCount;
    public float ChargeSpeedScale => Melee.ChargeSpeedScale;
    public float ChargeTimeoutSeconds => Melee.ChargeTimeoutSeconds;
    public float ChargeFollowProbability => Melee.ChargeFollowProbability;
    // Window for the charge rate: EffectiveAggression charges per this many seconds (SU-112).
    public float ChargeWindowSeconds => Melee.ChargeWindowSeconds;
    public float ClashCooldownSeconds => Melee.ClashCooldownSeconds;
    public float SupportRadius => Melee.SupportRadius;
    public float SpearSupportRangeMultiplier => Melee.SpearSupportRangeMultiplier;
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
    public float AutoLinkMaxFacingDeg => Chain.AutoLinkMaxFacingDeg;
    public float AutoLinkBreakFacingDeg => Chain.AutoLinkBreakFacingDeg;
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
    public float DoubleClickSeconds => Input.DoubleClickSeconds;

    // --- Derived values used in several places ---

    // A soldier's charge reach (SU-95): A_Distance between the two soldiers' edges, like the footprint gap, so centre to
    // centre it is A_Distance + 2 soldier radii. Lines held EngagementGap apart have their front ranks EngagementGap + 2
    // radii apart, so with A_Distance = EngagementGap the front ranks can still reach each other.
    public float ChargeRange => ADistance + 2f * TroopConstants.SoldierRadius;
    // Soldiers start charges only while their troop is this close (footprint gap) to an enemy troop (SU-95).
    public float ChargeGap => EngagementGap + ChargeGapTolerance;
    public bool FrontRankChargesOnly => Melee.FrontRankChargesOnly;
    // L_nat = Frontage x LineSpacing (SU-103).
    public float NaturalLineLength(int frontage) => LineGeometry.NaturalLength(frontage);
    // The troop's radius in world units from its frontage and soldier count (SU-103, LineGeometry.TroopRadius).
    public float TroopRadius(int frontage, int soldiers) =>
        LineGeometry.TroopRadius(frontage, soldiers, NaturalLineRadiusScale, BlobRadiusScale, TroopSizeScale);
}
