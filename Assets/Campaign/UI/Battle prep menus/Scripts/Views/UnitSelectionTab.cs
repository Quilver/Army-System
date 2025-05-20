using BattlePrep;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionTab : ITab
{
    [SerializeField]
    GameObject UnitPrefab;
    [SerializeField]
    Image buttonBackground;
    void Start()
    {
        foreach (var unit in Campaign.CampaignDataManager.Data.Characters)
        {
            var obj = GameObject.Instantiate(UnitPrefab, transform);
            obj.GetComponent<UnitSelectionView>().Set(unit);
        }
    }
    public override void Select(Color color, bool selected = false)
    {
        buttonBackground.color = color;
        gameObject.SetActive(selected);
    }
}
