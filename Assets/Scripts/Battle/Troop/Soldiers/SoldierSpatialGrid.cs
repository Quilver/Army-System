using System.Collections.Generic;
using UnityEngine;

// Spatial hash of a single troop's living soldiers (in troop-local space) for neighbour queries.
public sealed class SoldierSpatialGrid
{
    const float CellSize = TroopConstants.NeighborRadius;
    readonly Dictionary<Vector2Int, List<SoldierAgentState>> _buckets = new();
    readonly Stack<List<SoldierAgentState>> _unusedBuckets = new();

    public static Vector2Int CellFor(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize));
    }

    public bool TryGetBucket(Vector2Int cell, out List<SoldierAgentState> bucket) => _buckets.TryGetValue(cell, out bucket);

    public void Rebuild(List<SoldierAgentState> soldiers)
    {
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
            _unusedBuckets.Push(bucket);
        }
        _buckets.Clear();
        foreach (var soldier in soldiers)
        {
            if (!soldier.IsAlive) continue;
            Vector2Int cell = CellFor(soldier.Position);
            if (!_buckets.TryGetValue(cell, out var bucket))
            {
                bucket = _unusedBuckets.Count > 0 ? _unusedBuckets.Pop() : new List<SoldierAgentState>();
                _buckets.Add(cell, bucket);
            }
            bucket.Add(soldier);
        }
    }
}