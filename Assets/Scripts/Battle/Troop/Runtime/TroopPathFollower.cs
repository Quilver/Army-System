using System.Collections.Generic;
using UnityEngine;

// Plans a navmesh path to the troop's destination and steps the pivot along it.
public sealed class TroopPathFollower
{
    const float WaypointReachedDistance = 0.2f;
    const float TargetMovedSquared = 0.04f;
    const float ArrivedSquared = 0.0004f;

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

    // The rest of the planned path from the given position: the position, then every waypoint still ahead. Empty when no
    // path is planned yet. Read-only; used to draw the hovered troop's order (TroopOrderPathView).
    public void CollectRemainingPath(Vector2 position, List<Vector2> results)
    {
        results.Clear();
        if (_path == null || _path.Count == 0) return;
        results.Add(position);
        for (int i = Mathf.Clamp(_pathIndex, 0, _path.Count - 1); i < _path.Count; i++)
            results.Add(_path[i]);
    }

    // True once the planned path is used up: there is no route to the destination, or the pivot stands on the last
    // waypoint. The last waypoint is short of the destination when the destination is off the NavMesh (in water, say),
    // so the troop ends the order here instead of marching on the spot for ever (SU-73).
    public bool IsAtEnd(Vector2 position)
    {
        if (_invalid || _path == null) return false;
        if (_path.Count == 0) return true;
        return _pathIndex >= _path.Count - 1 && (position - _path[_path.Count - 1]).sqrMagnitude <= ArrivedSquared;
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
        // Start at the first waypoint: Step skips it when the pivot already stands on it, and walks to it first when the
        // pivot was off the NavMesh and the path starts at its snapped point.
        _pathIndex = 0;
        _invalid = false;
        _nextRebuild = Time.time + TroopTuning.Active.PathRebuildIntervalSeconds;
    }
}