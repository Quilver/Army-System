using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class ModelFacing : MonoBehaviour
    {
        IUnitData _data;
        ITakeDamage _damage;
        Rigidbody2D _unitBody, _modelBody;
        // Start is called before the first frame update
        void Start()
        {
            _data = GetComponent<IUnitData>();
            _modelBody = GetComponent<Rigidbody2D>();
            _unitBody = _data.Unit.GetComponent<Rigidbody2D>();
            _damage = GetComponent<ITakeDamage>();
            
        }

        void FixedUpdate()
        {
            if (_unitBody == null || _modelBody == null) return;

            _modelBody.angularVelocity = 0f;
            if (Mathf.Abs(Mathf.DeltaAngle(_modelBody.rotation, _unitBody.rotation)) > 0.1f)
                _modelBody.MoveRotation(_unitBody.rotation);
        }
        Vector2 ShiftDirection(Vector2 forward)=> Vector3.MoveTowards(transform.up, forward, Time.deltaTime * 4);
    }
}
