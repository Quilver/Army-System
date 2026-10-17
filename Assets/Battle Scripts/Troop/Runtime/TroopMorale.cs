using UnityEngine;

// The troop's will to fight. Damage adds shock (the damage taken since the troop last broke) and lost clashes add fault
// pressure. When shock exceeds morale the troop breaks (see Troop.TryBeginFaultSlip) and shock is reset. Disable to
// stop new shock, pressure and breaks; a retreat that has already started still finishes.
[DisallowMultipleComponent]
public sealed class TroopMorale : MonoBehaviour
{
    const float DecayTime = 22f;
    const float ClashCooldown = 0.3f;
    const float BreakCooldown = 1.6f;
    const float PulseLockDuration = 1.2f;
    const float MinimumBreakThreshold = 0.1f;
    const float DefaultMorale = 100f;  // used only when the troop has no profile
    const float DefaultChargeDistance = TroopDamageReceiver.DefaultChargeDistance;

    Troop _troop;
    float _shock;
    float _faultPressure;
    float _faultCooldownRemaining;
    float _clashCooldownRemaining;
    float _pulseCooldownRemaining;
    Vector2 _lastAttackerPivotPosition;
    float _lastChargeDistance = DefaultChargeDistance;

    public float Value => _troop != null && _troop.Profile != null ? _troop.Profile.Morale : DefaultMorale;
    public float Shock => _shock;
    public float FaultPressure => _faultPressure;
    public float BreakCooldownRemaining => _faultCooldownRemaining;
    // While true, soldiers do not start new charges.
    public bool IsPulseLocked => enabled && _pulseCooldownRemaining > 0f;
    public bool ShouldBreak => _shock > Mathf.Max(MinimumBreakThreshold, Value) && _faultCooldownRemaining <= 0f;
    public Vector2 LastAttackerPivotPosition => _lastAttackerPivotPosition;
    public float LastChargeDistance => _lastChargeDistance;

    internal void Bind(Troop troop) => _troop = troop;

    // Cooldowns run down and fault pressure fades over time. Shock does not fade: it is the damage taken so far, and is
    // only cleared when the troop breaks.
    internal void Tick(float deltaTime)
    {
        _faultCooldownRemaining = Mathf.Max(0f, _faultCooldownRemaining - deltaTime);
        _clashCooldownRemaining = Mathf.Max(0f, _clashCooldownRemaining - deltaTime);
        _pulseCooldownRemaining = Mathf.Max(0f, _pulseCooldownRemaining - deltaTime);
        _faultPressure *= Mathf.Exp(-deltaTime / DecayTime);
    }

    // Returns false while the loser is still inside its clash cooldown, which also blocks the winner's damage.
    internal bool RecordClashLoss(float pressureMargin)
    {
        if (!enabled) return true;
        if (_clashCooldownRemaining > 0f) return false;
        _clashCooldownRemaining = ClashCooldown;
        float threshold = 1f + _troop.AverageProfileValue(profile => profile.MeleeSkill) / 100f * 0.45f;
        float pressure = 0.28f + Mathf.Clamp01(pressureMargin) * 0.22f;
        _faultPressure = Mathf.Min(threshold * 2.5f, _faultPressure + pressure);
        return true;
    }

    internal void AddShock(float damage, Vector2 attackerPivotPosition, float chargeDistance)
    {
        if (!enabled) return;
        _shock += damage;
        _lastAttackerPivotPosition = attackerPivotPosition;
        _lastChargeDistance = chargeDistance;
    }

    // The troop has just broken: shock is spent, pressure drops by one morale's worth, and new breaks and charges wait.
    internal void OnBroke()
    {
        _shock = 0f;
        _faultPressure = Mathf.Max(0f, _faultPressure - Mathf.Max(MinimumBreakThreshold, Value));
        _faultCooldownRemaining = BreakCooldown;
        _pulseCooldownRemaining = PulseLockDuration;
    }
}