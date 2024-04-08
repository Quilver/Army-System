using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[System.Serializable]
public class UnitPositionR
{
	#region Properties
	UnitR unit;
	public UnitR unitSetter {
		set { unit = value; }
	}
	public PositionR position;

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
	}
	#endregion
	#region Helper Functions
	bool OverlappingWithUnit(PositionR pos, int avoidBy = 1, UnitR target = null)
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
	public List<UnitR> ChargeTargets()
	{
		List<UnitR> enemiesInCombat= new List<UnitR>();
        float angle = position.Rotation;
        Vector2 size = new(UnitWidth, (unit.Movement.Ranks - 1) / 2f + 1);
        if (angle % 10 != 0)
        {
            size *= 1.25f;
            size.x += 0.25f;
        }
        Vector2 rotatedOffset = Quaternion.Euler(0, 0, angle) * 
			new Vector3(-(unit.Movement.UnitWidth % 2 - 1) / 2f, size.y);
		var overlaps = Physics2D.OverlapBoxAll(rotatedOffset + position.Location, size, position.Rotation, 1 << 6);
		foreach (var collider in overlaps)
		{
			var target = collider.GetComponentInParent<UnitR>();
			if (Master.Instance.unitArmy[target] != Master.Instance.unitArmy[unit])
				enemiesInCombat.Add(target);
		}
		return enemiesInCombat;
	}
	UnitR UnitAhead()
	{
		for(int i= 0; i < UnitWidth; i++)
		{
			var model = unit.models[i];
			var forward1 = Map.Instance.getTile(model.ModelPosition + position.direction).unit;
            var forward2 = Map.Instance.getTile(model.ModelPosition + position.direction*2).unit;
			if (forward1 != null && unit != forward1) return forward1;
            if (forward2 != null && unit != forward2) return forward2;
        }
		return null;
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
		if(UnitAhead() != null && Master.Instance.unitArmy[unit] != Master.Instance.unitArmy[UnitAhead()])
		{
            Debug.Log("Charge");
            Master.Instance.AddCombat(unit, UnitAhead());
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
