using System.Collections.Generic;
using UnityEngine;

// Named, configurable tuning values (spec §7.2), split by section into ScriptableObject assets in
// Assets/Resources/Battle Tuning/:
//   Soldier Tuning     stats, actions and soldier steering          (SoldierTuning)
//   Formation Tuning   S_Distance, A_Distance, formation, movement  (FormationTuning)
//   Morale Tuning      morale, retreat, rout and faces              (MoraleTuning)
//   Melee Tuning       charges, feints, clashes, support, duels     (MeleeTuning)
//   Chain Tuning       chains and link springs                      (ChainTuning)
//   Ranged Tuning      ranged fire and composite ranks              (RangedTuning)
//   Input Tuning       player input thresholds                      (InputTuning)
//   Chain Bonus Tuning Defence and Morale per chain link (SU-152)   (ChainBonusTuning)
//   Battle End Tuning  slow-down and banner when the battle ends (SU-168)  (BattleEndTuning)
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
    public ChainBonusTuning ChainBonus { get; }
    public BattleEndTuning BattleEnd { get; }

    TroopTuning(SoldierTuning soldier, FormationTuning formation, MoraleTuning morale, MeleeTuning melee, ChainTuning chain,
        RangedTuning ranged, InputTuning input, ChainBonusTuning chainBonus, BattleEndTuning battleEnd)
    {
        Soldier = soldier;
        Formation = formation;
        Morale = morale;
        Melee = melee;
        Chain = chain;
        Ranged = ranged;
        Input = input;
        ChainBonus = chainBonus;
        BattleEnd = battleEnd;
    }

    // Every section, in Inspector order. Tests use it to find the section that owns a serialized field.
    public IReadOnlyList<ScriptableObject> Sections => new ScriptableObject[]
        { Soldier, Formation, Morale, Melee, Chain, Ranged, Input, ChainBonus, BattleEnd };

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
        Fresh<ChainTuning>(), Fresh<RangedTuning>(), Fresh<InputTuning>(), Fresh<ChainBonusTuning>(),
        Fresh<BattleEndTuning>());

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
        Load<InputTuning>(InputTuning.AssetName), Load<ChainBonusTuning>(ChainBonusTuning.AssetName),
        Load<BattleEndTuning>(BattleEndTuning.AssetName));

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
        && Ranged != null && Input != null && ChainBonus != null && BattleEnd != null;

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
    // Soldier wave (SU-166): the march's start, stop and turns ripple from the leading edge back.
    public bool SoldierWave => Soldier.SoldierWave;
    public float SoldierWaveSpeed => Soldier.SoldierWaveSpeed;
    public float SoldierWaveMaxLag => Soldier.SoldierWaveMaxLag;
    public float SoldierWaveBunchFraction => Soldier.SoldierWaveBunchFraction;
    public float SoldierWaveCatchUpSpeedScale => Soldier.SoldierWaveCatchUpSpeedScale;
    // Soldier facing in melee, display only (SU-158).
    public float EngagedFacingTurnRateDeg => Soldier.EngagedFacingTurnRateDeg;
    public float FacingTargetRepickSeconds => Soldier.FacingTargetRepickSeconds;
    public float FacingTargetKeepScale => Soldier.FacingTargetKeepScale;
    public float FacingReachSlack => Soldier.FacingReachSlack;

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
    public bool ChainFillToJoints => Formation.ChainFillToJoints;
    public float RoutScatterRadiusScale => Formation.RoutScatterRadiusScale;
    public float OutlinePadding => Formation.OutlinePadding;
    public bool LineMarchSlowdown => Formation.LineMarchSlowdown;
    public float LineMarchSpeedScale => Formation.LineMarchSpeedScale;
    public bool LineBackwardSlowdown => Formation.LineBackwardSlowdown;
    public float LineBackwardSpeedScale => Formation.LineBackwardSpeedScale;
    public float LineBackwardAngleDeg => Formation.LineBackwardAngleDeg;
    public bool LineWheel => Formation.LineWheel;
    public float LineWheelRateDeg => Formation.LineWheelRateDeg;
    public bool LineTurnSlowdown => Formation.LineTurnSlowdown;
    public float LineTurnAngleDeg => Formation.LineTurnAngleDeg;
    public bool AssaultWheel => Formation.AssaultWheel;
    public bool AssaultWheelAboutEngagedEnd => Formation.AssaultWheelAboutEngagedEnd;
    public float AssaultWheelToleranceDeg => Formation.AssaultWheelToleranceDeg;
    public bool AssaultFaceHold => Formation.AssaultFaceHold;
    public float AssaultFaceHysteresisDeg => Formation.AssaultFaceHysteresisDeg;
    public bool MutualPursuitFrontToFront => Formation.MutualPursuitFrontToFront;
    public bool AssaultHold => Formation.AssaultHold;
    // No strafing in line (SU-172).
    public bool LineNoStrafe => Formation.LineNoStrafe;
    public float LineAdvanceAngleDeg => Formation.LineAdvanceAngleDeg;
    public float LineStrafeMaxDistance => Formation.LineStrafeMaxDistance;
    public float LineWheelFirstMaxSeconds => Formation.LineWheelFirstMaxSeconds;
    // Wheeling on steering circles (SU-173).
    public bool LineWheelOnSteeringCircles => Formation.LineWheelOnSteeringCircles;
    public float LineTurnRadiusScale => Formation.LineTurnRadiusScale;
    public float LineTurnMinRadiusShare => Formation.LineTurnMinRadiusShare;
    // Troop border fill (SU-170).
    public float FriendlyFillAlpha => Formation.FriendlyFillAlpha;
    public float EnemyFillAlpha => Formation.EnemyFillAlpha;
    public float HoverFillAlphaBonus => Formation.HoverFillAlphaBonus;
    public float SelectedFillAlphaBonus => Formation.SelectedFillAlphaBonus;
    public float FillEdgeFade => Formation.FillEdgeFade;

    // --- Morale Tuning ---

    public int RetreatMoralePenalty => Morale.RetreatMoralePenalty;
    public int SurroundedMoralePenalty => Morale.SurroundedMoralePenalty;
    public float ShockDecayFractionPerWindow => Morale.ShockDecayFractionPerWindow;
    public float RoutMoraleThresholdFraction => Morale.RoutMoraleThresholdFraction;
    public float RoutDespawnSeconds => Morale.RoutDespawnSeconds;
    public float RetreatDistance => Morale.RetreatDistance;
    public float RetreatWaveDelaySeconds => Morale.RetreatWaveDelaySeconds;
    public float RetreatRearMoveSeconds => Morale.RetreatRearMoveSeconds;
    public float RetreatFrontMoveSeconds => Morale.RetreatFrontMoveSeconds;
    // Move time for a soldier at frontness 0 (rear) to 1 (front) of a retreat (SU-133).
    public float RetreatMoveSecondsAt(float frontness) =>
        Mathf.Lerp(RetreatRearMoveSeconds, RetreatFrontMoveSeconds, Mathf.Clamp01(frontness));
    // The slowest soldier's move time sets the retreat's length.
    public float RetreatSlowestMoveSeconds => Mathf.Max(RetreatRearMoveSeconds, RetreatFrontMoveSeconds);
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
    // Feints per real charge, a feint's knock-back share and its timeout (SU-164).
    public float FeintsPerCharge => Melee.FeintsPerCharge;
    public float FeintKnockBackScale => Melee.FeintKnockBackScale;
    public float FeintTimeoutSeconds => Melee.FeintTimeoutSeconds;
    // Whether an open Assault makes its soldiers charge (SU-165).
    public bool AssaultCharges => Melee.AssaultCharges;
    // The Assault's chain reaction (SU-171).
    public bool AssaultChainReaction => Melee.AssaultChainReaction;
    // The Assault goes in one wave once the troop is square (SU-174).
    public bool AssaultOneWave => Melee.AssaultOneWave;
    public float AssaultChainSpeed => Melee.AssaultChainSpeed;
    public float AssaultChainSettleSeconds => Melee.AssaultChainSettleSeconds;
    public float AssaultChainMaxWaitSeconds => Melee.AssaultChainMaxWaitSeconds;
    public float SupportRadius => Melee.SupportRadius;
    public float SpearSupportRangeMultiplier => Melee.SpearSupportRangeMultiplier;
    public float SupportBonusPerSupportPoint => Melee.SupportBonusPerSupportPoint;
    public int MaxSupporters => Melee.MaxSupporters;
    public float DominationDivisor => Melee.DominationDivisor;
    public bool EloDuel => Melee.EloDuel;
    public float EloScale => Melee.EloScale;

    // --- Chain Tuning ---

    public float ChainMinStretchScale => Chain.ChainMinStretchScale;
    public float ChainMaxStretchScale => Chain.ChainMaxStretchScale;
    public float ChainMinInnerAngleDeg => Chain.ChainMinInnerAngleDeg;
    public float AttachScale => Chain.AttachScale;
    public float LineOrderFacingDeadZone => Chain.LineOrderFacingDeadZone;
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

    // --- Chain Bonus Tuning (SU-152) ---

    // TotalDefence and BaseMorale bonuses for a troop with this many active chain links (0 while the bonus is off).
    public int ChainDefenceBonus(int activeLinks) => ChainBonus.DefenceBonus(activeLinks);
    public int ChainMoraleBonus(int activeLinks) => ChainBonus.MoraleBonus(activeLinks);

    // --- Ranged Tuning ---

    public int BaseShotDC => Ranged.BaseShotDC;
    public float FireRateJitterFraction => Ranged.FireRateJitterFraction;
    public float ProjectileSpeed => Ranged.ProjectileSpeed;
    public float RangedFrontArcDeg => Ranged.RangedFrontArcDeg;
    // Layers whose colliders block direct shots' line of sight (SU-136).
    public LayerMask SightBlockingLayers => Ranged.SightBlockingLayers;
    // SU-151: each soldier has its weapon's Ammunition for the battle.
    public bool LimitedAmmunition => Ranged.LimitedAmmunition;
    public float CompositeRankGain => Ranged.CompositeRankGain;
    public float CompositeRankDepthFraction => Ranged.CompositeRankDepthFraction;

    // --- Input Tuning ---

    public float BoxSelectMinDragPixels => Input.BoxSelectMinDragPixels;
    public float OrderDragMinPixels => Input.OrderDragMinPixels;
    public float ClickPickRadiusPixels => Input.ClickPickRadiusPixels;
    public float DoubleClickSeconds => Input.DoubleClickSeconds;
    public float AttackClickSlopPixels => Input.AttackClickSlopPixels;

    // --- Battle End Tuning (SU-168) ---

    public float EndSlowSpeed => BattleEnd.EndSlowSpeed;
    public float EndBannerSeconds => BattleEnd.EndBannerSeconds;

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
