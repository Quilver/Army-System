using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep
{
    public class SellTab : MonoBehaviour, ITab
    {
        [SerializeField]
        Image button;
        [SerializeField]
        GameObject BuyPrefab;
        private void Start()
        {
            var data = PreparationContoller.Data;
            foreach (var item in data.buyOrders)
            {
                Add(item);
            }
        }
        void Add(SellOrders order)
        {
            GameObject buy = GameObject.Instantiate(BuyPrefab, this.transform);
            buy.GetComponent<SellView>().Set(order);
        }

        public void Select(Color color, bool selected = false)
        {
            button.color = color;
            if(selected) gameObject.SetActive(true);
            else gameObject.SetActive(false);
        }
    }
}