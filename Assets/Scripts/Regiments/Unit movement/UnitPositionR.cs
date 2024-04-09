using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[System.Serializable]
public class UnitPositionR
{
	#region Properties
	UnitR unit;
	public PositionR position;
	ChargeSizer charge;
	int _unitWidth;
	public int UnitWidth
	{
		get
		{
			if (unit.models != null && unit.models.Count < _unitWidth)
				_unitWidth = unit.models.Count;
			return _unitWidth;
		}
	}
	public int Ranks
	{
		get
		{
			return (int)Mathf.Round((unit.models.Count * 1.0f) / UnitWidth);
		}
	}
	public void Init(UnitR unit, int Width)
	{
		this.unit= unit;
		this._unitWidth = Width;
		position.Location = new Vector2Int((int)unit.transform.position.x, (int)unit.transform.position.y);
		charge = unit.GetComponentInChildren<ChargeSizer>();
	}
	#endregion
	#region Helper Functions
	bool OverlappingWithUnit(PositionR pos, int avoidBy, UnitR target = null)
	{
		float angle = pos.Rotation;
		Vector2 size = new(UnitWidth + avoidBy, Ranks + avoidBy);
		if (angle % 10 != 0)
		{
			size *= 1.25f;
			size.x += 0.25f;
		}
		var midPoint = new Vector3(-(UnitWidth % 2 - 1) / 2f, -(Ranks - 1) / 2f);
		var rotatedOffset = Quaternion.Euler(0, 0, angle) * midPoint;
		var overlaps = Physics2D.OverlapBoxAll((Vector2)rotatedOffset + pos.Location, size, pos.Rotation, 1 << 6);
		foreach (var collider2D in overlaps)
		{
			var clipping = collider2D.GetComponentInParent<UnitR>();
			if (clipping != unit && clipping != target)
				return true;
		}
		return false;
	}
	public bool CanMoveOn(PositionR positionR, int avoidBy = 2, UnitR target = null)
	{
		if (OverlappingWithUnit(positionR, avoidBy, target))
			return false;
		foreach (var model in unit.models)
		{
			if (!Map.Instance.getTile(model.GetPosition(positionR)).Walkable(unit, target))
				return false;
		}
		return true;
	}
	#endregion
    #region Movement
    UnitMoveState moveState;
	[SerializeField]
	Stack<PositionR> waypoints;
	Pathfinding.Waypoint currentTarget, bufferTarget =null;
	//movement Buffer
	public void MoveTo(Vector2Int position)
	{
		if (unit.state == UnitState.Fighting) return;
		bufferTarget = new(position);
		unit.state = UnitState.Moving;
    }
	public void UpdateMovement()
	{
		if (unit.state != UnitState.Moving) return;

		if(AtPoint())
			UpdatePath();
	}
	bool AtPoint()
	{
		foreach (var model in unit.models)
			if(model.Moving)return false;
		return true;
	}
	void UpdatePath()
	{
		if(charge.UnitAhead && charge.Enemies.Count > 0)
		{
            Master.Instance.AddCombat(unit, charge.Enemies[0]);
            waypoints = null;
            return;
        }
		if (!(bufferTarget is null))
		{
			currentTarget = bufferTarget;
			bufferTarget = null;
			waypoints = currentTarget.GetPath(this);
		}
		else if(currentTarget != null)
			waypoints= currentTarget.GetPath(this);
		if(waypoints == null || waypoints.Count == 0)
		{
			unit.state = UnitState.Idle;
			currentTarget= null;
			return;
		}
		if(!CanMoveOn(waypoints.Peek(), 0)){
			waypoints = null;
            return;
		}
        position = waypoints.Pop();
	}
	#endregion
}
