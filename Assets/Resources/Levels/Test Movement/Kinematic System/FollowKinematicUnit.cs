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
        }
        [SerializeField] Vector2 _pos;
        
        [Header("Spring Variables"), Range(30, 300)] 
        public float _springForceK = 100;
        [SerializeField, Range(0, 10)] float _damping = 1;
        void UpdateUnitPos()
        {
            if (Vector2.Distance(UnitPosition, transform.position) > softFollowRadius)
                _mover.UpdatePosAndFacing((Vector2)transform.position - UnitPosition, Body.velocity.normalized, Body.velocity);
        }
        void SpringVelocity(Vector2 dirToMove)
        {
            Body.AddForce(dirToMove);
            Vector2 directionToUnit = UnitPosition - (Vector2)transform.position;
            Vector2 force = Time.deltaTime * _springForceK * directionToUnit - Body.velocity * _damping;
            Body.AddForce(force);
        }
        void OnDrawGizmos()
        {
            if(Unit == null) return;
            Gizmos.DrawLine(transform.position, UnitPosition);
            Gizmos.DrawSphere(UnitPosition, 0.2f);
        }
    }
}