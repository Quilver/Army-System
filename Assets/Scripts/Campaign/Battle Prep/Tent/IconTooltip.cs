using UnityEngine;
using UnityEngine.EventSystems;

namespace BattlePrep.Tent
{
    // Shows Text in the tent tooltip while the pointer is over this graphic (SU-91). Sits on the icons that stand in for
    // stat, class, weapon and armour names on the ledger and troop cards. Clicks pass on to the parent (a card's button).
    public class IconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Text { get; set; }

        public void OnPointerEnter(PointerEventData eventData) => TentTooltip.ShowIcon(this);
        public void OnPointerExit(PointerEventData eventData) => TentTooltip.HideIcon(this);
        void OnDisable() => TentTooltip.HideIcon(this);
    }
}
