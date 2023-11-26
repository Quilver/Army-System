using UnityEngine;
[System.Serializable]
public class Tile: SelectionData
{
    TileData _tileData;
    Vector2Int pos;
    public Vector2Int position { get { return pos; } }
    public Unit unit;
    public Tile(Vector2Int position, TileData tileData)
    {
        pos= position;
        _tileData = tileData;
        unit = null;
    }
    public bool Walkable { get { return _tileData.Walkable; } }
    public int WalkCost { get { return _tileData.WalkCost; } }

    public string GetData()
    {
        return "Tile X:" + pos.x + ", Y:" + pos.y + "\n Walkable: " + Walkable.ToString();
    }
}
