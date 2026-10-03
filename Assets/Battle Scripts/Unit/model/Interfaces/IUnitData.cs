using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IUnitData
    {
        public StatSystem.Refactor.IUnitStatBlock UnitStats { get; }
        public IUnit Unit { get; }
        public Formation.IFormationData Formation { get; }
        public void Setup(IUnit unit);

    }
}