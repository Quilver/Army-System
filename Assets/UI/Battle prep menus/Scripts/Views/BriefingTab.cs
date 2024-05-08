using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep
{
    public class BriefingTab : MonoBehaviour, ITab
    {
        [SerializeField]
        Image buttonBackground;
        [SerializeField]
        TextMeshProUGUI textBox;
        [SerializeField]
        Image image;
        void Start()
        {
            image.sprite = PreparationContoller.Data.image;
            textBox.text = PreparationContoller.Data.briefing;
        }
        public void Select(Color color, bool select)
        {
            if(!select)
            {
                Deselect(color);
                return;
            }
            buttonBackground.color = color;
            image.gameObject.SetActive(true);
            textBox.gameObject.SetActive(true);
        }
        public void Deselect(Color color)
        {
            buttonBackground.color = color;
            image.gameObject.SetActive(false);
            textBox.gameObject.SetActive(false);
        }
    }
}
