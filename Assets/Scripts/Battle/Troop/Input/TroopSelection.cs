using System.Collections.Generic;
using UnityEngine;

// Tracks the troops in the scene, which one the cursor is over, and which of the player's troops are selected (spec §6.1).
// Shift toggles: a selected troop is deselected and an unselected troop is added.
public sealed class TroopSelection
{
    const float RefreshInterval = 0.5f;
    readonly List<Troop> _troops = new();
    readonly List<Troop> _selected = new();
    Troop _hovered;
    float _nextRefresh;

    public int PlayerTeamId { get; set; }
    public IReadOnlyList<Troop> Troops => _troops;
    // The troop under the cursor (any team), or null. Shown on the hover troop card.
    public Troop Hovered => _hovered;
    // Orders reorder this list, so it is exposed mutable to the order planner.
    public List<Troop> Selected => _selected;

    public void Refresh()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + RefreshInterval;
        _troops.Clear();
        _troops.AddRange(Object.FindObjectsByType<Troop>(FindObjectsSortMode.None));
    }

    public void RemoveDestroyed()
    {
        for (int i = _selected.Count - 1; i >= 0; i--)
        {
            if (_selected[i] != null && !_selected[i].IsRouted) continue;
            if (_selected[i] != null) _selected[i].SetSelected(false);
            _selected.RemoveAt(i);
        }
    }

    // Hovers the troop of any team under the world position, picked the same way as a click (see Pick), or none.
    // pickRadius is in world units, converted from screen pixels by the caller.
    public void UpdateHover(Vector2? worldPosition, float pickRadius = 0f)
    {
        Troop hovered = worldPosition.HasValue ? Pick(worldPosition.Value, pickRadius, 0, TeamFilter.Any) : null;

        if (hovered == _hovered) return;
        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = hovered;
        if (_hovered != null) _hovered.SetHovered(true);
    }

    public void ClearHover()
    {
        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = null;
    }

    // Selects the player's troop under the cursor: one of its living soldiers within pickRadius, or inside its outline
    // (Troop.Outline, the formation outline the visualiser draws), or its pivot within pickRadius. pickRadius is in
    // world units, converted from screen pixels by the caller. Empty ground clears the selection unless Shift is held.
    public void SelectAtPoint(Vector2 worldPosition, float pickRadius, bool toggle)
    {
        Troop closest = TroopAtPoint(worldPosition, pickRadius, PlayerTeamId, true);
        if (!toggle)
        {
            Clear();
            if (closest != null) Add(closest);
            return;
        }
        if (closest != null) Toggle(closest);
    }

    // Nearest troop of the given team (or of any other team when sameTeam is false) under the point.
    public Troop TroopAtPoint(Vector2 worldPosition, float pickRadius, int teamId, bool sameTeam) =>
        Pick(worldPosition, pickRadius, teamId, sameTeam ? TeamFilter.Same : TeamFilter.Other);

    enum TeamFilter
    {
        Any,
        Same,
        Other
    }

    // The troop under a point. A troop is hit by (in order of preference):
    //   1. one of its living soldiers within pickRadius: the troop with the nearest soldier wins;
    //   2. the point being inside its outline: where outlines overlap, the one the point is deepest inside wins;
    //   3. its pivot within pickRadius: the nearest pivot wins.
    // A soldier you point at therefore beats the outline of a troop standing over it.
    Troop Pick(Vector2 worldPosition, float pickRadius, int teamId, TeamFilter filter)
    {
        Troop best = null;
        int bestTier = int.MaxValue;
        float bestScore = float.PositiveInfinity;
        foreach (var troop in _troops)
        {
            if (troop == null || troop.LivingSoldierCount == 0) continue;
            if (filter == TeamFilter.Same && troop.TeamId != teamId) continue;
            if (filter == TeamFilter.Other && troop.TeamId == teamId) continue;

            int tier;
            float score;
            float soldierDistance = troop.DistanceToNearestSoldier(worldPosition);
            if (soldierDistance <= pickRadius)
            {
                tier = 0;
                score = soldierDistance;
            }
            else if (Application.isPlaying && troop.Outline.Contains(worldPosition))
            {
                tier = 1;
                score = troop.Outline.SignedDistance(worldPosition);
            }
            else
            {
                float pivotDistance = Vector2.Distance(troop.transform.position, worldPosition);
                if (pivotDistance > pickRadius) continue;
                tier = 2;
                score = pivotDistance;
            }

            if (tier > bestTier || (tier == bestTier && score >= bestScore)) continue;
            best = troop;
            bestTier = tier;
            bestScore = score;
        }
        return best;
    }

    // Box select (Q-48): a troop is picked when its pivot or any living soldier is inside the box.
    public void SelectBox(Vector2 worldStart, Vector2 worldEnd, bool toggle)
    {
        if (!toggle) Clear();

        Rect box = Rect.MinMaxRect(
            Mathf.Min(worldStart.x, worldEnd.x),
            Mathf.Min(worldStart.y, worldEnd.y),
            Mathf.Max(worldStart.x, worldEnd.x),
            Mathf.Max(worldStart.y, worldEnd.y));

        var picked = new List<Troop>();
        foreach (var troop in _troops)
        {
            if (troop == null || troop.TeamId != PlayerTeamId) continue;
            if (box.Contains(troop.transform.position) || troop.HasLivingSoldierInRect(box)) picked.Add(troop);
        }
        foreach (var troop in picked)
        {
            if (toggle) Toggle(troop);
            else Add(troop);
        }
    }

    void Toggle(Troop troop)
    {
        if (_selected.Contains(troop)) Remove(troop);
        else Add(troop);
    }

    void Add(Troop troop)
    {
        if (_selected.Contains(troop)) return;
        _selected.Add(troop);
        troop.SetSelected(true);
    }

    void Remove(Troop troop)
    {
        _selected.Remove(troop);
        troop.SetSelected(false);
    }

    void Clear()
    {
        foreach (var troop in _selected)
            if (troop != null) troop.SetSelected(false);
        _selected.Clear();
    }
}
