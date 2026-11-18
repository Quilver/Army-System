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
    [SerializeField, Tooltip("On: a chained troop's soldiers fill its line right up to each joint, so the end files of " +
        "neighbouring troops stand one file apart, as inside a troop, and the chain reads as one line (SU-178). Off: linked " +
        "ends are pulled in by the joint clearance, leaving a gap of about two files at every joint.")]
    bool _chainFillToJoints = true;
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

    [Header("No strafing in line (SU-172)")]
    [SerializeField, Tooltip("On: a troop in Line or Chain only marches forward. When its march is more than Line Advance " +
        "Angle off its facing it stands and wheels (at Line Wheel Rate) until it faces the march, then advances; a line " +
        "order's front is taken on arrival. It may still strafe while engaged, and on a small move (Line Strafe Max " +
        "Distance) that does not end in an engagement. Off: lines march in any direction while they wheel (SU-155).")]
    bool _lineNoStrafe = true;
    [SerializeField, Range(1f, 90f), Tooltip("A line marches only while its march is within this many degrees of its facing.")]
    float _lineAdvanceAngleDeg = 15f;
    [SerializeField, Min(0f), Tooltip("A Move or line order no longer than this (units, from where the troop stood) whose " +
        "destination is not within A_Distance of an enemy is a small move: the line may strafe to it.")]
    float _lineStrafeMaxDistance = 4f;
    [SerializeField, Min(0f), Tooltip("A line that has stood wheeling this long (s) and still does not face its march marches " +
        "anyway, so it is never stuck (a facing it cannot reach).")]
    float _lineWheelFirstMaxSeconds = 4f;

    [SerializeField, Tooltip("On (with Line No Strafe): a line that has to turn wheels on steering circles (SU-173): its " +
        "front rank's centre marches along arcs and straight tangents and the line faces along them, arriving facing a line " +
        "order's front. Off: it stands and turns on the spot first (SU-172).")]
    bool _lineWheelOnSteeringCircles = true;
    [SerializeField, Min(0.1f), Tooltip("Turn radius of the steering-circle wheel, x half the line's length. 1: the drill " +
        "wheel, the inner end of the front rank stands while the outer end marches round it. Larger: a wider arc on which " +
        "the inner end walks forward too.")]
    float _lineTurnRadiusScale = 1f;
    [SerializeField, Range(0.25f, 1f), Tooltip("Tightest steering-circle wheel, as a share of the turn radius. A tighter wheel " +
        "is taken only where it is much shorter (a sidestep of a few lines' widths would otherwise loop round); it turns " +
        "about a point inside the line.")]
    float _lineTurnMinRadiusShare = 0.5f;

    [Header("Troop border fill (SU-170)")]
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity of a friendly troop's border when it is neither hovered nor selected.")]
    float _friendlyFillAlpha = 0.14f;
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity of an enemy troop's border when it is not hovered.")]
    float _enemyFillAlpha = 0.28f;
    [SerializeField, Range(0f, 1f), Tooltip("Added to the fill opacity while the troop is hovered.")]
    float _hoverFillAlphaBonus = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("Added to the fill opacity while the troop is selected.")]
    float _selectedFillAlphaBonus = 0.16f;
    [SerializeField, Min(0f), Tooltip("Width of the border's soft edge (units): the fill fades from full to nothing over this " +
        "much inside the border's edge.")]
    float _fillEdgeFade = 0.7f;

    [Header("Assault wheel (SU-163)")]
    [SerializeField, Tooltip("On: a troop with a Pursue order that is engaged with its target wheels at Line Wheel Rate until " +
        "its front is parallel to the face of the target it attacks (square to the target's direction for a blob or column), " +
        "so more of its front rank comes into contact. Off: it eases toward that face about its pivot (SU-75).")]
    bool _assaultWheel = true;
    [SerializeField, Tooltip("On: the assault wheel turns about the end of the front rank nearest the enemy, which stays put " +
        "while the far end swings in. Off: it turns about its pivot.")]
    bool _assaultWheelAboutEngagedEnd = true;
    [SerializeField, Range(0f, 45f), Tooltip("The assault wheel stops when the front is within this many degrees of square, " +
        "and starts again only when it is further out than this.")]
    float _assaultWheelToleranceDeg = 4f;
    [SerializeField, Tooltip("On: the face of the target that the assault wheel squares to is chosen when the troops engage " +
        "and kept until the attacker's bearing from the target is Assault Face Hysteresis past that face's boundary. " +
        "Off: it is re-classified every frame. The rules' flank test is not affected.")]
    bool _assaultFaceHold = true;
    [SerializeField, Range(0f, 45f), Tooltip("How many degrees past a face boundary (45 and 135 degrees from the target's " +
        "facing) the attacker's bearing must go before the assault wheel squares to the next face.")]
    float _assaultFaceHysteresisDeg = 20f;
    [SerializeField, Tooltip("On: two troops pursuing each other face pivot to pivot as they close and, once engaged, both " +
        "wheel square to the line between their pivots taken when they engaged, so they meet front to front.")]
    bool _mutualPursuitFrontToFront = true;
    [SerializeField, Tooltip("On: a pursuer engaged with its (unrouted) target within the charge gap (EngagementGap + " +
        "ChargeGapTolerance) does not march at it; the wheel is its only movement. It marches again when the gap opens.")]
    bool _assaultHold = true;

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
    public bool ChainFillToJoints => _chainFillToJoints;
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
    public bool AssaultWheel => _assaultWheel;
    public bool AssaultWheelAboutEngagedEnd => _assaultWheelAboutEngagedEnd;
    public float AssaultWheelToleranceDeg => _assaultWheelToleranceDeg;
    public bool AssaultFaceHold => _assaultFaceHold;
    public float AssaultFaceHysteresisDeg => _assaultFaceHysteresisDeg;
    public bool MutualPursuitFrontToFront => _mutualPursuitFrontToFront;
    public bool AssaultHold => _assaultHold;
    public bool LineNoStrafe => _lineNoStrafe;
    public float LineAdvanceAngleDeg => _lineAdvanceAngleDeg;
    public float LineStrafeMaxDistance => _lineStrafeMaxDistance;
    public float LineWheelFirstMaxSeconds => _lineWheelFirstMaxSeconds;
    public bool LineWheelOnSteeringCircles => _lineWheelOnSteeringCircles;
    public float LineTurnRadiusScale => _lineTurnRadiusScale;
    public float LineTurnMinRadiusShare => _lineTurnMinRadiusShare;
    public float FriendlyFillAlpha => _friendlyFillAlpha;
    public float EnemyFillAlpha => _enemyFillAlpha;
    public float HoverFillAlphaBonus => _hoverFillAlphaBonus;
    public float SelectedFillAlphaBonus => _selectedFillAlphaBonus;
    public float FillEdgeFade => _fillEdgeFade;
}
