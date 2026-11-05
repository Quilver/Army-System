using TMPro;
using UnityEngine;

namespace BattlePrep.Tent
{
    // The name label shown by a hovered or selected tent object (spec TentHub.md §3). Follows the pointer for mouse
    // hover; sits above the object for keyboard/controller selection. Never takes clicks.
    // Also names the icon under the pointer on the ledger and troop cards (IconTooltip, SU-91); that follows the pointer.
    public class TentTooltip : MonoBehaviour
    {
        static TentTooltip _instance;

        [SerializeField] RectTransform _window;
        [SerializeField] TextMeshProUGUI _text;
        [SerializeField] Vector2 _pointerOffset = new(18f, -18f);

        TentHotspot _source;
        IconTooltip _icon;
        bool _followPointer;
        Canvas _canvas;

        void Awake()
        {
            _instance = this;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            _window.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Show(TentHotspot source, bool followPointer)
        {
            if (_instance == null || source == null) return;
            _instance._source = source;
            _instance._icon = null;
            _instance._followPointer = followPointer;
            _instance._window.gameObject.SetActive(true);
            _instance.Refresh();
        }

        // Hides the tooltip if it belongs to source (or always, when source is null).
        public static void Hide(TentHotspot source = null)
        {
            if (_instance == null) return;
            if (source != null && _instance._source != source) return;
            _instance._source = null;
            _instance._icon = null;
            _instance._window.gameObject.SetActive(false);
        }

        // Shows the icon's name (IconTooltip.Text) next to the pointer.
        public static void ShowIcon(IconTooltip icon)
        {
            if (_instance == null || icon == null || string.IsNullOrEmpty(icon.Text)) return;
            _instance._source = null;
            _instance._icon = icon;
            _instance._followPointer = true;
            _instance._window.gameObject.SetActive(true);
            _instance.Refresh();
        }

        // Hides the tooltip if it belongs to the icon.
        public static void HideIcon(IconTooltip icon)
        {
            if (_instance == null || icon == null || _instance._icon != icon) return;
            Hide();
        }

        void LateUpdate()
        {
            if (_source != null || _icon != null) Refresh();
        }

        void Refresh()
        {
            if (_icon != null)
            {
                if (!_icon.isActiveAndEnabled || string.IsNullOrEmpty(_icon.Text))
                {
                    Hide();
                    return;
                }
                _text.text = _icon.Text;
            }
            else if (_source == null || !_source.isActiveAndEnabled)
            {
                Hide();
                return;
            }
            else
            {
                _text.text = _source.HoverText;
            }

            var parent = (RectTransform)_window.parent;
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screen;
            if (_followPointer) screen = TentInput.PointerPosition + _pointerOffset * _canvas.scaleFactor;
            else
            {
                var target = (RectTransform)_source.transform;
                Vector3 top = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMax, 0f));
                screen = RectTransformUtility.WorldToScreenPoint(cam, top) + new Vector2(0f, 8f * _canvas.scaleFactor);
            }

            // Flip to the other side of the anchor near the right / top edges so it stays on screen.
            float px = _followPointer ? (screen.x > Screen.width * 0.75f ? 1f : 0f) : 0.5f;
            float py = _followPointer ? (screen.y < Screen.height * 0.2f ? 0f : 1f) : 0f;
            _window.pivot = new Vector2(px, py);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var local))
                _window.localPosition = local;
        }
    }
}
