using UnityEngine;

namespace MovementSystem
{
    public enum UnitMovementRegime
    {
        Forward,
        Wheel,
        Turn,
        Free
    }

    [RequireComponent(typeof(UnitSteering), typeof(UnitSeparation))]
    public sealed class UnitBody : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float maxTurnRate = 180f;
        [SerializeField, Range(0f, 45f)] float forwardAngle = 10f;
        [SerializeField, Range(15f, 180f)] float turnInPlaceAngle = 60f;
        [SerializeField, Range(0f, 15f)] float turnHysteresis = 5f;
        [SerializeField, Min(0.1f)] float minimumTurnRadius = 0.5f;
        UnitSteering steering;
        UnitCohesion cohesion;
        Formation.IShape _formation;
        Rigidbody2D _body;
        IUnit _unit;
        [SerializeField]
        bool _canMove =false;
        Vector2 _currentSeparationVelocity;
        Vector2 _readySeparationVelocity;
        // Corrections generated this step are consumed next step by every unit.
        float _separationFixedTime = float.NegativeInfinity;

        public event System.Action<Vector2, float> MoveUpdate;
        public Vector2 CurrentVelocity { get; private set; }
        public UnitMovementRegime MovementRegime { get; private set; }

        void Awake()
        {
            _unit = GetComponentInParent<IUnit>();
            _body = _unit == null ? null : _unit.GetComponent<Rigidbody2D>();
            Debug.Assert(_body != null, $"UnitBody requires a Rigidbody2D on the parent with an IUnit component.");
            if (_body == null)
                _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            if (GetComponent<UnitSeparation>() == null)
                gameObject.AddComponent<UnitSeparation>();
            steering ??= GetComponentInParent<UnitSteering>();
            cohesion ??= GetComponentInParent<UnitCohesion>();
            _formation = _unit == null ? null : _unit.GetComponentInChildren<Formation.IShape>();
            _canMove = _unit == null || _unit.State != UnitState.Deployment;
        }

        void OnValidate()
        {
            turnInPlaceAngle = Mathf.Max(turnInPlaceAngle, forwardAngle + turnHysteresis);
        }

        void Start()
        {
            if (_unit != null)
            {
                _unit.StateChanged += OnStateChanged;
                _unit.UnitDestroyed += OnUnitDestroyed;
            }

            if (!_canMove && Battle.Instance != null)
                Battle.Instance.Deploy += EnableMovement;
        }

        void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.StateChanged -= OnStateChanged;
                _unit.UnitDestroyed -= OnUnitDestroyed;
            }
            if (Battle.Instance != null)
                Battle.Instance.Deploy -= EnableMovement;
        }

        void EnableMovement() => _canMove = true;
        void OnStateChanged(UnitState state) => _canMove = state != UnitState.Deployment;
        void OnUnitDestroyed() => enabled = false;

        void FixedUpdate()
        {
            if (!_canMove)
            {
                MoveUpdate?.Invoke(Vector2.zero, steering.MaxSpeed);
                ClearSeparationVelocity();
                CurrentVelocity = Vector2.zero;
                return;
            }

            steering.Tick(Time.fixedDeltaTime);
            Vector2 desiredVelocity = steering.DesiredVelocity;
            Vector2 facing = steering.DesiredFacing;
            float facingError = facing.sqrMagnitude > 0.0001f
                ? Mathf.Abs(Vector2.SignedAngle(_body.transform.up, facing))
                : 0f;
            MovementRegime = SelectMovementRegime(desiredVelocity.magnitude, facingError);

            Vector2 locomotionVelocity = MovementRegime switch
            {
                UnitMovementRegime.Free => desiredVelocity,
                UnitMovementRegime.Turn => Vector2.zero,
                _ => (Vector2)_body.transform.up * desiredVelocity.magnitude
            };
            Vector2 velocity = locomotionVelocity
                + (cohesion == null ? Vector2.zero : cohesion.GiveGround)
                + ConsumeSeparationVelocity();
            velocity = Vector2.ClampMagnitude(velocity, steering.MaxSpeed);

            _body.MovePosition(_body.position + velocity * Time.fixedDeltaTime);
            CurrentVelocity = velocity;
            if (facing.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
                float turnRate = MovementRegime == UnitMovementRegime.Free
                    ? maxTurnRate
                    : FormationLimitedTurnRate(MovementRegime == UnitMovementRegime.Turn
                        ? steering.MaxSpeed
                        : desiredVelocity.magnitude);
                float angle = Mathf.MoveTowardsAngle(_body.rotation, targetAngle, turnRate * Time.fixedDeltaTime);
                _body.MoveRotation(angle);
            }

            float moveSpeed = velocity.magnitude;
            MoveUpdate?.Invoke(moveSpeed > 0.0001f ? velocity / moveSpeed : Vector2.zero, steering.MaxSpeed);
        }

        UnitMovementRegime SelectMovementRegime(float desiredSpeed, float facingError)
        {
            if (steering.AllowsFreeMovement)
                return UnitMovementRegime.Free;
            if (desiredSpeed <= 0.0001f && facingError > 0.1f)
                return UnitMovementRegime.Turn;
            if (MovementRegime == UnitMovementRegime.Turn
                && facingError > turnInPlaceAngle - turnHysteresis)
                return UnitMovementRegime.Turn;
            if (facingError >= turnInPlaceAngle)
                return UnitMovementRegime.Turn;
            if (MovementRegime == UnitMovementRegime.Wheel
                && facingError > Mathf.Max(0f, forwardAngle - turnHysteresis))
                return UnitMovementRegime.Wheel;
            return facingError > forwardAngle
                ? UnitMovementRegime.Wheel
                : UnitMovementRegime.Forward;
        }

        float FormationLimitedTurnRate(float speed)
        {
            float formationRadius = minimumTurnRadius;
            if (_formation != null)
                formationRadius = Mathf.Max(formationRadius, _formation.SizeOfFormation.magnitude * 0.5f);
            return Mathf.Min(maxTurnRate, speed / formationRadius * Mathf.Rad2Deg);
        }

        public void AddSeparationVelocity(Vector2 velocity)
        {
            if (!Mathf.Approximately(_separationFixedTime, Time.fixedTime))
            {
                _readySeparationVelocity += _currentSeparationVelocity;
                _currentSeparationVelocity = Vector2.zero;
                _separationFixedTime = Time.fixedTime;
            }
            _currentSeparationVelocity += velocity;
        }

        Vector2 ConsumeSeparationVelocity()
        {
            Vector2 velocity = _readySeparationVelocity;
            _readySeparationVelocity = Vector2.zero;

            if (_separationFixedTime < Time.fixedTime)
            {
                velocity += _currentSeparationVelocity;
                _currentSeparationVelocity = Vector2.zero;
            }
            return velocity;
        }

        void ClearSeparationVelocity()
        {
            _currentSeparationVelocity = Vector2.zero;
            _readySeparationVelocity = Vector2.zero;
            _separationFixedTime = float.NegativeInfinity;
        }
    }
}
