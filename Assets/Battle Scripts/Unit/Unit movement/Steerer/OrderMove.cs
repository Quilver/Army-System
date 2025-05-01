using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SteeringSystem
{
    class OrderMove : MonoBehaviour, IMoveOrders
    {
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

        public event IMoveOrders.Moving moving;
        public event IMoveOrders.Pursuing pursuing;

        public void MoveTo(Vector2 position)
        {
            _orderedMove = true;
            _position = position;
            moving?.Invoke(position);
            
        }

        public void MoveTo(Transform target)
        {
            _orderedMove = true;
            _target = target;
            pursuing?.Invoke(target);
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