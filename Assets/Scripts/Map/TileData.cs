using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class TileData
{
    [SerializeField]
    bool _Walkable = true;
    public bool Walkable { get { return _Walkable; }  }
    [SerializeField]
    int _WalkCost = 1;
    public int WalkCost { get { return _WalkCost;} }
}
