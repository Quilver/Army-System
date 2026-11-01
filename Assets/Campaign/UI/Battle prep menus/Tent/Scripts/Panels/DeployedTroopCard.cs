using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A deployed troop on the Map's deployment page: portrait, name, gold spent on it, and the empty pairing slot
    // (stub, spec TentHub.md §4.3, PB-Q4).
    public class DeployedTroopCard : MonoBehaviour
    {
        [SerializeField] Image _portrait;
        [SerializeField] TextMeshProUGUI _name, _cost;
        [SerializeField] Button _pairSlot;

        public void Set(RosterCharacter character)
        {
            _portrait.sprite = character.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _name.text = $"{character.Name}\n<size=75%><color=#6b5a45>{character.ClassName}</color></size>";
            _cost.text = $"-{character.costToField} g";
            if (_pairSlot != null) _pairSlot.interactable = false;
        }
    }
}
