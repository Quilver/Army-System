using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class AttackClosestOpponent : UnitBehaviour
    {
        UnitOrder LastTarget;
        [SerializeField, Range(1, 2)]
        float AdditionalDistanceNeededToSwitch = 1.3f;
        protected override void MakeUnitMove()
        {
            var newOrder = GenerateOrder();
            if (newOrder.ValidOrder && newOrder != LastTarget)
            {
                newOrder.MakeOrder(unit);
                LastTarget = newOrder;
            }
        }
        UnitOrder GenerateOrder()
        {
            UnitOrder consideredOrder = new(GetClosestUnit().transform);
            if (LastTarget.target == null || !LastTarget.ValidOrder) return consideredOrder;
            else if(consideredOrder == LastTarget)
                return consideredOrder;
            else if (UnitDistance(LastTarget.target) > UnitDistance(consideredOrder.target) * AdditionalDistanceNeededToSwitch)
                return new();

            return LastTarget;
        }
        IUnit GetClosestUnit()
        {
            float distance = float.PositiveInfinity;
            IUnit closestEnemy = null;
            foreach (var enemy in map.relevantEnemies)
            {
                if (enemy == null) continue;
                float pathDistance = UnitDistance(enemy.transform);
                if(distance < pathDistance)continue;
                distance = pathDistance;
                closestEnemy = enemy;
                
            }
            return closestEnemy;
        }
        float UnitDistance(Transform enemy)
        {

            NavMeshPath path=new();
            if (!NavMesh.CalculatePath(unit.transform.position, enemy.position, MovementSystem.NavPathfinder.MASK, path))
                return float.PositiveInfinity;
            float distance = 0;
            for (int i = 1; i < path.corners.Length; i++)
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return distance;
        }

    }
}

