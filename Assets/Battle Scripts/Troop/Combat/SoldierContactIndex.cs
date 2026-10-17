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