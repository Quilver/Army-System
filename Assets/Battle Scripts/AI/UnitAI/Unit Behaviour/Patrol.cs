using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class Patrol : UnitBehaviour
    {
        UnitOrder LastPosition;
        [SerializeField, Range(1, 5)]
        float ReachedDistance;
        [SerializeField, Range(1, 5)]
        float timeToChangePoint = 3;
        float recordLastOrderTime = 0;
        protected override void MakeUnitMove()
        {
            UnitOrder newOrder = new(SelectRandomPoint());//GenerateOrder();
            if (newOrder != LastPosition) {
                recordLastOrderTime = Time.time;
                newOrder.MakeOrder(unit);
                LastPosition = newOrder;
            } 
        }
        UnitOrder GenerateOrder()
        {

            if (LastPosition.target != null || !LastPosition.ValidOrder) return new(SelectRandomPoint());
            else if(Time.time - recordLastOrderTime > timeToChangePoint) return new(SelectRandomPoint());
            else if (Vector2.Distance(unit.transform.position, LastPosition.position.Value) < ReachedDistance)
                return new(SelectRandomPoint());

            return LastPosition;
        }
        Vector2 SelectRandomPoint()
        {
            return map.closePositions[Random.Range(0, map.closePositions.Count)];
        }
    }
}