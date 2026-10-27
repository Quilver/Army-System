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
            _name.text = _unit.Stats.UnitName;
            _statBlock.text = _unit.Stats.ToString();
            HandleExtension();
        }

        // Shows a unit that has no legacy stat block of its own (e.g. a Troop, see TroopSelectionCard). A null unit
        // hides the card. The stat text (TextMeshPro rich text) shrinks to fit the stat panel.
        public void SetContent(IUnit unit, Sprite portrait, Color portraitTint, bool preserveAspect, string unitName, string stats)
        {
            if (unit == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            _unit = unit;
            if (_profile != null)
            {
                _profile.sprite = portrait;
                _profile.color = portraitTint;
                _profile.preserveAspect = preserveAspect;
                _profile.enabled = portrait != null;
            }
            if (_name != null) _name.text = unitName;
            if (_statBlock != null)
            {
                if (_statFontSize <= 0f) _statFontSize = _statBlock.fontSize;
                _statBlock.richText = true;
                _statBlock.enableAutoSizing = true;
                _statBlock.fontSizeMax = _statFontSize;
                _statBlock.fontSizeMin = Mathf.Min(_statFontSize, MinStatFontSize);
                _statBlock.text = stats;
                // The stats are the point of the card here, so the panel holding them is always shown.
                if (_extension != null && _statBlock.transform.IsChildOf(_extension)) _extension.gameObject.SetActive(true);
            }
        }

        const float MinStatFontSize = 8f;
        float _statFontSize;

        void HandleExtension()
        {
            if (_extension == null) return;
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