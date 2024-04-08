using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Army : MonoBehaviour {
    [SerializeField]
    List<UnitR> units;
    public List<Army> enemies, allies, non_aligned;
    public Controller controller;
    public enum Controller
    {
        Player,
        Computer,
        AltPlayer
    }
    public enum Allaince
    {
        Ally,
        Enemy,
        Non_aligned
    }
    public List<UnitR> Enemies
    {
        get
        {
            List<UnitR> _enemies= new List<UnitR>();
            foreach(var enemy in enemies)
            {
                foreach (var unit in enemy.units)
                {
                    _enemies.Add(unit);
                }
            }
            return _enemies;
        }
    }
    private void Start()
    {
        foreach (var unit in units)
        {
            Master.Instance.unitArmy.Add(unit, this);
        }
    }
    public Army create( Controller type)
    {
        //assigns if the army is controlled by the player or computer
        controller = type;
        //alignments
        enemies = new List<Army>();
        allies = new List<Army>();
        non_aligned = new List<Army>();
        //
        units = new List<UnitR>();
        return this;
    }
    public void addArmy(Army army, Allaince side)
    {
        if(this != army && !allies.Contains(army) && !non_aligned.Contains(army) && !enemies.Contains(army))
        {
            switch (side)
            {
                case Allaince.Ally:
                    allies.Add(army);
                    break;
                case Allaince.Enemy:
                    enemies.Add(army);
                    break;
                case Allaince.Non_aligned:
                    non_aligned.Add(army);
                    break;
                default:
                    break;
            }
        }
    }
    public void addUnit(UnitR unit)
    {
        if (!units.Contains(unit))
        {
            units.Add(unit);
            Master.Instance.unitArmy.Add(unit, this);
        }
    }
    // Update is called once per frame
    void Update () {
		
	}
}
