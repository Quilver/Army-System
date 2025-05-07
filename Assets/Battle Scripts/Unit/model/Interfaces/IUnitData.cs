using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IUnitData
    {
        public StatSystem.RegimentStats UnitStats { get; }
        public IUnit Unit { get; }
        public Formation.IFormationData Formation { get; }
        public void Setup(IUnit unit);
    }
}