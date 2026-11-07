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
             "units (blob, soldier clamp) is L_nat / this, or larger when its soldiers need more room. Provisional (Q-12).")]
    float _naturalLineRadiusScale = 1.8f;
    [SerializeField, Range(0.1f, 1f), Tooltip("Idle blob disc radius as a fraction of the troop radius.")]
    float _blobRadiusScale = 0.85f;
    [SerializeField, Min(0f), Tooltip("Minimum spacing of friendly pivots, x (r_A + r_B). Provisional.")]
    float _pivotSeparationScale = 0.25f;
    [SerializeField, Min(0f), Tooltip("Smallest clear gap between the outlines of friendly troops that are not linked in a " +
        "chain (units, SU-94). Overlapping friends are moved apart until they have it.")]
    float _friendlyGap = 0.5f;
    [SerializeField, Min(0f), Tooltip("How fast a troop gives way to an overlapping friend, x its march speed (SU-94). Below 1 " +
        "so its soldiers keep up and a marching troop is never held up for good.")]
    float _friendlyGiveWaySpeedScale = 0.5f;
    [SerializeField, Min(0.05f), Tooltip("Path rebuild interval while marching.")]
    float _pathRebuildIntervalSeconds = 0.5f;
    [SerializeField, Min(0f), Tooltip("Joint clearance at a linked end: min(this, fraction x length). Provisional.")]
    float _jointClearance = 0.45f;
    [SerializeField, Range(0f, 0.5f), Tooltip("Joint clearance as a fraction of the line length.")]
    float _jointClearanceFraction = 0.15f;
    [SerializeField, Min(0.5f), Tooltip("A routed troop's soldiers scatter in a disc this many times its radius.")]
    float _routScatterRadiusScale = 1.6f;

    public float SDistance => _sDistance;
    public float SDistanceExitHysteresis => _sDistanceExitHysteresis;
    public float ADistance => _aDistance;
    public float EngagementGap => _engagementGap;
    public float EngagementHoldMargin => _engagementHoldMargin;

    public float NaturalLineRadiusScale => _naturalLineRadiusScale;
    public float BlobRadiusScale => _blobRadiusScale;
    public float PivotSeparationScale => _pivotSeparationScale;
    public float FriendlyGap => _friendlyGap;
    public float FriendlyGiveWaySpeedScale => _friendlyGiveWaySpeedScale;
    public float PathRebuildIntervalSeconds => _pathRebuildIntervalSeconds;
    public float JointClearance => _jointClearance;
    public float JointClearanceFraction => _jointClearanceFraction;
    public float RoutScatterRadiusScale => _routScatterRadiusScale;
}
