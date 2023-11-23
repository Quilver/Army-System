using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile: Section
{
    public enum TileType
    {
        Empty,
        Floor,
        Floor2,
        Tree,
        Water
    };
    GameObject self;
    public TileType type = TileType.Empty;
    public TerrainFeature terrain;
    public Item item;
    //public Unit unit;
    public Tile() { }
    public Tile create(int x, int y, Map map)
    {
        unit = null;
        this.map = map;
        position = new Vector2(x, y);
        //this.x = x;
        //this.y = y;
        return this;
    }
    public Tile create(int x, int y, Map map, TileType type)
    {
        unit = null;
        this.map = map;
        position = new Vector2(x, y);
        this.type = type;
        string prefabName = "TerrainTiles/";
        switch (type)
        {
            case TileType.Empty:
                prefabName += "blackSquare";
                break;
            case TileType.Floor:
                prefabName += "grassTile";
                break;
            case TileType.Floor2:
                prefabName += "grassTile";
                break;
            case TileType.Tree:
                prefabName += "pineTree";
                break;
            case TileType.Water:
                prefabName += "water";
                break;
            default:
                prefabName += "grassTile";
                break;
        }
        self = Instantiate(Resources.Load(prefabName)) as GameObject;
        self.transform.position = position;
        self.name = "Tile " + position;
        self.transform.parent = Map.Instance.gameObject.transform;
        Map.Instance.get_tile.Add(self, this);
        return this;
    }
    public override Section select()
    {
        if (terrain != null)
        {
            //return terrain;
        }
        //else if (item != null)
        //{
            //return item;
        //}
        return this;
    }
    public override string getDetails()
    {
        return "tile: " + position.x + ", " + position.y;
    }
    public bool tileIsWalkable(Unit unit)
    {
        if(this.unit != null && unit != this.unit)
        {
            return false;
        }
        else if (type == TileType.Tree)
        {
            return false;
        }
        else if (type == TileType.Empty)
        {
            return false;
        }
        else if (type == TileType.Water)
        {
            return false;
        }
        else if (terrain != null)
        {
            return false;
        }
        return true;
    }
    public bool tileIsWalkable()
    {
        if (type == TileType.Tree)
        {
            return false;
        }
        else if (type == TileType.Empty)
        {
            return false;
        }
        else if (type == TileType.Water)
        {
            return false;
        }
        else if(terrain!=null)
        {
            return false;
        }
        return true;
    }
    public int tileSpeedCost()
    {
        return 1;
    }
}
