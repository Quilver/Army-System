using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
//using Random;
public class Map: MonoBehaviour{
    [SerializeField]
    Tilemap groundSource, waterSource, terrainSource;
    [SerializeField]
    TileData groundTile, waterTile, EmptyTile, MountainTile;
    Dictionary<Vector2Int, Tile> tiles;
    public static Map Instance { get; set; }
    void Awake()
    {
        Init();
    }
    public void Init()
    {
        if (Instance != null)
        {
            Debug.Log("Error: there should only be one map instance");
            return;
        }
        else
        {
            Instance = this;
        }
        tiles= new Dictionary<Vector2Int, Tile>();
    }
    public Tile getTile(int x, int y) {
        Vector2Int pos = new Vector2Int(x, y);
        if(tiles.ContainsKey(pos))
            return tiles[pos];
        TileData tileData = GetTileD(x, y);
        if (tileData == null) return null;
        tiles.Add(pos, new Tile(pos, tileData));
        return tiles[pos];
    }
    public Tile getTile(Vector2Int pos)
    {
        return getTile(pos.x, pos.y);
    }
    public Tile getTile(Vector2 pos)
    {
        return getTile((int)pos.x, (int)pos.y);
    }
    TileData GetTileD(int x, int y) {
        if (terrainSource.GetTile(new Vector3Int(x, y, 0)) != null)
            return MountainTile; 
        if (groundSource.GetTile(new Vector3Int(x, y, 0)) != null)
            return groundTile;
        if (waterSource.GetTile(new Vector3Int(x, y, 0)) != null)
            return waterTile;
        return EmptyTile;
    }
    TileData GetTileD(Vector2 position)
    {
        return GetTileD((int)position.x, (int)position.y);
        
    }
    public int Width { get { return groundSource.size.x; } }
    public int Height { get { return groundSource.size.y;} }
    
}
