using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class UnitPositionR
{
	#region Properties
	UnitR unit;
	public UnitR unitSetter{
		set { unit= value; }
	}
	public PositionR position;

	public int _unitWidth;
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
	#endregion
	public int DistanceFromTile(Vector2Int position)
	{
		var delta = position - this.position.Location;
		var rotated = this.position.UnitDirection.Item1 * delta.x + 
			this.position.UnitDirection.Item2 * delta.y;
        if (UnitWidth/2 > math.abs(rotated.x)) rotated.x = 0;
		else rotated.x = math.abs(rotated.x) - UnitWidth/2;
		if (rotated.y > 0)
		{
			rotated.y = math.abs(rotated.y) - Ranks;
        }
		rotated.y=math.abs(rotated.y);
		if (this.position.direction.x != 0 && this.position.direction.y != 0)
		{
			int x = rotated.x % 2;
			int y = rotated.y % 2;
			rotated /= 2;
			rotated.x += x;
			rotated.y += y;
        }
        return math.max(rotated.x, rotated.y);
	}
	public bool CanMoveOn(PositionR positionR, int avoidBy=1, UnitR target = null)
	{
		foreach (var model in unit.models)
		{
			if (!Map.Instance.getTile(model.GetPosition(positionR)).Walkable(unit, target))
				return false;
			foreach (var unit in Master.Instance.unitArmy.Keys)
			{
				if (this.unit == unit || unit == target) continue;
				if(avoidBy >= unit.Movement.DistanceFromTile(model.GetPosition(positionR)))
					return false;
			}
		}
		return true;
	}
	UnitR UnitAt(PositionR positionR)
    {
        foreach (var model in unit.models)
        {
            if (!Map.Instance.getTile(model.GetPosition(positionR)).Walkable(unit))
                return Map.Instance.getTile(model.GetPosition(positionR)).unit;
        }
        return null;
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
	#region Movement
    UnitMoveState moveState;
	Stack<PositionR> waypoints;
	public void MoveTo(Vector2Int position)
	{
		if (unit.state == UnitState.Fighting) return;
		unit.state = UnitState.Moving;
		var target = Map.Instance.getTile(position).unit;

        if (target != null && Master.Instance.unitArmy[unit] != Master.Instance.unitArmy[target])
			waypoints = Pathfinding.Pathfinder.Search(this, this.position, Map.Instance.getTile(position).unit);
		else
            waypoints = Pathfinding.Pathfinder.Search(this, this.position, position);
        UpdatePath();
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
		if(waypoints == null || waypoints.Count == 0)
		{
			unit.state = UnitState.Idle;
			return;
		}
		if(!CanMoveOn(waypoints.Peek(), 0)){
			
			Debug.Log("Charge");
			if(UnitAhead() != null && Master.Instance.unitArmy[unit] != Master.Instance.unitArmy[UnitAhead()])
				Master.Instance.AddCombat(unit, UnitAhead());
            waypoints = null;
            return;
		}

        position = waypoints.Pop();
	}
	#endregion
}
