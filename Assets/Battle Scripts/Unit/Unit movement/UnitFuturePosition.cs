using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MovementSystem
{
    public class UnitFuturePosition : MonoBehaviour
    {
        Formation.IShape _Shape;
        IMovementData _MovementData;
        void Start()
        {
            _Shape=transform.parent.parent.GetComponentInChildren<Formation.IShape>();
            _MovementData=GetComponentInParent<IMovementData>();
            
        }
        void Update()
        {
            transform.position = _MovementData.FuturePosition;
            transform.localScale = _Shape.SizeOfFormation;
        }
    }
}
