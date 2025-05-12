using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MovementSystem
{
    class OrderMove : IMoveOrders
    {
        #region Values      
        [SerializeField]
        bool _orderedMove;
        [SerializeField]
        Vector2 _position;
        [SerializeField]
        Transform _target;
        public override bool IsMoving => _orderedMove;

        public override Vector2 TargetPosition { 
            get { 
                if (_target == null)
                    return _position; 
                return _target.position;
            }
        }
        IUnit _unit;
        IUnit Unit
        {
            get
            {
                if (_unit == null) _unit = GetComponent<IUnit>();
                return _unit;
            }
        }
        public override Transform Target => _target;
        [SerializeField, Range(0.1f, 1)]
        float _maxReachedRange;
        bool ReachedPoint
        {
            get
            {
                if (!IsMoving) return true;
                else if (Vector2.Distance(transform.position, TargetPosition) < _maxReachedRange) return true;
                return false;
            }
        }
        #endregion
        public override void MoveTo(Vector2 position)
        {
            if(Unit.State == UnitState.Fleeing) return; 
            if(Unit.State == UnitState.Deployment)
            {
                DeploymentMove(position);
                return;
            }
            _orderedMove = true;
            _position = position;
            _target = null;
            InvokeMove(position);
            Unit.State =UnitState.Moving;
        }
        [SerializeField]
        bool TEST;
        void DeploymentMove(Vector2 position)
        {
            if (!Physics2D.OverlapPoint(position, 1 << 12)) return;
            transform.position = position;
            FinishedMovement();
        }
        public override void MoveTo(Transform target)
        {
            if (Unit.State == UnitState.Deployment || Unit.State == UnitState.Fleeing)
            {
                return;
            }
            _orderedMove = true;
            _target = target;
            InvokePursuit(target);
            Unit.State = UnitState.Moving;
        }
        void Update()
        {
            if(Unit.State == UnitState.Moving && ReachedPoint)
            {
                _orderedMove = false;
                FinishedMovement();
                Unit.State = UnitState.Idle;
            }
        }

        [SerializeField]
        bool DrawGizmo; 
        private void OnDrawGizmos()
        {
            if (!DrawGizmo) return;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(TargetPosition, 0.3f);
        }
    }
}