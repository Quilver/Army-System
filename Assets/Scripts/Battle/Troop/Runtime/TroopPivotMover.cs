using UnityEngine;

// Moves the troop pivot: marching to a destination (Move, or the centre of a line order), pursuing an enemy troop,
// fleeing after a rout, and the line-push drift from line pressure and link tension.
// Pivot speed is the troop's Move x SpeedUnitsPerStatPoint (spec §1.3, §3.2); Move is the slowest soldier's Speed.
public sealed class TroopPivotMover
{
    const float ArrivalDistanceSquared = 0.0004f;
    const float ArrivalPushSquared = 0.0025f;

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
    public bool IsPursuing => _pursuitActive;
    public Troop PursuitTarget => _pursuitActive ? _pursuitTarget : null;
    public bool HasPlainDestination => _hasDestination && !_pursuitActive;
    public Vector2 Destination => _destination;
    public Vector2 PivotVelocity => _pivotVelocity + _linePushVelocity;
    public Vector2 MarchVelocity => _pivotVelocity;
    public Vector2 LinePushVelocity => _linePushVelocity;
    public void SetLinePushVelocity(Vector2 velocity) => _linePushVelocity = velocity;

    // Pivot speed in units/s.
    public float PivotSpeed => _troop.Move * TroopTuning.Active.SpeedUnitsPerStatPoint;

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

    // Drops every movement order (used when the troop breaks or routs).
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
        if (_troop.IsRouted)
            Flee(deltaTime);
        else if (_troop.IsFaultSlipping)
            _troop.transform.position += (Vector3)(_linePushVelocity * deltaTime); // linked neighbours keep hold during retreat
        else
            MoveToDestination(deltaTime);
    }

    void Flee(float deltaTime)
    {
        _pivotVelocity = _troop.RoutFleeDirection * PivotSpeed;
        Vector2 position = _troop.transform.position;
        Vector2 step = _pivotVelocity * deltaTime;
        _troop.transform.position = position + step;
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
                Vector2 step = _path.Step(position, _destination, PivotSpeed, deltaTime);
                _pivotVelocity = step / deltaTime;
                position += step;
            }
        }

        _troop.transform.position = (Vector3)position + (Vector3)(_linePushVelocity * deltaTime);
    }

    // Pursuit ends when the target dies (or routs and despawns) or the player gives a new order (Q-24).
    void UpdatePursuit()
    {
        if (!_pursuitActive) return;
        if (_pursuitTarget == null || _pursuitTarget.LivingSoldierCount == 0)
        {
            _pursuitActive = false;
            _hasDestination = false;
            _pursuitTarget = null;
            _path.Invalidate();
            _troop.OnOrderComplete();
            return;
        }
        _destination = _pursuitTarget.transform.position;
        _hasDestination = true;
        _path.RequestRebuildIfTargetMoved(_destination);
    }

    void Arrive()
    {
        _hasDestination = false;
        _path.Invalidate();
        _troop.OnOrderComplete();
    }
}
