using Campaign;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public event Action Deploy;
    public Army player, enemy1;
    [SerializeField]
    bool UseDeployedTroops;
    void Awake () {
        if(Instance != null)
        {
            Debug.LogError("Multiple battle controllers");
            return;
        }
        else
        {
            Instance = this;
        }
        UpdateToBattleData();
    }
    public void StartBattle()=>Deploy?.Invoke();
    void UpdateToBattleData()
    {
        if (!UseDeployedTroops) return;
        Debug.Log("checking deployment system");
        throw new NotImplementedException();
    }
    public bool Enemies(IUnit unit1, IUnit unit2)
    {
        List<IUnit> units = player.GetComponentsInChildren<IUnit>().ToList();
        return units.Contains(unit1) != units.Contains(unit2);
    }
}
