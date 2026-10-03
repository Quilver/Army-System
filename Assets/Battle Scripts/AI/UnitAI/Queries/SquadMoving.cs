using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Query
{
    [System.Serializable]
    public class SquadMoving : IQuery
    {
        public override bool Query() => map.directionOfSquadTravel.magnitude > 0;
    }
}