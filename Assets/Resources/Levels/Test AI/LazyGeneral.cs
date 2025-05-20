using Formation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AISystem
{
    [RequireComponent(typeof(ISquad))]
    public class LazyGeneral : MonoBehaviour
    {
        ISquad squad;
        IPossibleMoves _possibleMoves;
        [SerializeField]
        IPossibleMoves pursue, returnToCenter, flank;
        List<UnitOrder> Orders
        {
            get
            {
                List<UnitOrder> orders = new();
                foreach (var point in squad.SamplePoints)
                    orders.Add(new UnitOrder(point + squad.Center));
                foreach (var enemy in squad.EnemiesInRadius)
                {
                    orders.Add(new UnitOrder(enemy.transform));
                    orders.Add(new UnitOrder(enemy.transform, squad.GetUnitsToOrder[0].transform.position, enemy.transform.position, false));
                }

                return orders;
            }
        }
        void Start ()
        {
            squad = GetComponent<ISquad>();
            _possibleMoves = GetComponent<IPossibleMoves>();
            foreach (var unit in squad.GetUnitsToOrder)
                StartCoroutine(UnitAI(unit));
        }
        IEnumerator UnitAI(IUnit unit)
        {
            UnitOrder lastOrder = new();
            yield return new WaitForSeconds(Random.Range(0, 0.3f));
            while (unit != null)
            {
                UnitOrder newOrder;
                newOrder = UtilityBehaviourSelector(unit, lastOrder);
                if (newOrder.ValidOrder && newOrder != lastOrder)
                    newOrder.MakeOrder(unit);
                lastOrder = newOrder;
                yield return new WaitForSeconds(0.4f);
            }
        }
        public List<UnitOrder> moves;
        public List<float> orderScores;
        UnitOrder UtilityBehaviourSelector(IUnit unit, UnitOrder lastOrder)
        {
            moves = Orders;
            orderScores = _possibleMoves.ScoreOrders(unit, Orders);
            var maxIndex = orderScores.IndexOf(orderScores.Max());
            UnitOrder bestOrder = moves[maxIndex];
            if (moves.Contains(lastOrder))
            {
                int indexOfLastOrder = moves.IndexOf(lastOrder);
                if (orderScores[indexOfLastOrder] > orderScores[maxIndex] / 2)
                    return lastOrder;
            }
            return bestOrder;
        }
        [SerializeField]
        bool DrawGizmos;
        private void OnDrawGizmos()
        {
            if (!DrawGizmos) return;
            if(squad == null) squad = GetComponent<ISquad>(); if(_possibleMoves==null)_possibleMoves = GetComponent<IPossibleMoves>();
            var orders = Orders;
            if (squad.GetUnitsToOrder.Count == 0 || squad.GetUnitsToOrder[0] == null) return;
            Gizmos.color = Color.blue;
            if(pursue != null) DrawMoveScores(pursue, orders, new Vector2(0.5f, 0.5f));
            Gizmos.color = Color.red;
            if (returnToCenter != null) DrawMoveScores(returnToCenter, orders, new Vector2(-0.5f, -0.5f));
            Gizmos.color = Color.green;
            if (flank != null) DrawMoveScores(flank, orders, new Vector2(0.5f, -0.5f));
        }
        void DrawMoveScores(IPossibleMoves scorer, List<UnitOrder> moves, Vector2 offset)
        {
            foreach(UnitOrder order in moves)
            {
                if(!order.ValidOrder) continue;
                Vector2 pos = (order.position.HasValue)? order.position.Value : order.target.position;
                pos += offset;
                float score = scorer.ScoreOrder(squad.GetUnitsToOrder[0], order);
                if (score > 0)
                    Gizmos.DrawSphere(pos, score);
            }
        }
    }
}