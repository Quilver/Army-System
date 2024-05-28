using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Campaign
{
    [System.Serializable]
    public class CampaignData
    {
        public int Money;
        public int MaxPrestige, CurrentPrestige;
        [Range(1, 10)]
        public int CurrentLevel;
        public List<BattlePrep.BuyOrders> BuyOrders;
        public List<StatWrapper> characters;
    }
}
