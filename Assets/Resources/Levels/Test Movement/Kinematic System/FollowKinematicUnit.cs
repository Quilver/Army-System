using MovementSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    [RequireComponent(typeof(Rigidbody2D))]
    class FollowKinematicUnit : MonoBehaviour, IModelFormation
    {
        [SerializeField]
        Transform Unit;
        KinematicMovement _mover;
        Rigidbody2D _body;
        Rigidbody2D Body
        {
            get
            {
                if (_body == null)_body = GetComponent<Rigidbody2D>();
                return _body;
            }
        }
        [SerializeField, Range(0.05f, 2)] float softFollowRadius;
        [SerializeField]
        Vector2 offsetPosition;
        Vector2 UnitPosition=>Unit.position + Unit.right*offsetPosition.x + Unit.up * offsetPosition.y;
        Vector2 previousVelocity;
        public void SetUp(Rigidbody2D[] pins, Vector2 offsetPos, Transform unit)
        {
            this.offsetPosition = offsetPos;
            this.Unit = unit;
            _mover=Unit.GetComponentInChildren<KinematicMovement>();
            _mover.UpdatePos += UpdateUnitPos;
            _mover.SetDirection += SpringVelocity;
            Unit.GetComponent<IUnit>().UnitDestroyed += () => Destroy(this);

        }
        private void OnDestroy()
        {
            _mover.UpdatePos -= UpdateUnitPos;
            _mover.SetDirection -= SpringVelocity;
        }
        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false)
        {
            this.offsetPosition = offsetPos;
            if(warpToPoint ) Unit.position = position;
        }
        [SerializeField] Vector2 _pos;
        
        [Header("Spring Variables"), Range(30, 300)] 
        public float _springForceK = 100;
        [SerializeField, Range(0, 1)] float _damping = 1;
        void UpdateUnitPos()
        {
            //if (Vector2.Distance(UnitPosition, transform.position) > softFollowRadius)
            _mover.UpdatePosAndFacing((Vector2)transform.position - UnitPosition, Body.velocity.normalized, Body.velocity);
        }
        Vector2 forwardForce, springForce; float distance;
        void SpringVelocity(Vector2 dirToMove)
        {
            forwardForce = dirToMove;
            Body.AddForce(dirToMove);
            Vector2 directionToUnit = UnitPosition - (Vector2)transform.position;
            distance = directionToUnit.magnitude;
            if(directionToUnit.magnitude < softFollowRadius) springForce = Vector2.zero;
            else
                SpringForce(directionToUnit);
        }
        void SpringForce(Vector2 directionToUnit)
        {
            Vector2 force = _springForceK * directionToUnit;// - Body.velocity * _damping;
            if (directionToUnit.sqrMagnitude < softFollowRadius * softFollowRadius)
                force -= force * _damping;
            springForce = force;
            Body.AddForce(Time.deltaTime * force);
        }
        void OnDrawGizmos()
        {
            if(Unit == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, forwardForce);
            Gizmos.color= Color.blue;
            Gizmos.DrawRay(transform.position, springForce * Time.deltaTime);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, UnitPosition);
            Gizmos.DrawSphere(UnitPosition, 0.2f);
        }
    }
}