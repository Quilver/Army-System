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

        // Update is called once per frame
        void Update()
        {
            if(_data.Unit.State == UnitState.Moving && !_data.Unit.InMelee)
                transform.up = _modelBody.linearVelocity.normalized;
            else
                transform.up = _unitBody.transform.up;
        }
        Vector2 ShiftDirection(Vector2 forward)=> Vector3.MoveTowards(transform.up, forward, Time.deltaTime * 4);
    }
}