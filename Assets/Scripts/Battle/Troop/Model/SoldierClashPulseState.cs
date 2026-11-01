using System;
using UnityEngine;

// Timer for the brief colour flash on a soldier that has just clashed.
[Serializable]
public sealed class SoldierClashPulseState
{
    [SerializeField] float _duration;
    [SerializeField] float _remaining;

    public bool IsPulsing => _remaining > 0f;
    // 0 at the start of the flash, 1 when finished.
    public float Progress => _duration > 0f ? 1f - _remaining / _duration : 1f;

    public void Begin(float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration < _remaining) return;
        _duration = duration;
        _remaining = duration;
    }

    // Returns true on the frame the pulse ends.
    public bool Tick(float deltaTime)
    {
        bool wasPulsing = IsPulsing;
        _remaining = Mathf.Max(0f, _remaining - deltaTime);
        return wasPulsing && !IsPulsing;
    }
}