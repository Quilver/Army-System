using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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