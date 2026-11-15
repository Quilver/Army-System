using UnityEngine;

// The troop's battle line (spec §3.3) and its own facing (SU-154). Each frame it builds the troop's natural line (its front
// rank, half the rank depth ahead of its pivot, SU-161) when the troop has an enemy within S_Distance or holds a player line order.
// Facing (SU-154): troops no longer turn toward enemies by themselves. The troop's heading is
//   - the ordered facing while it is in line under a line order;
//   - otherwise where it is marching (Move, Pursue, or the march to a line order's place);
//   - for a pursuer whose target is within S_Distance, squaring up to the face it attacks (SU-75);
//   - otherwise unchanged: a troop that stops keeps facing the way it last marched or was ordered.
// A troop that is already in line wheels to a new heading at LineWheelRateDeg when Line Wheel is on (SU-155); a troop
// forming its line, or not in line, turns at once. While the troop is engaged its facing is locked (spec §3.3, §4.1), except
// for the assault wheel (SU-163): a pursuer engaged with its target wheels square to the face it attacks, about the end of
// its front rank nearest the enemy (AssaultWheelRules), and stops within AssaultWheelToleranceDeg.
// The chain step (ChainLinker) then joins linked line points and publishes the final Formation.
// Disable to drop the line and stop it updating.
[DisallowMultipleComponent]
public sealed class TroopFormationState : MonoBehaviour
{
    const float HeadingSmoothingTime = 0.8f;
    const float MinimumHeadingSquared = 0.000001f;
    // March speed (squared, units/s) above which the troop turns toward its march.
    const float MarchHeadingSpeedSquared = 0.0025f;

    Troop _troop;
    TroopFormationData _natural;
    TroopFormationData _formation;
    bool _hasHeading;
    bool _hasThreat;
    bool _assaultWheeling;
    Vector2 _heading;

    // The line the troop is using this frame, after chain joints (null without a line, or while disabled).
    public TroopFormationData Formation
    {
        get => enabled ? _formation : null;
        internal set
        {
            if (enabled) _formation = value;
        }
    }

    // The troop's own line before chain joints are applied.
    public TroopFormationData NaturalFormation => enabled ? _natural : null;

    public bool HasHeading => _hasHeading;
    // An enemy is within S_Distance (footprint gap), so the troop forms a line (facing its own heading, SU-154).
    public bool HasThreat => _hasThreat;
    // The troop's own facing (SU-154): ordered, marched or kept.
    public Vector2 Heading => _heading;
    public bool IsThreatFacing => Formation != null && Formation.IsThreatFacing;

    internal void Bind(Troop troop) => _troop = troop;

    // Sets the troop's facing at once (deployment, placement).
    internal void SetHeading(Vector2 facing)
    {
        if (facing.sqrMagnitude < MinimumHeadingSquared) return;
        _heading = facing.normalized;
        _hasHeading = true;
    }

    void OnDisable()
    {
        _formation = null;
        _natural = null;
        _hasHeading = false;
        _hasThreat = false;
        _assaultWheeling = false;
    }

    // Called by the combat world each frame. threat: an enemy is within S_Distance. pursuitHeading: the heading of a
    // pursuer whose target is within S_Distance (SU-75), else null. While engaged the heading is locked.
    // assaultHeading: the heading a pursuer engaged with its target wheels to (SU-163), else null; it overrides both.
    internal void UpdateNatural(bool threat, Vector2? pursuitHeading, bool engaged, float deltaTime,
        Vector2? assaultHeading = null)
    {
        if (!enabled) return;
        _hasThreat = threat;
        if (_troop.IsRouted)
        {
            _natural = null;
            _formation = null;
            return;
        }
        if (!_hasHeading) SetHeading(transform.right);

        TroopLineOrder order = _troop.LineOrder;
        bool ordered = order != null && order.IsActive;
        bool holdingOrder = ordered && order.HasIdleLine && !_troop.IsMoving;
        bool inLine = _hasThreat || holdingOrder;
        bool wasInLine = _natural != null;

        if (assaultHeading.HasValue)
            AssaultWheel(assaultHeading.Value, inLine, deltaTime);
        else
        {
            _assaultWheeling = false;
            UpdateHeading(ordered ? order.Facing : (Vector2?)null, inLine, wasInLine, pursuitHeading, engaged, deltaTime);
        }

        if (!inLine)
        {
            _natural = null;
            _formation = null;
            return;
        }

        float length = ordered ? order.Length : _troop.NaturalLineLength;
        // SU-161: the line is the front rank, half the rank depth ahead of the pivot, so the soldiers' ranks stay centred on
        // the pivot (no lurch when a lone troop forms or drops its line) and troops of any depth are flush on one line.
        _natural = Build(transform.position, _heading, length, _hasThreat,
            LineGeometry.FrontRankOffset(length, _troop.LivingSoldierCount));
    }

    // SU-154, SU-155: turns the heading toward the ordered facing (in line under a line order) or the march, wheeling
    // when the troop was already in line.
    void UpdateHeading(Vector2? orderedFacing, bool inLine, bool wasInLine, Vector2? pursuitHeading, bool engaged,
        float deltaTime)
    {
        if (engaged) return;
        if (pursuitHeading.HasValue)
        {
            SmoothHeading(pursuitHeading.Value, deltaTime);
            return;
        }

        Vector2 target;
        if (orderedFacing.HasValue && inLine)
            target = orderedFacing.Value;
        else
        {
            Vector2 march = _troop.MarchVelocity;
            if (march.sqrMagnitude <= MarchHeadingSpeedSquared) return;
            target = march;
        }
        if (target.sqrMagnitude < MinimumHeadingSquared) return;

        TroopTuning tuning = TroopTuning.Active;
        _heading = inLine && wasInLine && tuning.LineWheel
            ? LineMarchRules.Wheel(_heading, target, tuning.LineWheelRateDeg * Mathf.Max(0f, deltaTime))
            : target.normalized;
    }

    // The line through the troop's front rank (SU-161): its middle stands frontOffset ahead of the pivot along facing
    // (LineGeometry.FrontRankOffset, half the rank depth), so the pivot is in the middle of the ranks behind it.
    public static TroopFormationData Build(Vector2 pivot, Vector2 facing, float length, bool threatFacing,
        float frontOffset = 0f)
    {
        if (facing.sqrMagnitude < MinimumHeadingSquared) facing = Vector2.right;
        facing.Normalize();
        Vector2 center = pivot + facing * Mathf.Max(0f, frontOffset);
        Vector2 tangent = new(-facing.y, facing.x);
        Vector2 half = tangent * (length * 0.5f);
        return new TroopFormationData
        {
            Start = center - half,
            End = center + half,
            Facing = facing,
            IsThreatFacing = threatFacing
        };
    }

    // World position of line point 0 or 1, or the pivot if there is no line.
    public Vector2 SegmentEndpoint(int end)
    {
        if (Formation == null) return transform.position;
        return end == 0 ? _formation.Start : _formation.End;
    }

    // SU-163: wheels the heading toward target at LineWheelRateDeg, capped so the swinging end moves no faster than the
    // troop marches. In line, with Assault Wheel About Engaged End on, the end of the front rank nearest the face (the
    // leading end along target) stays where it is: the pivot moves and the soldiers are left where they stand, so the far
    // end swings in. The wheel starts when the heading is more than AssaultWheelToleranceDeg off and runs until it is
    // square, so a troop that is nearly square does not hunt. The pivot step goes through Troop.ConstrainPivotStep, and
    // the barrier (TroopSegmentPressureRules) keeps the EngagementGap.
    void AssaultWheel(Vector2 target, bool inLine, float deltaTime)
    {
        if (target.sqrMagnitude < MinimumHeadingSquared) return;
        target.Normalize();
        TroopTuning tuning = TroopTuning.Active;
        float offAngle = Vector2.Angle(_heading, target);
        if (!_assaultWheeling)
        {
            if (offAngle <= tuning.AssaultWheelToleranceDeg) return;
            _assaultWheeling = true;
        }

        bool aboutEnd = inLine && tuning.AssaultWheelAboutEngagedEnd;
        float length = _troop.NaturalLineLength;
        float swingRadius = aboutEnd ? length : length * 0.5f;
        float rate = tuning.LineWheelRateDeg;
        if (swingRadius > 0.01f) rate = Mathf.Min(rate, _troop.PivotSpeed / swingRadius * Mathf.Rad2Deg);
        Vector2 next = LineMarchRules.Wheel(_heading, target, rate * Mathf.Max(0f, deltaTime));

        if (aboutEnd)
        {
            float frontOffset = LineGeometry.FrontRankOffset(length, _troop.LivingSoldierCount);
            Vector2 pivot = transform.position;
            int side = AssaultWheelRules.LeadingEnd(pivot, _heading, length, frontOffset, target);
            _troop.ShiftPivotKeepingSoldiers(
                AssaultWheelRules.PivotStepKeepingEnd(_heading, next, length, frontOffset, side));
        }
        _heading = next;
        if (Vector2.Angle(_heading, target) < 0.01f) _assaultWheeling = false;
    }

    // Whether the assault wheel is turning this frame (SU-163).
    public bool IsAssaultWheeling => _assaultWheeling;

    // A pursuer squares up to its target (SU-75): eased toward the target heading.
    void SmoothHeading(Vector2 target, float deltaTime)
    {
        if (target.sqrMagnitude < MinimumHeadingSquared) return;
        target.Normalize();
        float blend = 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / HeadingSmoothingTime);
        Vector2 smoothed = Vector2.Lerp(_heading, target, blend);
        if (smoothed.sqrMagnitude > MinimumHeadingSquared) _heading = smoothed.normalized;
    }
}
