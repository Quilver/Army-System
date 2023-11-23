using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour {
    public Sprite grassFloor, empty;
     public Map map;
    //Debug.Log(map.number);
    // Use this for initialization
    public MapController(Map map) {
        this.map = map;
        renderTiles();
    }
    void renderTile(int x, int y, Sprite sprite)
    {
        //game object
        GameObject tileGo = new GameObject();
        tileGo.transform.position = new Vector3(x, y, 0);
        tileGo.name = "Tile_" + x + "_" + y;
        tileGo.transform.SetParent(transform);
        //sprite
        SpriteRenderer tileSR = tileGo.AddComponent<SpriteRenderer>();
        tileSR.sprite = sprite;
        //box collider
        BoxCollider2D tileBox = tileGo.AddComponent<BoxCollider2D>();
        tileBox.size = new Vector3(1, 1, 1);
    }
	void renderTiles()
    {
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.hieght; y++)
            {
                Tile tile_data = map.getTile(x, y);
                Sprite sprite;
                if (tile_data.type == Tile.TileType.Empty)
                {
                    sprite = empty;
                }
                else if (tile_data.type == Tile.TileType.Floor)
                {
                    sprite = grassFloor;
                }
                else
                {
                    sprite = empty;
                }
                renderTile(x, y, sprite);
            }
        }
    }
	// Update is called once per frame
	void Update () {
		
	}
}
