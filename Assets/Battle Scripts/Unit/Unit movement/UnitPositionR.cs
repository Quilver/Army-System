using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnitMovement;
using UnityEngine;

[System.Serializable]
public class UnitPositionR : IMovement
{
    #region Properties
    UnitBase unit;
	public PositionR position;
	RegimentSizer _unitBody;
    ChargeSizer _charge;
    RegimentSizer IMovement.unitBody => _unitBody;
    ChargeSizer IMovement.charge => _charge;
    int _unitWidth;
	int UnitWidth
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

    public Vector2 Location => position.Location;

    public float Rotation => position.Rotation;

    public int Files => UnitWidth;

    

    public void Init(UnitBase unit, int Width)
	{
		this.unit= unit;
		this._unitWidth = Width;
		position.Location = new Vector2Int((int)unit.transform.position.x, (int)unit.transform.position.y);
		_charge = unit.GetComponentInChildren<ChargeSizer>();
		_unitBody=unit.GetComponentInChildren<RegimentSizer>();
		//Notifications.Reached(unit, position);
	}
	#endregion
	#region Helper Functions
	public bool CanMoveOn(PositionR positionR, float avoidBy = 1, UnitBase target = null)
	{
		if (_unitBody.CanBeOn(positionR, avoidBy, UnitWidth, Ranks, target))
			return false;
		else
			return true;
	}
	/*
	public bool InCombatWith(Vector2 position, float angle, UnitBase target)
	{
		List<UnitBase> targets = _charge.TargetsAt(position, angle);
		return targets.Contains(target);
	}
	*/
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
    public void MoveTo(UnitBase unit)
    {
        if (this.unit.State == UnitState.Fighting) return;
        bufferTarget = new(unit);
        this.unit.State = UnitState.Moving;
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
		if(_charge.UnitAhead && _charge.Enemies.Count > 0)
		{
            //Master.Instance.AddCombat(unit, charge.Enemies[0]);
            waypoints = null;
            return;
        }
		Notifications.Reached(unit, position);

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
		if (_unitBody.Clipping) return;
		if (!_charge.UnitAhead) return;
		Vector2 dir = position.Direction;
		PositionR advancePos = new(position, dir * 0.1f);
		if (CanMoveOn(advancePos, 2f, null))
			position = advancePos;
	}

    
    #endregion
}
