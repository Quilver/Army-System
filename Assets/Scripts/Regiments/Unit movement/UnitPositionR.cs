using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[System.Serializable]
public class UnitPositionR
{
    #region Properties
    UnitInterface unit;
	public PositionR position;
	RegimentSizer unitBody;
	ChargeSizer charge;
	int _unitWidth;
	public int UnitWidth
	{
		get
		{
			if (unit.ModelsRemaining < _unitWidth)
				_unitWidth = unit.ModelsRemaining;
			return _unitWidth;
		}
	}
	public int Ranks
	{
		get
		{
			return Mathf.CeilToInt((unit.ModelsRemaining * 1.0f) / UnitWidth);
		}
	}
	public void Init(UnitR unit, int Width)
	{
		this.unit= unit;
		this._unitWidth = Width;
		position.Location = new Vector2Int((int)unit.transform.position.x, (int)unit.transform.position.y);
		charge = unit.GetComponentInChildren<ChargeSizer>();
		unitBody=unit.GetComponentInChildren<RegimentSizer>();
	}
	#endregion
	#region Helper Functions
	public bool CanMoveOn(PositionR positionR, float avoidBy = 1, UnitR target = null)
	{
		if (unitBody.CanBeOn(positionR, avoidBy, UnitWidth, Ranks, target))
			return false;
		else
			return true;
	}
	public bool InCombatWith(PositionR positionR, UnitInterface target)
	{
		List<UnitInterface> targets = charge.TargetsAt(positionR);
		return targets.Contains(target);
	}
	#endregion
    #region Movement
    [SerializeField]
	Stack<PositionR> waypoints;
	Pathfinding.Waypoint currentTarget, bufferTarget =null;
	//movement Buffer
	public void MoveTo(Vector2 position)
	{
		if (unit.State == UnitState.Fighting) return;
		bufferTarget = new(position);
		unit.State = UnitState.Moving;
    }
	public void UpdateMovement()
	{
		if(unit.ModelsRemaining == 0) return;
        else if (unit.State == UnitState.Fighting) Pursuit();
        else if (unit.State != UnitState.Moving) return;
		if(!unit.ModelsAreMoving)
			UpdatePath();
	}
	void UpdatePath()
	{
		if(charge.UnitAhead && charge.Enemies.Count > 0)
		{
            //Master.Instance.AddCombat(unit, charge.Enemies[0]);
            waypoints = null;
            return;
        }

		if (bufferTarget != null)
		{
			currentTarget = bufferTarget;
			bufferTarget = null;
			waypoints = currentTarget.GetPath(this);
		}
		else if(currentTarget != null)
			waypoints= currentTarget.GetPath(this);
		if(waypoints == null || waypoints.Count == 0)
		{
			unit.State = UnitState.Idle;
			currentTarget= null;
			return;
		}
		if(!CanMoveOn(waypoints.Peek(), 0)){
			waypoints = null;
            return;
		}
        position = waypoints.Pop();
	}
	void Pursuit()
	{
		position.Location = unit.LeadModelPosition;
		if (unitBody.Clipping) return;
		if (!charge.UnitAhead) return;
		Vector2 dir = position.Direction;
		PositionR advancePos = new(position, dir * 0.1f);
		if (CanMoveOn(advancePos, 2f, null))
			position = advancePos;
		//else
		//	position.Location -= dir * 0.1f;
	}
	#endregion
}
