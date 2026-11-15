using UnityEngine;

// Moves the troop pivot: marching to a destination (Move, or the centre of a line order), pursuing an enemy troop,
// fleeing after a rout, and the line-push drift from line pressure and link tension.
// Pivot speed is the troop's Move x SpeedUnitsPerStatPoint (spec §1.3, §3.2); Move is the slowest soldier's Speed.
// In Line or Chain the march is slowed, more when moving back (LineMarchRules, SU-150) or turning: more than
// LineTurnAngleDeg from the line's facing, while it wheels to its march or keeps an ordered front (SU-155). Charges are
// soldier motion and keep their own speed.
// A pursuer engaged with its unrouted target within the charge gap (EngagementGap + ChargeGapTolerance) holds: it does
// not march at the target, so it no longer steps into the barrier every frame and rocks; the assault wheel is its only
// movement (SU-163). It marches again once the gap has stayed beyond the charge gap for ResumeDelay (0.25 s), so it never
// holds for long where its soldiers may not charge, and the recoil of its arrival (line pressure) or a soldier shuffling
// the footprint does not start the march for a frame. A pursuer that marches again after holding closes to half
// ChargeGapTolerance inside the charge gap before it holds again, so it does not step in for one frame and recoil out.
// While it closes on its engaged target it eases in: its step is cut to ApproachGain x the gap left to EngagementGap,
// and never past EngagementGap, so it does not step into the barrier.
public sealed class TroopPivotMover
{
    const float ArrivalDistanceSquared = 0.0004f;
    const float ArrivalPushSquared = 0.0025f;
    // How long (s) the gap must stay beyond the charge gap before a holding pursuer marches again (SU-163).
    const float ResumeDelay = 0.25f;
    // How fast (per second) an engaged pursuer closes the last of the gap to its target (SU-163).
    const float ApproachGain = 4f;

    readonly Troop _troop;
    readonly TroopPathFollower _path;
    bool _hasDestination;
    bool _pursuitActive;
    bool _holding;
    bool _reclosing;
    float _beyondChargeGapTime;
    Vector2 _destination;
    Troop _pursuitTarget;
    Vector2 _pivotVelocity;
    Vector2 _linePushVelocity;

    public TroopPivotMover(Troop troop, TroopPathFollower path)
    {
        _troop = troop;
        _path = path;
    }

    public bool IsMoving => _hasDestination || _pursuitActive;
    public bool IsPursuing => _pursuitActive;
    public Troop PursuitTarget => _pursuitActive ? _pursuitTarget : null;
    public bool HasPlainDestination => _hasDestination && !_pursuitActive;
    public Vector2 Destination => _destination;
    public Vector2 PivotVelocity => _pivotVelocity + _linePushVelocity;
    public Vector2 MarchVelocity => _pivotVelocity;
    public Vector2 LinePushVelocity => _linePushVelocity;
    public void SetLinePushVelocity(Vector2 velocity) => _linePushVelocity = velocity;

    // Pivot speed in units/s.
    public float PivotSpeed => _troop.Move * TroopTuning.Active.SpeedUnitsPerStatPoint;

    public void MoveTo(Vector2 worldPosition)
    {
        _pursuitActive = false;
        _pursuitTarget = null;
        _path.RequestRebuild();
        _destination = worldPosition;
        _hasDestination = true;
    }

    public void Pursue(Troop target)
    {
        _pursuitTarget = target;
        _pursuitActive = true;
        _hasDestination = true;
        _path.RequestRebuild();
    }

    // Drops every movement order (used when the troop breaks or routs).
    public void CancelOrders()
    {
        _hasDestination = false;
        _pursuitActive = false;
        _pursuitTarget = null;
        _holding = false;
        _reclosing = false;
        _beyondChargeGapTime = 0f;
        _path.Invalidate();
        _pivotVelocity = Vector2.zero;
    }

    public void Tick(float deltaTime)
    {
        if (_troop.IsRouted)
            Flee(deltaTime);
        else if (_troop.IsFaultSlipping)
            _troop.transform.position += (Vector3)(_linePushVelocity * deltaTime); // linked neighbours keep hold during retreat
        else
            MoveToDestination(deltaTime);
    }

    void Flee(float deltaTime)
    {
        _pivotVelocity = _troop.RoutFleeDirection * PivotSpeed;
        Vector2 position = _troop.transform.position;
        Vector2 step = _pivotVelocity * deltaTime;
        _troop.transform.position = position + step;
    }

    void MoveToDestination(float deltaTime)
    {
        Vector2 position = _troop.transform.position;
        _pivotVelocity = Vector2.zero;
        UpdatePursuit();

        _holding = HoldsAgainstPursuitTarget(deltaTime);
        if (_hasDestination && !_holding)
        {
            Vector2 offset = _destination - position;
            if (offset.sqrMagnitude <= ArrivalDistanceSquared && _linePushVelocity.sqrMagnitude <= ArrivalPushSquared)
            {
                position = _destination;
                if (!_pursuitActive) Arrive();
            }
            else if (deltaTime > 0f && offset.sqrMagnitude > 0.000001f)
            {
                Vector2 step = _path.Step(position, _destination, PivotSpeed, deltaTime);
                step *= LineMarchScale(step);
                step = LimitStepIntoTarget(step, deltaTime);
                _pivotVelocity = step / deltaTime;
                position += step;
            }
        }

        _troop.transform.position = (Vector3)position + (Vector3)(_linePushVelocity * deltaTime);
    }

    // A Pursue ends when the target is wiped out or leaves the field (reaches the edge after routing, or despawns), not
    // when it routs: the pursuer chases the routed troop and may catch it (SU-139, Q-24). A new order, and the pursuer's
    // own retreat, disorder or rout, end it too (Troop cancels its orders then).
    void UpdatePursuit()
    {
        if (!_pursuitActive) return;
        if (_pursuitTarget == null || _pursuitTarget.LivingSoldierCount == 0 || _pursuitTarget.HasLeftField)
        {
            _pursuitActive = false;
            _hasDestination = false;
            _pursuitTarget = null;
            _path.Invalidate();
            _troop.OnOrderComplete();
            return;
        }
        _destination = _pursuitTarget.transform.position;
        _hasDestination = true;
        _path.RequestRebuildIfTargetMoved(_destination);
    }

    // SU-163: an engaged pursuer does not march at its target within the charge gap, nor, once holding, until the gap
    // has stayed beyond it for ResumeDelay.
    bool HoldsAgainstPursuitTarget(float deltaTime)
    {
        if (!AppliesAssaultHold() || !_troop.TryGetEngagedGap(_pursuitTarget, out float gap))
        {
            _beyondChargeGapTime = 0f;
            _reclosing = false;
            return false;
        }
        TroopTuning tuning = TroopTuning.Active;
        if (_holding ? gap <= tuning.ChargeGap : gap <= HoldEntryGap(tuning))
        {
            _beyondChargeGapTime = 0f;
            _reclosing = false;
            return true;
        }
        if (!_holding) return false;
        _beyondChargeGapTime += Mathf.Max(0f, deltaTime);
        if (_beyondChargeGapTime < ResumeDelay) return true;
        _reclosing = true;
        return false;
    }

    // The gap at which a pursuer closing on its engaged target starts holding: the charge gap, or half
    // ChargeGapTolerance inside it when it is closing again after holding.
    float HoldEntryGap(TroopTuning tuning) =>
        _reclosing ? tuning.ChargeGap - tuning.ChargeGapTolerance * 0.5f : tuning.ChargeGap;

    bool AppliesAssaultHold() =>
        _pursuitActive && _pursuitTarget != null && !_pursuitTarget.IsRouted && TroopTuning.Active.AssaultHold;

    // SU-163: closing on its engaged target, the pursuer eases into the charge gap and never steps past EngagementGap.
    Vector2 LimitStepIntoTarget(Vector2 step, float deltaTime)
    {
        if (!AppliesAssaultHold() || !_troop.TryGetEngagedGap(_pursuitTarget, out float gap)) return step;
        float room = Mathf.Max(0f, gap - TroopTuning.Active.EngagementGap);
        room = Mathf.Min(room, room * ApproachGain * Mathf.Max(0f, deltaTime));
        float length = step.magnitude;
        return length > room && length > 1e-6f ? step * (room / length) : step;
    }

    // Whether the pursuer held against its target this frame (SU-163).
    public bool IsHoldingAgainstTarget => _holding;

    // SU-150, SU-155: the march scale for a step in this direction (1 outside Line and Chain).
    float LineMarchScale(Vector2 direction)
    {
        TroopTuning tuning = TroopTuning.Active;
        return LineMarchRules.SpeedScale(LineMarchRules.IsInLine(_troop.FormationMode), direction, _troop.Facing,
            tuning.LineMarchSlowdown, tuning.LineMarchSpeedScale, tuning.LineBackwardSlowdown,
            tuning.LineBackwardSpeedScale, tuning.LineBackwardAngleDeg, tuning.LineTurnSlowdown, tuning.LineTurnAngleDeg);
    }

    void Arrive()
    {
        _hasDestination = false;
        _path.Invalidate();
        _troop.OnOrderComplete();
    }
}
