using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface UnitInterface
{
    //public UnitStatsR StatsR { get; }
    public StatSystem.UnitStats UnitStats { get; }
    public UnitState State { get; set; }
    public UnitPositionR Movement { get; }
    public Weapon Melee { get; }
    public bool Wounded { get; }
    public void TakeDamage(int damage);
    public int ModelsRemaining { get; }
    public bool ModelsAreMoving { get; }
    public Vector3 LeadModelPosition { get; }
    public Vector3 RightMostModelPosition { get; }
    public Vector3 LeftMostModelPosition { get; }
}
