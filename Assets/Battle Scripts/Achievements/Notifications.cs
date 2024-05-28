using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notifications : MonoBehaviour 
{
    public static Action<UnitInterface> Deployed;
    public static Action<UnitInterface, UnitState> ChangeState;
    public static Action<UnitInterface, UnitInterface> StartFight, EndFight;
    public static Action<UnitInterface, UnitInterface, int> MeleeDamage, RangedDamage;
    public static Action<UnitInterface, PositionR> HeadingTo, Reached;
    public static Action<UnitInterface> Died;
    public static Action<Army> ArmyDestroyed;
    private void Awake()
    {
        InitEvents();
    }
    static void InitEvents()
    {
        Deployed += Default; Died += Default;
        ChangeState+= Default;
        StartFight += Default; EndFight += Default;
        MeleeDamage += Default; RangedDamage += Default;
        HeadingTo += Default; Reached += Default;
        ArmyDestroyed+= Default;
    }
    static void Default(UnitInterface unit) { }
    static void Default(UnitInterface unit, UnitState state) { }
    static void Default(Army army) { }
    static void Default(UnitInterface unit, UnitInterface unitInterface) { }
    static void Default(UnitInterface unit, UnitInterface unitInterface, int amount) { }
    static void Default(UnitInterface unit, PositionR pos) { }

}
