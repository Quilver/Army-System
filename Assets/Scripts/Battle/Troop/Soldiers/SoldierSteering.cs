using System.Collections.Generic;
using UnityEngine;

// Per-frame soldier steering (spec §2). Each soldier follows one of two rules:
//   - Chargers seek their enemy; followers keep near their charger (spec §4.4).
//   - Every other soldier follows the troop's vector field (SoldierFieldPlanner): it is attracted to the nearest point of
//     the troop's shape and spreads away from crowded neighbours, so the shape fills evenly without slots. Separation
//     keeps soldiers apart, and ResolveMinimumSeparation enforces the minimum when they press together.
//   - In a battle line (Line / Chain) the field also spreads soldiers evenly along the line, so it fills its whole
//     length instead of bunching at the middle (see PlanLineFill). A marching column is filled the same way, row by
//     row from the pivot back, so the soldiers form the column instead of a blob (SU-120).
// No soldier is pulled toward another.
//   - Composite troops (some soldiers with ranged weapons, some without) in a battle line (Line / Chain) sort themselves
//     by rank (Decisions D-55): when
//     not engaged, ranged soldiers steer toward the front of the formation and melee soldiers toward the back; when
//     engaged they swap, so the shooters fall back behind the melee soldiers. See CompositeRankPlan.
public sealed class SoldierSteering
{
    const float NeighborRadius = TroopConstants.NeighborRadius;
    const float SteeringDamping = 2.6f;
    const float VelocityMatchGain = 4f;  // velocity matching relative to the position gain (critically damped)
    // Retreat arrival: the position stiffness is this fraction of the velocity gain. Below 0.25 the approach is
    // overdamped, so a retreating soldier slows to its place without overshooting it.
    const float RetreatArrivalStiffness = 0.2f;
    // Retreating soldiers may move faster than their normal top speed to keep up with the scheduled retreat.
    const float RetreatSpeedHeadroom = 1.5f;
    const float RetreatAccelerationHeadroom = 3f;
    const float SeparationGain = 2.5f;   // separation strength relative to TroopTuning.SoldierSeparation
    // Soldiers try to keep their centres this far apart. Neighbours closer than this push each other out, and the
    // minimum is enforced exactly by ResolveMinimumSeparation.
    const float SeparationDistance = TroopConstants.SeparationRadius;
    // Density spreading: soldiers closer than SpreadRadius push each other apart along the field. The push fades to
    // zero at SpreadRadius, so a crowd spreads out and sparse soldiers do not move.
    const float SpreadRadius = TroopConstants.SpreadRadius;
    const float SpreadSpeedPerUnit = 1.2f;
    // Extra pressure mass a fully driving soldier has over a settled one. Mass sets how far each soldier of a pressed
    // pair gives way: the settled soldier moves most.
    const float PressureMassPerDrive = TroopConstants.PressureMassPerDrive;
    const int MinimumSeparationPasses = 3;
    // Line fill: pull toward the soldier's place along the line, relative to the attraction strength.
    const float LineFillGain = 0.8f;
    // Soldiers are pulled back inside this fraction of the troop radius (also sizes the column clamp reach,
    // SoldierMotionConstraints.TickColumnReach).
    internal const float SoftRadiusFraction = 0.92f;
    // Return after a charge (SU-43): the soldier is home within this distance of its formation band. The return gives up
    // after ReturnTimeoutSeconds so a soldier that cannot reach its place does not stay fast and unclamped.
    const float ReturnArrivalSlack = TroopConstants.LineSpacing;
    const float ReturnTimeoutSeconds = 6f;

    readonly Troop _troop;
    readonly SoldierMotionConstraints _constraints;
    readonly SoldierFieldPlanner _planner;
    readonly SoldierSpatialGrid _grid = new();
    readonly List<Vector2> _nextPositions = new();
    readonly List<Vector2> _nextVelocities = new();
    readonly List<Vector2> _seekPoints = new();
    readonly List<bool> _seeks = new();
    readonly List<float> _fillAlong = new();    // distance along the line core, per soldier
    readonly List<float> _fillTargets = new();  // place along the line core, per soldier (NaN: no line fill)
    readonly List<int> _fillOrder = new();
    readonly List<int> _fillOrderShooters = new();  // composite troops: the ranged soldiers, filled separately
    readonly System.Comparison<int> _byAlong;
    ITroopRandom _random;
    CompositeRankPlan _rankPlan;

    public SoldierSteering(Troop troop, SoldierMotionConstraints constraints)
    {
        _troop = troop;
        _constraints = constraints;
        _planner = new SoldierFieldPlanner(troop);
        _byAlong = (a, b) => _fillAlong[a].CompareTo(_fillAlong[b]);
    }

    public void Simulate(float deltaTime)
    {
        _random = _troop.TroopRandom;
        _constraints.TickColumnReach(deltaTime);
        List<SoldierAgentState> soldiers = _troop.Roster.SoldierList;
        _grid.Rebuild(soldiers);
        while (_nextPositions.Count < soldiers.Count)
        {
            _nextPositions.Add(Vector2.zero);
            _nextVelocities.Add(Vector2.zero);
            _seekPoints.Add(Vector2.zero);
            _seeks.Add(false);
            _fillAlong.Add(0f);
            _fillTargets.Add(float.NaN);
        }

        // Pass 1: decide each soldier's rule for this frame.
        FieldShape shape = _planner.Shape(_troop.LivingSoldierCount);
        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            if (soldier.IsAlive) UpdateReturn(soldier, shape, deltaTime);
            Vector2 seekPoint = Vector2.zero;
            bool seek = soldier.IsAlive && soldier.Character != null && _planner.TrySeek(soldier, out seekPoint);
            _seeks[i] = seek;
            _seekPoints[i] = seekPoint;
        }
        _rankPlan = CompositeRankPlan.For(_troop, shape);
        PlanLineFill(soldiers, shape);

        // Pass 2: integrate each living soldier.
        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            _nextPositions[i] = soldier.Position;
            _nextVelocities[i] = soldier.Velocity;
            if (!soldier.IsAlive || soldier.Character == null) continue;
            soldier.TickCooldowns(deltaTime);

            Vector2 velocity = Step(soldier, _seeks[i], _seekPoints[i], shape, _fillTargets[i], deltaTime,
                out Vector2 nextPosition, out float drive);
            soldier.DriveFraction = drive;
            _nextVelocities[i] = velocity;
            _nextPositions[i] = nextPosition;
        }

        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            if (!soldier.IsAlive) continue;
            soldier.Velocity = _nextVelocities[i];
            soldier.Position = _nextPositions[i];
            _constraints.ConstrainToRadius(soldier);
        }

        ResolveMinimumSeparation(soldiers);
    }

    Vector2 Step(SoldierAgentState soldier, bool seek, Vector2 seekPoint, FieldShape shape, float fillTarget, float deltaTime,
        out Vector2 nextPosition, out float drive)
    {
        float strength = soldier.IsCommitted || soldier.IsReturning ? 5.2f
            : _troop.PivotVelocity.sqrMagnitude > 0.0025f ? 3.2f
            : _troop.Formation != null ? 2.5f
            : 1.3f;

        Vector2 spread;
        Vector2 neighborForce = NeighborForces(soldier, out spread);

        // A retreating soldier arrives at its scheduled target (see FaultSlipMotion), so it is not on the field.
        bool retreating = !seek && soldier.IsRetreatTracking;
        Vector2 steering;
        if (seek)
        {
            steering = (seekPoint - soldier.Position) * strength;
            drive = 1f;
        }
        else if (retreating)
        {
            Vector2 desired = RetreatArrivalVelocity(soldier, strength);
            steering = (desired - soldier.Velocity) * (VelocityMatchGain * strength);
            drive = Mathf.Min(1f, desired.magnitude / soldier.Character.MaxSpeed);
        }
        else
        {
            Vector2 desired = FieldVelocity(soldier, shape, fillTarget, strength, spread, out drive);
            steering = (desired - soldier.Velocity) * (VelocityMatchGain * strength);
        }
        steering += neighborForce;
        steering += soldier.ExternalSeparation;

        // Retreating and charging soldiers may leave the troop radius; the radius pull would fight them.
        float radius = _constraints.EffectiveRadius;
        float distanceFromPivot = soldier.Position.magnitude;
        float softRadius = radius * SoftRadiusFraction;
        if (distanceFromPivot > softRadius && !LeavesRadius(soldier))
            steering -= soldier.Position / distanceFromPivot * ((distanceFromPivot - softRadius) * 3.3f);
        float brownianMotion = TroopTuning.Active.SoldierBrownianMotion;
        if (brownianMotion > 0f)
            steering += _random.InsideUnitCircle() * brownianMotion * 2.5f;

        // Chargers and their followers move at ChargeSpeedScale (2x). A retreating soldier gets headroom so it can keep
        // to its schedule.
        float speedScale = soldier.IsCommitted || soldier.IsReturning ? TroopTuning.Active.ChargeSpeedScale : 1f;
        float accelerationLimit = soldier.Character.Acceleration * speedScale;
        float speedLimit = soldier.Character.MaxSpeed * speedScale;
        if (retreating)
        {
            accelerationLimit *= RetreatAccelerationHeadroom;
            speedLimit = Mathf.Max(speedLimit, soldier.FaultTargetVelocity.magnitude * RetreatSpeedHeadroom);
        }
        Vector2 acceleration = Vector2.ClampMagnitude(steering, accelerationLimit);
        Vector2 velocity = soldier.Velocity + acceleration * deltaTime;
        velocity *= Mathf.Exp(-SteeringDamping * deltaTime);
        velocity = Vector2.ClampMagnitude(velocity, speedLimit);
        nextPosition = soldier.Position + velocity * deltaTime;
        _constraints.ConstrainToTerrain(soldier, ref velocity, ref nextPosition);
        return velocity;
    }

    // A retreating soldier follows its scheduled target: the target's velocity plus a position correction. The gain
    // is below the velocity-matching gain / 4, so the correction is overdamped and the soldier arrives without overshoot.
    Vector2 RetreatArrivalVelocity(SoldierAgentState soldier, float strength)
    {
        float stiffness = VelocityMatchGain * strength * RetreatArrivalStiffness;
        Vector2 error = soldier.FaultTargetPosition - soldier.Position;
        return soldier.FaultTargetVelocity + error * stiffness;
    }

    // The field's desired velocity at the soldier. Attraction is proportional to the distance outside the shape's core,
    // so the soldier slows as it closes on the shape. Spreading adds density push, and in a line the line fill pulls the
    // soldier along the core toward its place (fillTarget, NaN for none). The result is clamped to max speed, and its
    // size as a fraction of max speed is the soldier's drive (how hard it is pushing toward its place).
    Vector2 FieldVelocity(SoldierAgentState soldier, FieldShape shape, float fillTarget, float strength, Vector2 spread,
        out float drive)
    {
        Vector2 query = soldier.Position;
        Vector2 core = NearestOnSegment(query, shape.Start, shape.End);
        Vector2 offset = query - core;
        float distance = offset.magnitude;

        Vector2 desired = Vector2.zero;
        if (distance > shape.HalfWidth)
            desired -= offset / distance * ((distance - shape.HalfWidth) * strength);
        if (!soldier.IsCommitted)
            desired += spread * SpreadSpeedPerUnit;
        if (_rankPlan.Active && !soldier.IsCommitted)
            desired += _rankPlan.DesiredVelocity(soldier, offset, shape.HalfWidth, strength);
        if (!float.IsNaN(fillTarget))
        {
            Vector2 segment = shape.End - shape.Start;
            float length = segment.magnitude;
            if (length > 0.0001f)
            {
                Vector2 tangent = segment / length;
                float along = Vector2.Dot(query - shape.Start, tangent);
                desired += tangent * ((fillTarget - along) * strength * LineFillGain);
            }
        }

        // A soldier rushing back after a charge runs at charge speed (SU-43).
        float maxSpeed = soldier.Character.MaxSpeed * (soldier.IsReturning ? TroopTuning.Active.ChargeSpeedScale : 1f);
        desired = Vector2.ClampMagnitude(desired, maxSpeed);
        drive = desired.magnitude / maxSpeed;
        return desired;
    }

    // Line fill (spec §3.3 soldiers per rank). Soldiers still have no slots: each frame the living soldiers are ordered
    // by how far along the line core they stand and grouped into files of LineRanks soldiers, and each file is given an
    // evenly spaced place along the core, so the line fills its whole length. Depth inside a file is left to separation
    // and the half-width band. Chargers, followers and retreating soldiers keep their place in the order (so the line
    // does not shuffle while they are away) but are not pulled. Leaves every target NaN outside Line / Chain.
    // Composite troops (D-55): the ranged and the melee soldiers are filled separately, each group spread evenly over
    // the whole length (with as many ranks as it needs for the line's files), so both kinds are spread along the line
    // and the rank plan only sorts them front to back. Without this a group that ended up at one end stays there.
    // Column fill (SU-120): a marching column is filled the same way with files of ColumnLanes soldiers (its rows), the
    // first row at the core's start (behind the pivot) and the last at its end, since the column's core runs from the
    // first row's soldiers to the last's (LineGeometry.ColumnLength). The rank plan is off in a column, so composite
    // troops fill it as one group.
    void PlanLineFill(List<SoldierAgentState> soldiers, FieldShape shape)
    {
        for (int i = 0; i < soldiers.Count; i++) _fillTargets[i] = float.NaN;
        if (!(shape.IsLine || shape.IsColumn) || _troop.IsRouted) return;

        Vector2 segment = shape.End - shape.Start;
        float length = segment.magnitude;
        if (length < TroopConstants.LineSpacing) return;
        Vector2 tangent = segment / length;

        _fillOrder.Clear();
        _fillOrderShooters.Clear();
        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            if (!soldier.IsAlive || soldier.Character == null) continue;
            _fillAlong[i] = Vector2.Dot(soldier.Position - shape.Start, tangent);
            if (_rankPlan.Active && soldier.Character.RangedWeapon != null) _fillOrderShooters.Add(i);
            else _fillOrder.Add(i);
        }
        int count = _fillOrder.Count + _fillOrderShooters.Count;
        if (count == 0) return;

        if (shape.IsColumn)
        {
            _fillOrder.AddRange(_fillOrderShooters);
            AssignFill(soldiers, _fillOrder, shape.ColumnLanes, length, spanEnds: true);
            return;
        }
        int ranks = Mathf.Max(1, shape.LineRanks);
        if (_fillOrderShooters.Count == 0)
        {
            AssignFill(soldiers, _fillOrder, ranks, length);
            return;
        }
        int lineFiles = (count + ranks - 1) / ranks;
        AssignFill(soldiers, _fillOrder, GroupRanks(_fillOrder.Count, lineFiles), length);
        AssignFill(soldiers, _fillOrderShooters, GroupRanks(_fillOrderShooters.Count, lineFiles), length);
    }

    // Ranks a group needs so that it spans no more files than the line has.
    static int GroupRanks(int groupCount, int lineFiles) => Mathf.Max(1, (groupCount + lineFiles - 1) / Mathf.Max(1, lineFiles));

    // Orders the group along the line and spreads it over the whole length in files of `ranks` soldiers. Files sit in
    // the middle of equal shares of the length, or with spanEnds (column rows) from one end of the core to the other.
    void AssignFill(List<SoldierAgentState> soldiers, List<int> order, int ranks, float length, bool spanEnds = false)
    {
        int count = order.Count;
        if (count == 0) return;
        ranks = Mathf.Max(1, ranks);
        order.Sort(_byAlong);
        int files = (count + ranks - 1) / ranks;
        for (int k = 0; k < count; k++)
        {
            int index = order[k];
            var soldier = soldiers[index];
            if (_seeks[index] || soldier.IsRetreatTracking) continue;
            _fillTargets[index] = FillPlace(k / ranks, files, length, spanEnds);
        }
    }

    // Place along the core of file `file` of `files`: the middle of its share of the length, or with spanEnds evenly
    // from 0 to length (a single file in the middle).
    internal static float FillPlace(int file, int files, float length, bool spanEnds)
    {
        if (!spanEnds) return (file + 0.5f) * (length / Mathf.Max(1, files));
        if (files <= 1) return length * 0.5f;
        return file * length / (files - 1);
    }

    static Vector2 NearestOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.00000001f) return start;
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return start + segment * t;
    }

    // Hard minimum separation between soldiers of this troop. The soft separation force keeps soldiers apart when
    // there is room; this pass guarantees the minimum when they press together. Each contact moves both soldiers,
    // weighted by pressure: a driving soldier is heavier and gives way less, so it pushes a settled neighbour into
    // position. Approach velocity is shared the same way, so the pusher's momentum carries into the pushed soldier.
    // Chargers and followers are skipped in both directions, so they slip through their own troop (spec §2.3).
    void ResolveMinimumSeparation(List<SoldierAgentState> soldiers)
    {
        for (int pass = 0; pass < MinimumSeparationPasses; pass++)
        {
            _grid.Rebuild(soldiers);
            for (int i = 0; i < soldiers.Count; i++)
            {
                var a = soldiers[i];
                if (!a.IsAlive || a.IsCommitted) continue;
                Vector2Int cell = SoldierSpatialGrid.CellFor(a.Position);
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (!_grid.TryGetBucket(cell + new Vector2Int(x, y), out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a || b.IsCommitted) continue;
                        SeparatePair(a, b, SeparationDistance);
                    }
                }
            }
        }

        foreach (var soldier in soldiers)
            if (soldier.IsAlive) _constraints.ConstrainToRadius(soldier);
    }

    // Pairs are visited from both sides; the second visit finds them already separated and does nothing.
    static void SeparatePair(SoldierAgentState a, SoldierAgentState b, float minimumDistance)
    {
        Vector2 offset = a.Position - b.Position;
        float distanceSquared = offset.sqrMagnitude;
        if (distanceSquared >= minimumDistance * minimumDistance) return;

        float distance = Mathf.Sqrt(distanceSquared);
        Vector2 normal = distance > 0.0001f ? offset / distance : Vector2.right;
        float penetration = minimumDistance - distance;

        float inverseA = 1f / (1f + PressureMassPerDrive * a.DriveFraction);
        float inverseB = 1f / (1f + PressureMassPerDrive * b.DriveFraction);
        float inverseSum = inverseA + inverseB;
        a.Position += normal * (penetration * inverseA / inverseSum);
        b.Position -= normal * (penetration * inverseB / inverseSum);

        float closingSpeed = Vector2.Dot(a.Velocity - b.Velocity, normal);
        if (closingSpeed >= 0f) return;
        float impulse = -closingSpeed / inverseSum;
        a.Velocity += normal * (impulse * inverseA);
        b.Velocity -= normal * (impulse * inverseB);
    }

    // Retreating and committed soldiers, and every soldier of a routed troop, may leave the troop radius.
    bool LeavesRadius(SoldierAgentState soldier) =>
        soldier.IsRetreatTracking || soldier.IsCommitted || soldier.IsReturning || soldier.IsStunned || _troop.IsRouted;

    // Return after a charge (SU-43). When a charge ends, the soldier rushes back to the troop if its troop is retreating
    // or no longer engaged, or it ended up outside the troop's soft radius; otherwise it rejoins the field as before. The
    // return ends once the soldier is back in its formation band and inside the soft radius, after ReturnTimeoutSeconds,
    // or on a rout.
    void UpdateReturn(SoldierAgentState soldier, FieldShape shape, float deltaTime)
    {
        if (_troop.IsRouted)
        {
            soldier.EndReturn();
            return;
        }
        if (soldier.IsCommitted) return;
        if (soldier.ConsumeReturnPending()
            && (_troop.IsFaultSlipping || !_troop.IsEngaged || !InsideSoftRadius(soldier)))
            soldier.BeginReturn(ReturnTimeoutSeconds);
        if (!soldier.IsReturning) return;

        Vector2 core = NearestOnSegment(soldier.Position, shape.Start, shape.End);
        bool home = (soldier.Position - core).magnitude <= shape.HalfWidth + ReturnArrivalSlack && InsideSoftRadius(soldier);
        if (home) soldier.EndReturn();
        else soldier.TickReturn(deltaTime);
    }

    bool InsideSoftRadius(SoldierAgentState soldier) =>
        soldier.Position.magnitude <= _constraints.EffectiveRadius * SoftRadiusFraction;

    // Alignment and separation within NeighborRadius, plus the density spread (returned separately as a desired-velocity
    // push, because it steers the field rather than adding acceleration). Chargers and followers are not separated,
    // spread or pushed, and do not push others, so they slip through their own troop.
    Vector2 NeighborForces(SoldierAgentState soldier, out Vector2 spread)
    {
        Vector2 alignment = Vector2.zero;
        Vector2 separation = Vector2.zero;
        spread = Vector2.zero;
        int neighbors = 0;
        Vector2Int cell = SoldierSpatialGrid.CellFor(soldier.Position);
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            if (!_grid.TryGetBucket(cell + new Vector2Int(x, y), out var bucket)) continue;
            foreach (var other in bucket)
            {
                if (other == soldier) continue;
                Vector2 difference = soldier.Position - other.Position;
                float distanceSquared = difference.sqrMagnitude;
                if (distanceSquared >= NeighborRadius * NeighborRadius) continue;

                neighbors++;
                alignment += other.Velocity;

                if (!soldier.IsCommitted && !other.IsCommitted && distanceSquared < SeparationDistance * SeparationDistance)
                {
                    float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.001f));
                    separation += difference / distance * ((SeparationDistance - distance) / SeparationDistance * SeparationGain * TroopTuning.Active.SoldierSeparation);
                }

                if (!soldier.IsCommitted && !other.IsCommitted && distanceSquared < SpreadRadius * SpreadRadius)
                {
                    float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.001f));
                    float falloff = 1f - distance / SpreadRadius;
                    spread += difference / distance * (falloff * falloff);
                }
            }
        }

        Vector2 force = Vector2.zero;
        if (neighbors > 0)
        {
            alignment = alignment / neighbors - soldier.Velocity;
            force += alignment * TroopTuning.Active.SoldierAlignment;
        }
        return force + separation;
    }
}

// Rank sorting for composite troops (Decisions D-55). Each frame the troop decides whether it is composite (it has
// living soldiers with and without a ranged weapon) and which way is the front. Each field-following soldier is then
// steered along the front direction toward a target depth from the formation's centre line: ranged soldiers to
// +DepthFraction x half-depth (the front rank) and melee soldiers to -DepthFraction x half-depth (the back), or the
// reverse while the troop is engaged. Separation and the pressure-weighted minimum separation let the two groups pass
// each other. Applied only in a battle line (Line / Chain formation); idle blobs, columns and routed troops stay mixed.
public readonly struct CompositeRankPlan
{
    public readonly bool Active;
    public readonly Vector2 LocalFront;  // troop-local unit vector toward the enemy side of the formation
    public readonly bool ShootersForward;
    readonly float _gain;
    readonly float _depthFraction;

    public CompositeRankPlan(Vector2 localFront, bool shootersForward, float gain, float depthFraction)
    {
        Active = gain > 0f && localFront.sqrMagnitude > 1e-6f;
        LocalFront = Active ? localFront.normalized : Vector2.zero;
        ShootersForward = shootersForward;
        _gain = gain;
        _depthFraction = depthFraction;
    }

    public static CompositeRankPlan For(Troop troop, FieldShape shape)
    {
        TroopTuning tuning = TroopTuning.Active;
        // Only a battle line (Line / Chain) has a front rank to sort into. Idle blobs and columns are left mixed.
        if (tuning.CompositeRankGain <= 0f || troop.IsRouted || !shape.IsLine || !IsComposite(troop)) return default;
        Vector2 localFront = troop.transform.InverseTransformDirection(troop.Facing);
        return new CompositeRankPlan(localFront, !troop.IsEngaged, tuning.CompositeRankGain, tuning.CompositeRankDepthFraction);
    }

    // Living soldiers both with and without a ranged weapon.
    public static bool IsComposite(Troop troop)
    {
        bool ranged = false;
        bool melee = false;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive || soldier.Character == null) continue;
            if (soldier.Character.RangedWeapon != null) ranged = true;
            else melee = true;
            if (ranged && melee) return true;
        }
        return false;
    }

    // Target depth along LocalFront from the centre line (positive = toward the front).
    public float TargetDepth(bool isShooter, float halfWidth) =>
        (isShooter == ShootersForward ? 1f : -1f) * _depthFraction * Mathf.Max(0f, halfWidth);

    // offsetFromCore: the soldier's troop-local offset from the nearest point of the formation's centre line.
    public Vector2 DesiredVelocity(SoldierAgentState soldier, Vector2 offsetFromCore, float halfWidth, float strength)
    {
        float depth = Vector2.Dot(offsetFromCore, LocalFront);
        float target = TargetDepth(soldier.Character.RangedWeapon != null, halfWidth);
        return LocalFront * ((target - depth) * strength * _gain);
    }
}
