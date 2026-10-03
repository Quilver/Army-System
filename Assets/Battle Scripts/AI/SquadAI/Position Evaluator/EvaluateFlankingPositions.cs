using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;
namespace AISystem.MapEvaluator
{
    [System.Serializable]
    public class EvaluateFlankingPositions : IEvaluatePosTarget
    {
        [SerializeField, Range(4, 20)] float relevantFlankRange = 13;
        public override void EvaluateOrders(InfluenceMap map)
        {
            map.FlankEnemiesScore = ScoreOrders(map.closePositions, map);
        }
        public override ScoreAndUnit EvaluateOrder(Vector2 order, InfluenceMap map)
        {
            if (map.relevantEnemies.Count == 0) return new();
            return ScorePosition(order, map.relevantEnemies);
            
        }
        ScoreAndUnit ScorePosition(Vector2 position, List<IUnit> enemies)
        {
            float score = 0;
            IUnit unit = null;
            foreach (var enemy in enemies)
            {
                if (!CanWalkToTarget(position, enemy.transform)) continue;
                Vector2 attackDir = (Vector2)enemy.transform.position - position;
                if (attackDir.magnitude > relevantFlankRange) continue;
                float scoreForEnemy = FlankScore(enemy.transform.up, attackDir);
                if (scoreForEnemy == 0) return new();
                if(score < scoreForEnemy)unit = enemy;
                score = Mathf.Max(score, scoreForEnemy);
            }
            return new(unit, score);
        }
        [SerializeField, Range(0, 1)] float minAngle, maxAngle;
        float FlankScore(Vector2 enemyDir, Vector2 AttackDir)
        {
            float cosAngle = Vector2.Dot(enemyDir.normalized, AttackDir.normalized);
            float minCosAngle = Mathf.Lerp(minAngle, maxAngle, AttackDir.magnitude/relevantFlankRange);
            if(cosAngle > minCosAngle) return cosAngle;
            return 0;
        }
    }
}

