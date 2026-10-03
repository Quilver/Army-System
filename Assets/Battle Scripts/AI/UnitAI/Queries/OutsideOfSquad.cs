using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Query
{
    public class OutsideOfSquad : IQuery
    {
        IGenerator squadShape;
        public override void Setup(IUnit unit, ISquad squad, InfluenceMap map)
        {
            base.Setup(unit, squad, map);
            squadShape = squad.GetComponent<IGenerator>();
        }
        public override bool Query() => squadShape.PointRange(unit.transform.position) == IGenerator.Ranges.Out;
    }
}