using UnityEngine;

// Assault (spec §4.2): a troop with an active order (Move, Pursue or Line) that moves into engagement gets
// +AssaultAggressionBonus Aggression and +AssaultChargeBonus Charge (while charging) for AssaultDurationSeconds.
// A pursuing troop that re-engages refreshes the timer. A troop without an order never gains Assault.
public sealed class AssaultState
{
    float _remaining;

    public bool IsActive => _remaining > 0f;
    public float TimeRemaining => _remaining;

    // Called on the frame the troop becomes engaged.
    public void OnEngaged(bool hasActiveOrder, float duration)
    {
        if (hasActiveOrder) _remaining = Mathf.Max(0f, duration);
    }

    public void Tick(float deltaTime)
    {
        if (_remaining > 0f) _remaining = Mathf.Max(0f, _remaining - deltaTime);
    }

    public void Clear() => _remaining = 0f;
}
