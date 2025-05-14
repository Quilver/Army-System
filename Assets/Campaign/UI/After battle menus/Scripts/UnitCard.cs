using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EndGameUI
{
    public class UnitCard : MonoBehaviour
    {
        [SerializeField]
        Image portrait;
        [SerializeField]
        RectTransform levelBar, XPBar;
        [SerializeField]
        TextMeshProUGUI unitName, stats, levelUps;
        Campaign.PCWrapper unit;
        public void Set(Campaign.PCWrapper unit)
        {
            this.unit = unit;
            BattleReport.CharacterUpdate += CharacterUpdate;
            portrait.sprite = unit.statBase.Portrait;
            unitName.text = unit.statBase.UnitName;
            stats.text = unit.statBase.StatString();
            levelUps.text = unit.LevelBonuses();
            float width = unit.PercentToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
        void CharacterUpdate(Campaign.PCWrapper unit)
        {
            if (this.unit != unit) return;
            stats.text = unit.statBase.StatString();
            levelUps.text = unit.LevelBonuses();
            float width = unit.PercentToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
    }
}