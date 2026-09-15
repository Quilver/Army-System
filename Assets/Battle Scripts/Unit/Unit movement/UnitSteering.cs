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
        [SerializeField, Min(0.05f)] float repathInterval = 0.5f;
        [SerializeField, Min(0.01f)] float pathPointRadius = 0.3f;
        [SerializeField, Min(0.01f)] float velocitySmoothing = 0.3f;
        [SerializeField, Min(0f)] float fleeRadius = 12f;
        [SerializeField, Min(0.1f)] float smallAdjustmentDistance = 1f;

        IUnit _unit;
        IMoveOrders _orders;
        IPathfinder _pathfinder;
        UnitCohesion _cohesion;
        Transform _unitTransform;
        List<Vector2> _path;
        int _pathIndex;
        Vector2 _pathTarget;
        Vector2 _velocity;
        float _nextRepath;
        bool _pathInvalid = true;
        bool _smallAdjustment;

        public UnitSteeringMode Mode { get; private set; }
        public Vector2 DesiredVelocity => _velocity;
        public Vector2 DesiredFacing { get; private set; }
        public bool AllowsFreeMovement => Mode == UnitSteeringMode.Flee
            || (Mode == UnitSteeringMode.Move && (_smallAdjustment || (_unit != null && _unit.InMelee)));
        Transform AgentTransform => _unitTransform == null ? transform : _unitTransform;
        public float MaxSpeed => _unit == null || _unit.Stats == null ? 0f : _unit.Stats.Movement / 2f;

        void Awake()
        {
            _unit = GetComponentInParent<IUnit>();
            _orders = GetComponentInParent<IMoveOrders>();
            _unitTransform = _unit == null ? transform : _unit.transform;
            _pathfinder = GetComponentInChildren<IPathfinder>();
            _cohesion = GetComponentInParent<UnitCohesion>();
        }

        void OnEnable()
        {
            if (_orders == null) return;
            _orders.moving += InvalidatePath;
            _orders.pursuing += InvalidatePursuit;
        }

        void OnDisable()
        {
            if (_orders == null) return;
            _orders.moving -= InvalidatePath;
            _orders.pursuing -= InvalidatePursuit;
        }

        void InvalidatePath(Vector2 target)
        {
            _pathInvalid = true;
            _smallAdjustment = Vector2.Distance(AgentTransform.position, target) <= smallAdjustmentDistance;
        }

        void InvalidatePursuit(Transform _)
        {
            _pathInvalid = true;
            _smallAdjustment = false;
        }

        public void Tick(float deltaTime)
        {
            if (_unit == null || _orders == null)
            {
                _velocity = Vector2.zero;
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
            desired = Vector2.ClampMagnitude(desired, 1f) * speed;
            _velocity = completingFinalFacing
                ? Vector2.zero
                : Vector2.MoveTowards(_velocity, desired, speed * deltaTime / velocitySmoothing);

            if (completingFinalFacing)
                DesiredFacing = _orders.FaceTowards.Value - (Vector2)AgentTransform.position;
            else if (desired.sqrMagnitude > 0.0001f)
                DesiredFacing = desired;
            else
                DesiredFacing = AgentTransform.up;
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
