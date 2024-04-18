using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
[System.Serializable]
public struct UnitStatsR
{
    [SerializeField, Range(1,10)]
    int _speed;
    [SerializeField, Range(1, 20)]
    int _defence;
    [Range(1,10)]
    public int Power;
    public float Speed
    {
        get
        {
            return _speed / 2.5f;
        }
    }
    public float Defence
    {
        get
        {
            return _defence;
        }
    }
    public UnitStatsR(int speed, int power, int defence)
    {
        this._speed = speed;
        this.Power = power;
        this._defence = defence;
    }
    public override string ToString()
    {
        return "\nSpeed:" + _speed + "\nPower:" + Power + "\nDefence:" + Defence;
    }
}

public enum UnitState
{
    Idle,
    Moving,
    Shooting,
    Fighting,
    Fleeing
}
public enum UnitMoveState 
{
    Walk,
    Strafe,
    Wheel,
    Idle
}