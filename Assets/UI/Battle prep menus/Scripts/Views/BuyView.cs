using BattlePrep;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace BattlePrep
{

}
public class BuyView : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI title;
    [SerializeField]
    TextMeshProUGUI Cost;
    [SerializeField]
    TextMeshProUGUI Prestige;
    [SerializeField]
    TextMeshProUGUI timeLeft;
    public void Set(BuyOrders buy)
    {
        title.text = buy.orderInfo;
        Cost.text = buy.Cost.ToString();
        Prestige.text=buy.PrestigeReward.ToString();
        timeLeft.text=buy.TimeLeft.ToString() + " Months";

    }
}
