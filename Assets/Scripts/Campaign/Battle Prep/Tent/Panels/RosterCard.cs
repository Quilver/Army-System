using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A roster character on the Map's deployment page (SU-52, SU-53). Click to deploy / undeploy.
    public class RosterCard : MonoBehaviour
    {
        static readonly Color Normal = new Color32(0xE2, 0xD3, 0xB4, 0xFF);
        static readonly Color Deployed = new Color32(0xC9, 0xD8, 0xA8, 0xFF);
        static readonly Color Blocked = new Color32(0xD4, 0xC8, 0xB2, 0xFF);

        [SerializeField] Button _button;
        [SerializeField] Image _background, _portrait;
        [SerializeField] TextMeshProUGUI _name, _detail, _cost, _state;

        RosterCharacter _character;

        void Awake() => _button.onClick.AddListener(() => TentDeployment.Toggle(_character));

        public void Set(RosterCharacter character)
        {
            _character = character;
            _portrait.sprite = character.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _name.text = $"{character.Name}  <size=75%><color=#6b5a45>{character.ClassName}</color></size>";
            var c = character.character;
            _detail.text = c != null
                ? $"Level {character.level}  ·  {c.Numbers} soldiers  ·  Speed {c.Speed}  ·  Morale {c.Morale}"
                : $"Level {character.level}";
            _cost.text = $"{character.costToField} g";
            Refresh();
        }

        public void Refresh()
        {
            if (_character == null) return;
            if (TentDeployment.IsDeployed(_character))
            {
                _background.color = Deployed;
                _state.text = "<color=#3f6212>DEPLOYED</color>";
            }
            else if (TentDeployment.CanDeploy(_character, out string reason))
            {
                _background.color = Normal;
                _state.text = "<color=#6b5a45>Click to deploy</color>";
            }
            else
            {
                _background.color = Blocked;
                _state.text = $"<color=#9a2b12>{reason}</color>";
            }
        }
    }
}
