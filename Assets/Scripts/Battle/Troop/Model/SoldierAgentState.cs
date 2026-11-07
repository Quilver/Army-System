using System;
using UnityEngine;

public enum SoldierLifeState
{
    Active,
    Fatigued,
    Dead,
    // Brought to 0 HP mostly by bruises (SU-107): down but alive. Cannot charge or support; recovers at StunRecoverHp.
    Stunned
}

// One soldier: identity, life and motion. Charging, retreating, clash flashes and external separation each live in a
// small state object; the members below forward to them so callers deal with a single soldier.
[Serializable]
public sealed class SoldierAgentState
{
    [SerializeField] int _id;
    [SerializeField] CharacterProfile _character;
    [SerializeField] SoldierLifeState _lifeState;
    [SerializeField] SoldierHealth _health;
    [SerializeField] Vector2 _position;
    [SerializeField] Vector2 _velocity;
    [SerializeField] Vector2 _facing = Vector2.right;
    [SerializeField] float _driveFraction;
    [SerializeField] SoldierChargeState _charge = new();
    [SerializeField] SoldierFaultMotionState _fault = new();
    [SerializeField] SoldierClashPulseState _clashPulse = new();
    [SerializeField] SoldierSeparationState _separation = new();
    [SerializeField] float _clashCooldownRemaining;

    public int Id => _id;
    // The character this soldier belongs to: its soldier stats and equipment.
    public CharacterProfile Character => _character;
    public SoldierLifeState LifeState => _lifeState;
    public SoldierHealth Health => _health;
    public int CurrentHp => _health.CurrentHp;
    public int MaxHp => _health.MaxHp;
    public bool IsAlive => _lifeState != SoldierLifeState.Dead;

    // Alive and not fatigued or stunned: the soldier can take part in supporting its neighbours.
    public bool CanSupport => _lifeState == SoldierLifeState.Active;
    // Down at low HP from bruises (SU-107): alive, but cannot charge, follow or support.
    public bool IsStunned => _lifeState == SoldierLifeState.Stunned;

    // Defence plus the armour bonus. Without a character or armour, only the soldier's Defence (or 0) counts.
    public int TotalDefence => _character == null ? 0 : _character.Defence + (_character.Armour != null ? _character.Armour.DefenceBonus : 0);
    public Vector2 Position { get => _position; internal set => _position = value; }
    public Vector2 Velocity { get => _velocity; internal set => _velocity = value; }
    public Vector2 Facing { get => _facing; internal set => _facing = value; }
    // 0-1: how hard the soldier is driving toward its place in the field this frame (desired speed over max speed). A driving
    // soldier has more pressure and shoves settled neighbours aside instead of being stalled by them.
    public float DriveFraction { get => _driveFraction; internal set => _driveFraction = value; }

    // Clash pulse
    public bool IsClashColorPulsing => _clashPulse.IsPulsing;
    public float ClashColorPulseProgress => _clashPulse.Progress;

    // Charge (spec §4.3, §4.4)
    public ChargeRole ChargeRole => _charge.Role;
    public SoldierAgentState ChargeTarget => _charge.Target;
    public Troop ChargeTargetTroop => _charge.TargetTroop;
    public SoldierAgentState ChargeLeader => _charge.Leader;
    public float ChargeTimeRemaining => _charge.TimeRemaining;
    public float ChargeStartTime => _charge.StartTime;

    // Per-soldier clash rate limit for contacts that are not charges (spec §4.5, Q-26).
    public float ClashCooldownRemaining => _clashCooldownRemaining;

    // Retreat
    public float FaultOffset { get => _fault.Offset; internal set => _fault.Offset = value; }
    public float FaultRetreatScale { get => _fault.RetreatScale; internal set => _fault.RetreatScale = value; }
    public float FaultDelayRemaining { get => _fault.DelayRemaining; internal set => _fault.DelayRemaining = value; }
    public float FaultMoveDuration => _fault.MoveDuration;
    public bool IsRetreatTracking => _fault.Tracking;
    public Vector2 FaultHome => _fault.Home;
    public Vector2 FaultTargetPosition => _fault.TargetPosition;
    public Vector2 FaultTargetVelocity => _fault.TargetVelocity;
    internal void BeginFaultTracking() => _fault.BeginTracking(_position);
    internal void EndFaultTracking() => _fault.EndTracking();
    internal void SetFaultTarget(Vector2 position, Vector2 velocity) => _fault.SetTarget(position, velocity);

    // Separation from other troops
    public Vector2 ExternalSeparation => _separation.External;

    public SoldierAgentState(int id, CharacterProfile character, Vector2 position)
    {
        _id = id;
        _character = character;
        _lifeState = SoldierLifeState.Active;
        _health = new SoldierHealth(character != null ? character.MaxHealth : CombatStatLimits.HpDefault);
        _position = position;
    }

    public void TickCooldowns(float deltaTime) =>
        _clashCooldownRemaining = Mathf.Max(0f, _clashCooldownRemaining - deltaTime);

    public void StartClashCooldown(float seconds) =>
        _clashCooldownRemaining = Mathf.Max(_clashCooldownRemaining, seconds);

    // A soldier charging an enemy soldier.
    public bool IsCharging => _charge.Role == ChargeRole.Charger && _charge.Target != null;

    // A passive follower of a charger (spec §4.4).
    public bool IsFollowing => _charge.Role == ChargeRole.Follower && _charge.Leader != null;

    // Chargers and followers are committed: they leave the field, may leave the troop radius, and are exempt from
    // separation, density spreading and drive pressure in both directions (spec §2.3).
    public bool IsCommitted => IsCharging || IsFollowing;

    // Rushing back to its troop after a charge (see SoldierChargeState): charge speed, free of the troop radius, and
    // left out of the footprint until it is back in the formation.
    public bool IsReturning => _charge.IsReturning;

    public void BeginClashColorPulse(float duration) => _clashPulse.Begin(duration);
    public bool TickClashColorPulse(float deltaTime) => _clashPulse.Tick(deltaTime);

    public void BeginCharge(SoldierAgentState target, Troop targetTroop, float timeout, float startTime) =>
        _charge.BeginCharge(target, targetTroop, timeout, startTime);
    public void BeginFollow(SoldierAgentState leader, float timeout, float startTime) =>
        _charge.BeginFollow(leader, timeout, startTime);
    public void TickCharge(float deltaTime) => _charge.Tick(deltaTime);
    public void CancelCharge(bool returnToTroop = true) => _charge.End(returnToTroop);
    internal bool ConsumeReturnPending() => _charge.ConsumeReturnPending();
    internal void BeginReturn(float timeout) => _charge.BeginReturn(timeout);
    internal void TickReturn(float deltaTime) => _charge.TickReturn(deltaTime);
    internal void EndReturn() => _charge.ClearReturn();

    public void SetFaultMotion(float delay, float moveDuration) => _fault.Set(delay, moveDuration);

    public void AddExternalSeparation(Vector2 force) => _separation.Add(force);
    public void ClearExternalSeparation() => _separation.Clear();

    // Takes the rolled hit. Bleed and bruise are clamped to the HP that remains; overflow is applied last, so it cannot
    // push out bruise. Returns what was applied.
    public AppliedDamage ApplyMitigation(MitigationResult mitigation)
    {
        if (!IsAlive) return default;

        int shockBleed = _health.ApplyBleed(mitigation.Bled);
        int bruised = _health.ApplyBruise(mitigation.Bruised);
        int overflowBled = _health.ApplyBleed(mitigation.Overflow);
        SyncLifeState();
        return new AppliedDamage(bruised, shockBleed + overflowBled, shockBleed + bruised);
    }

    // Advances bruise healing by one frame's time. Called once per frame by the troop after its morale tick. A stunned
    // soldier heals too, from 0 HP.
    internal void TickHealing(float deltaTime, float healPerSecond)
    {
        if (!IsAlive) return;
        if (_health.TickBruiseHealing(deltaTime, healPerSecond, healWhileDown: IsStunned) > 0) SyncLifeState();
    }

    // A stunned soldier whose troop retreats from melee is left behind and killed (SU-107). Returns true when it died.
    internal bool KillIfStunned()
    {
        if (!IsStunned) return false;
        _health.BleedOut();
        _lifeState = SoldierLifeState.Dead;
        _charge.End();
        return true;
    }

    // Dead at 0 HP, or stunned when the bruises are at least half of it (SU-107). A stunned soldier stays stunned until it
    // has healed to StunRecoverHp; a hit that brings it back to 0 HP with Bleed over half kills it.
    void SyncLifeState()
    {
        TroopTuning tuning = TroopTuning.Active;
        if (_health.IsDead)
            _lifeState = tuning.StunAtZeroHp && _health.IsKnockedOut ? SoldierLifeState.Stunned : SoldierLifeState.Dead;
        else if (_lifeState == SoldierLifeState.Stunned && _health.CurrentHp < tuning.StunRecoverHp)
            _lifeState = SoldierLifeState.Stunned;
        else
            _lifeState = _health.IsFatigued(tuning.FatigueFraction) ? SoldierLifeState.Fatigued : SoldierLifeState.Active;
        // A stunned soldier stops where it is (no rush back, SU-43); it drifts back to its troop with the field.
        if (_lifeState == SoldierLifeState.Dead) _charge.End();
        else if (_lifeState == SoldierLifeState.Stunned) _charge.End(returnToTroop: false);
    }
}