using UnityEngine;

// A troop's rout (spec §5.4). Final: a routed troop never recovers. It breaks its chain links, scatters into a loose blob
// and flees away from the nearest enemy (Q-35) at its Move speed. It still takes hits. After RoutDespawnSeconds it is
// destroyed without raising the destroyed event again (the event is raised once, at rout; Q-36).
public sealed class TroopRout
{
    readonly Troop _troop;
    float _elapsed;
    Vector2 _fleeDirection;

    public TroopRout(Troop troop)
    {
        _troop = troop;
    }

    public bool IsRouted { get; private set; }
    public float Elapsed => _elapsed;
    public Vector2 FleeDirection => _fleeDirection;

    public void Begin(Vector2 fleeDirection)
    {
        IsRouted = true;
        _elapsed = 0f;
        _fleeDirection = fleeDirection.sqrMagnitude > 1e-6f ? fleeDirection.normalized : -(Vector2)_troop.transform.right;
    }

    // Returns true when the despawn time has passed.
    public bool Tick(float deltaTime, float despawnSeconds)
    {
        if (!IsRouted) return false;
        _elapsed += deltaTime;
        return _elapsed >= despawnSeconds;
    }

    // Keeps fleeing away from whichever enemy is nearest now.
    public void UpdateFleeDirection(Vector2 awayFromNearestEnemy)
    {
        if (awayFromNearestEnemy.sqrMagnitude > 1e-6f) _fleeDirection = awayFromNearestEnemy.normalized;
    }
}
