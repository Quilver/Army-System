using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPositionR : MonoBehaviour
{
	#region Properties
	UnitR unit;
	PositionR position;

	public int numberOfFiles;
	public int UnitWidth
	{
		get
		{
			if (unit.models != null && unit.models.Count < numberOfFiles)
				numberOfFiles = unit.models.Count;
			return numberOfFiles;
		}
	}
	#endregion
	public int DistanceFromTile(Vector2Int position)
	{
		throw new System.NotImplementedException();
	}
	public bool CanMoveOn(PositionR positionR, int avoidBy=3, UnitR target = null)
	{
		throw new System.NotImplementedException();
	}
}
