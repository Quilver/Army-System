using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UnitTemplate : MonoBehaviour
{
	#region Properties
	public int Ranks {
		get
		{
            return (ModelCount % Files > 0) ? ModelCount / Files + 1 : ModelCount / Files;
        } 
	}
	public abstract int Files { get; }
	public abstract int ModelCount { get; }
	public abstract float ModelSize { get; }
	public abstract StatSystem.UnitStats Stats { get; }
	public UnitState unitState { get; set; }
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
