using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace MovementSystem
{
    public class KinematicMovement : MonoBehaviour, IMoveUnit
    {
        Transform UnitB;
        IGetSteerDirection _direction;
        IGetSteerDirection GetSteerDirection
        {
            get
            {
                if (_direction == null) _direction = GetComponent<IGetSteerDirection>();
                return _direction;
            }
        }
        [SerializeField]
        IMovementData _moveData;
        void Start()
        {
            UnitB = transform.parent;
            if(GetComponentInParent<IUnit>().State == UnitState.Deployment)
            {
                enabled=false;
                Battle.Instance.Deploy += () => enabled = true;
            }
            GetComponentInParent<IUnit>().UnitDestroyed += () => Destroy(gameObject);
            _direction = GetComponent<IGetSteerDirection>();
            _moveData = GetComponent<IMovementData>();
        }
        [SerializeField] Vector2 _velocity;
        void FixedUpdate()
        {
            MoveUnit(Vector2.zero, Vector2.up);
        }
        public event System.Action<Vector2, float> SetDirection;
        
        public float _averageSpeed;
        public void MoveUnit(Vector2 direction, Vector2 facing)
        {
            //Set desired facing
            Vector3 faceDir = (Vector3)GetSteerDirection.GetFacingDirection();
            if (faceDir != Vector3.zero)
                UnitB.up = Vector2.MoveTowards(UnitB.up, faceDir, 40 * Time.fixedDeltaTime);// faceDir;
            //Set movement
            _velocity = GetSteerDirection.GetDirection();
            SetDirection?.Invoke(_velocity, _moveData.MaxSpeed);
        }
        [SerializeField]
        bool DrawGizmo;
        void OnDrawGizmos()
        {
            //transform.rotation= Quaternion.LookRotation(Vector3.forward,lookDir)*Quaternion.Euler(0,0,90);
            if(!DrawGizmo || UnitB ==  null) return;
            Gizmos.color= Color.white;
            Gizmos.DrawRay(UnitB.position, UnitB.up.normalized * 3);
            Gizmos.DrawSphere(UnitB.position + UnitB.up.normalized * 3, 0.1f);

        }
    }
}