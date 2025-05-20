using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    public class OneManSquad : ISquad
    {
        [SerializeField] IUnit _unit;
        public override List<IUnit> GetUnitsToOrder => new List<IUnit>{ _unit };

    }
}

