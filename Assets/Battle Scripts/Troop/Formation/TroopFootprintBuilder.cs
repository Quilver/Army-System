using System.Collections.Generic;
using UnityEngine;

// Builds each troop's footprint from its current shape and soldiers (spec §3.3, Q-32):
//   Line / Chain  rectangle around the line; half-width = furthest soldier from the centre line + SoldierRadius.
//   Column        capsule along the march, as wide as the furthest soldier from the march axis.
//   Idle / Rout   circle around the pivot, out to the furthest soldier.
public static class TroopFootprintBuilder
{
    const float SoldierRadius = TroopConstants.SoldierRadius;

    public static TroopFootprint Build(Troop troop)
    {
        Vector2 pivot = troop.transform.position;
        TroopFormationData formation = troop.Formation;
        if (formation != null && !troop.IsRouted)
            return TroopFootprint.Rect(formation.Start, formation.End, formation.Facing, LineHalfWidth(troop, formation));

        Vector2 march = troop.PivotVelocity;
        if (!troop.IsRouted && troop.IsMoving && march.sqrMagnitude > 0.0025f)
        {
            Vector2 heading = march.normalized;
            MeasureAlong(troop, pivot, heading, out float back, out float across);
            return TroopFootprint.Capsule(pivot, pivot - heading * back, across + SoldierRadius, heading);
        }

        return TroopFootprint.Circle(pivot, troop.Roster.FurthestLivingDistance() + SoldierRadius, troop.Facing);
    }

    // Furthest living soldier on either side of the centre line, among soldiers alongside it, plus the soldier radius.
    public static float LineHalfWidth(Troop troop, TroopFormationData formation)
    {
        Vector2 tangent = formation.Tangent;
        Vector2 normal = new(-tangent.y, tangent.x);
        float length = Vector2.Distance(formation.Start, formation.End);
        float furthest = 0f;
        IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierAgentState soldier = soldiers[i];
            if (!soldier.IsAlive || soldier.IsCommitted) continue;
            Vector2 offset = troop.WorldPosition(soldier) - formation.Start;
            float along = Vector2.Dot(offset, tangent);
            if (along < -SoldierRadius || along > length + SoldierRadius) continue;
            furthest = Mathf.Max(furthest, Mathf.Abs(Vector2.Dot(offset, normal)));
        }
        return furthest + SoldierRadius;
    }

    // How far the column trails behind the pivot and how wide it is across the heading.
    static void MeasureAlong(Troop troop, Vector2 pivot, Vector2 heading, out float back, out float across)
    {
        back = 0f;
        across = 0f;
        Vector2 normal = new(-heading.y, heading.x);
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive || soldier.IsCommitted) continue;
            Vector2 offset = troop.WorldPosition(soldier) - pivot;
            back = Mathf.Max(back, -Vector2.Dot(offset, heading));
            across = Mathf.Max(across, Mathf.Abs(Vector2.Dot(offset, normal)));
        }
    }
}
