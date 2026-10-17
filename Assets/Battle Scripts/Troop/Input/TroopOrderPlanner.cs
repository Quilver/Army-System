using UnityEngine;

// Turns a right-click or right-drag into orders for the selected troops: pursue, move, or form a battle line.
public sealed class TroopOrderPlanner
{
    const float MinSpacingRadii = FriendlyOverlapResolver.MinimumFriendlySpacingRadii;
    const float MaxSpacingRadii = 1.5f;
    readonly TroopSelection _selection;

    public TroopOrderPlanner(TroopSelection selection)
    {
        _selection = selection;
    }

    // Start and end are world positions. Drags shorter than clickOrderThreshold are treated as clicks.
    public void FinishOrder(Vector2 start, Vector2 end, float clickOrderThreshold, float pickRadius)
    {
        var selected = _selection.Selected;
        if (selected.Count == 0) return;
        Vector2 drag = end - start;
        if (drag.magnitude < clickOrderThreshold)
        {
            Troop enemy = EnemyAtPoint(end, pickRadius);
            if (enemy == null)
            {
                OrderAtPoint(end);
                return;
            }

            foreach (Troop troop in selected)
                if (troop != null) troop.Pursue(enemy);
            return;
        }

        Vector2 tangent = drag.normalized;
        selected.Sort((a, b) =>
        {
            float aPosition = Vector2.Dot((Vector2)a.transform.position - start, tangent);
            float bPosition = Vector2.Dot((Vector2)b.transform.position - start, tangent);
            int alongLine = aPosition.CompareTo(bPosition);
            return alongLine != 0 ? alongLine : a.transform.position.y.CompareTo(b.transform.position.y);
        });

        ComputeLineLayout(start, end, out Vector2 lineEnd, out float spacing, out _);
        for (int i = 0; i < selected.Count; i++)
        {
            Troop troop = selected[i];
            Vector2 center = LinePoint(start, lineEnd, i, selected.Count);
            Vector2 enemy = NearestEnemyPosition(troop, center);
            float width = selected.Count > 1 ? spacing * 0.9f : drag.magnitude * 0.9f;
            troop.OrderLine(center, tangent, width, enemy);
        }
    }

    // One troop sits at the centre of the line. Two or more are spread evenly with troops on both ends,
    // and the spacing is limited to between MinSpacingRadii and MaxSpacingRadii of the troop radius.
    public void ComputeLineLayout(Vector2 start, Vector2 end, out Vector2 lineEnd, out float spacing, out bool clamped)
    {
        lineEnd = end;
        spacing = 0f;
        clamped = false;
        int count = _selection.Selected.Count;
        Vector2 drag = end - start;
        float length = drag.magnitude;
        if (count < 2 || length < 0.0001f)
        {
            spacing = length;
            return;
        }

        float radius = AverageSelectedRadius();
        float minSpacing = radius * MinSpacingRadii;
        float maxSpacing = radius * MaxSpacingRadii;
        float rawSpacing = length / (count - 1);
        spacing = Mathf.Clamp(rawSpacing, minSpacing, maxSpacing);
        clamped = !Mathf.Approximately(spacing, rawSpacing);
        lineEnd = start + drag / length * (spacing * (count - 1));
    }

    public static Vector2 LinePoint(Vector2 start, Vector2 end, int index, int count)
    {
        return count <= 1 ? (start + end) * 0.5f : Vector2.Lerp(start, end, (float)index / (count - 1));
    }

    float AverageSelectedRadius()
    {
        float total = 0f;
        int count = 0;
        foreach (var troop in _selection.Selected)
        {
            if (troop == null) continue;
            total += troop.Radius;
            count++;
        }
        return count > 0 ? total / count : 1f;
    }

    void OrderAtPoint(Vector2 destination)
    {
        var selected = _selection.Selected;
        Vector2 oldCenter = Vector2.zero;
        foreach (var troop in selected) oldCenter += (Vector2)troop.transform.position;
        oldCenter /= selected.Count;

        foreach (var troop in selected)
        {
            Vector2 offset = (Vector2)troop.transform.position - oldCenter;
            Vector2 center = destination + offset;
            troop.OrderLine(center, troop.LineAxis, troop.LineWidth, NearestEnemyPosition(troop, center));
        }
    }

    Vector2 NearestEnemyPosition(Troop subject, Vector2 from)
    {
        Vector2 nearest = from;
        float nearestDistance = float.PositiveInfinity;
        foreach (var troop in _selection.Troops)
        {
            if (troop == null || troop == subject || troop.TeamId == subject.TeamId) continue;
            float distance = Vector2.SqrMagnitude((Vector2)troop.transform.position - from);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = troop.transform.position;
        }
        return nearest;
    }

    Troop EnemyAtPoint(Vector2 worldPosition, float minimumPickRadius)
    {
        Troop closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Troop troop in _selection.Troops)
        {
            if (troop == null || troop.TeamId == _selection.PlayerTeamId) continue;
            float distance = Mathf.Min(
                troop.DistanceToNearestSoldier(worldPosition),
                Vector2.Distance(troop.transform.position, worldPosition));
            float pickRadius = Mathf.Max(minimumPickRadius, troop.Radius * 0.12f);
            if (distance >= pickRadius || distance >= closestDistance) continue;
            closest = troop;
            closestDistance = distance;
        }
        return closest;
    }
}