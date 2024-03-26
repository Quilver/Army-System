using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Combats
{
    public UnitR attacker, defender;
    public Combats(UnitR attacker, UnitR defender)
    {
        this.attacker = attacker;
        this.defender = defender;
    }
}
