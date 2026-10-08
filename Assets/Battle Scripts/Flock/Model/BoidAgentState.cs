using System;
using UnityEngine;

[Serializable]
public sealed class BoidAgentState
{
    [SerializeField] int _id;
    [SerializeField] BoidProfile _profile;
    [SerializeField] BoidLifeState _lifeState;
    [SerializeField] float _health;
    [SerializeField] Vector2 _position;
    [SerializeField] Vector2 _velocity;
    [SerializeField] Vector2 _facing = Vector2.right;
    [SerializeField] float _clashColorPulseDuration;
    [SerializeField] float _clashColorPulseRemaining;
    [SerializeField] Vector2 _homePosition;
    [SerializeField] float _chargeCooldownRemaining;
    [SerializeField] BoidAgentState _chargeTarget;
    [SerializeField] Flock _chargeTargetFlock;
    [SerializeField] float _chargeTimeRemaining;
    [SerializeField] float _faultDelayRemaining;
    [SerializeField] float _faultMoveDuration;
    [SerializeField] float _faultOffset;
    [SerializeField] Vector2 _externalSeparation;

    public int Id => _id;
    public BoidProfile Profile => _profile;
    public BoidLifeState LifeState => _lifeState;
    public float Health => _health;
    public Vector2 Position { get => _position; internal set => _position = value; }
    public Vector2 Velocity { get => _velocity; internal set => _velocity = value; }
    public Vector2 Facing { get => _facing; internal set => _facing = value; }
    public bool IsClashColorPulsing => _clashColorPulseRemaining > 0f;
    public float ClashColorPulseProgress => _clashColorPulseDuration > 0f
        ? 1f - _clashColorPulseRemaining / _clashColorPulseDuration
        : 1f;
    public float ChargeCooldownRemaining => _chargeCooldownRemaining;
    public BoidAgentState ChargeTarget => _chargeTarget;
    public Flock ChargeTargetFlock => _chargeTargetFlock;
    public float ChargeTimeRemaining => _chargeTimeRemaining;
    public float FaultOffset { get => _faultOffset; internal set => _faultOffset = value; }
    public float FaultDelayRemaining { get => _faultDelayRemaining; internal set => _faultDelayRemaining = value; }
    public float FaultMoveDuration => _faultMoveDuration;
    public Vector2 ExternalSeparation => _externalSeparation;
    public Vector2 HomePosition { get => _homePosition; internal set => _homePosition = value; }
    public bool IsAlive => _lifeState != BoidLifeState.Dead;

    public BoidAgentState(int id, BoidProfile profile, Vector2 position)
    {
        _id = id;
        _profile = profile;
        _lifeState = BoidLifeState.Active;
        _health = profile != null ? profile.MaxHealth : 1f;
        _position = position;
        _homePosition = position;
    }

    public void TickCooldowns(float deltaTime)
    {
        _chargeCooldownRemaining = Mathf.Max(0f, _chargeCooldownRemaining - deltaTime);
    }

    public bool TryBeginMelee(float aggression)
    {
        return IsAlive && aggression > 0f && UnityEngine.Random.value < Mathf.Clamp01(aggression / 100f);
    }

    public void BeginClashColorPulse(float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration < _clashColorPulseRemaining) return;
        _clashColorPulseDuration = duration;
        _clashColorPulseRemaining = duration;
    }

    public bool TickClashColorPulse(float deltaTime)
    {
        bool wasPulsing = IsClashColorPulsing;
        _clashColorPulseRemaining = Mathf.Max(0f, _clashColorPulseRemaining - deltaTime);
        return wasPulsing && !IsClashColorPulsing;
    }

    public void SetCharge(BoidAgentState target, Flock targetFlock, float duration, float cooldown)
    {
        _chargeTarget = target;
        _chargeTargetFlock = targetFlock;
        _chargeTimeRemaining = Mathf.Max(0f, duration);
        _chargeCooldownRemaining = Mathf.Max(0f, cooldown);
    }

    public void TickCharge(float deltaTime)
    {
        if (_chargeTarget != null) _chargeTimeRemaining = Mathf.Max(0f, _chargeTimeRemaining - deltaTime);
    }

    public void CancelCharge(float cooldown = 0f)
    {
        _chargeTarget = null;
        _chargeTargetFlock = null;
        _chargeTimeRemaining = 0f;
        _chargeCooldownRemaining = Mathf.Max(_chargeCooldownRemaining, cooldown);
    }

    public void SetFaultMotion(float delay, float moveDuration)
    {
        _faultDelayRemaining = Mathf.Max(0f, delay);
        _faultMoveDuration = Mathf.Max(0.01f, moveDuration);
        _faultOffset = 0f;
    }

    public void ResetExternalSeparation()
    {
        _externalSeparation = Vector2.zero;
    }

    public void AddExternalSeparation(Vector2 force)
    {
        _externalSeparation += force;
    }

    public void ClearExternalSeparation()
    {
        _externalSeparation = Vector2.zero;
    }

    public float ApplyDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
            return 0f;

        float appliedDamage = Mathf.Min(_health, damage);
        _health -= appliedDamage;
        _lifeState = _health <= 0f
            ? BoidLifeState.Dead
            : _health < (_profile != null ? _profile.MaxHealth : 1f)
                ? BoidLifeState.Injured
                : BoidLifeState.Active;
        return appliedDamage;
    }
}
