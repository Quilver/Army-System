using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Random;
public class Map: MonoBehaviour{
    public Tile[,] tiles;
    public Texture2D map;
    public Dictionary<GameObject, Tile> get_tile;
    public Dictionary<GameObject, Unit> get_unit;
    //public int[,] til;
    public static Map Instance { get; protected set; }
    public int width = 150;
    public int hieght = 100;
    void Start()
    {
        Debug.Log("Hello Map");
        if (Instance != null)
        {
            Debug.Log("Error: there should only be one map instance");
        }
        else
        {
            Instance = this;
        }
        get_tile = new Dictionary<GameObject, Tile>();
        if(map == null)
        {
            proceduralLevel();
        }
        else
        {
            generateLevel();
        }
    }
    void proceduralLevel()
    {
        tiles = new Tile[width, hieght];
        // til = new int[width, hieght];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < hieght; y++)
            {
                if (x == 30 && (y < 40 && y > 20))
                {
                    tiles[x, y] = gameObject.AddComponent<Tile>().create(x, y, this, Tile.TileType.Tree) as Tile;
                    tiles[x, y].hideFlags = HideFlags.HideInInspector;
                    continue;
                }
                tiles[x, y] = gameObject.AddComponent<Tile>().create(x, y, this, Tile.TileType.Floor) as Tile;
                tiles[x, y].hideFlags = HideFlags.HideInInspector;
            }
        }
        //new SegmentGraph();
    }
    void generateLevel() {
        width = map.width;
        hieght = map.height;
        tiles = new Tile[width, hieght];
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                generateTile(x, y);
            }
        }
    }
    void generateTerrain()
    {

    }
    void generateDeploymentZone()
    {

    }
    void generateTile(int x, int y)
    {
        Color pixelColour = map.GetPixel(x, y);
        Tile.TileType type;
        //transparent, therfore do nothing
        if(pixelColour.a == 0)
        {
            return;
        }
        else if (pixelColour.g == 1)
        {
            type = Tile.TileType.Floor;
        }
        else if(pixelColour.b == 1)
        {
            type = Tile.TileType.Water;
        }
        else
        {
            type = Tile.TileType.Empty;
        }
        tiles[x, y] = gameObject.AddComponent<Tile>().create(x, y, this, type) as Tile;
        tiles[x, y].hideFlags = HideFlags.HideInInspector;
    }
    void generateTerrain(int x, int y)
    {

    }
    void generateDeploymentZone(int x, int y)
    {

    }
    public Tile getTile(int x, int y) {
        //Debug.Log(" x: " + x + " y " + y + " " + tiles[x, y]);
 
        if (x>=0 && y>=0 && x<width && y < hieght)
        {
            return tiles[x, y];
        }
        else
        {
            return null;
        }
    }
    public Tile getTile(Vector2 pos)
    {
        //Debug.Log(" x: " + x + " y " + y + " " + tiles[x, y]);

        if (pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < hieght)
        {
            return tiles[(int)pos.x, (int)pos.y];
        }
        else
        {
            return null;
        }
    }
    public bool inMap(float x, float y)
    {
        return (x >= 0 && x < width) && (y >= 0 && y < hieght);
    }
    public bool inMap(Vector2 position)
    {
        return (position.x >= 0 && position.x < width) && (position.y >= 0 && position.y < hieght);
    }
}
