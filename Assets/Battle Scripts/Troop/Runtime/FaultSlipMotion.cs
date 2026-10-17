using System.Collections.Generic;
using UnityEngine;

// The retreat a troop performs when it breaks: the pivot slips back from the attacker while each soldier falls back
// with a delay (front soldiers last), holds, then returns. Soldiers near a linked line end retreat less.
public sealed class FaultSlipMotion
{
    const float MaximumDelay = 1f;
    const float RearMoveDuration = 0.5f;
    const float FrontMoveDuration = 1f;
    const float HoldDuration = 0.26f;
    const float ReturnDuration = 0.88f;
    const float PivotSlipTime = 0.14f;
    public const float SlipDuration = MaximumDelay + FrontMoveDuration + HoldDuration + ReturnDuration;

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
    // Unit vector from the troop toward the attacker; the retreat travels along its negative.
    public Vector2 Axis => _axis;

    public void Begin(Vector2 attackerPivotPosition, float chargeDistance)
    {
        Transform transform = _troop.transform;
        Vector2 towardWinner = attackerPivotPosition - (Vector2)transform.position;
        if (towardWinner.sqrMagnitude < 0.001f) towardWinner = transform.right;
        _axis = towardWinner.normalized;
        Vector2 localAxis = transform.InverseTransformVector(_axis).normalized;

        _living.Clear();
        foreach (var soldier in _troop.Roster.SoldierList)
            if (soldier.IsAlive) _living.Add(soldier);
        AssignSoldierMotion(localAxis);

        _distance = Mathf.Min(Mathf.Max(chargeDistance * 1.8f, _troop.Radius * 0.9f), _troop.Radius * 1.5f);
        _pivotDistance = _distance * AverageRetreatScale();
        _pivotDisplacement = 0f;
        _age = 0f;
        _recoveryRemaining = SlipDuration;
        TroopDiagnostics.RecordRetreatBegin(_troop, _axis, _distance, _pivotDistance, _living);
    }

    // Counts down the recovery period; true on the frame it finishes.
    public bool TickRecovery(float deltaTime)
    {
        if (_recoveryRemaining <= 0f) return false;
        _recoveryRemaining -= deltaTime;
        return _recoveryRemaining <= 0f;
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;
        _age += deltaTime;
        SlipPivot();

        bool recovered = true;
        foreach (var soldier in _troop.Roster.SoldierList)
        {
            if (!soldier.IsAlive) continue;
            float previousOffset = soldier.FaultOffset;
            soldier.FaultOffset = OffsetAt(soldier);
            TroopDiagnostics.RecordRetreatStep(_troop, soldier, previousOffset, soldier.FaultOffset);
            float retreatStep = soldier.FaultOffset - previousOffset;
            if (Mathf.Abs(retreatStep) > 0f)
                soldier.Position += (Vector2)_troop.transform.InverseTransformVector(-_axis * retreatStep);
            if (soldier.FaultOffset >= 1f) recovered = false;
        }

        if (_age <= SlipDuration || !recovered) return;
        TroopDiagnostics.RecordRetreatEnd(_troop, _age);
        _age = -1f;
        foreach (var soldier in _troop.Roster.SoldierList)
        {
            soldier.FaultOffset = 0f;
            soldier.SetFaultMotion(0f, 0.01f);
            soldier.FaultDelayRemaining = 0f;
            soldier.FaultRetreatScale = 1f;
        }
    }

    // Front soldiers (nearest the attacker) start late and move slowly; rear soldiers start at once.
    void AssignSoldierMotion(Vector2 localAxis)
    {
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
            if (soldier.IsCharging) TroopDiagnostics.RecordChargeEnd(_troop, soldier, "fault slip", float.NaN);
            soldier.CancelCharge();
            soldier.SetFaultMotion(frontness * MaximumDelay, Mathf.Lerp(RearMoveDuration, FrontMoveDuration, frontness));
            soldier.FaultRetreatScale = LinkedEndRetreat.ScaleFor(_troop, soldier);
        }
    }

    float AverageRetreatScale()
    {
        if (_living.Count == 0) return 1f;
        float total = 0f;
        foreach (var soldier in _living) total += soldier.FaultRetreatScale;
        return total / _living.Count;
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
        foreach (var soldier in _troop.Roster.SoldierList)
            soldier.Position += localCompensation;
        _pivotDisplacement = pivotDisplacement;
    }

    // Retreat offset: wait for the delay, ease out, hold, then ease back.
    float OffsetAt(SoldierAgentState soldier)
    {
        float full = _distance * soldier.FaultRetreatScale;
        float localTime = _age - soldier.FaultDelayRemaining;
        float move = soldier.FaultMoveDuration;
        if (localTime <= 0f) return 0f;
        if (localTime < move) return full * Mathf.SmoothStep(0f, 1f, localTime / move);
        if (localTime < move + HoldDuration) return full;
        if (localTime < move + HoldDuration + ReturnDuration)
            return full * (1f - Mathf.SmoothStep(0f, 1f, (localTime - move - HoldDuration) / ReturnDuration));
        return 0f;
    }
}