using System.Collections.Generic;
using UnityEngine;

// The accordion retreat a troop performs when it breaks (spec §5.5, FaultSlipMotion). The pivot slips back by the
// retreat distance (TroopTuning.RetreatDistance) at once, but every soldier holds its ground until its own start time
// and then falls back to its place in the new line. Start times run as a wave from the rear (the side away from the
// attacker) to the front: the rearmost soldier moves at once and the frontmost one RetreatWaveDelaySeconds later, so the
// troop stretches out and then closes up again (accordion). Each soldier takes RetreatSoldierMoveSeconds to fall back.
// The same motion carries a sympathetic retreat of a chained neighbour (spec §3.4 shared retreat), which costs no morale.
public sealed class FaultSlipMotion
{
    const float PivotSlipTime = 0.14f;
    // Time after the last soldier arrives before the troop counts as recovered (Fleeing -> Idle) and can break again.
    const float SettleDuration = 0.5f;
    const float ArrivalTolerance = 0.05f;
    const float ArrivalGrace = 1f;

    static TroopTuning Tuning => TroopTuning.Active;

    // Length of the retreat motion: the wave delay, one soldier's move, then the settle time.
    public static float SlipDuration =>
        Tuning.RetreatWaveDelaySeconds + Tuning.RetreatSoldierMoveSeconds + SettleDuration;

    readonly Troop _troop;
    readonly List<SoldierAgentState> _living = new();
    float _age = -1f;
    float _distance;
    float _pivotDistance;
    float _pivotDisplacement;
    float _recoveryRemaining;
    Vector2 _axis;

    public FaultSlipMotion(Troop troop)
    {
        _troop = troop;
    }

    public bool IsActive => _age >= 0f;
    // True while the active motion is a sympathetic retreat (no morale cost).
    public bool IsSympathetic { get; private set; }
    // Unit vector from the troop toward the attacker; the retreat travels along its negative.
    public Vector2 Axis => _axis;
    // Distance the pivot travels in the current retreat.
    public float Distance => _pivotDistance;

    // Starts a retreat of distance units along retreatDirection (away from the attacker).
    public void Begin(Vector2 retreatDirection, float distance, bool sympathetic = false)
    {
        Transform transform = _troop.transform;
        if (retreatDirection.sqrMagnitude < 0.001f) retreatDirection = -(Vector2)transform.right;
        _axis = -retreatDirection.normalized;
        Vector2 localAxis = transform.InverseTransformVector(_axis).normalized;

        _living.Clear();
        foreach (var soldier in _troop.Roster.SoldierList)
            if (soldier.IsAlive) _living.Add(soldier);
        AssignSoldierMotion(localAxis);

        IsSympathetic = sympathetic;
        _distance = Mathf.Max(0f, distance);
        _pivotDistance = _distance;
        _pivotDisplacement = 0f;
        _age = 0f;
        _recoveryRemaining = SlipDuration;
    }

    // Counts down the recovery period; true on the frame it finishes.
    public bool TickRecovery(float deltaTime)
    {
        if (_recoveryRemaining <= 0f) return false;
        _recoveryRemaining -= deltaTime;
        return _recoveryRemaining <= 0f;
    }

    // Each soldier's target starts at the ground it stood on (its home in the new line, moved forward by the distance the
    // pivot has slipped) and eases back to its home once its start time comes. FaultOffset is how much of that distance
    // is still to cover. The steering arrives at the target, so the soldier slows to a stop at its new place.
    public void Tick(float deltaTime)
    {
        if (!IsActive) return;
        _age += deltaTime;
        SlipPivot();

        Vector2 localAxis = _troop.transform.InverseTransformVector(_axis).normalized;
        bool arrived = true;
        foreach (var soldier in _troop.Roster.SoldierList)
        {
            if (!soldier.IsAlive) continue;
            soldier.FaultOffset = RemainingAt(soldier);
            Vector2 target = soldier.FaultHome + localAxis * soldier.FaultOffset;
            Vector2 targetVelocity = deltaTime > 0f ? (target - soldier.FaultTargetPosition) / deltaTime : Vector2.zero;
            soldier.SetFaultTarget(target, targetVelocity);
            if ((soldier.Position - target).sqrMagnitude > ArrivalTolerance * ArrivalTolerance) arrived = false;
        }

        // Finish once every soldier has arrived. The grace period stops a soldier that cannot reach its place from
        // holding the troop in the retreat state.
        if (_age <= SlipDuration || (!arrived && _age <= SlipDuration + ArrivalGrace)) return;
        _age = -1f;
        IsSympathetic = false;
        foreach (var soldier in _troop.Roster.SoldierList)
        {
            soldier.FaultOffset = 0f;
            soldier.SetFaultMotion(0f, 0.01f);
            soldier.FaultDelayRemaining = 0f;
            soldier.FaultRetreatScale = 1f;
            soldier.EndFaultTracking();
        }
    }

    // The wave: rear soldiers (furthest from the attacker) start at once, front soldiers (nearest it) start last.
    void AssignSoldierMotion(Vector2 localAxis)
    {
        float waveDelay = Tuning.RetreatWaveDelaySeconds;
        float moveDuration = Tuning.RetreatSoldierMoveSeconds;
        float rearDepth = float.PositiveInfinity;
        float frontDepth = float.NegativeInfinity;
        foreach (var soldier in _living)
        {
            float depth = Vector2.Dot(soldier.Position, localAxis);
            rearDepth = Mathf.Min(rearDepth, depth);
            frontDepth = Mathf.Max(frontDepth, depth);
        }

        foreach (var soldier in _living)
        {
            float depth = Vector2.Dot(soldier.Position, localAxis);
            float frontness = frontDepth - rearDepth > 0.0001f
                ? Mathf.InverseLerp(rearDepth, frontDepth, depth)
                : 0f;
            soldier.CancelCharge();
            soldier.SetFaultMotion(frontness * waveDelay, moveDuration);
            soldier.FaultRetreatScale = 1f;
            soldier.BeginFaultTracking();
        }
    }

    // The pivot eases back over PivotSlipTime; soldiers are compensated so they do not move with it.
    void SlipPivot()
    {
        float pivotProgress = Mathf.Clamp01(_age / PivotSlipTime);
        float easedPivotProgress = pivotProgress * pivotProgress * (3f - 2f * pivotProgress);
        float pivotDisplacement = _pivotDistance * easedPivotProgress;
        float pivotStep = pivotDisplacement - _pivotDisplacement;
        Vector2 pivotDelta = -_axis * pivotStep;
        _troop.transform.position += (Vector3)pivotDelta;
        Vector2 localCompensation = (Vector2)_troop.transform.InverseTransformVector(-pivotDelta);
        // Homes stay in troop-local space: a soldier's target is the new line, which the pivot has moved back.
        foreach (var soldier in _troop.Roster.SoldierList)
            soldier.Position += localCompensation;
        _pivotDisplacement = pivotDisplacement;
    }

    // Distance (along the attack axis, ahead of its home) the soldier's target still has to fall back. Before its start
    // time the target stays on the ground the soldier held, so it covers the pivot's slip; then it eases back over the
    // move duration. A soldier with a FaultRetreatScale below 1 falls back only that fraction of the distance.
    float RemainingAt(SoldierAgentState soldier)
    {
        float localTime = _age - soldier.FaultDelayRemaining;
        float progress = localTime <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localTime / soldier.FaultMoveDuration));
        return _pivotDisplacement * (1f - Mathf.Clamp01(soldier.FaultRetreatScale) * progress);
    }
}