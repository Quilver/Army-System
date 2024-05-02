using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace EndGameUI
{
    public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        string tip;
        float timeToWait = 0.5f;
        public void OnPointerEnter(PointerEventData eventData)
        {
            StopAllCoroutines();
            StartCoroutine(StartTimer());
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            StopAllCoroutines();
            HoverTipManager.OnMouseLoseFocus();
        }
        void ShowMessage()
        {
            HoverTipManager.OnMouseHover(tip, Input.mousePosition);
        }
        IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(timeToWait);
            ShowMessage();
        }
    }
}