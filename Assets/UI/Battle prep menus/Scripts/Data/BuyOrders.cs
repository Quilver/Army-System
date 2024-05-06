using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    [System.Serializable]
    public class BuyOrders 
    {
        public string orderInfo;
        [Range(10, 1000)]
        public int Cost;
        [Range(10, 1000)]
        public int PrestigeReward;
        [Range(1, 10)]
        public int TimeLeft;
        [TextArea(1, 5)]
        public string orderDetails;
    }
}