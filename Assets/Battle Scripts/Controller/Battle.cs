using Campaign;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public event Action Deploy;
    [SerializeField]
    UnityEvent _Deploy;
    public ArmyData player, enemy;
    [SerializeField]
    bool UseDeployedTroops;
    [ExecuteAlways]
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
    #region DeploymentSytem
    [SerializeField]
    int invalidDeployments = 0;
    public void StartBattle()
    {
        if(invalidDeployments!=0) return;
        _Deploy?.Invoke();
        Deploy?.Invoke();
    }
    public void UpdateUnitDeployment(bool validDeployment)
    {
        if (validDeployment) invalidDeployments--;
        else invalidDeployments++;
    }
    #endregion
    void UpdateToBattleData()
    {
        if (!UseDeployedTroops) return;
        Debug.Log("checking deployment system");
        throw new NotImplementedException();
    }
    public bool Enemies(IUnit unit1, IUnit unit2)
    {
        return unit1.transform.parent != unit2.transform.parent;
    }
}
