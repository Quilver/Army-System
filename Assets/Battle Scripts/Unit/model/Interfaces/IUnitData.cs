using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ModelComponents
{
    public interface IUnitData
    {
        public StatSystem.RegimentStats UnitStats { get; }
        public UnitTemplate Unit { get; }
        public UnitFormation Formation { get; }
        public void Setup(UnitTemplate unit);
    }
}