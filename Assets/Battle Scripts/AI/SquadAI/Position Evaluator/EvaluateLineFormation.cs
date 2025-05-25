using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.MapEvaluator
{
    [System.Serializable]
    public class EvaluateLineFormation : IEvaluatePosition
    {
        public override void EvaluateOrders(InfluenceMap map)=>
            map.DefaultScores = ScoreOrders(map.closePositions, map);
        public override float EvaluateOrder(Vector2 position, InfluenceMap map)
        {
            Vector2 directionFromSquad = position - squad.Center;
            float distanceFromSquad = directionFromSquad.magnitude;
            if (distanceFromSquad > 10) return 0.2f;
            else if (distanceFromSquad < 3) return 1;
            float offsetFromForwardDir = Vector2.Dot(directionFromSquad.normalized, squad.Direction);
            return 1 - Mathf.Abs(offsetFromForwardDir);
        }

        
    }
}