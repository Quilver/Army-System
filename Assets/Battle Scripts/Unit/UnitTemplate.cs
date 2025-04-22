using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UnitTemplate : MonoBehaviour
{
	#region Properties
	public Army army
	{
		get
		{
			return GetComponentInParent<Army>();
		}
	}
	public int Ranks {
		get
		{
            return (ModelCount % Files > 0) ? ModelCount / Files + 1 : ModelCount / Files;
        } 
	}
	public abstract int Files { get; }
	public abstract int ModelCount { get; }
	public abstract float ModelSize { get; }
	public abstract StatSystem.RegimentStats Stats { get; }
	[SerializeField]
	UnitState _unitState;
    protected Action<UnitState, UnitState> Transition;
    public UnitState unitState { 
		get { return _unitState; }
		set
		{
			if (_unitState == UnitState.Fleeing && value != UnitState.Idle) return;
			Transition(unitState, value);
			_unitState = value; 
		} 
	}
    #endregion
    #region Events
    public static Action<UnitTemplate> Deployed, Died;
    public Action _Deployed, _Died;
    public static Action<UnitTemplate, UnitState> ChangeState;
	public Action<UnitTemplate, UnitState> _ChangeState;
    private void OnEnable()
    {
        _Deployed += ()=>UnitTemplate.Deployed(this);
    }
    #endregion
    #region Methods
    public abstract void MoveTo(Vector2 position);
	public abstract void MoveTo(Transform target);
	public abstract void TakeDamage(int damage);
	protected void Die()
	{
        if (this == null) return;
        Notifications.Died(this);
        //Battle.Instance.EndCombat(this);
        Destroy(this.gameObject);
    }
	protected void Setup()
	{
        unitState = UnitState.Idle;
        if (Battle.Instance.player.Units.Contains(this))
            Stats.Load();
    }
    
    #endregion
}
