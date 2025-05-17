using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    public class SimpleMoves : IPossibleMoves
    {
        [SerializeField]
        Army army;
        public override List<UnitOrder> GetMoves(IUnit unit)
        {
            return ChargeNearbyEnemies(unit);
        }
        [SerializeField, Range(3, 10)]
        float _maxDistance = 4;
        List<UnitOrder> ChargeNearbyEnemies(IUnit unit){
            List<UnitOrder> chase = new();
            foreach (var enemy in army.Enemies)
            {
                if(Vector2.Distance(enemy.transform.position, unit.transform.position) <_maxDistance)
                chase.Add(new(enemy.transform));
            }
            return chase;
        }
        private void OnDrawGizmos()
        {
            if(army == null) return;
            foreach (var unit in army.Units)
            {
                Gizmos.DrawWireSphere(unit.transform.position, _maxDistance);
            }
        }
    }
}