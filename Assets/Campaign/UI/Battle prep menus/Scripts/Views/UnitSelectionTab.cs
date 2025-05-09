using BattlePrep;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionTab : MonoBehaviour, ITab
{
    [SerializeField]
    GameObject UnitPrefab;
    [SerializeField]
    Image buttonBackground;
    void Start()
    {
        foreach (var unit in Campaign.CampaignDataManager.Data.characters)
        {
            var obj = GameObject.Instantiate(UnitPrefab, transform);
            obj.GetComponent<UnitSelectionView>().Set(unit);
        }
    }
    public void Select(Color color, bool selected = false)
    {
        buttonBackground.color = color;
        gameObject.SetActive(selected);
    }
}
