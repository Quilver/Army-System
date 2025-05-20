using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    //Pursue nearest enemies in range
    public class MoveToFlank : IPossibleMoves
    {
        public override List<UnitOrder> GenerateOrders(IUnit unit)
        {
            return FlankNearbyEnemies(unit);
        }

        public override float ScoreOrder(IUnit unit, UnitOrder order)
        {
            if(Squad.EnemiesInRadius.Count == 0)return 0;
            if (order.position.HasValue)
                return ScorePosition(unit, order.position.Value);
            else
                return ScoreChargingEnemy(unit, order.target);
        }
        [SerializeField, Range(-1, 1)] float maxAngleForRear;
        [SerializeField, Range(2, 7)] float minRangeToConsiderPoint;
        float ScoreChargingEnemy(IUnit unit, Transform enemy)
        {
            float cosAngle = Vector2.Dot(enemy.up, (enemy.position - unit.transform.position).normalized);
            //You're charging in the enemies front
            if(cosAngle < maxAngleForRear)
                return 0;
            return FlankScore(enemy.up, (enemy.position - unit.transform.position));
        } 
        float ScorePosition(IUnit unit, Vector2 position)
        {
            var distanceFromPoint = Vector2.Distance(unit.transform.position, position) ;
            if (distanceFromPoint < minRangeToConsiderPoint) return 0;
            float score = 0;
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                float distance = Vector2.Distance(enemy.transform.position, position);
                if (distance > 10) continue;
                float scoreForEnemy = FlankScore(enemy.transform.up, ((Vector2)enemy.transform.position - position));
                if (scoreForEnemy == 0) return 0;
                score += scoreForEnemy;//  Mathf.RoundToInt(scoreForEnemy / distance);
            }
            float distanceMod = Mathf.InverseLerp(Squad.HardRadius, 0, distanceFromPoint);
            return score * distanceMod;
        }
        List<UnitOrder> FlankNearbyEnemies(IUnit unit)
        {
            List<UnitOrder> chase = new();
            foreach (var enemy in Squad.EnemiesInRadius)
            {
                Vector3 pos = enemy.transform.position;
                float dist = 10;
                chase.Add(new(pos + dist * enemy.transform.right));
                chase.Add(new(pos - dist * enemy.transform.right));
                chase.Add(new(pos - dist * enemy.transform.up));
            }
            return chase;
        }
        float FlankScore(Vector2 AttackDir, Vector2 targetDir)
        {
            float cosAngle = Vector2.Dot(targetDir.normalized, AttackDir.normalized);
            //You're charging in the enemies front
            if (cosAngle < maxAngleForRear)
                return 0;
            return Mathf.InverseLerp(maxAngleForRear, 1, cosAngle);
        }
    }
}
