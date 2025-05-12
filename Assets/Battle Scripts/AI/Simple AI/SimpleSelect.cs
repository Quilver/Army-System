using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    public class SimpleSelect : ISelectUnits
    {
        [SerializeField]
        ArmyData _armyToControl;
        public override List<IUnit> GetUnitsToOrder
        {
            get
            {
                return _armyToControl.Units;
            }
        }
    }
}