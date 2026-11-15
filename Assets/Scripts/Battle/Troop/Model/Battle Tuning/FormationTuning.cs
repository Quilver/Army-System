using UnityEngine;

// Battle Tuning: engagement distances, formation and movement (spec §3, §4.1, §7.2).
// Asset: Assets/Resources/Battle Tuning/Formation Tuning.asset. Read through TroopTuning.Active.
[CreateAssetMenu(menuName = "Battle Tuning/Formation Tuning")]
public sealed class FormationTuning : ScriptableObject
{
    public const string AssetName = "Formation Tuning";

    [Header("Distances (spec §3, §4.1)")]
    [SerializeField, Min(0f), Tooltip("S_Distance: enemy footprint gap at which a troop forms a battle line.")]
    float _sDistance = 30f;
    [SerializeField, Min(0f), Tooltip("Minimum extra footprint gap before a threat-facing line is dropped again. The exit margin is at least BlobRadiusScale x (both troop radii), so a formation change cannot flick a troop across S_Distance.")]
    float _sDistanceExitHysteresis = 0f;
    [SerializeField, Min(0f), Tooltip("A_Distance: footprint gap at which two opposing troops are engaged.")]
    float _aDistance = 5.2f;
    [SerializeField, Min(0f), Tooltip("EngagementGap: clear footprint gap two engaged lines hold between them. Replaces LineGap (0.3).")]
    float _engagementGap = 2f;
    [SerializeField, Min(0f), Tooltip("Engagement hold (exit hysteresis, Q-16): troops that are engaged stay engaged until their footprint gap passes A_Distance + this.")]
    float _engagementHoldMargin = 1f;

    [Header("Formation and movement (spec §3)")]
    [SerializeField, Min(0.1f), Tooltip("L_nat / troop radius. The natural line is Frontage x LineSpacing long (SU-103); the troop's radius in " +
             "units (blob, soldier clamp) is L_nat / this, or larger when its soldiers need more room.")]
    float _naturalLineRadiusScale = 1.8f;
    [SerializeField, Min(0.1f), Tooltip("Multiplies the troop's radius (blob, soldier clamp, pivot and deployment spacing) so its soldiers " +
             "have room (SU-131). 1 is the size from frontage and soldier count alone.")]
    float _troopSizeScale = 1.5f;
    [SerializeField, Range(0.1f, 1f), Tooltip("Idle blob disc radius as a fraction of the troop radius.")]
    float _blobRadiusScale = 0.85f;
    [SerializeField, Min(0f), Tooltip("Minimum spacing of friendly pivots, x (r_A + r_B).")]
    float _pivotSeparationScale = 0.25f;
    [SerializeField, Min(0f), Tooltip("Smallest clear gap between the outlines of friendly troops that are not linked in a " +
        "chain (units, SU-94). Overlapping friends are moved apart until they have it.")]
    float _friendlyGap = 0.5f;
    [SerializeField, Min(0f), Tooltip("How fast a troop gives way to an overlapping friend, x its march speed (SU-94). Below 1 " +
        "so its soldiers keep up and a marching troop is never held up for good.")]
    float _friendlyGiveWaySpeedScale = 0.5f;
    [SerializeField, Min(0.05f), Tooltip("Path rebuild interval while marching.")]
    float _pathRebuildIntervalSeconds = 0.5f;
    [SerializeField, Min(0f), Tooltip("Joint clearance at a linked end: min(this, fraction x length).")]
    float _jointClearance = 0.45f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Joint clearance as a fraction of the line length.")]
    float _jointClearanceFraction = 0.15f;
    [SerializeField, Min(0.5f), Tooltip("A routed troop's soldiers scatter in a disc this many times its radius.")]
    float _routScatterRadiusScale = 1.6f;
    [SerializeField, Min(0f), Tooltip("Margin of the troop border (the shape shown under a hovered or selected troop and " +
        "clicked to pick it) beyond its outermost soldier centres, on every side (units, SU-162). Display and picking only.")]
    float _outlinePadding = 0.65f;

    [Header("Marching in line (SU-150)")]
    [SerializeField, Tooltip("On: a troop in Line or Chain marches at Line March Speed Scale of its speed. Charges are not " +
        "slowed.")]
    bool _lineMarchSlowdown = true;
    [SerializeField, Range(0f, 1f), Tooltip("Pivot speed in Line or Chain, x the troop's march speed (0.8 = 20% slower).")]
    float _lineMarchSpeedScale = 0.8f;
    [SerializeField, Tooltip("On: a troop in Line or Chain moving back (more than Line Backward Angle from its facing) marches " +
        "at Line Backward Speed Scale, in place of the line scale. Charges are not slowed.")]
    bool _lineBackwardSlowdown = true;
    [SerializeField, Range(0f, 1f), Tooltip("Pivot speed moving back in Line or Chain, x the troop's march speed (0.6 = 40% " +
        "slower).")]
    float _lineBackwardSpeedScale = 0.6f;
    [SerializeField, Range(0f, 180f), Tooltip("A march more than this many degrees from the line's facing counts as moving " +
        "back.")]
    float _lineBackwardAngleDeg = 90f;

    [Header("Turning in line (SU-155)")]
    [SerializeField, Tooltip("On: a troop in Line or Chain wheels its facing toward where it is marching (or to a new ordered " +
        "front) at Line Wheel Rate, instead of turning at once. A troop that is not yet in line turns at once.")]
    bool _lineWheel = true;
    [SerializeField, Min(1f), Tooltip("How fast a line wheels, in degrees per second (60 = a quarter turn in 1.5 s).")]
    float _lineWheelRateDeg = 60f;
    [SerializeField, Tooltip("On: a troop in Line or Chain marching more than Line Turn Angle from its facing (still wheeling " +
        "to its march, or keeping an ordered front) marches at Line Backward Speed Scale, as if moving back.")]
    bool _lineTurnSlowdown = true;
    [SerializeField, Range(0f, 180f), Tooltip("A march more than this many degrees from the line's facing counts as turning.")]
    float _lineTurnAngleDeg = 45f;

    public float SDistance => _sDistance;
    public float SDistanceExitHysteresis => _sDistanceExitHysteresis;
    public float ADistance => _aDistance;
    public float EngagementGap => _engagementGap;
    public float EngagementHoldMargin => _engagementHoldMargin;

    public float NaturalLineRadiusScale => _naturalLineRadiusScale;
    public float TroopSizeScale => _troopSizeScale;
    public float BlobRadiusScale => _blobRadiusScale;
    public float PivotSeparationScale => _pivotSeparationScale;
    public float FriendlyGap => _friendlyGap;
    public float FriendlyGiveWaySpeedScale => _friendlyGiveWaySpeedScale;
    public float PathRebuildIntervalSeconds => _pathRebuildIntervalSeconds;
    public float JointClearance => _jointClearance;
    public float JointClearanceFraction => _jointClearanceFraction;
    public float RoutScatterRadiusScale => _routScatterRadiusScale;
    public float OutlinePadding => _outlinePadding;

    public bool LineMarchSlowdown => _lineMarchSlowdown;
    public float LineMarchSpeedScale => _lineMarchSpeedScale;
    public bool LineBackwardSlowdown => _lineBackwardSlowdown;
    public float LineBackwardSpeedScale => _lineBackwardSpeedScale;
    public float LineBackwardAngleDeg => _lineBackwardAngleDeg;
    public bool LineWheel => _lineWheel;
    public float LineWheelRateDeg => _lineWheelRateDeg;
    public bool LineTurnSlowdown => _lineTurnSlowdown;
    public float LineTurnAngleDeg => _lineTurnAngleDeg;
}
