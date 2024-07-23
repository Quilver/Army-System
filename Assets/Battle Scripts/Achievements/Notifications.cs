using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notifications : MonoBehaviour 
{
    public static Action<UnitBase> Deployed;
    public static Action<UnitBase, UnitState> ChangeState;
    public static Action<UnitBase, UnitBase> StartFight, EndFight;
    public static Action<UnitBase, UnitBase, int> MeleeDamage, RangedDamage;
    public static Action<UnitBase, PositionR> HeadingTo, Reached;
    public static Action<UnitBase> Died;
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
    static void Default(UnitBase unit) { }
    static void Default(UnitBase unit, UnitState state) { }
    static void Default(Army army) { }
    static void Default(UnitBase unit, UnitBase unitInterface) { }
    static void Default(UnitBase unit, UnitBase unitInterface, int amount) { }
    static void Default(UnitBase unit, PositionR pos) { }

}
