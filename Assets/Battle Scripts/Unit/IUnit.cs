using System.Collections;
using System.Collections.Generic;
using UnitMovement;
using UnityEngine;

//Interface for the unit during the battles
public abstract class IUnit : MonoBehaviour
{
    public abstract StatSystem.UnitStats UnitStats { get; }
    public abstract UnitState State { get; set; }
    public abstract IMovement Movement { get; }
    public abstract void TakeDamage(int damage);
}