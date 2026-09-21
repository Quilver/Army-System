using System.Collections.Generic;
using UnityEngine;

namespace MovementSystem
{
    public enum UnitSteeringMode
    {
        Idle,
        Move,
        Charge,
        Flee
    }

    public sealed class UnitSteering : MonoBehaviour
    {
        const int DirectionCount = 16;
        static readonly List<UnitSteering> Active = new();
        static readonly Vector2[] Directions = CreateDirections();

        [SerializeField, Min(0.05f)] float repathInterval = 0.5f;
        [SerializeField, Min(0.01f)] float pathPointRadius = 0.3f;
        [SerializeField, Min(0.01f)] float velocitySmoothing = 0.3f;
        [SerializeField, Min(0f)] float fleeRadius = 12f;
        [SerializeField, Min(0.1f)] float directMovementDistance = 3f;

        [Header("Avoidance")]
        [SerializeField, Min(0.1f)] float neighborDistance = 12f;
        [SerializeField, Min(0.1f)] float timeToCollisionHorizon = 2f;
        [SerializeField, Min(0f)] float avoidancePadding = 0.2f;
        [SerializeField, Min(0f)] float dangerWeight = 2f;
        [SerializeField, Range(0f, 1f)] float rightOfWayDangerScale = 0.35f;
        [SerializeField, Range(0f, 0.5f)] float avoidanceActivationDanger = 0.05f;
        [SerializeField, Range(0f, 1f)] float directionSwitchThreshold = 0.15f;
        [SerializeField, Range(0f, 0.5f)] float retainedDirectionBonus = 0.1f;
        [SerializeField, Range(0.5f, 1f)] float stopDanger = 0.95f;
        [SerializeField, Min(0f)] float staticProbeBase = 0.5f;
        [SerializeField, Min(0f)] float staticLookAheadTime = 0.75f;
        [SerializeField] LayerMask staticObstacleMask;

        IUnit _unit;
        IMoveOrders _orders;
        IPathfinder _pathfinder;
        UnitCohesion _cohesion;
        UnitBody _body;
        BoxCollider2D _formationCollider;
        Army _army;
        Transform _unitTransform;
        List<Vector2> _path;
        int _pathIndex;
        Vector2 _pathTarget;
        Vector2 _velocity;
        float _currentSpeed;
        float _nextRepath;
        bool _pathInvalid = true;
        bool _smallAdjustment;
        int _chosenDirection = -1;
        readonly float[] _directionScores = new float[DirectionCount];
        readonly float[] _directionDangers = new float[DirectionCount];
        readonly bool[] _rejectedDirections = new bool[DirectionCount];
        readonly RaycastHit2D[] _staticHits = new RaycastHit2D[4];
        ContactFilter2D _staticFilter;

        public UnitSteeringMode Mode { get; private set; }
        public Vector2 DesiredVelocity => _velocity;
        public Vector2 DesiredFacing { get; private set; }
        public bool AllowsFreeMovement => Mode == UnitSteeringMode.Flee
            || (Mode == UnitSteeringMode.Move && (_smallAdjustment || (_unit != null && _unit.InMelee)));
        Transform AgentTransform => _unitTransform == null ? transform : _unitTransform;
        public float MaxSpeed => _unit == null || _unit.Stats == null ? 1f : _unit.Stats.Movement / 5f;

        void Awake()
        {
            _unit = GetComponentInParent<IUnit>();
            _orders = GetComponentInParent<IMoveOrders>();
            _unitTransform = _unit == null ? transform : _unit.transform;
            _pathfinder = GetComponentInChildren<IPathfinder>();
            _cohesion = GetComponentInParent<UnitCohesion>();
            _body = GetComponentInParent<UnitBody>();
            _army = GetComponentInParent<Army>();
            _formationCollider = FindFormationCollider();
            if (staticObstacleMask.value == 0)
                staticObstacleMask = LayerMask.GetMask("Water/flat impassible terrain", "Terrain");
            _staticFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = staticObstacleMask,
                useTriggers = false
            };
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
            if (_orders != null)
            {
                _orders.moving += InvalidatePath;
                _orders.pursuing += InvalidatePursuit;
            }
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (_orders != null)
            {
                _orders.moving -= InvalidatePath;
                _orders.pursuing -= InvalidatePursuit;
            }
        }

        void InvalidatePath(Vector2 target)
        {
            _pathInvalid = true;
            _smallAdjustment = Vector2.Distance(AgentTransform.position, target) <= directMovementDistance;
            _chosenDirection = -1;
        }

        void InvalidatePursuit(Transform _)
        {
            _pathInvalid = true;
            _smallAdjustment = false;
            _chosenDirection = -1;
        }

        public void Tick(float deltaTime)
        {
            if (_unit == null || _orders == null)
            {
                _velocity = Vector2.zero;
                _currentSpeed = 0f;
                _chosenDirection = -1;
                return;
            }

            Mode = GetMode();
            bool completingFinalFacing = Mode == UnitSteeringMode.Move
                && _orders.FaceTowards.HasValue
                && _orders.HasReachedPosition;
            Vector2 desired = completingFinalFacing ? Vector2.zero : Mode switch
            {
                UnitSteeringMode.Move => SeekPosition(_orders.TargetPosition),
                UnitSteeringMode.Charge => SeekPosition(_orders.TargetPosition),
                UnitSteeringMode.Flee => FleeDirection(),
                _ => Vector2.zero
            };

            float speed = MaxSpeed * (_cohesion == null ? 1f : _cohesion.CohesionFactor);
            Vector2 desiredFacing = desired;
            desired = Vector2.ClampMagnitude(desired, 1f) * speed;
            if (!completingFinalFacing
                && desired.sqrMagnitude > 0.0001f
                && (Mode == UnitSteeringMode.Move || Mode == UnitSteeringMode.Charge))
            {
                desired = AvoidUnitsAndStatics(desired.normalized, speed, deltaTime, out desiredFacing);
            }
            else if (Mode == UnitSteeringMode.Idle)
            {
                _chosenDirection = -1;
            }

            _currentSpeed = completingFinalFacing
                ? 0f
                : Mathf.MoveTowards(
                    _currentSpeed,
                    desired.magnitude,
                    MaxSpeed * deltaTime / velocitySmoothing);
            _velocity = desired.sqrMagnitude <= 0.0001f
                ? Vector2.zero
                : desired.normalized * _currentSpeed;

            if (completingFinalFacing)
                DesiredFacing = _orders.FaceTowards.Value - (Vector2)AgentTransform.position;
            else if (desiredFacing.sqrMagnitude > 0.0001f)
                DesiredFacing = desiredFacing;
            else
                DesiredFacing = AgentTransform.up;
        }

        Vector2 AvoidUnitsAndStatics(Vector2 preferred, float speed, float deltaTime, out Vector2 facing)
        {
            if (_formationCollider == null)
                _formationCollider = FindFormationCollider();
            if (_formationCollider == null)
            {
                facing = preferred;
                return preferred * speed;
            }

            if (_chosenDirection >= 0 && Vector2.Dot(Directions[_chosenDirection], preferred) < 0.25f)
                _chosenDirection = -1;

            float probeDistance = staticProbeBase + speed * staticLookAheadTime;
            IUnit ignoredTarget = Mode == UnitSteeringMode.Charge && _orders.Target != null
                ? _orders.Target.GetComponentInParent<IUnit>()
                : null;
            Army targetArmy = ignoredTarget == null ? null : ignoredTarget.GetComponentInParent<Army>();
            if (_army == null || targetArmy == null || targetArmy == _army)
                ignoredTarget = null;

            Vector2 preferredVelocity = _body == null
                ? preferred * speed
                : _body.PredictLocomotionVelocity(preferred, speed, timeToCollisionHorizon);
            float preferredDanger = Mathf.Max(
                DynamicDanger(preferredVelocity, ignoredTarget),
                StaticRayDanger(preferred, probeDistance));
            if (preferredDanger <= avoidanceActivationDanger
                && !StaticBoxBlocked(preferred, probeDistance))
            {
                _chosenDirection = -1;
                facing = preferred;
                return preferred * speed;
            }

            for (int i = 0; i < DirectionCount; i++)
            {
                Vector2 candidateVelocity = _body == null
                    ? Directions[i] * speed
                    : _body.PredictLocomotionVelocity(Directions[i], speed, timeToCollisionHorizon);
                float danger = Mathf.Max(
                    DynamicDanger(candidateVelocity, ignoredTarget),
                    StaticRayDanger(Directions[i], probeDistance));
                float interest = 0.5f + 0.5f * Vector2.Dot(Directions[i], preferred);
                _directionDangers[i] = danger;
                _directionScores[i] = interest - dangerWeight * danger
                    + (i == _chosenDirection ? retainedDirectionBonus : 0f);
                _rejectedDirections[i] = false;
            }

            int best = BestUnblockedDirection(probeDistance);
            if (best < 0)
            {
                _chosenDirection = -1;
                facing = preferred;
                return Vector2.zero;
            }
            if (_chosenDirection >= 0
                && !_rejectedDirections[_chosenDirection]
                && _directionScores[_chosenDirection] >= _directionScores[best] - directionSwitchThreshold
                && !StaticBoxBlocked(Directions[_chosenDirection], probeDistance))
            {
                best = _chosenDirection;
            }

            _chosenDirection = best;
            facing = Directions[best];
            return _directionDangers[best] >= stopDanger ? Vector2.zero : facing * speed;
        }

        int BestUnblockedDirection(float probeDistance)
        {
            int best = -1;
            for (int probe = 0; probe < DirectionCount; probe++)
            {
                best = -1;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < DirectionCount; i++)
                {
                    if (_rejectedDirections[i] || _directionScores[i] <= bestScore)
                        continue;
                    best = i;
                    bestScore = _directionScores[i];
                }

                if (best < 0 || !StaticBoxBlocked(Directions[best], probeDistance))
                    return best;
                _rejectedDirections[best] = true;
                _directionDangers[best] = 1f;
            }

            return -1;
        }

        float DynamicDanger(Vector2 candidateVelocity, IUnit ignoredTarget)
        {
            Vector2 center = _formationCollider.bounds.center;
            float radius = _formationCollider.bounds.extents.magnitude + avoidancePadding;
            float danger = 0f;

            for (int i = 0; i < Active.Count; i++)
            {
                UnitSteering other = Active[i];
                if (other == null || other == this || other._unit == ignoredTarget)
                    continue;
                if (other._formationCollider == null)
                    other._formationCollider = other.FindFormationCollider();
                if (other._formationCollider == null || !other._formationCollider.enabled)
                    continue;

                Vector2 otherCenter = other._formationCollider.bounds.center;
                float otherRadius = other._formationCollider.bounds.extents.magnitude + avoidancePadding;
                Vector2 separation = otherCenter - center;
                float broadPhase = neighborDistance + radius + otherRadius;
                if (separation.sqrMagnitude > broadPhase * broadPhase)
                    continue;

                Vector2 otherVelocity = other._body == null ? Vector2.zero : other._body.CurrentVelocity;
                float candidateDanger = FormationTimeToCollisionDanger(
                    other,
                    otherVelocity - candidateVelocity);
                bool otherIsIntentionallyStationary = other.Mode == UnitSteeringMode.Idle
                    || (other._unit != null && other._unit.State == UnitState.Deployment);
                if (!otherIsIntentionallyStationary
                    && _unit != null
                    && other._unit != null
                    && _unit.GetInstanceID() < other._unit.GetInstanceID())
                {
                    candidateDanger *= rightOfWayDangerScale;
                }
                danger = Mathf.Max(danger, candidateDanger);
            }
            return danger;
        }

        float FormationTimeToCollisionDanger(UnitSteering other, Vector2 relativeVelocity)
        {
            GetDiscLayout(_formationCollider, out Vector2 center, out Vector2 axis, out float span, out float radius);
            GetDiscLayout(other._formationCollider, out Vector2 otherCenter, out Vector2 otherAxis, out float otherSpan, out float otherRadius);

            float danger = 0f;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 discCenter = center + axis * (span * i);
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 otherDiscCenter = otherCenter + otherAxis * (otherSpan * j);
                    danger = Mathf.Max(danger, TimeToCollisionDanger(
                        otherDiscCenter - discCenter,
                        relativeVelocity,
                        radius + otherRadius));
                }
            }
            return danger;
        }

        void GetDiscLayout(
            BoxCollider2D collider,
            out Vector2 center,
            out Vector2 axis,
            out float span,
            out float radius)
        {
            Vector3 scale = collider.transform.lossyScale;
            Vector2 size = Vector2.Scale(
                collider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            bool horizontal = size.x >= size.y;
            center = collider.bounds.center;
            axis = horizontal ? collider.transform.right : collider.transform.up;
            radius = Mathf.Min(size.x, size.y) * 0.5f + avoidancePadding;
            span = Mathf.Max(0f, Mathf.Max(size.x, size.y) * 0.5f - radius);
        }

        float TimeToCollisionDanger(Vector2 separation, Vector2 relativeVelocity, float combinedRadius)
        {
            float c = separation.sqrMagnitude - combinedRadius * combinedRadius;
            if (c <= 0f)
            {
                // Padded prediction discs may overlap before the formation boxes do.
                // Keep outward escape directions available instead of deadlocking.
                return Vector2.Dot(separation, relativeVelocity) > 0.0001f ? 0f : 1f;
            }

            float a = relativeVelocity.sqrMagnitude;
            if (a <= 0.0001f || Vector2.Dot(separation, relativeVelocity) >= 0f)
                return 0f;

            float b = 2f * Vector2.Dot(separation, relativeVelocity);
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
                return 0f;

            float collisionTime = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            if (collisionTime < 0f || collisionTime > timeToCollisionHorizon)
                return 0f;
            return 1f - collisionTime / timeToCollisionHorizon;
        }

        float StaticRayDanger(Vector2 direction, float distance)
        {
            if (staticObstacleMask.value == 0 || distance <= 0f)
                return 0f;
            int hitCount = Physics2D.Raycast(
                _formationCollider.bounds.center,
                direction,
                _staticFilter,
                _staticHits,
                distance);
            if (hitCount == 0)
                return 0f;
            float hitDistance = NearestPositiveHitDistance(hitCount);
            return float.IsPositiveInfinity(hitDistance)
                ? 0f
                : 1f - Mathf.Clamp01(hitDistance / distance);
        }

        bool StaticBoxBlocked(Vector2 direction, float distance)
        {
            if (staticObstacleMask.value == 0 || distance <= 0f)
                return false;

            Vector3 scale = _formationCollider.transform.lossyScale;
            Vector2 size = Vector2.Scale(
                _formationCollider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            int hitCount = Physics2D.BoxCast(
                _formationCollider.bounds.center,
                size,
                _formationCollider.transform.eulerAngles.z,
                direction,
                _staticFilter,
                _staticHits,
                distance);
            for (int i = 0; i < hitCount; i++)
            {
                if (_staticHits[i].distance > 0.001f)
                    return true;

                Collider2D obstacle = _staticHits[i].collider;
                if (obstacle == null)
                    continue;
                ColliderDistance2D overlap = _formationCollider.Distance(obstacle);
                if (!overlap.isOverlapped)
                    return true;

                Vector2 away = _formationCollider.bounds.center - obstacle.bounds.center;
                if (away.sqrMagnitude <= 0.0001f || Vector2.Dot(direction, away) <= 0f)
                    return true;
            }
            return false;
        }

        float NearestPositiveHitDistance(int hitCount)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                if (_staticHits[i].distance > 0.001f)
                    nearest = Mathf.Min(nearest, _staticHits[i].distance);
            }
            return nearest;
        }

        BoxCollider2D FindFormationCollider()
        {
            if (_unit == null)
                return null;
            Rigidbody2D unitBody = _unit.GetComponent<Rigidbody2D>();
            foreach (var collider in _unit.GetComponentsInChildren<BoxCollider2D>())
            {
                if (collider.attachedRigidbody == unitBody)
                    return collider;
            }
            return null;
        }

        static Vector2[] CreateDirections()
        {
            var directions = new Vector2[DirectionCount];
            for (int i = 0; i < DirectionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / DirectionCount;
                directions[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            }
            return directions;
        }

        UnitSteeringMode GetMode()
        {
            if (_unit.State == UnitState.Fleeing) return UnitSteeringMode.Flee;
            if (_orders.Target != null) return UnitSteeringMode.Charge;
            if (_unit.State == UnitState.Moving && _orders.IsMoving) return UnitSteeringMode.Move;
            return UnitSteeringMode.Idle;
        }

        Vector2 SeekPosition(Vector2 target)
        {
            if (_pathfinder == null)
                return target - (Vector2)AgentTransform.position;

            if (_pathInvalid || Time.time >= _nextRepath || Vector2.Distance(target, _pathTarget) > pathPointRadius)
            {
                _path = _pathfinder.GetPath(target);
                _pathIndex = _path != null && _path.Count > 1 ? 1 : 0;
                _pathTarget = target;
                _nextRepath = Time.time + repathInterval;
                _pathInvalid = false;
            }

            if (_path == null || _path.Count == 0)
                return target - (Vector2)AgentTransform.position;

            while (_pathIndex < _path.Count - 1 && ReachedOrPassedPathPoint(_pathIndex))
                _pathIndex++;
            return _path[_pathIndex] - (Vector2)AgentTransform.position;
        }

        bool ReachedOrPassedPathPoint(int index)
        {
            Vector2 position = AgentTransform.position;
            Vector2 point = _path[index];
            if (Vector2.Distance(position, point) <= pathPointRadius)
                return true;

            Vector2 nextSegment = _path[index + 1] - point;
            return nextSegment.sqrMagnitude > 0.0001f
                && Vector2.Dot(position - point, nextSegment) >= 0f;
        }

        Vector2 FleeDirection()
        {
            Army army = GetComponentInParent<Army>();
            if (army == null || army.Enemies == null) return Vector2.zero;

            IUnit nearest = null;
            float bestDistance = fleeRadius;
            foreach (var enemy in army.Enemies)
            {
                if (enemy == null) continue;
                float distance = Vector2.Distance(AgentTransform.position, enemy.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest == null ? Vector2.zero : (Vector2)AgentTransform.position - (Vector2)nearest.transform.position;
        }
    }
}
