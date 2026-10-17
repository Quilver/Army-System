using System.Collections.Generic;
using UnityEngine;

// The troop's soldiers: spawns them from a composition and answers questions about them (living count, positions,
// averages). Disable to stop the troop spawning and steering soldiers; existing soldiers stay visible.
[DisallowMultipleComponent]
public sealed class TroopRoster : MonoBehaviour
{
    [SerializeField, HideInInspector] List<TroopProfileCount> _composition = new();

    readonly List<SoldierAgentState> _soldiers = new();
    Troop _troop;
    int _nextSoldierId;

    public IReadOnlyList<SoldierAgentState> Soldiers => _soldiers;
    // Same list without interface boxing, for per-frame loops inside the troop.
    internal List<SoldierAgentState> SoldierList => _soldiers;
    public IReadOnlyList<TroopProfileCount> Composition => _composition;
    public int Count => _soldiers.Count;

    public int LivingCount
    {
        get
        {
            int count = 0;
            foreach (var soldier in _soldiers)
                if (soldier.IsAlive) count++;
            return count;
        }
    }

    internal void Bind(Troop troop) => _troop = troop;

    internal void SetComposition(IEnumerable<TroopProfileCount> composition)
    {
        _composition = new List<TroopProfileCount>(composition);
    }

    internal void Clear() => _soldiers.Clear();

    public bool Contains(SoldierAgentState soldier) => _soldiers.Contains(soldier);

    // Replaces the soldiers with one per unit in the composition, scattered in a gaussian blob around the pivot.
    internal void Spawn(IReadOnlyList<TroopProfileCount> composition)
    {
        _soldiers.Clear();
        foreach (var entry in composition)
        {
            if (entry == null || entry.Profile == null) continue;
            for (int i = 0; i < Mathf.Max(0, entry.Count); i++)
                _soldiers.Add(new SoldierAgentState(_nextSoldierId++, entry.Profile, RandomBlobOffset()) { Facing = transform.right });
        }
    }

    public float AverageProfileValue(System.Func<SoldierProfile, float> valueSelector)
    {
        float total = 0f;
        int count = 0;
        foreach (var soldier in _soldiers)
        {
            if (!soldier.IsAlive || soldier.Profile == null) continue;
            total += valueSelector(soldier.Profile);
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    public float MaxProfileValue(System.Func<SoldierProfile, float> valueSelector)
    {
        float max = 0f;
        foreach (var soldier in _soldiers)
            if (soldier.IsAlive && soldier.Profile != null)
                max = Mathf.Max(max, valueSelector(soldier.Profile));
        return max;
    }

    public float DistanceToNearestSoldier(Vector2 worldPosition)
    {
        float nearestDistance = float.PositiveInfinity;
        foreach (var soldier in _soldiers)
        {
            if (!soldier.IsAlive) continue;
            Vector2 position = transform.TransformPoint(soldier.Position);
            nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(worldPosition, position));
        }
        return nearestDistance;
    }

    public bool HasLivingSoldierInRect(Rect worldRect)
    {
        foreach (var soldier in _soldiers)
        {
            if (!soldier.IsAlive) continue;
            if (worldRect.Contains(transform.TransformPoint(soldier.Position))) return true;
        }
        return false;
    }

    public Vector2 WorldPosition(SoldierAgentState soldier) => transform.TransformPoint(soldier.Position);

    public Vector2 WorldVelocity(SoldierAgentState soldier) =>
        (Vector2)transform.TransformVector(soldier.Velocity) + _troop.PivotVelocity;

    // Applies a world-space position and velocity correction to a soldier.
    public void ResolveContact(SoldierAgentState soldier, Vector2 worldPositionDelta, Vector2 worldVelocityDelta)
    {
        soldier.Position += (Vector2)transform.InverseTransformVector(worldPositionDelta);
        soldier.Velocity += (Vector2)transform.InverseTransformVector(worldVelocityDelta);
    }

    Vector2 RandomBlobOffset()
    {
        float radius = _troop.Radius;
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
        float gaussianRadius = Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(0.0001f, Random.value))) * radius * 0.22f;
        return direction * Mathf.Min(gaussianRadius, radius * 0.85f);
    }
}