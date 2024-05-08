using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep
{
    public class SellView : EndGameUI.HoverTip
    {
        Color selectedColor = new(0.5f, 0.5f, 0.5f, 0.5f);
        Color deselectedColor = new(1, 1, 1, 0.5f);
        [SerializeField]
        Image background;
        [SerializeField]
        SellOrders sellData;
        [SerializeField]
        TextMeshProUGUI Title, money, interest, prestige, dueDate;
        bool selected = false;
        public void Set(SellOrders sell)
        {
            sellData = sell;
            Title.text = sell.orderInfo;
            money.text = sell.gold.ToString()+"G";
            interest.text = sell.interestRate + "%";
            prestige.text = sell.PrestigeRequired.ToString();
            dueDate.text = sell.timeLeft + " Months";
            tip = sell.orderDetails;
        }
        public void Select()
        {
            if(selected)
            {
                selected=!selected;
                background.color = deselectedColor;
                Campaign.CampaignDataManager.Data.Money -= sellData.gold;
                Campaign.CampaignDataManager.Data.CurrentPrestige += sellData.PrestigeRequired;
                Campaign.CampaignDataManager.Data.BuyOrders.Remove(sellData.Buy());
            }
            else
            {
                if (sellData.PrestigeRequired > Campaign.CampaignDataManager.Data.CurrentPrestige)
                    return;
                selected=!selected; 
                background.color = selectedColor;
                Campaign.CampaignDataManager.Data.Money += sellData.gold;
                Campaign.CampaignDataManager.Data.CurrentPrestige -= sellData.PrestigeRequired;
                Campaign.CampaignDataManager.Data.BuyOrders.Add(sellData.Buy());
            }
        }
    }
}