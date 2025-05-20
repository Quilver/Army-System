using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    public class WholeArmySquad : ISquad
    {
        [SerializeField]
        Army _armyToControl;
        public override List<IUnit> GetUnitsToOrder
        {
            get
            {
                return _armyToControl.Units;
            }
        }
    }
}