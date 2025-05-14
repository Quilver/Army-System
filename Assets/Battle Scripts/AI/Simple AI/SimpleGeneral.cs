using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AISystem.Simple
{
    public class SimpleGeneral : MonoBehaviour
    {
        ISelectUnits _selectUnits;
        IPossibleMoves _possibleMoves;
        [SerializeField]
        bool _waitForDeployment = true;
        Dictionary<IUnit, UnitOrder> _lastOrder;
        // Start is called before the first frame update
        void Awake()
        {
            _selectUnits=GetComponent<ISelectUnits>();
            _possibleMoves=GetComponent<IPossibleMoves>();
            _lastOrder = new();
            if ( _waitForDeployment )enabled=false;
        }
        public List<IUnit> unit;
        public List<UnitOrder> order;
        // Update is called once per frame
        void Update()
        {
            unit = _selectUnits.GetUnitsToOrder;
            
            foreach (var unit in _selectUnits.GetUnitsToOrder)
            {
                OrderUnit(unit, _possibleMoves.GetMoves(unit));
            }
        }
        void OrderUnit(IUnit unit, List<UnitOrder> orders)
        {
            order=orders;
            if (orders == null || orders.Count == 0) return;
            if (!_lastOrder.ContainsKey(unit)) _lastOrder.Add(unit, new());
            if (orders.Contains(_lastOrder[unit]))return;
            Debug.Log($"{unit.gameObject.name}: is switching order to {orders[0].ToString()}");
            orders[0].MakeOrder(unit);
            _lastOrder[unit] = orders[0];

        }
    }
}
