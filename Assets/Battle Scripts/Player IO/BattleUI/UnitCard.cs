using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BattleUI
{
    public class UnitCard : MonoBehaviour
    {
        IUnit _unit;
        [SerializeField]
        bool _fullView;

        [Header("Handles for UI components")]
        [SerializeField]
        Image _profile;
        [SerializeField]
        TextMeshProUGUI _name;
        [SerializeField]
        TextMeshProUGUI _statBlock;
        [SerializeField]
        RectTransform _extension;
        public void SetUnit(IUnit unit)
        {
            if (unit == null)
            {
                gameObject.SetActive(false);
                return;
            }
            else gameObject.SetActive(true);
            _unit = unit;
            _profile.sprite = _unit.Stats.Portrait;
            _name.text = _unit.Stats.name;
            _statBlock.text = _unit.Stats.ToString();
            HandleExtension();
        }
        void HandleExtension()
        {
            _extension.gameObject.SetActive(_fullView);
            RectTransform main = GetComponent<RectTransform>();
            float _width = main.rect.width;
            if (main.pivot.x == 0)
            {
                _extension.pivot = new Vector2(0, 0.5f);
                _extension.anchoredPosition = new Vector2(_width, 0);
            }
            else
            {
                _extension.pivot = new Vector2(0, 0.5f);
                _extension.anchoredPosition = new Vector2(-_extension.rect.width, 0);
            }
        }
        void OnValidate()
        {
            HandleExtension();
        }
    }
}