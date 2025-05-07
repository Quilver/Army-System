using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IUnit: MonoBehaviour
{
    public event System.Action<UnitState> StateChanged;
    protected void ChangeState(UnitState state)=>StateChanged?.Invoke(state); 
    public abstract StatSystem.RegimentStats Stats { get; }
    public abstract UnitState State { get; set; }
}
