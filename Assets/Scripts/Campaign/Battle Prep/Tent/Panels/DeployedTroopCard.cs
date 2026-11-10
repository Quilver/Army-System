using Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A deployed character on the Map's deployment page: portrait, name and gold spent on it.
    // The class is shown as its icon after the name (class name on hover), or as its name under the character's name when
    // it has no icon (SU-91).
    // Composite troops (SU-117): drag a deployed card onto another to put that character under the other's command; the
    // two fight as one troop. A character serving under another is listed right after its leader, indented (the deployed
    // list is right-aligned and the follower's card is FollowerIndent narrower), both cards tinted. Dragging the follower
    // off its leader (dropped anywhere but a card) breaks the pair.
    public class DeployedTroopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        const float ClassIconSize = 20f;
        const float ClassIconGap = 8f;
        const float GhostAlpha = 0.75f;
        // How far a follower's card is indented under its leader's.
        public const float FollowerIndent = 36f;

        static readonly Color CompositeTint = new Color32(0xDD, 0xE6, 0xC4, 0xFF);
        static readonly Color FollowerTint = new Color32(0xE8, 0xEE, 0xD6, 0xFF);
        static readonly Color DropTint = new Color32(0xF5, 0xDE, 0x9C, 0xFF);

        // Raised with the reason when a drop cannot make a composite troop (MapPanel shows it).
        public static event System.Action<string> PairRefused;

        [SerializeField] Image _portrait;
        [SerializeField] TextMeshProUGUI _name, _cost;
        [SerializeField, Tooltip("Old pairing slot (SU-117 replaced it with drag and drop); hidden at runtime.")]
        Button _pairSlot;

        IconBadge _classBadge;
        RosterCharacter _character;
        Image _background;
        Color _defaultColor = Color.white, _baseColor = Color.white;
        float _fullWidth;
        RectTransform _ghost;
        bool _hovered;

        // Set by the card a drag was dropped on, so the dragged card knows the drop was handled.
        static bool _dropHandled;
        static DeployedTroopCard _dragging;

        public RosterCharacter Character => _character;

        bool _initialised;

        void Awake() => Initialise();

        // Runs once, from Awake or from the first Set, whichever comes first: a card set while the Deployment page is
        // hidden (Julian, deployed as the tent opens) gets Set before its Awake, and must still read its full width and
        // colour before Set changes them.
        void Initialise()
        {
            if (_initialised) return;
            _initialised = true;
            _background = GetComponent<Image>();
            if (_background != null)
            {
                _defaultColor = _baseColor = _background.color;
                _background.raycastTarget = true; // receives drags and drops
            }
            if (_pairSlot != null) _pairSlot.gameObject.SetActive(false);
            _fullWidth = ((RectTransform)transform).sizeDelta.x;
        }

        void OnDisable() => DestroyGhost();

        public void Set(RosterCharacter character)
        {
            Initialise();
            _character = character;
            _portrait.sprite = character.Portrait;
            _portrait.enabled = _portrait.sprite != null;

            RosterCharacter leader = TentDeployment.LeaderOf(character);
            var followers = TentDeployment.FollowersOf(character);
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
            int cost = TentDeployment.CostOf(character);
            _cost.text = TentDeployment.IsMandatory(character) ? "Free" : $"-{cost} g";

            // A follower is indented under its leader: the list is right-aligned, so a narrower card sits further right.
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(leader != null ? _fullWidth - FollowerIndent : _fullWidth, rect.sizeDelta.y);
            _baseColor = leader != null ? FollowerTint : followers.Count > 0 ? CompositeTint : _defaultColor;
            ApplyTint();
        }

        void ApplyTint()
        {
            if (_background == null) return;
            bool dropTarget = _hovered && _dragging != null && _dragging != this;
            _background.color = dropTarget ? DropTint : _baseColor;
        }

        // --- Drag and drop (SU-117) ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_character == null || eventData.button != PointerEventData.InputButton.Left) return;
            _dragging = this;
            _dropHandled = false;
            CreateGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData) => MoveGhost(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            DestroyGhost();
            if (_dragging == this && !_dropHandled && TentDeployment.IsFollower(_character))
                TentDeployment.Unpair(_character); // dragged off its leader
            _dragging = null;
            _dropHandled = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<DeployedTroopCard>() : null;
            if (dragged == null || dragged == this || dragged._character == null || _character == null) return;
            _dropHandled = true;
            _hovered = false;
            ApplyTint();
            // Dropped back onto its own leader: nothing changes.
            if (TentDeployment.LeaderOf(dragged._character) == _character) return;
            if (!TentDeployment.CanPair(dragged._character, _character, out string reason))
            {
                PairRefused?.Invoke(reason);
                return;
            }
            TentDeployment.Pair(dragged._character, _character);
        }

        // Highlight as a drop target while another card is dragged over this one.
        void SetHovered(bool entered)
        {
            _hovered = entered;
            ApplyTint();
        }

        void Update()
        {
            // Drop-target tint: while another card is being dragged, check whether the pointer is over this one.
            if (_dragging == null || _dragging == this)
            {
                if (_hovered) SetHovered(false);
                return;
            }
            var rect = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 pointer = UnityEngine.InputSystem.Pointer.current != null
                ? UnityEngine.InputSystem.Pointer.current.position.ReadValue()
                : Vector2.negativeInfinity;
            bool over = RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, cam);
            if (over != _hovered) SetHovered(over);
        }

        void CreateGhost(PointerEventData eventData)
        {
            DestroyGhost();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Canvas root = canvas.rootCanvas;
            var copy = Instantiate(gameObject, root.transform, false);
            copy.name = "Drag Ghost";
            var copiedCard = copy.GetComponent<DeployedTroopCard>();
            copiedCard.enabled = false;
            Destroy(copiedCard);
            foreach (var selectable in copy.GetComponentsInChildren<Selectable>(true)) selectable.interactable = false;
            var group = copy.GetComponent<CanvasGroup>();
            if (group == null) group = copy.AddComponent<CanvasGroup>();
            group.alpha = GhostAlpha;
            group.blocksRaycasts = false;
            group.interactable = false;
            _ghost = (RectTransform)copy.transform;
            _ghost.anchorMin = _ghost.anchorMax = _ghost.pivot = new Vector2(0.5f, 0.5f);
            _ghost.sizeDelta = ((RectTransform)transform).rect.size;
            _ghost.SetAsLastSibling();
            MoveGhost(eventData);
        }

        void MoveGhost(PointerEventData eventData)
        {
            if (_ghost == null) return;
            var parent = (RectTransform)_ghost.parent;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position,
                    eventData.pressEventCamera, out Vector3 world))
                _ghost.position = world;
        }

        void DestroyGhost()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
        }
    }
}
