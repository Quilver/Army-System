using System.Collections.Generic;
using UnityEngine;

// Plans a navmesh path to the troop's destination and steps the pivot along it.
public sealed class TroopPathFollower
{
    const float WaypointReachedDistance = 0.2f;
    const float TargetMovedSquared = 0.04f;

    readonly Troop _troop;
    MovementSystem.IPathfinder _pathfinder;
    List<Vector2> _path;
    Vector2 _pathTarget;
    int _pathIndex;
    float _nextRebuild;
    bool _invalid = true;

    public TroopPathFollower(Troop troop)
    {
        _troop = troop;
    }

    public void EnsurePathfinder()
    {
        if (_pathfinder != null) return;
        var pathObject = new GameObject("Troop NavPathfinder");
        pathObject.transform.SetParent(_troop.transform, false);
        var pathfinder = pathObject.AddComponent<MovementSystem.NavPathfinder>();
        pathfinder.SetNavigationSize(Vector2.one * (_troop.Radius * 2f));
        _pathfinder = pathfinder;
    }

    // Forces a rebuild on the next step without discarding the current path.
    public void RequestRebuild() => _invalid = true;

    // Pursuit destinations move; rebuild when the target has drifted from where the path was planned.
    public void RequestRebuildIfTargetMoved(Vector2 destination)
    {
        if ((destination - _pathTarget).sqrMagnitude > TargetMovedSquared) _invalid = true;
    }

    public void Invalidate()
    {
        _path = null;
        _pathIndex = 0;
        _invalid = true;
    }

    // Returns the displacement to apply this frame, at most maxSpeed * deltaTime along the path.
    public Vector2 Step(Vector2 position, Vector2 destination, float maxSpeed, float deltaTime)
    {
        if (_invalid || _path == null || Time.time >= _nextRebuild)
            Rebuild(destination);
        if (_path == null || _path.Count == 0) return Vector2.zero;

        while (_pathIndex < _path.Count - 1
            && Vector2.Distance(position, _path[_pathIndex]) <= WaypointReachedDistance)
            _pathIndex++;

        Vector2 waypointOffset = _path[_pathIndex] - position;
        if (waypointOffset.sqrMagnitude <= 0.000001f) return Vector2.zero;
        return waypointOffset.normalized * Mathf.Min(waypointOffset.magnitude, maxSpeed * deltaTime);
    }

    void Rebuild(Vector2 destination)
    {
        EnsurePathfinder();
        _pathTarget = destination;
        _path = _pathfinder.GetPath(destination);
        _pathIndex = _path != null && _path.Count > 1 ? 1 : 0;
        _invalid = false;
        _nextRebuild = Time.time + TroopTuning.Active.PathRebuildIntervalSeconds;
    }
}