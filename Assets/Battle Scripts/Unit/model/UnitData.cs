using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class UnitData : MonoBehaviour, IUnitData
    {
        IUnit _unit;
        Formation.IFormationData _formation;
        public RegimentStats UnitStats => _unit.Stats;

        public IUnit Unit => _unit;

        public Formation.IFormationData Formation => _formation;

        public void Setup(IUnit unit)
        {
            _unit = unit;
            _formation = unit.GetComponent<Formation.IFormationData>();
        }
    }
}