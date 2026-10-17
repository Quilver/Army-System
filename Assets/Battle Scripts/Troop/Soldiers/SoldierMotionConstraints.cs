using UnityEngine;

// Keeps soldiers inside the troop radius and stops them moving through terrain obstacles.
public sealed class SoldierMotionConstraints
{
    const float CollisionSkin = 0.005f;
    readonly Troop _troop;
    readonly LayerMask _obstacleMask;
    readonly ContactFilter2D _obstacleFilter;
    readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[4];

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

    public void ConstrainToRadius(SoldierAgentState soldier)
    {
        // Retreating and charging soldiers may leave the troop radius.
        if (soldier.FaultOffset > 0f || soldier.IsCommitted) return;
        float radius = _troop.Radius;
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