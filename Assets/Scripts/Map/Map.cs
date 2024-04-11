using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
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
    public Tile GetTile(int x, int y) {
        Vector2Int pos = new(x, y);
        if(tiles.ContainsKey(pos))
            return tiles[pos];
        TileData tileData = GetTileD(x, y);
        if (tileData == null) return null;
        tiles.Add(pos, new Tile(pos, tileData));
        return tiles[pos];
    }
    public Tile GetTile(Vector2Int pos)
    {
        return GetTile(pos.x, pos.y);
    }
    public Tile GetTile(Vector2 pos)
    {
        return GetTile((int)pos.x, (int)pos.y);
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
    
}
