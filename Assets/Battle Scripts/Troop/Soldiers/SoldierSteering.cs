using System.Collections.Generic;
using UnityEngine;

// Per-frame soldier steering. Each soldier follows one of two rules:
//   - Charging and lunging soldiers seek their enemy.
//   - Every other soldier follows the troop's vector field (SoldierFieldPlanner): it is attracted to the nearest point of
//     the troop's shape and spreads away from crowded neighbours, so the shape fills evenly without slots. Separation
//     keeps soldiers apart, and ResolveMinimumSeparation enforces the minimum when they press together.
// No soldier is pulled toward another.
public sealed class SoldierSteering
{
    const float NeighborRadius = TroopConstants.NeighborRadius;
    const float SteeringDamping = 2.6f;
    const float VelocityMatchGain = 4f;  // velocity matching relative to the position gain (critically damped)
    const float SeparationGain = 2.5f;   // separation strength relative to SoldierProfile.Separation
    // Soldiers try to keep their centres this far apart. Neighbours closer than this push each other out, and the
    // minimum is enforced exactly by ResolveMinimumSeparation.
    const float SeparationDistance = TroopConstants.SeparationRadiusScale * TroopConstants.SoldierRadius;
    // Density spreading: soldiers closer than SpreadRadius push each other apart along the field. The push fades to
    // zero at SpreadRadius, so a crowd spreads out and sparse soldiers do not move.
    const float SpreadRadius = 1.2f;
    const float SpreadSpeedPerUnit = 1.2f;
    // Extra pressure mass a fully driving soldier has over a settled one. Mass sets how far each soldier of a pressed
    // pair gives way: the settled soldier moves most.
    const float PressureMassPerDrive = 3f;
    const int MinimumSeparationPasses = 3;

    readonly Troop _troop;
    readonly SoldierMotionConstraints _constraints;
    readonly SoldierFieldPlanner _planner;
    readonly SoldierSpatialGrid _grid = new();
    readonly List<Vector2> _nextPositions = new();
    readonly List<Vector2> _nextVelocities = new();
    readonly List<Vector2> _seekPoints = new();
    readonly List<bool> _seeks = new();

    public SoldierSteering(Troop troop, SoldierMotionConstraints constraints)
    {
        _troop = troop;
        _constraints = constraints;
        _planner = new SoldierFieldPlanner(troop);
    }

    public void Simulate(float deltaTime)
    {
        List<SoldierAgentState> soldiers = _troop.Roster.SoldierList;
        _grid.Rebuild(soldiers);
        while (_nextPositions.Count < soldiers.Count)
        {
            _nextPositions.Add(Vector2.zero);
            _nextVelocities.Add(Vector2.zero);
            _seekPoints.Add(Vector2.zero);
            _seeks.Add(false);
        }

        // Pass 1: decide each soldier's rule for this frame.
        FieldShape shape = _planner.Shape(_troop.LivingSoldierCount);
        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            Vector2 seekPoint = Vector2.zero;
            bool seek = soldier.IsAlive && soldier.Profile != null && _planner.TrySeek(soldier, out seekPoint);
            _seeks[i] = seek;
            _seekPoints[i] = seekPoint;
        }

        // Pass 2: integrate each living soldier.
        for (int i = 0; i < soldiers.Count; i++)
        {
            var soldier = soldiers[i];
            _nextPositions[i] = soldier.Position;
            _nextVelocities[i] = soldier.Velocity;
            if (!soldier.IsAlive || soldier.Profile == null) continue;
            soldier.TickCooldowns(deltaTime);

            Vector2 velocity = Step(soldier, _seeks[i], _seekPoints[i], shape, deltaTime, out Vector2 nextPosition, out float drive);
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
        TroopDiagnostics.RecordSoldiers(_troop, soldiers, _seeks, _seekPoints);
    }

    Vector2 Step(SoldierAgentState soldier, bool seek, Vector2 seekPoint, FieldShape shape, float deltaTime,
        out Vector2 nextPosition, out float drive)
    {
        float strength = soldier.IsCommitted ? 5.2f
            : _troop.PivotVelocity.sqrMagnitude > 0.0025f ? 3.2f
            : _troop.Formation != null ? 2.5f
            : 1.3f;

        Vector2 spread;
        Vector2 neighborForce = NeighborForces(soldier, out spread);

        Vector2 steering;
        if (seek)
        {
            steering = (seekPoint - soldier.Position) * strength;
            drive = 1f;
        }
        else
        {
            Vector2 desired = FieldVelocity(soldier, shape, strength, spread, out drive);
            steering = (desired - soldier.Velocity) * (VelocityMatchGain * strength);
        }
        steering += neighborForce;
        steering += soldier.ExternalSeparation;

        // Retreating and charging soldiers may leave the troop radius; the radius pull would fight them.
        float radius = _troop.Radius;
        float distanceFromPivot = soldier.Position.magnitude;
        float softRadius = radius * 0.92f;
        if (distanceFromPivot > softRadius && !LeavesRadius(soldier))
            steering -= soldier.Position / distanceFromPivot * ((distanceFromPivot - softRadius) * 3.3f);
        if (soldier.Profile.BrownianMotion > 0f)
            steering += Random.insideUnitCircle * soldier.Profile.BrownianMotion * 2.5f;

        // A charging soldier moves at double speed.
        float speedScale = soldier.ChargeTarget != null ? TroopConstants.ChargeSpeedScale : 1f;
        Vector2 acceleration = Vector2.ClampMagnitude(steering, soldier.Profile.Acceleration * speedScale);
        Vector2 velocity = soldier.Velocity + acceleration * deltaTime;
        velocity *= Mathf.Exp(-SteeringDamping * deltaTime);
        velocity = Vector2.ClampMagnitude(velocity, soldier.Profile.MaxSpeed * speedScale);
        nextPosition = soldier.Position + velocity * deltaTime;
        _constraints.ConstrainToTerrain(soldier, ref velocity, ref nextPosition);
        return velocity;
    }

    // The field's desired velocity at the soldier. Attraction is proportional to the distance outside the shape's core,
    // so the soldier slows as it closes on the shape. Spreading adds density push. The result is clamped to max speed,
    // and its size as a fraction of max speed is the soldier's drive (how hard it is pushing toward its place).
    Vector2 FieldVelocity(SoldierAgentState soldier, FieldShape shape, float strength, Vector2 spread, out float drive)
    {
        // A retreating soldier is pulled toward the shape shifted back by its fault offset. Querying the unshifted field
        // at the offset position is the same thing, so the shape itself does not need to move.
        Vector2 query = soldier.Position + _planner.LocalFaultAxis * soldier.FaultOffset;
        Vector2 core = NearestOnSegment(query, shape.Start, shape.End);
        Vector2 offset = query - core;
        float distance = offset.magnitude;

        Vector2 desired = Vector2.zero;
        if (distance > shape.HalfWidth)
            desired -= offset / distance * ((distance - shape.HalfWidth) * strength);
        if (!soldier.IsCommitted)
            desired += spread * SpreadSpeedPerUnit;

        desired = Vector2.ClampMagnitude(desired, soldier.Profile.MaxSpeed);
        drive = desired.magnitude / soldier.Profile.MaxSpeed;
        return desired;
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
    // Charging soldiers are skipped, so they slip through their own flock to reach an enemy.
    void ResolveMinimumSeparation(List<SoldierAgentState> soldiers)
    {
        for (int pass = 0; pass < MinimumSeparationPasses; pass++)
        {
            _grid.Rebuild(soldiers);
            for (int i = 0; i < soldiers.Count; i++)
            {
                var a = soldiers[i];
                if (!a.IsAlive || a.IsCharging) continue;
                Vector2Int cell = SoldierSpatialGrid.CellFor(a.Position);
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (!_grid.TryGetBucket(cell + new Vector2Int(x, y), out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a || b.IsCharging) continue;
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

    // Retreating and committed soldiers may leave the troop radius.
    static bool LeavesRadius(SoldierAgentState soldier) => soldier.FaultOffset > 0f || soldier.IsCommitted;

    // Alignment and separation within NeighborRadius, plus the density spread (returned separately as a desired-velocity
    // push, because it steers the field rather than adding acceleration). Charging and committed soldiers are not spread
    // or pushed, so they slip through their own flock.
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

                if (!soldier.IsCharging && distanceSquared < SeparationDistance * SeparationDistance)
                {
                    float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.001f));
                    separation += difference / distance * ((SeparationDistance - distance) / SeparationDistance * SeparationGain * soldier.Profile.Separation);
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
            force += alignment * soldier.Profile.Alignment;
        }
        return force + separation;
    }
}
