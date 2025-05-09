using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep
{
    public class BuyTab : MonoBehaviour, ITab
    {
        [SerializeField]
        GameObject BuyPrefab;
        private void Start()
        {
            var data = Campaign.CampaignDataManager.Data;
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
        [SerializeField]
        Image button;
        public void Select(Color color, bool selected)
        {
            button.color = color;
            if(selected)gameObject.SetActive(true);
            else gameObject.SetActive(false);
        }
    }
}