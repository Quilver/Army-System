using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArmyData : MonoBehaviour
{
    [SerializeField]
    List<IUnit> _units;
    public Army.Controller controller;

    public List<IUnit> Units
    {
        get
        {
            if(transform==null)return null;
            if (_units == null || _units.Count != transform.childCount) _units = GetComponentsInChildren<IUnit>().ToList();
            return _units;
        }
    }
    [SerializeField]
    ArmyData _enemy;
    public List<IUnit> Enemies
    {
        get=> _enemy.Units;
    }
}
