using MovementSystem;
using UnityEngine;

namespace ModelComponents
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FollowKinematicUnit : MonoBehaviour, IModelFormation
    {
        [SerializeField] Transform Unit, Formation;
        [SerializeField, Range(0.1f, 2f)] float arriveTime = 0.25f;
        [SerializeField, Range(1f, 3f)] float recoverySpeedMultiplier = 2f;
        [SerializeField, Min(0.01f)] float velocityBlendTime = 0.08f;
        [SerializeField, Min(0.001f)] float slotDeadband = 0.03f;

        Rigidbody2D _body;
        FrictionJoint2D _friction;
        UnitBody _unitBody;
        UnitCohesion _cohesion;
        Vector2 _offsetPosition;
        float _offsetForwardAngle;
        Vector2 _commandedVelocity;
        Vector2 _velocityBlend;
        float _maxSpeed;
        bool _detached;

        Rigidbody2D Body => _body != null ? _body : _body = GetComponent<Rigidbody2D>();
        FrictionJoint2D Friction => _friction != null ? _friction : _friction = GetComponentInChildren<FrictionJoint2D>();
        Vector2 UnitPosition => Formation.position + Formation.right * _offsetPosition.x + Formation.up * _offsetPosition.y;
        public Vector2 SlotDisplacement => (Vector2)transform.position - UnitPosition;

        public void SetUp(Transform formation, Vector2 offsetPos, Transform unit)
        {
            Unit = unit;
            Formation = formation;
            _unitBody = Unit.GetComponentInChildren<UnitBody>();
            _cohesion = Unit.GetComponentInChildren<UnitCohesion>();
            _unitBody.MoveUpdate += UpdateForces;

            float maxSpeed = GetComponent<UnitData>().UnitStats.Movement;
            _maxSpeed = maxSpeed / 2f;
            Friction.maxForce = Body.mass * maxSpeed * GetComponent<UnitData>().UnitStats.MoveForce;
            Unit.GetComponent<IUnit>().UnitDestroyed += OnUnitDestroyed;
            SetPosition(formation.position, offsetPos);
            RegisterDisplacement();
        }

        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false)
        {
            _offsetPosition = offsetPos;
            _offsetForwardAngle = offsetPos == Vector2.zero
                ? 0f
                : Vector2.SignedAngle(offsetPos.normalized, Vector2.up);
            if (warpToPoint)
                transform.position = UnitPosition;
            RegisterDisplacement();
        }

        void FixedUpdate()
        {
            RegisterDisplacement();
        }

        void RegisterDisplacement()
        {
            if (_cohesion != null && Unit != null && Formation != null)
                _cohesion.SetSlotDisplacement(this, SlotDisplacement);
        }

        void OnUnitDestroyed()
        {
            DetachFromFormation();
            Destroy(this);
        }

        public void DetachFromFormation()
        {
            if (_detached)
                return;
            _detached = true;
            enabled = false;

            if (_unitBody != null)
                _unitBody.MoveUpdate -= UpdateForces;
            if (_cohesion != null)
                _cohesion.RemoveSlot(this);
            if (Unit != null)
                Unit.GetComponent<IUnit>().UnitDestroyed -= OnUnitDestroyed;
            if (_friction != null && _friction.connectedBody != null)
                _friction.connectedBody.linearVelocity = Vector2.zero;
        }

        void OnDestroy() => DetachFromFormation();

        void UpdateForces(Vector2 direction, float maxSpeed)
        {
            Vector2 toSlot = UnitPosition - (Vector2)transform.position;
            Vector2 matchVelocity = MatchUnitVelocityAtPosition();
            float displacement = toSlot.magnitude;
            float followerMaxSpeed = _maxSpeed * recoverySpeedMultiplier;
            Vector2 desired = Vector2.ClampMagnitude(
                matchVelocity + Vector2.ClampMagnitude(toSlot / arriveTime, followerMaxSpeed),
                followerMaxSpeed);

            _commandedVelocity = Vector2.SmoothDamp(
                _commandedVelocity,
                desired,
                ref _velocityBlend,
                velocityBlendTime,
                Mathf.Max(maxSpeed, followerMaxSpeed),
                Time.fixedDeltaTime);
            if (displacement <= slotDeadband && matchVelocity.sqrMagnitude <= 0.0001f)
            {
                _commandedVelocity = Vector2.zero;
                _velocityBlend = Vector2.zero;
            }
            if (Friction.connectedBody != null)
                Friction.connectedBody.linearVelocity = _commandedVelocity;
        }

        Vector2 MatchUnitVelocityAtPosition()
        {
            if (_unitBody == null)
                return Vector2.zero;

            Vector2 offset = UnitPosition - (Vector2)Unit.position;
            float angularVelocity = _unitBody.CurrentAngularVelocity * Mathf.Deg2Rad;
            Vector2 tangentialVelocity = new Vector2(-offset.y, offset.x) * angularVelocity;
            return _unitBody.CurrentVelocity + tangentialVelocity;
        }

        Vector2 Facing
        {
            get
            {
                Vector2 fromUnit = transform.position - Formation.position;
                if (fromUnit.sqrMagnitude < 0.0025f)
                    return Vector2.up;
                return Quaternion.AngleAxis(_offsetForwardAngle, Vector3.forward) * fromUnit.normalized;
            }
        }


        void OnDrawGizmosSelected()
        {
            if (Unit == null || Formation == null) return;
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(UnitPosition, 0.1f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, Facing);
        }
    }
}
