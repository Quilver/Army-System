using AISystem.MapEvaluator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class FlankAndCharge : UnitBehaviour
    {
        [SerializeField, Range(0, 1)] float FlankCosAngle = 0.4f;
        UnitOrder order = new();
        protected override void MakeUnitMove()
        {
            var lastOrder = order;
            if (map.relevantEnemies.Count == 0) return;
            if (ViableRearCharge(order))
            {
                //order = new(order.target);
                //order.MakeOrder(unit);
            }
            else MoveIntoPosition();


            if (!order.ValidOrder || lastOrder == order) return;
            order.MakeOrder(unit);
        }
        bool ViableRearCharge(UnitOrder order)
        {
            if (order.target != null)
            {
                var direction = order.target.transform.position - unit.transform.position;
                return Vector2.Dot(direction.normalized, order.target.transform.up) > FlankCosAngle;
            }
            var target = GetClosestUnit();
            if (target == null)
                return false;

            this.order = new(target.transform);
            
            return true;            
        }
        void MoveIntoPosition()
        {
            float bestScore = float.MinValue;
            for (int i = 0; i < map.closePositions.Count; i++)
            {
                if (map.FlankEnemiesScore[i].score <= 0) continue;
                if(!IEvaluate.CanWalkToTarget(unit.transform.position, map.closePositions[i]))
                    continue;
                var distanceScore = 1 - Vector2.Distance(unit.transform.position, map.closePositions[i]) / 20;
                float flankScore = distanceScore * map.FlankEnemiesScore[i].score;
                if (flankScore < bestScore) continue;
                bestScore = flankScore;
                order = new(map.closePositions[i]);
                //order.target = map.FlankEnemiesScore[i].closestUnit.transform;
            }
        }
        IUnit GetClosestUnit()
        {
            float distance = float.PositiveInfinity;
            IUnit closestEnemy = null;
            foreach (var enemy in map.relevantEnemies)
            {
                var direction = enemy.transform.position - unit.transform.position;
                if (Vector2.Dot(direction.normalized, enemy.transform.up) < FlankCosAngle)
                    continue;
                float pathDistance = UnitDistance(enemy.transform);
                if (distance < pathDistance) continue;
                distance = pathDistance;
                closestEnemy = enemy;

            }
            return closestEnemy;
        }
        float UnitDistance(Transform enemy)
        {

            NavMeshPath path = new();
            if (!NavMesh.CalculatePath(unit.transform.position, enemy.position, MovementSystem.NavPathfinder.MASK, path))
                return float.PositiveInfinity;
            float distance = 0;
            for (int i = 1; i < path.corners.Length; i++)
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return distance;
        }
    }
}
