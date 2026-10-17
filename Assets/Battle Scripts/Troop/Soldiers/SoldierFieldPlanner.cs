using UnityEngine;

// The shape each troop's soldiers flow toward. Soldiers do not have slots: each one is pulled toward the nearest point
// of the troop's shape (a capsule in troop-local space) and spreads out from crowded neighbours (see SoldierSteering).
//   Battle line  the line's endpoints. Linked ends are pulled in by a clearance so soldiers stay clear of the joint.
//   Column       a trailing lane behind the pivot while the troop is moving, wide enough for the troop's soldiers.
//   Ordered line the local Y axis of an idle troop after a line order.
//   Blob         a disc around the pivot (zero-length core).
// Charging and lunging soldiers are not on the field; they seek their enemy instead.
public readonly struct FieldShape
{
    public readonly Vector2 Start;     // troop-local core start
    public readonly Vector2 End;       // troop-local core end
    public readonly float HalfWidth;   // soldiers within this distance of the core feel no pull toward it

    public FieldShape(Vector2 start, Vector2 end, float halfWidth)
    {
        Start = start;
        End = end;
        HalfWidth = halfWidth;
    }
}

public sealed class SoldierFieldPlanner
{
    // Lateral half-thickness of a single rank. Soldiers stay in the rank but can spread along it.
    const float RankThickness = TroopConstants.SoldierRadius * 0.5f;
    // Pulls a linked line end's soldiers back from the joint, as far as the line allows.
    const float LinkedEndClearance = 0.45f;
    const float LinkedEndClearanceFraction = 0.15f;
    // Column lane: starts just behind the pivot and runs back for this fraction of the troop radius.
    const float ColumnTrailFraction = 0.05f;
    const float ColumnLengthFraction = 0.8f;
    // Distance between ranks or lanes. Soldiers in a single rank can be this close, so this is the minimum spacing.
    const float RankSpacing = TroopConstants.SeparationRadiusScale * TroopConstants.SoldierRadius;
    const float BlobFraction = 0.85f;
    const float MovingSpeedSquared = 0.0025f;

    readonly Troop _troop;

    public SoldierFieldPlanner(Troop troop)
    {
        _troop = troop;
    }

    // Retreat axis in troop-local space. Used to shift the field back for soldiers that are retreating.
    public Vector2 LocalFaultAxis => _troop.transform.InverseTransformVector(_troop.FaultSlipAxis);

    // Computed once per frame for the whole troop.
    public FieldShape Shape(int livingCount)
    {
        if (HasBattleLine) return LineShape(livingCount);
        if (_troop.PivotVelocity.sqrMagnitude > MovingSpeedSquared) return ColumnShape(livingCount);
        if (_troop.Formation != null) return LineShape(livingCount);
        if (_troop.LineOrder.HasIdleLine) return IdleLineShape(livingCount);
        return BlobShape();
    }

    // A charging or lunging soldier seeks its enemy. Returns false when the soldier follows the field instead.
    public bool TrySeek(SoldierAgentState soldier, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (_troop.IsFaultSlipping) return false;

        if (soldier.ChargeTarget != null && soldier.ChargeTargetTroop != null)
        {
            Vector2 targetWorld = soldier.ChargeTargetTroop.WorldPosition(soldier.ChargeTarget)
                + soldier.ChargeTargetTroop.WorldVelocity(soldier.ChargeTarget) * 0.12f;
            localPoint = _troop.transform.InverseTransformPoint(targetWorld);
            return true;
        }

        if (soldier.Melee.IsLunging)
        {
            // An attacking soldier aims at its opponent's current position.
            Vector2 lungeWorld = soldier.Melee.LungeTroop.WorldPosition(soldier.Melee.LungeTarget);
            localPoint = _troop.transform.InverseTransformPoint(lungeWorld);
            return true;
        }
        return false;
    }

    // A battle line is only formed near an enemy (a threat-facing line, or a line linked to one). A troop that is merely
    // travelling, including one following a line order, marches in a column instead.
    bool HasBattleLine
    {
        get
        {
            TroopFormationData formation = _troop.Formation;
            return formation != null && (formation.IsThreatFacing || formation.StartLinked || formation.EndLinked);
        }
    }

    FieldShape LineShape(int livingCount)
    {
        TroopFormationData formation = _troop.Formation;
        Transform transform = _troop.transform;
        Vector2 start = transform.InverseTransformPoint(formation.Start);
        Vector2 end = transform.InverseTransformPoint(formation.End);
        Vector2 delta = end - start;
        float length = delta.magnitude;
        Vector2 tangent = length > 0.0001f ? delta / length : Vector2.up;
        float startGap = _troop.GetLineLink(0) != null ? Mathf.Min(LinkedEndClearance, length * LinkedEndClearanceFraction) : 0f;
        float endGap = _troop.GetLineLink(1) != null ? Mathf.Min(LinkedEndClearance, length * LinkedEndClearanceFraction) : 0f;
        return new FieldShape(start + tangent * startGap, end - tangent * endGap, RankHalfWidth(length, livingCount));
    }

    FieldShape IdleLineShape(int livingCount)
    {
        float length = _troop.LineWidth;
        float halfWidth = length * 0.5f;
        return new FieldShape(new Vector2(0f, -halfWidth), new Vector2(0f, halfWidth), RankHalfWidth(length, livingCount));
    }

    // Ranks are added when the soldiers do not fit in one rank at minimum spacing along the length.
    static float RankHalfWidth(float length, int livingCount)
    {
        int perRank = Mathf.Max(1, Mathf.FloorToInt(length / RankSpacing));
        int ranks = Mathf.Max(1, Mathf.CeilToInt((float)livingCount / perRank));
        return (ranks - 1) * RankSpacing * 0.5f + RankThickness;
    }

    // Lanes are added as the troop grows, so the column stays within the troop radius.
    FieldShape ColumnShape(int livingCount)
    {
        float radius = _troop.Radius;
        Vector2 heading = _troop.transform.InverseTransformVector(_troop.PivotVelocity).normalized;
        float trail = radius * ColumnTrailFraction;
        float length = radius * ColumnLengthFraction;
        return new FieldShape(-heading * trail, -heading * (trail + length), RankHalfWidth(length, livingCount));
    }

    FieldShape BlobShape()
    {
        return new FieldShape(Vector2.zero, Vector2.zero, _troop.Radius * BlobFraction);
    }
}
