using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Query
{
    [System.Serializable]
    public class EnemiesInShootingRadius : IQuery
    {
        public override bool Query() => map.shootableEnemies.Count > 0;
    }
}