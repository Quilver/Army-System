using System;
using UnityEngine;

// One soldier: identity, life and motion. Charging, retreating, clash flashes and external separation each live in a
// small state object; the members below forward to them so callers deal with a single soldier.
[Serializable]
public sealed class SoldierAgentState
{
    [SerializeField] int _id;
    [SerializeField] SoldierProfile _profile;
    [SerializeField] SoldierLifeState _lifeState;
    [SerializeField] float _health;
    [SerializeField] Vector2 _position;
    [SerializeField] Vector2 _velocity;
    [SerializeField] Vector2 _facing = Vector2.right;
    [SerializeField] float _driveFraction;
    [SerializeField] SoldierChargeState _charge = new();
    [SerializeField] SoldierFaultMotionState _fault = new();
    [SerializeField] SoldierClashPulseState _clashPulse = new();
    [SerializeField] SoldierSeparationState _separation = new();
    [SerializeField] SoldierMeleeState _melee = new();

    public int Id => _id;
    public SoldierProfile Profile => _profile;
    public SoldierLifeState LifeState => _lifeState;
    public float Health => _health;
    public bool IsAlive => _lifeState != SoldierLifeState.Dead;
    public Vector2 Position { get => _position; internal set => _position = value; }
    public Vector2 Velocity { get => _velocity; internal set => _velocity = value; }
    public Vector2 Facing { get => _facing; internal set => _facing = value; }
    // 0-1: how hard the soldier is driving toward its place in the field this frame (desired speed over max speed). A driving
    // soldier has more pressure and shoves settled neighbours aside instead of being stalled by them.
    public float DriveFraction { get => _driveFraction; internal set => _driveFraction = value; }

    // Clash pulse
    public bool IsClashColorPulsing => _clashPulse.IsPulsing;
    public float ClashColorPulseProgress => _clashPulse.Progress;

    // Charge
    public float ChargeCooldownRemaining => _charge.CooldownRemaining;
    public SoldierAgentState ChargeTarget => _charge.Target;
    public Troop ChargeTargetTroop => _charge.TargetTroop;
    public float ChargeTimeRemaining => _charge.TimeRemaining;

    // Retreat
    public float FaultOffset { get => _fault.Offset; internal set => _fault.Offset = value; }
    public float FaultRetreatScale { get => _fault.RetreatScale; internal set => _fault.RetreatScale = value; }
    public float FaultDelayRemaining { get => _fault.DelayRemaining; internal set => _fault.DelayRemaining = value; }
    public float FaultMoveDuration => _fault.MoveDuration;

    // Separation from other troops
    public Vector2 ExternalSeparation => _separation.External;

    public SoldierAgentState(int id, SoldierProfile profile, Vector2 position)
    {
        _id = id;
        _profile = profile;
        _lifeState = SoldierLifeState.Active;
        _health = profile != null ? profile.MaxHealth : 1f;
        _position = position;
    }

    public void TickCooldowns(float deltaTime) => _charge.TickCooldown(deltaTime);

    // Melee attack accrual and discipline timing (see SoldierMeleeState).
    public SoldierMeleeState Melee => _melee;

    // Spends a ready melee attack. Dead soldiers cannot attack.
    public bool TrySpendMeleeAttack() => IsAlive && _melee.TrySpendAttack();
    public bool HasMeleeAttack => IsAlive && _melee.ReadyAttacks > 0;

    // Committed soldiers are charging an enemy or lunging at one. They move toward it and may leave the troop radius.
    public bool IsCommitted => ChargeTarget != null || _melee.IsLunging;

    // A soldier charging an enemy. Charging soldiers slip between other soldiers instead of being pushed apart.
    public bool IsCharging => ChargeTarget != null;

    public void BeginClashColorPulse(float duration) => _clashPulse.Begin(duration);
    public bool TickClashColorPulse(float deltaTime) => _clashPulse.Tick(deltaTime);

    public void SetCharge(SoldierAgentState target, Troop targetTroop, float duration, float cooldown) =>
        _charge.Begin(target, targetTroop, duration, cooldown);
    public void TickCharge(float deltaTime) => _charge.Tick(deltaTime);
    public void CancelCharge(float cooldown = 0f) => _charge.Cancel(cooldown);

    public void SetFaultMotion(float delay, float moveDuration) => _fault.Set(delay, moveDuration);

    public void AddExternalSeparation(Vector2 force) => _separation.Add(force);
    public void ClearExternalSeparation() => _separation.Clear();

    public float ApplyDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
            return 0f;

        float appliedDamage = Mathf.Min(_health, damage);
        _health -= appliedDamage;
        _lifeState = _health <= 0f
            ? SoldierLifeState.Dead
            : _health < (_profile != null ? _profile.MaxHealth : 1f)
                ? SoldierLifeState.Injured
                : SoldierLifeState.Active;
        return appliedDamage;
    }
}