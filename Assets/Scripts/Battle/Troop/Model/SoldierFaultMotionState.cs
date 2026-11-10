using System;
using UnityEngine;

// A soldier's part in its troop's retreat: when it starts (the rear-to-front wave), how long it takes, how far its
// target still is from its new place (Offset) and how much of the full distance it travels.
[Serializable]
public sealed class SoldierFaultMotionState
{
    [SerializeField] float _delayRemaining;
    [SerializeField] float _moveDuration;
    [SerializeField] float _offset;
    [SerializeField] float _retreatScale = 1f;
    [SerializeField] bool _tracking;
    [SerializeField] Vector2 _home;
    [SerializeField] Vector2 _targetPosition;
    [SerializeField] Vector2 _targetVelocity;

    public float Offset { get => _offset; set => _offset = value; }
    // Fraction of the retreat distance this soldier travels; soldiers near a linked line end retreat less so the link holds.
    public float RetreatScale { get => _retreatScale; set => _retreatScale = value; }
    public float DelayRemaining { get => _delayRemaining; set => _delayRemaining = value; }
    public float MoveDuration => _moveDuration;

    // While tracking, the soldier steers toward a scheduled target (its home position plus its remaining offset) and
    // arrives with it, instead of being moved directly.
    public bool Tracking => _tracking;
    public Vector2 Home => _home;
    public Vector2 TargetPosition => _targetPosition;
    public Vector2 TargetVelocity => _targetVelocity;

    public void Set(float delay, float moveDuration)
    {
        _delayRemaining = Mathf.Max(0f, delay);
        _moveDuration = Mathf.Max(0.01f, moveDuration);
        _offset = 0f;
    }

    public void BeginTracking(Vector2 home)
    {
        _tracking = true;
        _home = home;
        _targetPosition = home;
        _targetVelocity = Vector2.zero;
    }

    public void EndTracking()
    {
        _tracking = false;
        _targetVelocity = Vector2.zero;
    }

    public void SetTarget(Vector2 position, Vector2 velocity)
    {
        _targetPosition = position;
        _targetVelocity = velocity;
    }
}