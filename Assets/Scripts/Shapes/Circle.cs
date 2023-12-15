using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle{

    int radius;
    int x, y;
    public Circle(int x, int y, int radius)
    {
        this.x = x;
        this.y = y;
        this.radius = radius;
    }
    public void changePosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    public void changeSize(int radius)
    {
        this.radius = radius;
    }
    public List<Tile> getTileCollisions()
    {
        List<Tile> tiles = new List<Tile>();
        for (int a = x; a < radius+x; a++)
        {
            for (int b = y; b < radius+y; b++)
            {
                if(tileInCirlce(Map.Instance.getTile(a, b))){
                    tiles.Add(Map.Instance.getTile(a, b));
                }
            }
        }
        return tiles;
    }
    public bool tileInCirlce(Tile tile)
    {
        return false;
    }
}
