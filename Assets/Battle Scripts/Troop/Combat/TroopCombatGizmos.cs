using System.Collections.Generic;
using UnityEngine;

// Scene-view gizmos for the combat world: engagement ranges, selected soldiers' attack state and battle lines.
public static class TroopCombatGizmos
{
    // Battle line key:
    //   line        army colour = threat-facing, dim army colour = ordered/manual line
    //   facing tick short line from the line's midpoint towards the side the line faces
    //   endpoints   red = free end, green = linked end (white ring = the joint shared with the linked troop's end)
    //   link bar    drawn between the two troops' own un-snapped ends; cyan = relaxed, red = stretched
    //   pivot       small sphere at the troop centre
    //   soldiers    small sphere per soldier of a selected troop: white = has a banked attack, black = none
    static readonly Color FreeEndColor = new(1f, 0.25f, 0.25f, 1f);
    static readonly Color LinkedEndColor = new(0.25f, 1f, 0.4f, 1f);
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
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.65f);
            DrawCircle(pivot, troop.Radius + sDistance, CircleSegments);

            DrawBattleLine(troop);

            if (!troop.IsSelected) continue;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                Gizmos.color = soldier.HasMeleeAttack ? Color.white : Color.black;
                Gizmos.DrawSphere(troop.WorldPosition(soldier), SoldierStateSphereSize);
            }
        }
        Gizmos.color = oldColor;
    }

    static void DrawBattleLine(Troop troop)
    {
        TroopFormationData formation = troop.Formation;
        Vector2 pivot = troop.transform.position;
        Color army = troop.ArmyColor;

        Gizmos.color = new Color(army.r, army.g, army.b, 0.6f);
        Gizmos.DrawSphere(pivot, EndpointSphereSize * 0.7f);
        if (formation == null) return;

        Color lineColor = formation.IsThreatFacing ? army
            : new Color(army.r, army.g, army.b, 0.45f);
        Gizmos.color = lineColor;
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

        Gizmos.color = linked ? LinkedEndColor : FreeEndColor;
        Gizmos.DrawSphere(endpoint, EndpointSphereSize);
        if (!linked) return;

        // Only draw the shared joint and link bar once per pair.
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(endpoint, EndpointSphereSize * 1.6f);
        if (troop.GetInstanceID() >= link.Other.GetInstanceID()) return;

        Vector2 halfTension = link.Tension * 0.5f;
        float stretch = Mathf.Clamp01(link.Tension.magnitude / Mathf.Max(0.0001f, troop.Radius * 0.25f));
        Gizmos.color = Color.Lerp(Color.cyan, Color.red, stretch);
        Gizmos.DrawLine(endpoint - halfTension, endpoint + halfTension);
        Gizmos.DrawWireSphere(endpoint - halfTension, EndpointSphereSize * 0.6f);
        Gizmos.DrawWireSphere(endpoint + halfTension, EndpointSphereSize * 0.6f);
    }

    static void DrawCircle(Vector2 center, float radius, int segments)
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