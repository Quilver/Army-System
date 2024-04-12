using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface UnitInterface
{
    public UnitStatsR StatsR{ get; }
    public UnitState UnitStateState { get; }
    public UnitPositionR Movement { get; }
    public Weapon Melee { get; }
    public bool Wounded { get; }
    public void TakeDamage(int damage);
}
