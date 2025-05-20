using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    public class Patrol : IPossibleMoves
    {
        [SerializeField, Range(5, 30)] float _minNextPointDistance;

        Vector2 _nextPoint;
        public override List<UnitOrder> GenerateOrders(IUnit unit)
        {
            if (Vector2.Distance(unit.transform.position, _nextPoint) < 2 || Vector2.Distance(unit.transform.position, Squad.Center) > Squad.SoftRadius)
                _nextPoint = GenerateNextPoint(unit);
            return new List<UnitOrder>{ new UnitOrder(_nextPoint) };
        }

        public override float ScoreOrder(IUnit unit, UnitOrder order)
        {
            if (!order.position.HasValue) return 0;
            Vector2 dirToPoint = (order.position.Value - (Vector2)unit.transform.position);
            float outsideSoftRadiusMod = (Vector2.Distance(order.position.Value, Squad.Center) > Squad.SoftRadius) ? 0.5f : 1;
            if (dirToPoint.magnitude > _minNextPointDistance)
                return outsideSoftRadiusMod;
            float cosAngle = Vector2.Dot(unit.transform.up, dirToPoint.normalized);
            if (cosAngle > 0.6f && dirToPoint.magnitude > 4)
                return 0.7f * outsideSoftRadiusMod;
            return 0;

        }

        Vector2 GenerateNextPoint(IUnit unit)
        {
            return Squad.Center + Squad.SoftRadius * Random.insideUnitCircle;
        }
    }
}