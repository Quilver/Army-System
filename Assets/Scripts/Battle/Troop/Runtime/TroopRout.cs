using System.Collections.Generic;
using UnityEngine;

// A troop's rout (spec §5.4). Final: a routed troop never recovers. It breaks its chain links, scatters into a loose blob
// and flees away from the nearest enemy (Q-35) at its Move speed. It still takes hits. It leaves the battle when its
// pivot reaches the battlefield edge (IsAtFieldEdge, SU-138), or after RoutDespawnSeconds wherever it is; either way it
// is destroyed without raising the destroyed event again (the event is raised once, at rout; Q-36).
// With a NavMesh the flight follows a flee goal chosen by RoutFleePlanner along a NavMesh path (the troop's
// TroopPathFollower, as a march uses), so the troop runs around woods and rivers instead of into them (SU-121). Its
// goals lie on the battlefield edge, so reaching one takes the troop off the field. Without a NavMesh there is no edge
// and the troop despawns after RoutDespawnSeconds, as before.
// The path is followed by steering: MoveDirection points at the path's next waypoint and TroopPivotMover.Flee runs the
// pivot along it at the troop's speed. Without a NavMesh (or a goal) MoveDirection is the straight flight away from the
// nearest enemy, as before.
public sealed class TroopRout
{
    readonly Troop _troop;
    readonly TroopPathFollower _path;
    readonly RoutFleePlanner _planner = new();
    readonly List<Vector2> _enemyPositions = new();
    float _elapsed;
    Vector2 _fleeDirection;
    Vector2 _moveDirection;

    public TroopRout(Troop troop) : this(troop, null) { }

    public TroopRout(Troop troop, TroopPathFollower path)
    {
        _troop = troop;
        _path = path;
    }

    public bool IsRouted { get; private set; }
    public float Elapsed => _elapsed;
    public Vector2 FleeDirection => _fleeDirection;
    // The goal the routed troop runs to along the NavMesh; false without a NavMesh (straight-line flight).
    public bool HasFleeGoal => IsRouted && _planner.HasGoal;
    public Vector2 FleeGoal => _planner.Goal;

    // The way the pivot runs this frame: toward the next waypoint of the flee path, or straight away from the enemy.
    public Vector2 MoveDirection => _moveDirection.sqrMagnitude > 1e-6f ? _moveDirection : _fleeDirection;

    public void Begin(Vector2 fleeDirection)
    {
        IsRouted = true;
        _elapsed = 0f;
        _fleeDirection = fleeDirection.sqrMagnitude > 1e-6f ? fleeDirection.normalized : -(Vector2)_troop.transform.right;
        _moveDirection = Vector2.zero;
        _planner.Reset(_troop.transform.position, _elapsed);
    }

    // Returns true when the despawn time has passed.
    public bool Tick(float deltaTime, float despawnSeconds)
    {
        if (!IsRouted) return false;
        _elapsed += deltaTime;
        return _elapsed >= despawnSeconds;
    }

    // SU-138: the pivot stands at the battlefield edge (within RoutFleePlanner.EdgeReachedDistance of the edge inset by
    // the blob radius, where Troop.ConstrainPivotStep holds it). Never without a battlefield edge (no NavMesh).
    public bool IsAtFieldEdge => IsRouted && BattlefieldBounds.TryGet(out Rect field)
        && RoutFleePlanner.IsAtEdge(_troop.transform.position, field, EdgeInset);

    // Keeps fleeing away from whichever enemy is nearest now, and (with a NavMesh) re-plans the flee goal around terrain.
    public void UpdateFleeDirection(Vector2 awayFromNearestEnemy)
    {
        if (awayFromNearestEnemy.sqrMagnitude > 1e-6f) _fleeDirection = awayFromNearestEnemy.normalized;
        if (!IsRouted) return;
        Vector2 position = _troop.transform.position;
        if (!BattlefieldBounds.TryGet(out Rect field))
        {
            if (_planner.HasGoal) _planner.Clear(position, _elapsed);
            _moveDirection = Vector2.zero;
            return;
        }
        CollectEnemyPositions();
        _planner.Tick(_elapsed, position, _fleeDirection, _enemyPositions, field, EdgeInset, RoutFleePlanner.NavMeshRoute);
    }

    // Points MoveDirection along the flee path for this frame (call after UpdateFleeDirection, before the pivot moves).
    // At the goal or without a route it falls back to the straight flight until the planner picks the next goal.
    public void SteerAlongPath(float pivotSpeed, float deltaTime)
    {
        _moveDirection = Vector2.zero;
        if (!HasFleeGoal || _path == null || deltaTime <= 0f || pivotSpeed <= 0f) return;
        Vector2 goal = _planner.Goal;
        _path.RequestRebuildIfTargetMoved(goal);
        Vector2 step = _path.Step(_troop.transform.position, goal, pivotSpeed, deltaTime);
        if (step.sqrMagnitude > 1e-12f) _moveDirection = step.normalized;
    }

    // The pivot is kept this far inside the battlefield edge (Troop.ConstrainPivotStep).
    float EdgeInset => _troop.Radius * TroopTuning.Active.BlobRadiusScale;

    // The pivots of the enemies the troop flees from: the same ones Troop.AwayFromNearestEnemy looks at.
    void CollectEnemyPositions()
    {
        _enemyPositions.Clear();
        var world = TroopCombatWorld.Current;
        if (world == null) return;
        foreach (var other in world.Troops)
        {
            if (other == null || other == _troop || other.TeamId == _troop.TeamId || other.IsRouted) continue;
            _enemyPositions.Add(other.transform.position);
        }
    }
}
