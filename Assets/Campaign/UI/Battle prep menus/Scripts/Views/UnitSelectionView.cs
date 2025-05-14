using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep
{
    public class UnitSelectionView : MonoBehaviour
    {
        [SerializeField]
        Image portrait;
        [SerializeField]
        Image CheckBox;
        [SerializeField]
        RectTransform levelBar, XPBar;
        [SerializeField]
        TextMeshProUGUI unitName, stats, cost;
        Campaign.PCWrapper unit;
        public void Set(Campaign.PCWrapper unit)
        {
            this.unit = unit;
            unit.Update();
            portrait.sprite = unit.statBase.Portrait;
            unitName.text= unit.statBase.UnitName;
            stats.text = unit.statBase.StatString();
            cost.text = unit.CostToField + "G";
            float width = unit.PercentToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
        bool selected;
        public void Select()
        {
            selected = !selected;
            if (selected)
            {
                if(unit.CostToField > Campaign.CampaignDataManager.Data.Money)
                {
                    selected= false;
                    return;
                }
                Campaign.CampaignDataManager.Data.Money -= unit.CostToField;
                BattleReport.DeployedCharacters.Add(unit);
                CheckBox.color = Color.black;
            }

            else
            {
                Campaign.CampaignDataManager.Data.Money += unit.CostToField;
                BattleReport.DeployedCharacters.Remove(unit);
                CheckBox.color = Color.white;
            } 
                
            
        }
    }
}