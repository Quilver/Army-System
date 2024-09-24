using System.Collections;
using System.Collections.Generic;
using UnitMovement;
using UnityEngine;
//Interface for the unit during the battles
public abstract class IUnit : MonoBehaviour
{
    //Stores the information around the unit
    public StatSystem.UnitStats Stats;
    //Represents if the unit is Idle, Moving, Fighting
    protected UnitState _state;
    public UnitState State {
        get { return _state; }
        set
        {
            _state = value;
        } 
    }
    //Holds the units current position information, and logic for moving 
    public IMovement Movement { get; protected set; }
    //Handles damage, melee and ranged combat
    public ICombat Combat { get; protected set; }
    private void Start()
    {
        Init();
        Notifications.StartFight += StartFight;
    }
    void StartFight(UnitBase unit1, UnitBase unit2)
    {
        if (unit1 != this && unit2 != this) return;
        if (!Battle.Instance.Enemies(unit1, unit2)) return;
        State = UnitState.Fighting;
    }
    public abstract void Init();
    public override string ToString()
    {
        return Stats.ToString();
    }
}
