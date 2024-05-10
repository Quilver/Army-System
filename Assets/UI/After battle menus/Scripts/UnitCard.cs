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
            portrait.sprite = unit.statBase.portrait;
            unitName.text = unit.statBase.name;
            stats.text = unit.statBase.StatString();
            float width = unit.statBase.FractionToNextLevel() * levelBar.sizeDelta.x;
            XPBar.sizeDelta = new Vector2(width, levelBar.sizeDelta.y);
        }
    }
}