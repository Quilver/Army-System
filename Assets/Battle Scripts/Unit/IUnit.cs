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
    public void KillUnit()
    {
        InvokeUnitDestroyed(this);
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        
    }
    public static LayerMask walkMask;
    public static LayerMask sightMask;
    Formation.IShape _formation;
    public RaycastHit2D UnitRaycast(Vector2 atPosition, Vector2 direction, bool shooting = false)
    {
        if (_formation == null) {
            walkMask = 1 << 3 | LayerMask.GetMask("Unit") | LayerMask.GetMask("Terrain");
            sightMask = LayerMask.GetMask("Unit") | LayerMask.GetMask("Terrain");
            _formation = GetComponentInChildren<Formation.IShape>();
        } 
        Vector3 size = _formation.SizeOfFormation;
        float angle = Vector2.SignedAngle(Vector2.up, direction.normalized);
        if(shooting)
            return Physics2D.BoxCast(atPosition, size, angle, direction, (direction - atPosition).magnitude, sightMask);
        return Physics2D.BoxCast(atPosition, size, angle, direction, (direction - atPosition).magnitude, walkMask);


    }

}
