using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    [RequireComponent(typeof(Rigidbody2D))]
    class ModelFollower : MonoBehaviour, IModelFormation
    {
        [SerializeField]
        Transform Unit;
        Rigidbody2D _unitBody;
        Rigidbody2D UnitBody
        {
            get
            {
                if (_unitBody == null) _unitBody = Unit.GetComponent<Rigidbody2D>();
                return _unitBody;
            }
        }
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
        }
        public void SetPosition(Vector3 position, Vector2 offsetPos, bool warpToPoint = false)
        {
            this.offsetPosition = offsetPos;
        }
        [SerializeField] Vector2 _pos;
        private void FixedUpdate()
        {
            if(Unit == null)
            {
                enabled = false;
                return;
            }
            _pos = UnitPosition;
            MoveToPosition();
            //transform.position = UnitPosition;
        }
        
        [SerializeField] bool _affectsUnit;
        void MoveToPosition()
        {
            Vector2 direction = UnitPosition - (Vector2)transform.position;
            SpringVelocity(direction);
        }

        void MatchUnitVelocity(Vector2 directionToUnit)
        {

        }
        [Header("Spring Variables"), Range(30, 300)] 
        public float _springForceK = 100;
        [SerializeField, Range(0, 10)] float _damping = 1;
        void SpringVelocity(Vector2 directionToUnit)
        {
            Vector2 force = Time.deltaTime * _springForceK * directionToUnit - Body.linearVelocity * _damping;
            Body.AddForce(force);
            UnitBody.AddForceAtPosition(-force, transform.position);
        }
    }
}