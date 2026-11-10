using System.Collections.Generic;
using UnityEngine;

// Spatial hash of every living soldier across all troops. Contacts are stored in a flat list and the grid buckets
// hold indices into it.
public sealed class SoldierContactIndex
{
    const float CellSize = TroopConstants.NeighborRadius;
    readonly List<SoldierContact> _contacts = new();
    readonly Dictionary<Vector2Int, List<int>> _buckets = new();
    readonly Stack<List<int>> _unusedBuckets = new();

    public IReadOnlyList<SoldierContact> Contacts => _contacts;

    public static Vector2Int CellFor(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize));
    }

    public bool TryGetBucket(Vector2Int cell, out List<int> bucket) => _buckets.TryGetValue(cell, out bucket);

    // Indices of every contact within radius of position (spatial query, not a loop over all contacts).
    public void GatherWithin(Vector2 position, float radius, List<int> results)
    {
        results.Clear();
        if (radius <= 0f) return;
        Vector2Int center = CellFor(position);
        int reach = Mathf.CeilToInt(radius / CellSize);
        float radiusSquared = radius * radius;
        for (int x = -reach; x <= reach; x++)
        for (int y = -reach; y <= reach; y++)
        {
            if (!_buckets.TryGetValue(new Vector2Int(center.x + x, center.y + y), out var bucket)) continue;
            foreach (int index in bucket)
                if ((_contacts[index].Position - position).sqrMagnitude <= radiusSquared) results.Add(index);
        }
    }

    public void Rebuild(IReadOnlyList<Troop> troops)
    {
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
            _unusedBuckets.Push(bucket);
        }
        _buckets.Clear();
        _contacts.Clear();

        foreach (var troop in troops)
        {
            if (troop == null) continue;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                int contactIndex = _contacts.Count;
                Vector2 position = troop.WorldPosition(soldier);
                _contacts.Add(new SoldierContact(troop, soldier, position));
                Vector2Int cell = CellFor(position);
                if (!_buckets.TryGetValue(cell, out var bucket))
                {
                    bucket = _unusedBuckets.Count > 0 ? _unusedBuckets.Pop() : new List<int>();
                    _buckets.Add(cell, bucket);
                }
                bucket.Add(contactIndex);
            }
        }
    }
}