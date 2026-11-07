using UnityEngine;

// The shape each troop's soldiers flow toward (spec §2.1, §3.1). Soldiers do not have slots: each one is pulled toward the
// nearest point of the troop's shape (a capsule in troop-local space) and spreads out from crowded neighbours (see
// SoldierSteering). The shape follows the troop's FormationMode:
//   Line / Chain  the battle line. Linked ends are pulled in by the joint clearance so soldiers stay off the joint.
//                 Soldiers are also spread evenly along the line so it fills its length (SoldierSteering line fill).
//   Column        a column trailing the pivot while marching: up to LineGeometry.MaxColumnLanes lanes, as many rows as
//                 the soldiers need, always longer than wide (LineGeometry column helpers, SU-120). Soldiers are spread
//                 along it row by row (SoldierSteering column fill).
//   Idle          a disc of BlobRadiusScale x TroopRadius around the pivot (zero-length core).
//   Rout          a wide disc: the soldiers scatter as the troop flees.
// Chargers seek their enemy and followers keep near their charger instead of following the field.
public readonly struct FieldShape
{
    public readonly Vector2 Start;     // troop-local core start
    public readonly Vector2 End;       // troop-local core end
    public readonly float HalfWidth;   // soldiers within this distance of the core feel no pull toward it
    // Ranks of a battle line (Line / Chain), used to spread the soldiers along it (SoldierSteering line fill).
    // Zero for every other shape.
    public readonly int LineRanks;
    // Lanes of a marching column, used to spread the soldiers along it row by row (SoldierSteering column fill, SU-120).
    // Zero for every other shape.
    public readonly int ColumnLanes;

    public bool IsLine => LineRanks > 0;
    public bool IsColumn => ColumnLanes > 0;

    public FieldShape(Vector2 start, Vector2 end, float halfWidth, int lineRanks = 0, int columnLanes = 0)
    {
        Start = start;
        End = end;
        HalfWidth = halfWidth;
        LineRanks = lineRanks;
        ColumnLanes = columnLanes;
    }
}

public sealed class SoldierFieldPlanner
{
    // The column's core starts this fraction of the troop radius behind the pivot (LineGeometry.ColumnTrailFraction).
    // Its length and width come from the soldier count and frontage (LineGeometry column helpers, SU-120), shared with
    // the troop outline (TroopOutline), which draws the same column.
    public const float ColumnTrailFraction = LineGeometry.ColumnTrailFraction;
    public const float MovingSpeedSquared = 0.0025f;
    // A follower keeps within this fraction of SupportRadius of its charger.
    const float FollowerSpacingFraction = 0.6f;

    readonly Troop _troop;

    public SoldierFieldPlanner(Troop troop)
    {
        _troop = troop;
    }

    // Computed once per frame for the whole troop.
    public FieldShape Shape(int livingCount)
    {
        switch (_troop.FormationMode)
        {
            case FormationMode.Rout:
                return new FieldShape(Vector2.zero, Vector2.zero, _troop.Radius * TroopTuning.Active.RoutScatterRadiusScale);
            case FormationMode.Line:
            case FormationMode.Chain:
                if (_troop.Formation != null) return LineShape(livingCount);
                break;
            case FormationMode.Column:
                if (_troop.PivotVelocity.sqrMagnitude > MovingSpeedSquared) return ColumnShape(livingCount);
                break;
        }
        return BlobShape();
    }

    // A charger seeks its enemy; a follower keeps close to its charger. Returns false when the soldier follows the field.
    public bool TrySeek(SoldierAgentState soldier, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        // Charges carry on through a retreat (SU-43); only a rout ends them.
        if (_troop.IsRouted) return false;

        if (soldier.IsCharging && soldier.ChargeTargetTroop != null)
        {
            Vector2 targetWorld = soldier.ChargeTargetTroop.WorldPosition(soldier.ChargeTarget)
                + soldier.ChargeTargetTroop.WorldVelocity(soldier.ChargeTarget) * 0.12f;
            localPoint = _troop.transform.InverseTransformPoint(targetWorld);
            return true;
        }

        if (soldier.IsFollowing)
        {
            SoldierAgentState leader = soldier.ChargeLeader;
            float keep = TroopTuning.Active.SupportRadius * FollowerSpacingFraction;
            Vector2 offset = Vector2.ClampMagnitude(soldier.Position - leader.Position, keep);
            localPoint = leader.Position + offset;
            return true;
        }
        return false;
    }

    FieldShape LineShape(int livingCount)
    {
        TroopTuning tuning = TroopTuning.Active;
        TroopFormationData formation = _troop.Formation;
        Transform transform = _troop.transform;
        Vector2 start = transform.InverseTransformPoint(formation.Start);
        Vector2 end = transform.InverseTransformPoint(formation.End);
        Vector2 delta = end - start;
        float length = delta.magnitude;
        Vector2 tangent = length > 0.0001f ? delta / length : Vector2.up;
        float clearance = LineGeometry.JointClearance(length, tuning.JointClearance, tuning.JointClearanceFraction);
        float startGap = _troop.IsLinkActive(0) ? clearance : 0f;
        float endGap = _troop.IsLinkActive(1) ? clearance : 0f;
        return new FieldShape(start + tangent * startGap, end - tangent * endGap, LineGeometry.RankHalfWidth(length, livingCount),
            LineGeometry.Ranks(livingCount, length));
    }

    FieldShape ColumnShape(int livingCount)
    {
        Vector2 heading = _troop.transform.InverseTransformVector(_troop.PivotVelocity);
        return Column(heading, _troop.Radius, _troop.Frontage, livingCount);
    }

    // The marching column in troop-local space (spec §3.2, SU-120): a core trailing the pivot along -heading, as long as
    // the column's rows and as wide as its lanes (LineGeometry column helpers), so it is longer than it is wide for every
    // troop size. It may reach past the troop radius; SoldierMotionConstraints widens the clamp to it.
    public static FieldShape Column(Vector2 heading, float troopRadius, int frontage, int livingCount)
    {
        Vector2 forward = heading.sqrMagnitude > 1e-8f ? heading.normalized : Vector2.right;
        float trail = LineGeometry.ColumnTrail(troopRadius);
        float length = LineGeometry.ColumnLength(frontage, livingCount);
        return new FieldShape(-forward * trail, -forward * (trail + length), LineGeometry.ColumnHalfWidth(frontage, livingCount),
            columnLanes: LineGeometry.ColumnLanes(frontage, livingCount));
    }

    FieldShape BlobShape()
    {
        return new FieldShape(Vector2.zero, Vector2.zero, _troop.Radius * TroopTuning.Active.BlobRadiusScale);
    }
}
