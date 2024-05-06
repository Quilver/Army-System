using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Army : MonoBehaviour {
    public List<Army> enemies, allies;
    public Controller controller;
    public enum Controller
    {
        Player,
        Computer,
        AltPlayer
    }
    public HashSet<UnitInterface> EnemyUnits { 
        get
        {
            HashSet<UnitInterface> result = new();
            foreach (var enemy in enemies)
            {
                result.UnionWith(enemy._units);
            }
            return result;
        }
    }
    private void Awake()
    {
        Initiliase();
        
    }
    
    HashSet<UnitInterface> _units;
    void Initiliase()
    {
        _units= new HashSet<UnitInterface>();
        foreach (var unit in GetComponentsInChildren<UnitInterface>())
        {
            _units.Add(unit);
        }
        foreach (var unit in _units)
        {
            Battle.Instance.unitArmy.Add(unit, this);
        }
        Notifications.Died += UnitDied;
    }
    public UnitInterface GetUnit(int index)
    {
        var units = _units.ToList();
        index = index % units.Count;
        if (index < 0) index += units.Count;
        return units[index];
    }
    void UnitDied(UnitInterface unit)
    {
        if(_units.Contains(unit))
            _units.Remove(unit);
        if (_units.Count == 0)
            Notifications.ArmyDestroyed(this);
    }
}
