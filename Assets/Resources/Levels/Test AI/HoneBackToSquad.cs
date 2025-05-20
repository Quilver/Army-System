using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    //Pursue nearest enemies in range
    public class HoneBackToSquad : IPossibleMoves
    {
        public override List<UnitOrder> GenerateOrders(IUnit unit)
        {
            return HoneBack(unit);
        }

        public override float ScoreOrder(IUnit unit, UnitOrder order)
        {
            //if (Vector2.Distance(unit.transform.position, Squad.Center) <= Squad.SoftRadius)return 0;
            if(order.position == null) return 0;
            float pointDistance = Squad.HardRadius - Vector2.Distance(Squad.Center, order.position.Value);
            float distFromCenter = Vector2.Distance(unit.transform.position, Squad.Center);
            float distanceFromCenterMod;
            if (distFromCenter > Squad.HardRadius) distanceFromCenterMod = 1;
            else if (distFromCenter > Squad.SoftRadius) distanceFromCenterMod = 0.5f;
            else distanceFromCenterMod = 0.1f;
            float proximityMod = Mathf.Clamp(distanceFromCenterMod, Squad.SoftRadius, Squad.HardRadius);
            return pointDistance * distanceFromCenterMod / proximityMod;
        }

        List<UnitOrder> HoneBack(IUnit unit)
        {
            List<UnitOrder> homePoints = new();
            if (Vector2.Distance(unit.transform.position, Squad.Center) <= Squad.SoftRadius) return homePoints;
            for (int i = 0; i < 5; i++)
            {
                UnitOrder order = new(Squad.Center + Squad.SoftRadius * Random.insideUnitCircle);
                homePoints.Add(order);
            }
            return homePoints;
        }
    }
}