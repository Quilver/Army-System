using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemType
    {
        Empty,
        Chest,
        MagicItem
    };
    public ItemType type = ItemType.Empty;
    Tile tile;
    public Item(Tile tile)
    {
        this.tile = tile;
        this.tile.item = this;
    }
}

