using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A deployed troop on the Map's deployment page: portrait, name, gold spent on it, and the empty pairing slot
    // (stub, spec TentHub.md §4.3, PB-Q4). The class is shown as its icon after the name (class name on hover), or as its
    // name under the character's name when it has no icon (SU-91).
    public class DeployedTroopCard : MonoBehaviour
    {
        const float ClassIconSize = 20f;
        const float ClassIconGap = 8f;

        [SerializeField] Image _portrait;
        [SerializeField] TextMeshProUGUI _name, _cost;
        [SerializeField] Button _pairSlot;

        IconBadge _classBadge;

        public void Set(RosterCharacter character)
        {
            _portrait.sprite = character.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _classBadge ??= TentIcons.BadgeAtLeft(_name.transform, "Class Icon", ClassIconSize);
            if (_classBadge.Set(character.ClassIcon, character.ClassName))
            {
                _name.text = character.Name;
                TentIcons.PlaceAfterText(_classBadge, _name, ClassIconGap);
            }
            else
            {
                _name.text = $"{character.Name}\n<size=75%><color=#6b5a45>{character.ClassName}</color></size>";
            }
            _cost.text = $"-{character.costToField} g";
            if (_pairSlot != null) _pairSlot.interactable = false;
        }
    }
}
