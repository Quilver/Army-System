using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class IUnit: MonoBehaviour
{
    public event System.Action<UnitState> StateChanged;
    public UnityEvent<Transform> DeadModel;
    public event System.Action UnitDestroyed;
    protected void ChangeState(UnitState state)=>StateChanged?.Invoke(state); 
    public abstract StatSystem.RegimentStats Stats { get; }
    public abstract UnitState State { get; set; }
    private void OnDestroy()
    {
        UnitDestroyed?.Invoke();
    }
}
