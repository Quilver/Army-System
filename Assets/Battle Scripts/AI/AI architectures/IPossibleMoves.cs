using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem
{
    [RequireComponent(typeof(ISquad))]
    public abstract class IPossibleMoves : MonoBehaviour
    {
        ISquad _squad;
        public ISquad Squad
        {
            get
            {
                if (_squad == null)_squad = GetComponent<ISquad>();
                return _squad;  
            }
        }
        public abstract List<UnitOrder> GenerateOrders(IUnit unit);
        public virtual List<float> ScoreOrders(IUnit unit, List<UnitOrder> orders)
        {
            List<float> scores = new List<float>();
            foreach (var order in orders)
                scores.Add(ScoreOrder(unit, order));
            return scores;
        }

        public abstract float ScoreOrder(IUnit unit, UnitOrder order);
    }
    
}