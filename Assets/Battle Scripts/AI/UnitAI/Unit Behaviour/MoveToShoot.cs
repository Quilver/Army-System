using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class MoveToPositionAndShoot : UnitBehaviour
    {
        RangedWeapon weapon;
        Shooting.IRangedTargeter rangedTargeter;
        public override void Setup(IUnit unit, ISquad squad, InfluenceMap map)
        {
            if (unit == null) throw new System.Exception("Move to shoot ai is missing unit");
            base.Setup(unit, squad, map);
            rangedTargeter = unit.GetComponentInChildren<Shooting.IRangedTargeter>();
            weapon = unit.GetComponentInChildren<RangedWeapon>();
            if (rangedTargeter == null) throw new System.Exception($"{unit.name} is missing a ranged targeter for AI in squad: {squad.name}");

        }
        UnitOrder lastOrder;
        protected override void MakeUnitMove()
        {
            //If it can shoot its target do nothing
            if (rangedTargeter.Target != null) return;
            //If there is a valid target it can shoot at, switch target
            else if (rangedTargeter.ValidTargets.Count > 0)
                (new UnitOrder(rangedTargeter.ValidTargets[Random.Range(0, rangedTargeter.ValidTargets.Count)], false)).MakeOrder(unit);
            //If it needs to move and is not moving to shoot at a target, do that
            else if (!lastOrder.position.HasValue || (map.relevantEnemies.Count > 0 && !InSweetSpot(lastOrder.position.Value)))
            {
                float distance = 1000;
                float distance2 = 1000;
                UnitOrder secondChoice = new();
                for (int i = 0; i < map.ShootingScores.Count; i++)
                {
                    var distanceFromPoint = Vector2.Distance(unit.transform.position, map.closePositions[i]);
                    if (distance < distanceFromPoint)
                        continue;
                    if (InSweetSpot(map.closePositions[i]))
                    {
                        distance = distanceFromPoint;
                        lastOrder = new(map.closePositions[i]);
                        lastOrder.faceDirection = map.ShootingScores[i].closestUnit.transform.position;
                    }
                    else if(distanceFromPoint < distance2 && distanceFromPoint < weapon.Range)
                    {
                        distance2 = distanceFromPoint;
                    }
                }
                if (!lastOrder.ValidOrder && secondChoice.ValidOrder) lastOrder = secondChoice;
                lastOrder.MakeOrder(unit);
                return;
            }
            //Else do nothing
            return;
        }
        bool InSweetSpot(Vector2 order)
        {
            Vector2 position = order;
            float min = weapon.Range / 2; float max = weapon.Range * 0.75f;
            foreach (var enemy in map.relevantEnemies)
            {
                float distance = Vector2.Distance(position, enemy.transform.position);
                if (distance > min && distance < max) return true;
            }
            return false;
        }
        bool InSweetSpot(Vector2 order, float score)
        {
            float min = weapon.Range / 2; float max = weapon.Range * 0.75f;
            float dist = 1 / score;
            return dist > min && dist < max;

        }


    }
}