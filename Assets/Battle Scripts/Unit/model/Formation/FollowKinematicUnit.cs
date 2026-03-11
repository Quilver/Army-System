using MovementSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    [RequireComponent(typeof(Rigidbody2D))]
    class FollowKinematicUnit : MonoBehaviour, IModelFormation
    {
        #region References
        [Header("References")]
        [SerializeField]
        Transform Unit;
        KinematicMovement _mover; KinematicMoveData _moveData;
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if (_body == null)_body = GetComponent<Rigidbody2D>();
                return _body;
            }
        }
        [SerializeField]
        FrictionJoint2D _friction;
        FrictionJoint2D Friction
        {
            get
            {
                if (_friction == null) _friction = GetComponentInChildren<FrictionJoint2D>();
                return _friction;
            }
        }
        #endregion
        #region Properties
        [Header("Properties")]
        [SerializeField, Range(0.05f, 1)] float softFollowRadius;
        [SerializeField, Range(0.4f, 2)] float hardFollowRadius;
        [SerializeField, Range(0.4f, 2)] float secondsToMaxVelocity=1;
        [SerializeField, Range(1, 10)] float _springForceK = 2;
        #endregion
        #region Setup
        public void SetUp(Rigidbody2D[] pins, Vector2 offsetPos, Transform unit)
        {
            this.Unit = unit;
            _mover = Unit.GetComponentInChildren<KinematicMovement>(); _moveData = Unit.GetComponentInChildren<KinematicMoveData>();
            _moveData.UpdatePos += UpdateUnitPos;
            float maxSpeed = GetComponent<UnitData>().UnitStats.Movement;
            float mass = Body.mass;
            float force = mass * maxSpeed;
            Friction.maxForce = force * GetComponent<UnitData>().UnitStats.MoveForce;
            //_moveData.ApplyForce += (Vector2 force) => Body.AddForce(force);
            _mover.SetDirection += UpdateForces;
            
            Unit.GetComponent<IUnit>().UnitDestroyed += () => Destroy(this);
            SetPosition(unit.position, offsetPos);
        }
        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false)
        {
            this.offsetPosition = offsetPos;
            if(offsetPos == Vector2.zero) offsetForwardAngle = 0;
            else
                offsetForwardAngle = Vector2.SignedAngle(offsetPos.normalized,  Vector2.up);
            if (warpToPoint) Unit.position = position;
        }
        private void OnDestroy()
        {
            if (_moveData != null) _moveData.UpdatePos -= UpdateUnitPos;
            if (_mover != null) _mover.SetDirection -= UpdateForces;
        }
        #endregion
        #region Position Calculation
        [SerializeField]
        Vector2 offsetPosition; 
        [SerializeField]float offsetForwardAngle;
        Vector2 UnitPosition => Unit.position + Unit.right * offsetPosition.x + Unit.up * offsetPosition.y;
        void UpdateUnitPos()
        {
            //if (Vector2.Distance(UnitPosition, transform.position) > softFollowRadius)
            Vector2 delta = (Vector2)transform.position - UnitPosition;
            _moveData.UpdatePosAndFacing(Vector2.ClampMagnitude(delta, 25), Facing, Body.linearVelocity);
        }
        Vector2 Facing
        {
            get
            {
                Vector2 dirToUnit = transform.position - Unit.position;
                if(dirToUnit.magnitude < 0.05f)return Vector2.up;
                return Quaternion.AngleAxis(offsetForwardAngle, Vector3.forward) * dirToUnit.normalized;
            }
        }
        #endregion
        #region ForceManager
        public float LookAheadFactor = 0.3f;
        Vector2 IdealVec(Vector2 moveVec) => UnitPosition + LookAheadFactor * moveVec - (Vector2)transform.position;
        Vector2 FormationDirection => (UnitPosition - (Vector2)transform.position);
        Vector2 MoveVector(Vector2 direction) =>
            direction.magnitude * (FormationDirection + LookAheadFactor * direction).normalized;

        Vector2 MoveForce, idealPos; public float moveSpeed, moveDirectionMagnitude; Color moveType;
        Vector2 SteerVelocity(Vector2 moveDirection, float maxMoveForce, float maxSpringForce, float accelerationModifier)
        {
            //Gizmo debug
            moveSpeed= maxMoveForce; moveDirectionMagnitude=moveDirection.magnitude; 
            idealPos = UnitPosition + LookAheadFactor*(moveDirection);moveType = Color.green;

            //Code starts here
            MoveForce = IdealVec(moveDirection);
            float projectedDistanceFromPoint = (MoveForce-LookAheadFactor*Body.linearVelocity).magnitude;
            MoveForce = maxMoveForce * MoveForce.normalized;
            //Out of formation steering force
            
            if (projectedDistanceFromPoint > hardFollowRadius)
            {
                moveType = Color.red;
                float hardDeviations = projectedDistanceFromPoint/hardFollowRadius;
                MoveForce *= hardDeviations * _springForceK;
                return Vector2.MoveTowards(Body.linearVelocity, MoveForce, (maxMoveForce * hardDeviations * maxSpringForce) * accelerationModifier);
            }
            //Arrive steering force
            if (projectedDistanceFromPoint < softFollowRadius && softFollowRadius < Vector2.Distance(transform.position, idealPos))
            {
                moveType = Color.blue;
                float softDeviations = projectedDistanceFromPoint / softFollowRadius;
                MoveForce*= softDeviations;
                return Vector2.MoveTowards(Body.linearVelocity, MoveForce, (maxMoveForce) * accelerationModifier);
            }          
                
                
            
            //Default steering force
            return Vector2.MoveTowards(Body.linearVelocity, MoveForce, (maxMoveForce) * accelerationModifier);
        }
        
        Vector2 GetVelocity(Vector2 direction, float maxUnitSpeed, float maxReturnSpeedFactor, float accelerationModifier)
        {
            
            Vector2 MoveForce = IdealVec(direction);
            Vector2 returnDirection = (UnitPosition - (Vector2)transform.position);
            float projectedDistanceFromPoint = returnDirection.magnitude;
            float ratio = Mathf.Clamp01(projectedDistanceFromPoint / hardFollowRadius);
            return ((1-ratio) * MoveForce + ratio * maxReturnSpeedFactor * returnDirection.normalized) * maxUnitSpeed;
        }
        
        Vector2 GetFormationSteering(Vector2 unitDirection, float unitSpeed, float maxSpeedFactor)
        {
            const float MIN_DISTANCE = 0.005f;
            Vector2 toPosition = UnitPosition - (Vector2)transform.position;
            if(toPosition.magnitude < MIN_DISTANCE) return unitDirection* unitSpeed;
            Vector2 arrivalVelocity = toPosition * Mathf.Min(maxSpeedFactor, toPosition.magnitude);
            Vector2 desiredVelocity = unitDirection * unitSpeed + arrivalVelocity;
            return desiredVelocity;
        }
        void UpdateForces(Vector2 dirToMove, float MaxSpeed)
        {
            float acceleration = Time.fixedDeltaTime / secondsToMaxVelocity;
            Friction.connectedBody.linearVelocity =
                //SteerVelocity(MoveVector(dirToMove), MaxSpeed, _springForceK, acceleration);
                GetFormationSteering(dirToMove, MaxSpeed, 1.5f);

        }
        #endregion
        #if UNITY_EDITOR
        //Gizmo, Debugging data
        [SerializeField] bool DrawGizmo;
        public float speed;
        
        void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (Unit == null) return;
            DrawPositions();
            //
            DrawMove();

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, 2*Facing);
            Gizmos.DrawSphere(transform.position + 2* (Vector3)Facing, 0.2f);
        }
        void DrawMove()
        {
            Gizmos.color = Color.black;
            var futurePos = (Vector2)transform.position + Body.linearVelocity;
            Gizmos.DrawCube(futurePos, Vector3.one / 8);
            Gizmos.DrawWireSphere(futurePos, softFollowRadius);
            Gizmos.DrawWireSphere(futurePos, hardFollowRadius);
            

            speed = MoveForce.magnitude;
            Gizmos.color = moveType;
            Gizmos.DrawCube(idealPos, Vector3.one / 8);
            Gizmos.DrawRay(transform.position, MoveForce * LookAheadFactor);
        }
        void DrawPositions()
        {
            Gizmos.color = Color.white;
            //Gizmos.DrawLine(transform.position, UnitPosition);
            Gizmos.DrawSphere(UnitPosition, 0.1f);
            Gizmos.color = Color.black;
            Gizmos.DrawSphere((Vector2)transform.position + Body.linearVelocity, 0.1f);
            Gizmos.DrawLine(UnitPosition, (Vector2)transform.position + Body.linearVelocity);
        }
        #endif        
    }
}