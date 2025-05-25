using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Query
{
    [System.Serializable]
    public class EnemiesInRadius : IQuery
    {
        public override bool Query()=> map.relevantEnemies.Count > 0;
    }
}