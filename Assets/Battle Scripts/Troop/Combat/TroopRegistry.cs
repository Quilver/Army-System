using System.Collections.Generic;
using UnityEngine;

// Keeps a periodically refreshed, deterministically ordered list of every troop in the scene.
public sealed class TroopRegistry
{
    const float RefreshInterval = 0.5f;
    readonly List<Troop> _troops = new();
    float _nextRefresh;

    public IReadOnlyList<Troop> Troops => _troops;

    public bool Contains(Troop troop) => _troops.Contains(troop);

    public void Refresh()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + RefreshInterval;
        _troops.Clear();
        _troops.AddRange(Object.FindObjectsByType<Troop>(FindObjectsSortMode.None));
        _troops.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
    }

    public void RemoveDestroyed() => _troops.RemoveAll(troop => troop == null);
}