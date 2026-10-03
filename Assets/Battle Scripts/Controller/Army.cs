using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Army : MonoBehaviour {
    public Color ArmyColour;
    [SerializeField]
    List<IUnit> _units;
    
    public List<IUnit> Units
    {
        get
        {
            if (transform == null) return null;
            if (_units == null || _units.Count != transform.childCount) _units = GetComponentsInChildren<IUnit>().ToList();
            else _units = _units.Where(x => x != null).ToList();
            return _units;
        }
    }
    [SerializeField]
    Army _enemy;
    public List<IUnit> Enemies
    {
        get => _enemy.Units;
    }
}
