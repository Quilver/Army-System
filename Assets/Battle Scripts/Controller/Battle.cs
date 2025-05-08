using Campaign;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Battle : MonoBehaviour {
    public static Battle Instance;
    public InfluenceMap.HighLevelMap highLevelMap;
    public Dictionary<UnitTemplate, Army> unitArmy;
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
        unitArmy = new Dictionary<UnitTemplate, Army>();
        UpdateToBattleData();
    }
    void UpdateToBattleData()
    {
        if (!UseDeployedTroops) return;
        Debug.Log("checking deployment system");
        var units = player.GetComponentsInChildren<UnitTemplate>();
        for (int unitIndex = units.Length- 1; unitIndex >= 0; unitIndex--)
        {
            bool flag = true;
            for (int deployableUnit = CampaignDataManager.instance.deployedCharacters.Count - 1; deployableUnit >= 0; deployableUnit--)
            {
                if (units[unitIndex].Stats == CampaignDataManager.instance.deployedCharacters[deployableUnit].statBase)
                {
                    CampaignDataManager.instance.deployedCharacters.RemoveAt(deployableUnit);
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                Destroy(units[unitIndex].gameObject);
            }
        }
    }
    public bool Enemies(UnitTemplate unit1, UnitTemplate unit2)
    {
        return unitArmy[unit1] != unitArmy[unit2];
    }
    public bool Enemies(IUnit unit1, IUnit unit2)
    {
        List<IUnit> units = player.GetComponentsInChildren<IUnit>().ToList();
        return units.Contains(unit1) != units.Contains(unit2);
    }
}
