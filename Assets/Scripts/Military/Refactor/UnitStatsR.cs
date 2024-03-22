using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct UnitStatsR 
{
    public int Speed, Power, Defence;
    public UnitStatsR(int speed, int power, int defence)
    {
        this.Speed = speed;
        this.Power = power;
        this.Defence = defence;
    }
    public override string ToString()
    {
        return "Stats->Sp:"+Speed+", Pwr:"+Power+", D:"+Defence;
    }
}
