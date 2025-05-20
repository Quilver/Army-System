using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    //Pursue nearest enemies in range
    public class PursueEnemiesInRange : IPossibleMoves
    {
        public override List<UnitOrder> GenerateOrders(IUnit unit)
        {
            return ChargeNearbyEnemies(unit);
        }

        public override float ScoreOrder(IUnit unit, UnitOrder order)
        {
            if (order.target == null) return 0;
            else if (Vector2.Distance(order.target.position, Squad.Center) < Squad.HardRadius) return 1;
            else return 0;
        }

        List<UnitOrder> ChargeNearbyEnemies(IUnit unit){
            List<UnitOrder> chase = new();
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                chase.Add(new(enemy.transform));
            }
            return chase;
        }
    }
}