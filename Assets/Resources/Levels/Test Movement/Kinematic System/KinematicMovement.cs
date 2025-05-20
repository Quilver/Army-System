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
        Formation.IShape _shapeData;
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
            _shapeData = UnitB.GetComponentInChildren<Formation.IShape>();
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
        public event System.Action<Vector2> SetDirection;
        Vector2 _meanPos, _meanFacing, _meanVelocity;float _total;
        public void UpdatePosAndFacing(Vector2 pos, Vector2 facing, Vector2 velocity)
        {
            _total++;
            _meanPos += pos + (Vector2)transform.position;
            _meanFacing += facing;
            _meanVelocity += velocity;
        }
        public float _averageSpeed;
        Vector2 NewPosition => UnitB.position + (Vector3)_meanPos;
        Vector2 Offset => -UnitB.up * _shapeData.OffsetFromUnit.y - UnitB.right * _shapeData.OffsetFromUnit.x;
        public void MoveUnit(Vector2 direction, Vector2 facing)
        {
            //Set desired facing
            Vector3 faceDir = (Vector3)_direction.GetFacingDirection();
            if(faceDir!=Vector3.zero) 
                UnitB.up = faceDir;
            //Set movement
            _velocity = _direction.GetDirection();
            SetDirection?.Invoke(_velocity * _moveData.Force);
        }
        [SerializeField]
        bool DrawGizmo;
        void OnDrawGizmos()
        {
            //transform.rotation= Quaternion.LookRotation(Vector3.forward,lookDir)*Quaternion.Euler(0,0,90);
            if(!DrawGizmo || UnitB ==  null) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(UnitB.position, _meanFacing.normalized * 3);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(UnitB.position, _meanVelocity.normalized * 3);
            Gizmos.color= Color.white;
            Gizmos.DrawRay(UnitB.position, UnitB.up.normalized * 3);
            Gizmos.DrawSphere(UnitB.position + UnitB.up.normalized * 3, 0.1f);

        }
    }
}