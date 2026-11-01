using System.Collections.Generic;
using UnityEngine;

// Scene-view gizmos for the combat world: sensing range, footprints with their faces, battle lines, chain joints and the
// charge state of selected troops' soldiers.
public static class TroopCombatGizmos
{
    // Key:
    //   ring        S_Distance around the footprint (approximate, drawn around the pivot)
    //   footprint   army colour outline; the front face is drawn brighter
    //   line        army colour = threat-facing, dim = ordered line; tick shows the facing
    //   line points red = free, green = linked (white ring = the shared joint), yellow = pending link from an order
    //   soldiers    of a selected troop: red = charging, orange = following a charger, grey = fatigued
    static readonly Color FreeEndColor = new(1f, 0.25f, 0.25f, 1f);
    static readonly Color LinkedEndColor = new(0.25f, 1f, 0.4f, 1f);
    static readonly Color PendingEndColor = new(1f, 0.9f, 0.2f, 1f);
    static readonly Color FollowerColor = new(1f, 0.6f, 0.1f, 1f);
    const float EndpointSphereSize = 0.1f;
    const float SoldierStateSphereSize = 0.12f;
    const int CircleSegments = 32;

    public static void Draw(IReadOnlyList<Troop> troops, float sDistance)
    {
        if (troops == null) return;
        Color oldColor = Gizmos.color;
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            Vector2 pivot = troop.transform.position;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            DrawCircle(pivot, troop.Radius + sDistance, CircleSegments);

            if (Application.isPlaying) DrawFootprint(troop.Footprint, troop.ArmyColor);
            DrawBattleLine(troop);

            if (!troop.IsSelected) continue;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                if (soldier.IsCharging) Gizmos.color = Color.red;
                else if (soldier.IsFollowing) Gizmos.color = FollowerColor;
                else if (soldier.LifeState == SoldierLifeState.Fatigued) Gizmos.color = Color.grey;
                else continue;
                Gizmos.DrawSphere(troop.WorldPosition(soldier), SoldierStateSphereSize);
            }
        }
        Gizmos.color = oldColor;
    }

    public static void DrawFootprint(in TroopFootprint footprint, Color army)
    {
        Gizmos.color = new Color(army.r, army.g, army.b, 0.5f);
        if (footprint.Kind == FootprintKind.Circle)
        {
            DrawCircle(footprint.Center, footprint.Rounding, CircleSegments);
            return;
        }
        for (int i = 0; i < 4; i++)
        {
            Gizmos.color = i == 0 ? army : new Color(army.r, army.g, army.b, 0.5f);
            Gizmos.DrawLine(footprint.OuterCorner(i), footprint.OuterCorner(i + 1));
        }
    }

    static void DrawBattleLine(Troop troop)
    {
        TroopFormationData formation = troop.Formation;
        Vector2 pivot = troop.transform.position;
        Color army = troop.ArmyColor;

        Gizmos.color = new Color(army.r, army.g, army.b, 0.6f);
        Gizmos.DrawSphere(pivot, EndpointSphereSize * 0.7f);
        if (formation == null) return;

        Gizmos.color = formation.IsThreatFacing ? army : new Color(army.r, army.g, army.b, 0.45f);
        Gizmos.DrawLine(formation.Start, formation.End);

        Vector2 midpoint = (formation.Start + formation.End) * 0.5f;
        Gizmos.DrawLine(midpoint, midpoint + formation.Facing.normalized * 0.35f);
        Gizmos.DrawLine(pivot, midpoint);

        for (int end = 0; end < 2; end++)
            DrawEndpoint(troop, end);
    }

    static void DrawEndpoint(Troop troop, int end)
    {
        Vector2 endpoint = troop.SegmentEndpoint(end);
        TroopEndpointLink link = troop.GetLineLink(end);
        bool linked = link != null && link.Other != null;

        Gizmos.color = !linked ? FreeEndColor : link.Pending ? PendingEndColor : LinkedEndColor;
        Gizmos.DrawSphere(endpoint, EndpointSphereSize);
        if (!linked || link.Pending) return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(link.Joint, EndpointSphereSize * 1.6f);
        if (troop.GetInstanceID() >= link.Other.GetInstanceID()) return;

        // Natural line points of the pair (the gap the link tension closes); cyan = relaxed, red = stretched.
        Vector2 halfTension = link.Tension * 0.5f;
        float stretch = Mathf.Clamp01(link.Tension.magnitude / Mathf.Max(0.0001f, troop.NaturalLineLength * 0.25f));
        Gizmos.color = Color.Lerp(Color.cyan, Color.red, stretch);
        Gizmos.DrawLine(link.Joint - halfTension, link.Joint + halfTension);
    }

    public static void DrawCircle(Vector2 center, float radius, int segments)
    {
        Vector3 previous = center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
