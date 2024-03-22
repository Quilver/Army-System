using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Tile: SelectionData
{
    TileData _tileData;
    Vector2Int pos;
    public Vector2Int position { get { return pos; } }
    Unit _unit;
    public Unit unit { 
        get { return _unit; }
        set { 
            if(_unit == null || value == null)_unit = value;
            if(value != _unit)
            {
                Debug.LogError("Trying to place 2 units on the same Tile");
            }
        }
    }
    public Tile(Vector2Int position, TileData tileData)
    {
        pos= position;
        _tileData = tileData;
        unit = null;
    }
    public bool Walkable(Unit unit, Unit target = null)
    {
        bool walkable = _tileData.Walkable;
        bool occupied = this.unit!= null;
        if (this.unit == unit || this.unit == target) occupied = false;
        return walkable && !occupied;
    }
    //public bool Walkable { get { return _tileData.Walkable; } }
    public int WalkCost { get { return _tileData.WalkCost; } }

    public override string ToString()
    {
        string start = "Tile -> (X:" + pos.x + ", Y:" + pos.y + ", ";
        if (_tileData.Walkable)
            start += "Walkable)";
        else start += "Unmoveable)";
        return start;
    }
    public string GetData()
    {
        return ToString();
    }
}
