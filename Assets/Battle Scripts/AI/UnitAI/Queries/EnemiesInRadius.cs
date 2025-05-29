using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AISystem.Query
{
    [System.Serializable]
    public class EnemiesInRadius : IQuery
    {
        [SerializeField]
        List<IUnit> enemies = new();
        public override bool Query() { 

            foreach(var enemy in map.closeEnemies)
                if(!enemies.Contains(enemy)) enemies.Add(enemy);
            var enemyList = enemies.ToList();
            foreach(var enemy in enemyList)
                if(enemy == null || !map.relevantEnemies.Contains(enemy))
                    enemies.Remove(enemy);
            return enemies.Count > 0;
        }
    }
}