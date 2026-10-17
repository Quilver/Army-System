using System;
using UnityEngine;

// A soldier's part in its troop's retreat: when it starts, how long it takes, how far it has fallen back and how much
// of the full distance it travels.
[Serializable]
public sealed class SoldierFaultMotionState
{
    [SerializeField] float _delayRemaining;
    [SerializeField] float _moveDuration;
    [SerializeField] float _offset;
    [SerializeField] float _retreatScale = 1f;

    public float Offset { get => _offset; set => _offset = value; }
    // Fraction of the retreat distance this soldier travels; soldiers near a linked line end retreat less so the link holds.
    public float RetreatScale { get => _retreatScale; set => _retreatScale = value; }
    public float DelayRemaining { get => _delayRemaining; set => _delayRemaining = value; }
    public float MoveDuration => _moveDuration;

    public void Set(float delay, float moveDuration)
    {
        _delayRemaining = Mathf.Max(0f, delay);
        _moveDuration = Mathf.Max(0.01f, moveDuration);
        _offset = 0f;
    }
}