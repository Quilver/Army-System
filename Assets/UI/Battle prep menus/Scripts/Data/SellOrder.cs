using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattlePrep
{
    [System.Serializable]
    public class SellOrders
    {
        public string orderInfo;
        [Range(1, 100)]
        public int PrestigeRequired;
        [Range(10, 500)]
        public int interestRate;
        [Range(10, 1000)]
        public int gold;
        [TextArea(1, 5)]
        public string orderDetails;
        public BuyOrders Buy()
        {
            BuyOrders buy = new();
            buy.orderInfo = orderInfo;
            buy.orderDetails= orderDetails;
            float percent = interestRate / 100f;
            buy.Cost = (int) percent * gold;
            return buy;
        }
    }
}
