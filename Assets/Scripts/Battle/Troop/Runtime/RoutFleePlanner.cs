using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Chooses where a routed troop runs to (spec §5.4, Q-35) so that it flees around woods and rivers instead of running
// straight into them. The troop still flees away from the nearest enemy: on rout, every ReplanIntervalSeconds and
// whenever the goal is reached, candidate goals are sampled in directions within ±SearchHalfAngle of "away" (on the
// battlefield edge when there is one, FleeGoalDistance away otherwise), the ones with a route are kept and the best is
// scored by alignment with "away" (of the goal and of the route's first leg), distance from the enemies, detour, and
// whether the route passes closer to an enemy than the troop stands now. When nothing within ±SearchHalfAngle is
// reachable the whole circle is searched. A troop whose pivot moves less than StuckDistance over StuckCheckSeconds
// picks a new goal and avoids that heading for AvoidSeconds. Without a goal the rout falls back to the straight-line
// flight (no NavMesh: tests and scenes without one). Plain class; the route query is passed in so tests can fake it.
public sealed class RoutFleePlanner
{
    // Fills corners with a route from `from` toward `to` (first corner the start, last the reachable goal) and returns
    // true, or returns false when there is no route.
    public delegate bool RouteFinder(Vector2 from, Vector2 to, List<Vector2> corners);

    public const float ReplanIntervalSeconds = 1f;
    public const float StuckCheckSeconds = 1f;
    public const float StuckDistance = 0.5f;
    public const float AvoidSeconds = 5f;
    public const float AvoidHalfAngle = 30f;
    public const float SearchHalfAngle = 90f;
    public const float CandidateStepDegrees = 15f;
    public const float FleeGoalDistance = 20f;
    public const float MinGoalDistance = 2f;
    public const float GoalReachedDistance = 0.5f;
    // How close to the (inset) battlefield edge counts as being at it (IsAtEdge). A routed troop whose pivot gets this
    // close leaves the battle (SU-138). It matches GoalReachedDistance, so a troop that reaches a flee goal on the edge
    // counts as at the edge.
    public const float EdgeReachedDistance = 0.5f;

    // Scoring weights.
    const float AlignmentWeight = 1f;
    const float FirstLegWeight = 0.5f;
    const float ClearanceWeight = 0.5f;
    const float ClearanceScale = 20f;
    const float DetourWeight = 0.25f;
    const float ApproachWeight = 1f;
    const float AvoidPenalty = 2f;
    const float ContinuityBonus = 0.15f;
    const float ContinuityHalfAngle = 20f;
    const float FullCircle = 180f;

    // NavMesh route query: how far the pivot and a candidate goal may be from the NavMesh and still be snapped onto it.
    const float OriginSnapDistance = 6f;
    const float GoalSnapDistance = 6f;

    struct Avoided
    {
        public Vector2 Direction;
        public float Until;
    }

    readonly List<Avoided> _avoided = new();
    readonly List<Vector2> _corners = new();
    float _nextReplan;
    float _stuckWindowStart;
    Vector2 _stuckWindowPosition;

    public bool HasGoal { get; private set; }
    public Vector2 Goal { get; private set; }
    public int AvoidedCount => _avoided.Count;

    public void Reset(Vector2 position, float time)
    {
        HasGoal = false;
        _avoided.Clear();
        _nextReplan = time;
        _stuckWindowStart = time;
        _stuckWindowPosition = position;
    }

    // Drops the goal (no NavMesh any more, say); the stuck window restarts.
    public void Clear(Vector2 position, float time)
    {
        HasGoal = false;
        _stuckWindowStart = time;
        _stuckWindowPosition = position;
    }

    // One frame of planning. enemies are the non-routed enemy pivots; field is the battlefield edge (null: none).
    public void Tick(float time, Vector2 position, Vector2 away, IReadOnlyList<Vector2> enemies, Rect? field,
        float edgeInset, RouteFinder route)
    {
        for (int i = _avoided.Count - 1; i >= 0; i--)
            if (time >= _avoided[i].Until) _avoided.RemoveAt(i);
        bool stuck = false;
        bool replan = time >= _nextReplan;
        if (HasGoal && (Goal - position).sqrMagnitude <= GoalReachedDistance * GoalReachedDistance) replan = true;

        if (time - _stuckWindowStart >= StuckCheckSeconds)
        {
            if (HasGoal && (position - _stuckWindowPosition).sqrMagnitude < StuckDistance * StuckDistance)
            {
                Vector2 heading = Goal - position;
                if (heading.sqrMagnitude > 1e-6f) Avoid(heading.normalized, time);
                stuck = true;
                replan = true;
            }
            _stuckWindowStart = time;
            _stuckWindowPosition = position;
        }

        if (!replan) return;
        _nextReplan = time + ReplanIntervalSeconds;
        // Keep to the current heading unless it just failed, so the goal does not flicker between near-equal choices.
        Vector2? current = HasGoal && !stuck ? Goal - position : null;
        HasGoal = TryPick(position, away, enemies, field, edgeInset, route, current, out Vector2 goal);
        Goal = goal;
    }

    // Marks a heading as failed until time + AvoidSeconds; candidates within AvoidHalfAngle of it are penalised.
    public void Avoid(Vector2 direction, float time) =>
        _avoided.Add(new Avoided { Direction = direction, Until = time + AvoidSeconds });

    // The best reachable flee goal, searching ±SearchHalfAngle of away first and the whole circle if that finds none.
    public bool TryPick(Vector2 position, Vector2 away, IReadOnlyList<Vector2> enemies, Rect? field, float edgeInset,
        RouteFinder route, Vector2? currentHeading, out Vector2 goal)
    {
        if (away.sqrMagnitude <= 1e-6f) away = Vector2.right;
        away.Normalize();
        float nearestEnemy = NearestDistance(position, enemies);
        if (Search(position, away, SearchHalfAngle, 0f, enemies, nearestEnemy, field, edgeInset, route, currentHeading,
                out goal))
            return true;
        return Search(position, away, FullCircle, SearchHalfAngle, enemies, nearestEnemy, field, edgeInset, route,
            currentHeading, out goal);
    }

    // True when the pivot stands within EdgeReachedDistance of the battlefield edge (inset as BattlefieldBounds clamps).
    public static bool IsAtEdge(Vector2 position, Rect field, float edgeInset)
    {
        float inset = Mathf.Clamp(edgeInset, 0f, Mathf.Min(field.width, field.height) * 0.5f);
        float toEdge = Mathf.Min(
            Mathf.Min(position.x - (field.xMin + inset), (field.xMax - inset) - position.x),
            Mathf.Min(position.y - (field.yMin + inset), (field.yMax - inset) - position.y));
        return toEdge <= EdgeReachedDistance;
    }

    // The route query on the baked 2D NavMesh: both ends are snapped onto it and the path must be complete.
    static NavMeshPath _navPath;
    public static bool NavMeshRoute(Vector2 from, Vector2 to, List<Vector2> corners)
    {
        corners.Clear();
        if (!NavMesh.SamplePosition(from, out NavMeshHit start, OriginSnapDistance, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(to, out NavMeshHit end, GoalSnapDistance, NavMesh.AllAreas)) return false;
        _navPath ??= new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, _navPath)
            || _navPath.status != NavMeshPathStatus.PathComplete
            || _navPath.corners.Length == 0)
            return false;
        corners.Add(from);
        foreach (Vector3 corner in _navPath.corners) corners.Add(corner);
        return true;
    }

    // Candidates at angles in (innerHalfAngle, halfAngle] on both sides of away (and away itself when inner is 0).
    bool Search(Vector2 position, Vector2 away, float halfAngle, float innerHalfAngle, IReadOnlyList<Vector2> enemies,
        float nearestEnemy, Rect? field, float edgeInset, RouteFinder route, Vector2? currentHeading, out Vector2 goal)
    {
        goal = position;
        bool found = false;
        float bestScore = float.NegativeInfinity;
        for (float angle = 0f; angle <= halfAngle + 1e-3f; angle += CandidateStepDegrees)
        {
            if (innerHalfAngle > 0f && angle <= innerHalfAngle + 1e-3f) continue;
            for (int side = 0; side < 2; side++)
            {
                if (side == 1 && (angle <= 1e-3f || angle >= FullCircle - 1e-3f)) continue;
                Vector2 direction = Rotate(away, side == 0 ? angle : -angle);
                Vector2 candidate = CandidatePoint(position, direction, field, edgeInset);
                if ((candidate - position).sqrMagnitude < MinGoalDistance * MinGoalDistance) continue;
                if (!route(position, candidate, _corners) || _corners.Count == 0) continue;
                Vector2 end = _corners[_corners.Count - 1];
                if ((end - position).sqrMagnitude < MinGoalDistance * MinGoalDistance) continue;
                float score = Score(position, away, end, enemies, nearestEnemy, currentHeading);
                if (score <= bestScore) continue;
                bestScore = score;
                goal = end;
                found = true;
            }
        }
        return found;
    }

    float Score(Vector2 position, Vector2 away, Vector2 end, IReadOnlyList<Vector2> enemies, float nearestEnemy,
        Vector2? currentHeading)
    {
        Vector2 toGoal = end - position;
        float straight = toGoal.magnitude;
        Vector2 goalDirection = toGoal / straight;

        Vector2 firstLeg = goalDirection;
        bool hasFirstLeg = false;
        float length = 0f;
        float closestApproach = float.PositiveInfinity;
        for (int i = 1; i < _corners.Count; i++)
        {
            Vector2 leg = _corners[i] - _corners[i - 1];
            if (!hasFirstLeg && leg.sqrMagnitude > 1e-6f)
            {
                firstLeg = leg.normalized;
                hasFirstLeg = true;
            }
            length += leg.magnitude;
            closestApproach = Mathf.Min(closestApproach, NearestDistance(_corners[i], enemies));
        }

        float score = AlignmentWeight * Vector2.Dot(goalDirection, away)
            + FirstLegWeight * Vector2.Dot(firstLeg, away)
            + ClearanceWeight * Mathf.Clamp01(NearestDistance(end, enemies) / ClearanceScale)
            - DetourWeight * Mathf.Max(0f, length / Mathf.Max(straight, 1e-3f) - 1f);
        if (!float.IsPositiveInfinity(nearestEnemy) && !float.IsPositiveInfinity(closestApproach))
            score -= ApproachWeight * Mathf.Max(0f, nearestEnemy - closestApproach) / ClearanceScale;
        foreach (Avoided avoided in _avoided)
            if (Vector2.Angle(goalDirection, avoided.Direction) <= AvoidHalfAngle) score -= AvoidPenalty;
        if (currentHeading.HasValue && currentHeading.Value.sqrMagnitude > 1e-6f
            && Vector2.Angle(goalDirection, currentHeading.Value) <= ContinuityHalfAngle)
            score += ContinuityBonus;
        return score;
    }

    // Where the ray from position along direction leaves the inset battlefield, or FleeGoalDistance along it.
    static Vector2 CandidatePoint(Vector2 position, Vector2 direction, Rect? field, float edgeInset)
    {
        if (!field.HasValue) return position + direction * FleeGoalDistance;
        Rect rect = field.Value;
        float inset = Mathf.Clamp(edgeInset, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        float t = float.PositiveInfinity;
        if (direction.x > 1e-6f) t = Mathf.Min(t, (rect.xMax - inset - position.x) / direction.x);
        else if (direction.x < -1e-6f) t = Mathf.Min(t, (rect.xMin + inset - position.x) / direction.x);
        if (direction.y > 1e-6f) t = Mathf.Min(t, (rect.yMax - inset - position.y) / direction.y);
        else if (direction.y < -1e-6f) t = Mathf.Min(t, (rect.yMin + inset - position.y) / direction.y);
        if (float.IsPositiveInfinity(t)) t = FleeGoalDistance;
        return position + direction * Mathf.Max(0f, t);
    }

    static float NearestDistance(Vector2 point, IReadOnlyList<Vector2> enemies)
    {
        float best = float.PositiveInfinity;
        if (enemies == null) return best;
        foreach (Vector2 enemy in enemies) best = Mathf.Min(best, Vector2.Distance(point, enemy));
        return best;
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
