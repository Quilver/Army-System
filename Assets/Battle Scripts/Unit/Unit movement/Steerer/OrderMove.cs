using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class OrderMove : MonoBehaviour, IMoveOrders
    {
        #region Values      
        [SerializeField]
        bool _orderedMove;
        [SerializeField]
        Vector2 _position;
        [SerializeField]
        Transform _target;
        public bool IsMoving => _orderedMove;

        public Vector2 TargetPosition { 
            get { 
                if (_target == null)
                    return _position; 
                return _target.position;
            }
        }

        public Transform Target => _target;
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
        #region Events
        public event Action finishedMovement;
        public event Action<Vector2> moving;
        public event Action<Transform> pursuing;
        #endregion
        public void MoveTo(Vector2 position)
        {
            _orderedMove = true;
            _position = position;
            _target = null;
            moving?.Invoke(position);
            GetComponent<IUnit>().State =UnitState.Moving;
        }
        public void MoveTo(Transform target)
        {
            _orderedMove = true;
            _target = target;
            pursuing?.Invoke(target);
            GetComponent<IUnit>().State = UnitState.Moving;
        }
        void Update()
        {
            if(ReachedPoint)
            {
                _orderedMove = false;
                finishedMovement?.Invoke();
                GetComponent<IUnit>().State = UnitState.Idle;
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