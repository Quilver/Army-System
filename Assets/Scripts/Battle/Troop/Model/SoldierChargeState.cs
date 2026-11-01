using System;
using UnityEngine;

public enum ChargeRole
{
    None,
    Charger,   // running at an enemy soldier (spec §4.3)
    Follower   // passive support following a charger (spec §4.4)
}

// A soldier's charge (spec §4.3, §4.4). A charger runs at one enemy soldier until it makes contact (one charge = one
// clash), its target dies, its troop routs or the timeout passes. A retreat does not cancel it (SU-43). A follower stays
// near its charger and supports its clash without starting clashes of its own; it returns to the line when the charge
// ends.
// Return: a charge that ends without a rout leaves a pending return. SoldierSteering turns it into a rush back to the
// troop (charge speed, free of the troop radius) when the soldier is far from its troop or its troop is no longer
// fighting, and ends it when the soldier is back in its formation or the return times out.
// Runtime state only: the references are not serialized.
[Serializable]
public sealed class SoldierChargeState
{
    [SerializeField] ChargeRole _role;
    [SerializeField] float _timeRemaining;
    [SerializeField] float _startTime;

    [NonSerialized] SoldierAgentState _target;
    [NonSerialized] Troop _targetTroop;
    [NonSerialized] SoldierAgentState _leader;
    [NonSerialized] bool _returnPending;
    [NonSerialized] bool _returning;
    [NonSerialized] float _returnRemaining;

    public ChargeRole Role => _role;
    public SoldierAgentState Target => _role == ChargeRole.Charger ? _target : null;
    public Troop TargetTroop => _role == ChargeRole.Charger ? _targetTroop : null;
    public SoldierAgentState Leader => _role == ChargeRole.Follower ? _leader : null;
    public float TimeRemaining => _timeRemaining;
    // Time.time when the charge began. Decides which soldier is A when both are charging (Q-28).
    public float StartTime => _startTime;
    public bool IsReturning => _returning;

    public void BeginCharge(SoldierAgentState target, Troop targetTroop, float timeout, float startTime)
    {
        _role = ChargeRole.Charger;
        _target = target;
        _targetTroop = targetTroop;
        _leader = null;
        _timeRemaining = Mathf.Max(0f, timeout);
        _startTime = startTime;
        ClearReturn();
    }

    public void BeginFollow(SoldierAgentState leader, float timeout, float startTime)
    {
        _role = ChargeRole.Follower;
        _leader = leader;
        _target = null;
        _targetTroop = null;
        _timeRemaining = Mathf.Max(0f, timeout);
        _startTime = startTime;
        ClearReturn();
    }

    public void Tick(float deltaTime)
    {
        if (_role != ChargeRole.None) _timeRemaining = Mathf.Max(0f, _timeRemaining - deltaTime);
    }

    // Ends the charge. returnToTroop leaves a pending return (see above); a rout passes false.
    public void End(bool returnToTroop = true)
    {
        if (!returnToTroop) ClearReturn();
        else if (_role != ChargeRole.None) _returnPending = true;
        _role = ChargeRole.None;
        _target = null;
        _targetTroop = null;
        _leader = null;
        _timeRemaining = 0f;
    }

    // True once after a charge ended with a pending return.
    public bool ConsumeReturnPending()
    {
        bool pending = _returnPending;
        _returnPending = false;
        return pending;
    }

    public void BeginReturn(float timeout)
    {
        _returning = true;
        _returnRemaining = Mathf.Max(0f, timeout);
    }

    // Counts the return down; it ends when the timeout passes.
    public void TickReturn(float deltaTime)
    {
        if (!_returning) return;
        _returnRemaining -= deltaTime;
        if (_returnRemaining <= 0f) _returning = false;
    }

    public void ClearReturn()
    {
        _returnPending = false;
        _returning = false;
        _returnRemaining = 0f;
    }
}
