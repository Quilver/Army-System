using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class ModelFacing : MonoBehaviour
    {
        IUnitData _data;
        Rigidbody2D _unitBody, _modelBody;
        // Start is called before the first frame update
        void Start()
        {
            _data = GetComponent<IUnitData>();
            _modelBody = GetComponent<Rigidbody2D>();
            _unitBody = _data.Unit.GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        void Update()
        {
            if(_data.Unit.State == UnitState.Moving)
                transform.up = _modelBody.velocity.normalized;
            else
                transform.up = _unitBody.transform.up;
        }
    }
}