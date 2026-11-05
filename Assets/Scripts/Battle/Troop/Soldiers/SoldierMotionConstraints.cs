using UnityEngine;

// Keeps soldiers inside the troop radius and stops them, and the troop pivot, moving through terrain obstacles.
public sealed class SoldierMotionConstraints
{
    const float CollisionSkin = 0.005f;
    readonly Troop _troop;
    readonly LayerMask _obstacleMask;
    readonly ContactFilter2D _obstacleFilter;
    readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[4];
    readonly RaycastHit2D[] _pivotHits = new RaycastHit2D[4];

    public SoldierMotionConstraints(Troop troop, LayerMask obstacleMask)
    {
        _troop = troop;
        _obstacleMask = obstacleMask;
        _obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };
    }

    public static LayerMask DefaultObstacleMask()
    {
        return LayerMask.GetMask(
            "Terrain",
            "Water",
            "Water/flat impassible terrain",
            "Impassable Terrain",
            "Impassable");
    }

    public void ConstrainAllToRadius()
    {
        foreach (var soldier in _troop.Roster.SoldierList)
            if (soldier.IsAlive) ConstrainToRadius(soldier);
    }

    // The clamp radius. A stretched line (longer than the troop is wide) widens it to reach its line points: soldiers of a
    // linked, stretched line may sit outside TroopRadius (Q-11 option a, working assumption). A routed troop scatters.
    public float EffectiveRadius
    {
        get
        {
            float radius = _troop.Radius;
            if (_troop.IsRouted) return radius * TroopTuning.Active.RoutScatterRadiusScale;
            TroopFormationData formation = _troop.Formation;
            if (formation == null) return radius;
            Vector2 pivot = _troop.transform.position;
            float reach = Mathf.Max(Vector2.Distance(pivot, formation.Start), Vector2.Distance(pivot, formation.End));
            return Mathf.Max(radius, reach + TroopConstants.LineSpacing);
        }
    }

    public void ConstrainToRadius(SoldierAgentState soldier)
    {
        // Retreating, committed and returning soldiers may leave the troop radius.
        if (soldier.IsRetreatTracking || soldier.IsCommitted || soldier.IsReturning) return;
        float radius = EffectiveRadius;
        float distance = soldier.Position.magnitude;
        if (distance <= radius) return;
        Vector2 normal = soldier.Position / distance;
        soldier.Position = normal * radius;
        float outwardSpeed = Vector2.Dot(soldier.Velocity, normal);
        if (outwardSpeed > 0f) soldier.Velocity -= normal * outwardSpeed;
    }

    // Clips the soldier's step against terrain and removes the velocity component pointing into the obstacle.
    public void ConstrainToTerrain(SoldierAgentState soldier, ref Vector2 velocity, ref Vector2 nextPosition)
    {
        if (_obstacleMask.value == 0) return;

        Transform transform = _troop.transform;
        Vector2 start = transform.TransformPoint(soldier.Position);
        Vector2 end = transform.TransformPoint(nextPosition);
        Vector2 displacement = end - start;
        float distance = displacement.magnitude;
        if (distance <= 0.0001f) return;

        Vector2 direction = displacement / distance;
        int hitCount = Physics2D.CircleCast(
            start,
            TroopConstants.SoldierRadius,
            direction,
            _obstacleFilter,
            _obstacleHits,
            distance);
        int closestIndex = ClosestBlockingHit(hitCount, direction);
        if (closestIndex < 0) return;

        RaycastHit2D obstacleHit = _obstacleHits[closestIndex];
        end = start + direction * Mathf.Max(0f, obstacleHit.distance - CollisionSkin);
        Vector2 worldVelocity = transform.TransformVector(velocity);
        float intoObstacle = Vector2.Dot(worldVelocity, obstacleHit.normal);
        if (intoObstacle < 0f)
            worldVelocity -= obstacleHit.normal * intoObstacle;
        velocity = transform.InverseTransformVector(worldVelocity);
        nextPosition = transform.InverseTransformPoint(end);
    }

    // Clips a pivot displacement so the pivot does not cross terrain obstacles (rivers, woods, the map's boundary walls),
    // and slides the rest of the step along the obstacle (SU-74). Soldiers are placed relative to the pivot, so a pivot
    // that crossed terrain would carry the whole troop across it. A pivot that already stands inside an obstacle is let
    // out: queries that start inside a collider ignore it (Physics 2D 'Queries Start In Colliders' is off).
    public Vector2 ClipPivotStep(Vector2 start, Vector2 delta)
    {
        if (_obstacleMask.value == 0 || delta.sqrMagnitude <= 1e-12f) return delta;
        if (!CastPivot(start, delta, out Vector2 moved, out Vector2 normal)) return delta;

        Vector2 rest = delta - moved;
        Vector2 slide = rest - normal * Vector2.Dot(rest, normal);
        if (slide.sqrMagnitude > 1e-12f)
        {
            CastPivot(start + moved, slide, out Vector2 slid, out _);
            moved += slid;
        }
        return moved;
    }

    // Moves a point along delta until the first blocking obstacle. Returns true when it was blocked.
    bool CastPivot(Vector2 start, Vector2 delta, out Vector2 moved, out Vector2 normal)
    {
        moved = delta;
        normal = Vector2.zero;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return false;

        Vector2 direction = delta / distance;
        int hitCount = Physics2D.Raycast(start, direction, _obstacleFilter, _pivotHits, distance);
        int closest = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D candidate = _pivotHits[i];
            if (candidate.collider == null || candidate.distance >= closestDistance) continue;
            if (candidate.distance <= 0.0001f && Vector2.Dot(direction, candidate.normal) >= 0f) continue;
            closest = i;
            closestDistance = candidate.distance;
        }
        if (closest < 0) return false;

        normal = _pivotHits[closest].normal;
        moved = direction * Mathf.Max(0f, closestDistance - CollisionSkin);
        return true;
    }

    // Nearest hit, ignoring obstacles the soldier is already overlapping but moving away from.
    int ClosestBlockingHit(int hitCount, Vector2 direction)
    {
        int closestIndex = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D candidate = _obstacleHits[i];
            if (candidate.collider == null || candidate.distance >= closestDistance) continue;
            if (candidate.distance <= 0.0001f
                && candidate.normal.sqrMagnitude > 0.0001f
                && Vector2.Dot(direction, candidate.normal) >= 0f)
                continue;
            closestIndex = i;
            closestDistance = candidate.distance;
        }
        return closestIndex;
    }
}