using StatSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    class UnitData : MonoBehaviour, IUnitData
    {
        UnitTemplate _unit;
        UnitFormation _formation;
        public RegimentStats UnitStats => _unit.Stats;

        public UnitTemplate Unit => _unit;

        public UnitFormation Formation => _formation;

        public void Setup(UnitTemplate unit)
        {
            _unit = unit;
            _formation = unit.GetComponent<UnitFormation>();
        }
    }
}