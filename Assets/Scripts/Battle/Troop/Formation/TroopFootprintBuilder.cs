using System.Collections.Generic;
using UnityEngine;

// Builds each troop's footprint from its current shape and soldiers (spec §3.3, Q-32):
//   Line / Chain  rectangle from line point to line point; ahead of and behind the line separately, it reaches the
//                 furthest soldier on that side + SoldierRadius, and at least the line's ranks (LineFootprint, SU-123).
//   Column        capsule along the march, as wide as the furthest soldier from the march axis.
//   Idle / Rout   circle around the pivot, out to the furthest soldier.
public static class TroopFootprintBuilder
{
    const float SoldierRadius = TroopConstants.SoldierRadius;
    // A soldier stands up to RankThickness inside its rank, so the rank field (LineGeometry.RankHalfWidth) less twice
    // that is never further out than the soldiers of a formed-up line: the floor below leaves such a line's footprint as
    // its soldiers make it, and only holds the ranks when the soldiers have not reached them.
    const float RankFloorSlack = 2f * LineGeometry.RankThickness;

    public static TroopFootprint Build(Troop troop)
    {
        Vector2 pivot = troop.transform.position;
        TroopFormationData formation = troop.Formation;
        if (formation != null && !troop.IsRouted)
            return LineFootprint(troop, formation);

        Vector2 march = troop.PivotVelocity;
        if (!troop.IsRouted && troop.IsMoving && march.sqrMagnitude > 0.0025f)
        {
            Vector2 heading = march.normalized;
            MeasureAlong(troop, pivot, heading, out float back, out float across);
            return TroopFootprint.Capsule(pivot, pivot - heading * back, across + SoldierRadius, heading);
        }

        return TroopFootprint.Circle(pivot, troop.Roster.FurthestLivingDistance() + SoldierRadius, troop.Facing);
    }

    // Line / Chain footprint (spec §3.3, SU-123). Each side of the line is measured on its own: the furthest living
    // soldier ahead of the line (behind it), among soldiers alongside it and not charging or returning, plus
    // SoldierRadius; at least the rank field less RankFloorSlack, so the box always holds the line itself and the ranks
    // the soldiers are steered into. It used to be one half-width mirrored to both sides, so when the line ran ahead of
    // its soldiers (a retreat's pivot slip, a pivot pressed into an enemy) the box reached as far again over empty ground
    // on the other side of the line, and gave false gaps, faces and surrounds.
    public static TroopFootprint LineFootprint(Troop troop, TroopFormationData formation)
    {
        Vector2 facing = TroopFootprint.RectFacing(formation.Start, formation.End, formation.Facing);
        MeasureAcross(troop, formation, facing, out float front, out float rear);
        float length = Vector2.Distance(formation.Start, formation.End);
        float rankFloor = Mathf.Max(0f, LineGeometry.RankHalfWidth(length, troop.LivingSoldierCount) - RankFloorSlack);
        return TroopFootprint.RectAcross(formation.Start, formation.End, facing,
            Mathf.Max(front, rankFloor) + SoldierRadius, Mathf.Max(rear, rankFloor) + SoldierRadius);
    }

    // How far the soldiers alongside the line stand ahead of it (front) and behind it (rear), along facing; 0 for a side
    // with no soldier on it.
    static void MeasureAcross(Troop troop, TroopFormationData formation, Vector2 facing, out float front, out float rear)
    {
        Vector2 tangent = formation.Tangent;
        float length = Vector2.Distance(formation.Start, formation.End);
        front = 0f;
        rear = 0f;
        IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierAgentState soldier = soldiers[i];
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning) continue;
            Vector2 offset = troop.WorldPosition(soldier) - formation.Start;
            float along = Vector2.Dot(offset, tangent);
            if (along < -SoldierRadius || along > length + SoldierRadius) continue;
            float across = Vector2.Dot(offset, facing);
            front = Mathf.Max(front, across);
            rear = Mathf.Max(rear, -across);
        }
    }

    // The old symmetric half-width (spec §3.3 as written): furthest living soldier on either side of the centre line,
    // among soldiers alongside it, plus the soldier radius. No longer used by Build (see LineFootprint).
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
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning) continue;
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
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning) continue;
            Vector2 offset = troop.WorldPosition(soldier) - pivot;
            back = Mathf.Max(back, -Vector2.Dot(offset, heading));
            across = Mathf.Max(across, Mathf.Abs(Vector2.Dot(offset, normal)));
        }
    }
}
