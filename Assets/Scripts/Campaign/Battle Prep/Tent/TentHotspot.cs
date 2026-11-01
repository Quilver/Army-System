using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A person or object in the tent the player can hover and click (spec TentHub.md §2, §3).
    // Mouse hover and keyboard/controller selection both highlight it and show its tooltip; click or Submit performs
    // its action through TentHubController. Hit testing uses the sprite's alpha.
    [RequireComponent(typeof(Image))]
    public class TentHotspot : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] string _displayName = "Object";
        [SerializeField] TentAction _action = TentAction.None;
        [SerializeField, Tooltip("Court member index (0 Tactician, 1 Captain, 2 Dispatch-bearer) when Action is Court.")]
        int _courtIndex;

        [Header("Highlight")]
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _highlightColor = new(1f, 0.96f, 0.86f, 1f);
        [SerializeField, Min(1f)] float _highlightScale = 1.04f;
        [SerializeField] Outline _outline;
        [SerializeField, Range(0.01f, 1f)] float _alphaThreshold = 0.1f;

        Image _image;
        bool _pointerInside;

        public string DisplayName => _displayName;
        public TentAction Action => _action;
        public int CourtIndex => _courtIndex;

        // Text shown in the tooltip. Read every frame while shown, so it can show live values (gold, readiness).
        public virtual string HoverText => _displayName;

        protected override void Awake()
        {
            base.Awake();
            _image = GetComponent<Image>();
            if (targetGraphic == null) targetGraphic = _image;
            transition = Transition.None;
            if (Application.isPlaying) TentAlphaHitTest.Apply(_image, _alphaThreshold);
            ApplyHighlight(false);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (!Application.isPlaying) return;
            bool on = state == SelectionState.Highlighted || state == SelectionState.Pressed || state == SelectionState.Selected;
            ApplyHighlight(on);
        }

        void ApplyHighlight(bool on)
        {
            if (_image == null) return;
            _image.color = on ? _highlightColor : _normalColor;
            transform.localScale = Vector3.one * (on ? _highlightScale : 1f);
            if (_outline != null) _outline.enabled = on;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _pointerInside = true;
            if (IsInteractable()) TentTooltip.Show(this, true);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _pointerInside = false;
            TentTooltip.Hide(this);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (!_pointerInside && IsInteractable()) TentTooltip.Show(this, false);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            if (!_pointerInside) TentTooltip.Hide(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Activate();
        }

        public void OnSubmit(BaseEventData eventData) => Activate();

        void Activate()
        {
            if (!IsActive() || !IsInteractable()) return;
            OnActivated();
        }

        protected virtual void OnActivated()
        {
            if (TentHubController.Instance != null) TentHubController.Instance.Perform(_action, _courtIndex);
        }

        // Called when the hub stops taking input (a panel or dialogue opens).
        public void ResetHover()
        {
            _pointerInside = false;
            ApplyHighlight(false);
        }

#if UNITY_EDITOR
        // Used by the scene builder.
        public void EditorSetup(string displayName, TentAction action, int courtIndex, Outline outline)
        {
            _displayName = displayName;
            _action = action;
            _courtIndex = courtIndex;
            _outline = outline;
        }
#endif
    }
}
