using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public InfluenceMap.HighLevelMap highLevelMap;
    public Dictionary<UnitInterface, Army> unitArmy;
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
        unitArmy = new Dictionary<UnitInterface, Army>();
    }
    public bool Enemies(UnitR unit1, UnitR unit2)
    {
        return unitArmy[unit1] != unitArmy[unit2];
    }
}
