using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // A court member: hover shows their name; the dispatch-bearer also mentions the sealed dispatch when a quest is
    // available (spec TentHub.md §2 rows 5-7).
    // The tent shows the member as the primary speaker of their court scene (SU-187): ShowPortrait swaps the placeholder
    // figure for that character's portrait, as tall as the figure's slot, keeping the slot's centre and bottom edge.
    public class CourtHotspot : TentHotspot
    {
        Image _figure;
        Sprite _placeholder;
        Vector2 _placeholderSize;
        bool _cached;

        public override string HoverText
        {
            get
            {
                var hub = TentHubController.Instance;
                string name = hub != null ? hub.CourtName(CourtIndex) : DisplayName;
                bool quest = hub != null && hub.HasQuestCue(CourtIndex);
                return quest ? $"<b>{name}</b>\n<i>Holds a sealed dispatch</i>" : $"<b>{name}</b>";
            }
        }

        // Shows the portrait in place of the placeholder figure; null puts the placeholder back.
        public void ShowPortrait(Sprite portrait)
        {
            CachePlaceholder();
            if (_figure == null) return;
            var rect = (RectTransform)transform;
            if (portrait == null)
            {
                _figure.sprite = _placeholder;
                _figure.preserveAspect = false;
                rect.sizeDelta = _placeholderSize;
                _figure.alphaHitTestMinimumThreshold = 0f;
                TentAlphaHitTest.Apply(_figure, AlphaThreshold);
                return;
            }
            float height = _placeholderSize.y;
            float aspect = portrait.rect.height > 0f ? portrait.rect.width / portrait.rect.height : 1f;
            _figure.sprite = portrait;
            _figure.preserveAspect = true;
            // The slot's pivot is its centre, so keeping the height keeps the bottom edge where the figure stood.
            rect.sizeDelta = new Vector2(height * aspect, height);
            // Portraits are usually opaque and not Read/Write: click anywhere on them. A readable cut-out portrait
            // keeps alpha hit testing.
            _figure.alphaHitTestMinimumThreshold = portrait.texture != null && portrait.texture.isReadable ? AlphaThreshold : 0f;
        }

        void CachePlaceholder()
        {
            if (_cached) return;
            _cached = true;
            _figure = GetComponent<Image>();
            if (_figure == null) return;
            _placeholder = _figure.sprite;
            _placeholderSize = ((RectTransform)transform).sizeDelta;
        }
    }
}
