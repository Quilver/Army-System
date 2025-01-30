using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notifications : MonoBehaviour 
{
    public static Action<UnitTemplate> Deployed;
    public static Action<UnitTemplate, UnitState> ChangeState;
    public static Action<UnitTemplate, UnitTemplate> StartFight, EndFight;
    public static Action<UnitTemplate, UnitTemplate, int> MeleeDamage, RangedDamage;
    public static Action<UnitTemplate, Vector2> HeadingTo, Reached;
    public static Action<UnitTemplate> Died;
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
    static void Default(UnitTemplate unit) { }
    static void Default(UnitTemplate unit, UnitState state) { }
    static void Default(Army army) { }
    static void Default(UnitTemplate unit, UnitTemplate unitInterface) { }
    static void Default(UnitTemplate unit, UnitTemplate unitInterface, int amount) { }
    static void Default(UnitTemplate unit, Vector2 pos) { }

}
