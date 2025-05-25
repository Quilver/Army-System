using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UIElements;
namespace AISystem.MapEvaluator
{
    [System.Serializable]
    public class EvaluateShootPositions : IEvaluatePosTarget
    {
        public override void EvaluateOrders(InfluenceMap map)
        {
            map.ShootingScores = ScoreOrders(map.closePositions, map);
        }
        protected override List<ScoreAndUnit> ScoreOrders(List<Vector2> orders, InfluenceMap map)
        {
            return base.ScoreOrders(orders, map);
        }
        public override ScoreAndUnit EvaluateOrder(Vector2 order, InfluenceMap map)
        {
            float max = 0;
            IUnit unit = null;
            foreach (var enemy in map.relevantEnemies)
            {
                float score = 1f/Vector2.Distance(order, enemy.transform.position);
                if (score < max || !CanSeeTarget(order, enemy.transform)) continue;
                unit = enemy;
                max = 1/Vector2.Distance(enemy.transform.position, order);
            }
            return new(unit, Mathf.Clamp01(max));
        }
    }
}