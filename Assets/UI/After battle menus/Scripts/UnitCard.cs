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
        Campaign.StatWrapper unit;
        public void Set(Campaign.StatWrapper unit)
        {
            this.unit = unit;
            BattleReport.CharacterUpdate += CharacterUpdate;
            portrait.sprite = unit.statBase.portrait;
            unitName.text = unit.statBase.UnitName;
            stats.text = unit.statBase.StatString();
            levelUps.text = unit.statBase.LevelUpBonusesString();
            float width = unit.statBase.FractionToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
        void CharacterUpdate(Campaign.StatWrapper unit)
        {
            if (this.unit != unit) return;
            portrait.sprite = unit.statBase.portrait;
            unitName.text = unit.statBase.UnitName;
            stats.text = unit.statBase.StatString();
            float width = unit.statBase.FractionToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
    }
}