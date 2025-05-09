using BattlePrep;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BattlePrep
{

}
public class BuyView : EndGameUI.HoverTip
{
    Color selectedColor = new(0.5f, 0.5f, 0.5f, 0.5f);
    Color deselectedColor = new(1, 1, 1, 0.5f);
    [SerializeField]
    Image background;
    [SerializeField]
    TextMeshProUGUI title;
    [SerializeField]
    TextMeshProUGUI Cost;
    [SerializeField]
    TextMeshProUGUI Prestige;
    [SerializeField]
    TextMeshProUGUI timeLeft;
    BuyOrders buyData;
    public void Set(BuyOrders buy)
    {
        buyData = buy;
        title.text = buy.orderInfo;
        Cost.text = buy.Cost.ToString();
        Prestige.text=buy.PrestigeReward.ToString();
        timeLeft.text=buy.TimeLeft.ToString() + " Months";
        tip = buy.orderDetails;

    }
    bool selected = false;
    public void Select()
    {
        if (!selected)
        {
            if (buyData.Cost > Campaign.CampaignDataManager.Data.Money)
                return;
            Debug.Log("buying back bond");
            selected = !selected;
            background.color = selectedColor;
            Campaign.CampaignDataManager.Data.Money -= buyData.Cost;
            //Campaign.CampaignDataManager.Data.CurrentPrestige += buyData.PrestigeRequired;
            Campaign.CampaignDataManager.Data.BuyOrders.Remove(buyData);
        }
        else
        {
            Debug.Log("not buying bond");
            selected = !selected;
            background.color = deselectedColor;
            Campaign.CampaignDataManager.Data.Money += buyData.Cost;
            Campaign.CampaignDataManager.Data.BuyOrders.Add(buyData);
        }
    }
}
