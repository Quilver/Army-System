using System;
using UnityEngine;
using UnityEngine.Serialization;

// A soldier's current charge at an enemy soldier, and the cooldown before it may charge again.
[Serializable]
public sealed class SoldierChargeState
{
    [SerializeField] float _cooldownRemaining;
    [SerializeField] SoldierAgentState _target;
    [SerializeField, FormerlySerializedAs("_targetFlock")] Troop _targetTroop;
    [SerializeField] float _timeRemaining;

    public float CooldownRemaining => _cooldownRemaining;
    public SoldierAgentState Target => _target;
    public Troop TargetTroop => _targetTroop;
    public float TimeRemaining => _timeRemaining;

    public void TickCooldown(float deltaTime)
    {
        _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
    }

    public void Begin(SoldierAgentState target, Troop targetTroop, float duration, float cooldown)
    {
        _target = target;
        _targetTroop = targetTroop;
        _timeRemaining = Mathf.Max(0f, duration);
        _cooldownRemaining = Mathf.Max(0f, cooldown);
    }

    public void Tick(float deltaTime)
    {
        if (_target != null) _timeRemaining = Mathf.Max(0f, _timeRemaining - deltaTime);
    }

    public void Cancel(float cooldown = 0f)
    {
        _target = null;
        _targetTroop = null;
        _timeRemaining = 0f;
        _cooldownRemaining = Mathf.Max(_cooldownRemaining, cooldown);
    }
}