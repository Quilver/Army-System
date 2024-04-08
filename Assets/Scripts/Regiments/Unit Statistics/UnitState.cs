using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct UnitStatsR
{
    [SerializeField, Range(1,10)]
    int _speed;
    [Range(1,10)]
    public int AttackSpeed, Power, Defence;
    public float Speed
    {
        get
        {
            return _speed / 2.5f;
        }
    }
    public UnitStatsR(int speed, int power, int defence, int AttackSpeed)
    {
        this.AttackSpeed = AttackSpeed;
        this._speed = speed;
        this.Power = power;
        this.Defence = defence;
    }
    public override string ToString()
    {
        return "Stats->Sp:" + Speed + ", Pwr:" + Power + ", D:" + Defence;
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