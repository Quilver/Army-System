using UnityEngine;

// Moves the troop pivot: marching to a destination, pursuing an enemy troop, or being pushed by allied line links
// while retreating.
public sealed class TroopPivotMover
{
    const float ArrivalDistanceSquared = 0.0004f;
    const float ArrivalPushSquared = 0.0025f;
    const float MaxPivotSpeedCap = 6.24f;

    readonly Troop _troop;
    readonly TroopPathFollower _path;
    bool _hasDestination;
    bool _pursuitActive;
    Vector2 _destination;
    Troop _pursuitTarget;
    Vector2 _pivotVelocity;
    Vector2 _linePushVelocity;

    public TroopPivotMover(Troop troop, TroopPathFollower path)
    {
        _troop = troop;
        _path = path;
    }

    public bool IsMoving => _hasDestination || _pursuitActive;
    public bool HasPlainDestination => _hasDestination && !_pursuitActive;
    public Vector2 Destination => _destination;
    public Vector2 PivotVelocity => _pivotVelocity + _linePushVelocity;
    public Vector2 LinePushVelocity => _linePushVelocity;
    public void SetLinePushVelocity(Vector2 velocity) => _linePushVelocity = velocity;

    public void MoveTo(Vector2 worldPosition)
    {
        _pursuitActive = false;
        _pursuitTarget = null;
        _path.RequestRebuild();
        _destination = worldPosition;
        _hasDestination = true;
    }

    public void Pursue(Troop target)
    {
        _pursuitTarget = target;
        _pursuitActive = true;
        _hasDestination = true;
        _path.RequestRebuild();
    }

    // Drops every movement order (used when the troop breaks and retreats).
    public void CancelOrders()
    {
        _hasDestination = false;
        _pursuitActive = false;
        _pursuitTarget = null;
        _path.Invalidate();
        _pivotVelocity = Vector2.zero;
    }

    public void Tick(float deltaTime)
    {
        Transform transform = _troop.transform;
        Vector2 before = transform.position;
        if (_troop.State == UnitState.Fleeing)
            transform.position += (Vector3)(_linePushVelocity * deltaTime); // linked neighbours keep hold during retreat
        else
            MoveToDestination(deltaTime);
        TroopDiagnostics.RecordPivot(_troop, _hasDestination, _pursuitTarget, _pivotVelocity, _linePushVelocity,
            (Vector2)transform.position - before);
    }

    void MoveToDestination(float deltaTime)
    {
        Vector2 position = _troop.transform.position;
        _pivotVelocity = Vector2.zero;
        UpdatePursuit();

        if (_hasDestination)
        {
            Vector2 offset = _destination - position;
            if (offset.sqrMagnitude <= ArrivalDistanceSquared && _linePushVelocity.sqrMagnitude <= ArrivalPushSquared)
            {
                position = _destination;
                if (!_pursuitActive) Arrive();
            }
            else if (deltaTime > 0f && offset.sqrMagnitude > 0.000001f)
            {
                Vector2 step = _path.Step(position, _destination, MaxPivotSpeed(), deltaTime);
                _pivotVelocity = step / deltaTime;
                position += step;
            }
        }

        _troop.transform.position = (Vector3)position + (Vector3)(_linePushVelocity * deltaTime);
    }

    void UpdatePursuit()
    {
        if (!_pursuitActive) return;
        if (_pursuitTarget == null)
        {
            _pursuitActive = false;
            _hasDestination = false;
            _path.Invalidate();
            _troop.State = UnitState.Idle;
            return;
        }
        _destination = _pursuitTarget.transform.position;
        _hasDestination = true;
        _path.RequestRebuildIfTargetMoved(_destination);
    }

    void Arrive()
    {
        _hasDestination = false;
        _troop.State = UnitState.Idle;
        _path.Invalidate();
        _troop.LineOrder.CompleteOnArrival();
    }

    float MaxPivotSpeed() => Mathf.Min(MaxPivotSpeedCap, _troop.Roster.MaxProfileValue(profile => profile.MaxSpeed) * 0.65f);
}