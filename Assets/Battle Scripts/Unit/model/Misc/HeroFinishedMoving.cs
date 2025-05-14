using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    //makes the unit faces in the same direction at the hero
    public class HeroFinishedMoving : MonoBehaviour
    {
        IUnit _unit;
        void Start()
        {
            _unit = GetComponent<IUnitData>().Unit;
        }

        void Update()
        {
            if(_unit == null) return;
            _unit.transform.rotation = transform.rotation;
        }
    }
}