using System;
using UnityEngine;

public enum ChargeRole
{
    None,
    Charger,   // running at an enemy soldier (spec §4.3)
    Follower   // passive support following a charger (spec §4.4)
}

// A soldier's charge (spec §4.3, §4.4). A charger runs at one enemy soldier until it makes contact (one charge = one
// clash), its target dies, its troop breaks or the timeout passes. A follower stays near its charger and supports its
// clash without starting clashes of its own; it returns to the line when the charge ends.
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

    public ChargeRole Role => _role;
    public SoldierAgentState Target => _role == ChargeRole.Charger ? _target : null;
    public Troop TargetTroop => _role == ChargeRole.Charger ? _targetTroop : null;
    public SoldierAgentState Leader => _role == ChargeRole.Follower ? _leader : null;
    public float TimeRemaining => _timeRemaining;
    // Time.time when the charge began. Decides which soldier is A when both are charging (Q-28).
    public float StartTime => _startTime;

    public void BeginCharge(SoldierAgentState target, Troop targetTroop, float timeout, float startTime)
    {
        _role = ChargeRole.Charger;
        _target = target;
        _targetTroop = targetTroop;
        _leader = null;
        _timeRemaining = Mathf.Max(0f, timeout);
        _startTime = startTime;
    }

    public void BeginFollow(SoldierAgentState leader, float timeout, float startTime)
    {
        _role = ChargeRole.Follower;
        _leader = leader;
        _target = null;
        _targetTroop = null;
        _timeRemaining = Mathf.Max(0f, timeout);
        _startTime = startTime;
    }

    public void Tick(float deltaTime)
    {
        if (_role != ChargeRole.None) _timeRemaining = Mathf.Max(0f, _timeRemaining - deltaTime);
    }

    public void End()
    {
        _role = ChargeRole.None;
        _target = null;
        _targetTroop = null;
        _leader = null;
        _timeRemaining = 0f;
    }
}
