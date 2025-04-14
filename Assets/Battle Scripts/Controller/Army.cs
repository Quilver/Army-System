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
    public HashSet<UnitTemplate> EnemyUnits { 
        get
        {
            HashSet<UnitTemplate> result = new();
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
    
    HashSet<UnitTemplate> _units;
    public List<UnitTemplate> Units
    {
        get { return _units.ToList(); }
    }
    void Initiliase()
    {
        _units= new HashSet<UnitTemplate>();
        foreach (var unit in GetComponentsInChildren<UnitTemplate>())
        {
            _units.Add(unit);
        }
        foreach (var unit in _units)
        {
            Battle.Instance.unitArmy.Add(unit, this);
        }
        Notifications.Died += UnitDied;
    }
    public void AddUnit(UnitTemplate unit)
    {
        _units.Add(unit);
        Battle.Instance.unitArmy.Add(unit, this);
    }
    public UnitTemplate GetUnit(int index)
    {
        var units = _units.ToList();
        index = index % units.Count;
        if (index < 0) index += units.Count;
        return units[index];
    }
    void UnitDied(UnitTemplate unit)
    {
        if(_units.Contains(unit)) 
            Debug.Log(gameObject.name + " lost: " + unit.Stats.name + ", Remain units:" + (_units.Count - 1));
        if(_units.Contains(unit))
            _units.Remove(unit);
        if (_units.Count == 0)
            Notifications.ArmyDestroyed(this);
    }
}
