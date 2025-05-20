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
        #endregion
        #region Properties
        [Header("Properties")]
        [SerializeField, Range(0.05f, 2)] float softFollowRadius;
        [Range(30, 300)]
        public float _springForceK = 100;
        [SerializeField, Range(0, 1)] float _damping = 1;
        public AnimationCurve knockBackForceCurve;
        #endregion
        #region Setup
        public void SetUp(Rigidbody2D[] pins, Vector2 offsetPos, Transform unit)
        {
            this.Unit = unit;
            _mover = Unit.GetComponentInChildren<KinematicMovement>(); _moveData = Unit.GetComponentInChildren<KinematicMoveData>();
            _moveData.UpdatePos += UpdateUnitPos;
            _mover.SetDirection += SpringVelocity;
            
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
            _moveData.UpdatePos -= UpdateUnitPos;
            _mover.SetDirection -= SpringVelocity;
        }
        #endregion
        #region Position Calculation
        [SerializeField]
        Vector2 offsetPosition; 
        [SerializeField]float offsetForwardAngle;
        Vector2 UnitPosition => Unit.position + Unit.right * offsetPosition.x + Unit.up * offsetPosition.y;
        Vector2 previousVelocity;
        void UpdateUnitPos()
        {
            //if (Vector2.Distance(UnitPosition, transform.position) > softFollowRadius)
            Vector2 delta = (Vector2)transform.position - UnitPosition;
            _moveData.UpdatePosAndFacing(Vector2.ClampMagnitude(delta, 25), Facing, Body.velocity);
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
        Vector2 forwardForce, springForce; float distance;
        void SpringVelocity(Vector2 dirToMove)
        {
            Vector2 directionToUnit = UnitPosition - (Vector2)transform.position;
            distance = directionToUnit.magnitude;
            //Move along steering behaviour
            forwardForce = MoveVelocity(dirToMove, distance);
            Body.AddForce(dirToMove);
            //Move back to unit
            springForce = SpringForce(directionToUnit, distance);
            Body.AddForce(Time.fixedDeltaTime * springForce);
        }
        Vector2 MoveVelocity(Vector2 dirToMove, float distanceFromFormation)
        {
            if (distanceFromFormation < softFollowRadius)
                return dirToMove;
            else return Vector2.zero;
        }
        Vector2 SpringForce(Vector2 directionToUnit, float distance)
        {
            Vector2 force = _springForceK * directionToUnit;// - Body.velocity * _damping;
            if (distance < softFollowRadius)
                force -= force * _damping;
            return force;
        }
        IEnumerator KnockBackReaction(Vector2 hitDirection, Vector2 constantForceDirection, Vector2 inputDirection)
        {
            yield return null;
        }
        #endregion
        #region Gizmo, Debugging data
        [SerializeField] bool DrawGizmo;
        void OnDrawGizmos()
        {
            if(!DrawGizmo) return;
            if (Unit == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, forwardForce);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, springForce);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, UnitPosition);
            Gizmos.DrawSphere(UnitPosition, 0.2f);
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, 2*Facing);
            Gizmos.DrawSphere(transform.position + 2* (Vector3)Facing, 0.2f);
        }
        #endregion        
    }
}