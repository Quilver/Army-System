using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    public class BuyerView : MonoBehaviour
    {
        [SerializeField]
        GameObject BuyPrefab;
        private void Start()
        {
            var data = Campaign.CampaignDataManager.instance.data;
            foreach (var item in data.BuyOrders)
            {
                Add(item);
            }
        }
        void Add(BuyOrders order)
        {
            GameObject buy = GameObject.Instantiate(BuyPrefab, this.transform);
            buy.GetComponent<BuyView>().Set(order);
        }
    }
}