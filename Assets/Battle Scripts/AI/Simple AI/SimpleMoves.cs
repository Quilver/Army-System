using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    public class SimpleMoves : IPossibleMoves
    {
        [SerializeField]
        ArmyData army;
        public override List<UnitOrder> GetMoves(IUnit unit)
        {
            return ChargeNearbyEnemies(unit);
        }
        List<UnitOrder> ChargeNearbyEnemies(IUnit unit){
            List<UnitOrder> chase = new();
            foreach (var enemy in army.Enemies)
            {
                chase.Add(new(enemy.transform));
            }
            return chase;
        } 
    }
}