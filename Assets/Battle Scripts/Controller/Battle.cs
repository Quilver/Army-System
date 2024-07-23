using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public InfluenceMap.HighLevelMap highLevelMap;
    public Dictionary<UnitBase, Army> unitArmy;
    public Army player, enemy1;
    void Awake () {
        if(Instance != null)
        {
            return;
        }
        else
        {
            Instance = this;
        }
        unitArmy = new Dictionary<UnitBase, Army>();
    }
    public bool Enemies(UnitBase unit1, UnitBase unit2)
    {
        return unitArmy[unit1] != unitArmy[unit2];
    }
}
