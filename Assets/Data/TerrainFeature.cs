using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFeature{
    public enum TerrainType
    {
        Empty,
        Tree,
        Boulder
    };
    public TerrainType type = TerrainType.Empty;
    Tile tile;
    public TerrainFeature(Tile tile)
    {
        this.tile= tile;
        this.tile.terrain = this;
    }

}
