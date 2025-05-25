using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Behaviour
{
    //Pursue nearest enemies in range
    public class HoneBackToSquad : UnitBehaviour
    {
        UnitOrder LastOrder;
        protected override void MakeUnitMove()
        {
            var newOrder = GenerateOrder();
            if (newOrder.ValidOrder && newOrder != LastOrder)
            {
                newOrder.MakeOrder(unit);
                LastOrder = newOrder;
            }
        }
        UnitOrder GenerateOrder()
        {
            return new(squad.Center);
        }
    }
}