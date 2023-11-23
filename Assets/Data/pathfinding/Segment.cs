using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
public class Segment{
    public int x, y;
    public int width, hieght, size;
    int[,] areas;
    public Segment(int x, int y)
    {
        this.x = x;
        this.y = y;
        width = 1;
        hieght = 1;
        size = 1;
        getArea();
    }
    public void getArea()
    {
        bool xExpand = true;
        bool yExpand = true;
        bool sizeExpand = true;
        int newX = x;
        int newY = y;
        int newSize = size;
        while (xExpand || yExpand)
        {
            if (xExpand) { newX++; }
            if (yExpand) { newY++; }
            for (int a = newX; a < newX; a++)
            {
                if (Map.Instance.getTile(a, newY).tileIsWalkable() == false)
                {
                    yExpand = false;
                    newY--;
                    break;
                }
            }
            for (int b = newY; b < newY; b++)
            {
                if (Map.Instance.getTile(newX, b).tileIsWalkable() == false)
                {
                    xExpand = false;
                    newX--;
                    break;
                }
            }

        }
        width = newX - x + 1;
        hieght = newY - y + 1;
        if (width > hieght) size = hieght;
        else size = width;
    }
    public bool valid()
    {
        if(Map.Instance.getTile(x, y).tileIsWalkable() == false)
        {
            return false;
        }
        return true;
    }
}
*/