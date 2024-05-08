using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace BattlePrep
{
    public class TopLineView : MonoBehaviour
    {
        
        [SerializeField]
        TextMeshProUGUI level, Gold, prestige;
        private void Start()
        {
            var data = Campaign.CampaignDataManager.Data;
            Set(data.CurrentLevel, data.Money, data.CurrentPrestige, data.MaxPrestige);
        }
        private void Update()
        {
            var data = Campaign.CampaignDataManager.Data;
            Set(data.CurrentLevel, data.Money, data.CurrentPrestige, data.MaxPrestige);
        }
        public void Set(int level, int gold, int currentPresitge, int maxPrestige)
        {
            this.level.text = "Level " + level;
            this.Gold.text = "Gold: " + gold;
            this.prestige.text = "Prestige: " + currentPresitge + "/" + maxPrestige;
        }
    }
}