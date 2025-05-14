using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class IUnit: MonoBehaviour
{
    public event System.Action<UnitState> StateChanged;
    public UnityEvent<Transform> DeadModel;
    public event System.Action UnitDestroyed, EnteredMelee, ExitedMelee;
    public static event System.Action<IUnit> OnUnitDestroyed;
    static void InvokeUnitDestroyed(IUnit unit)
    {
        unit.UnitDestroyed?.Invoke();
        OnUnitDestroyed?.Invoke(unit);
    }
    public abstract StatSystem.Refactor.IUnitStatBlock Stats { get; set; }
    public abstract UnitState State { get; set; }
    public abstract bool InMelee { get; }
    
    
    
    protected void ChangeState(UnitState state)=>StateChanged?.Invoke(state); 
    protected void Melee(bool enter)
    {
        if(enter)EnteredMelee?.Invoke();
        else ExitedMelee?.Invoke();
    }
    private void OnDestroy()
    {
        InvokeUnitDestroyed(this);
        //UnitDestroyed?.Invoke();
    }
}
