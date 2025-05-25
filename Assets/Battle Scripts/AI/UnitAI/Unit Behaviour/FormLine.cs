using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AISystem.Behaviour
{
    [System.Serializable]
    public class FormLine : UnitBehaviour
    {
        UnitOrder order;
        protected override void MakeUnitMove()
        {
            var idealOrder = GetNearestPointInLine();

            
            if (!idealOrder.ValidOrder && Vector2.Distance(order.position.Value, idealOrder.position.Value) > 3)
                return;
            order = idealOrder;
            order.MakeOrder(unit);

        }
        
        UnitOrder GetNearestPointInLine()
        {
            var formup = GetIdealPosition();
            return new(formup, squad.Direction + formup);
        }
        Vector2 GetIdealPosition()
        {
            Vector2 pos = squad.Center;
            foreach (var unit in squad.GetUnitsToOrder)
            {
                if (this.unit == unit) continue;
                if (GetRelativePosition(unit))
                    pos -= Right;
                else
                    pos += Right;
            }
            return pos;
        }
        bool GetRelativePosition(IUnit otherUnit)//left is relatively closer
        {
            Vector2 right = Vector2.Perpendicular(squad.Direction);
            float unitD = 
                Vector2.Dot(right, ((Vector2)unit.transform.position - squad.Center).normalized);
            float unitD2 =
                Vector2.Dot(right, ((Vector2)otherUnit.transform.position - squad.Center).normalized);
            return unitD > unitD2;
        }
        [SerializeField, Range(2, 5)]
        float spacing = 3;
        Vector2 Right => -spacing * Vector2.Perpendicular(squad.Direction);

    }
}
