using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // An icon on a small dark badge (the icons are drawn for a dark background) that names itself in the tent tooltip
    // on hover (SU-91). Hidden when it has no sprite, so the caller can show the name as text instead.
    public sealed class IconBadge
    {
        public readonly GameObject Root;
        readonly Image _icon;
        readonly IconTooltip _tooltip;

        public IconBadge(GameObject root, Image icon, IconTooltip tooltip)
        {
            Root = root;
            _icon = icon;
            _tooltip = tooltip;
        }

        public RectTransform Rect => (RectTransform)Root.transform;

        // Shows the sprite with name as its tooltip, or hides the badge when sprite is null. Returns whether it shows.
        public bool Set(Sprite sprite, string name)
        {
            _icon.sprite = sprite;
            _tooltip.Text = name;
            Root.SetActive(sprite != null);
            return sprite != null;
        }
    }

    // An icon badge followed by a text. Set shows the icon and the value, or the name and the value as text when there
    // is no icon.
    public sealed class IconText
    {
        public readonly IconBadge Badge;
        public readonly TextMeshProUGUI Text;

        public IconText(IconBadge badge, TextMeshProUGUI text)
        {
            Badge = badge;
            Text = text;
        }

        public void Set(Sprite sprite, string name, string value)
        {
            bool icon = Badge.Set(sprite, name);
            string text = icon ? value : string.IsNullOrEmpty(value) ? name : name + " " + value;
            Text.text = text;
            Text.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void Hide()
        {
            Badge.Root.SetActive(false);
            Text.gameObject.SetActive(false);
        }
    }

    // Builds, at runtime, the icons that stand in for stat, class, weapon and armour names on the tent's ledger pages
    // and troop cards (SU-91), so the built Tent Hub scene needs no rebuild.
    public static class TentIcons
    {
        public static readonly Color BadgeColor = new Color32(0x2B, 0x21, 0x18, 0xFF);
        const float IconInset = 0.1f;   // the icon's margin inside its badge, as a fraction of the badge size

        // A size x size badge under parent; laid out by a parent layout group through its LayoutElement. Reuses an
        // existing child with the same name (cards are cloned from a template, which may already have one).
        public static IconBadge Badge(Transform parent, string name, float size)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.GetComponent<IconTooltip>() != null)
            {
                Transform iconChild = existing.Find("Icon");
                if (iconChild != null && iconChild.TryGetComponent(out Image existingIcon))
                    return new IconBadge(existing.gameObject, existingIcon, existing.GetComponent<IconTooltip>());
            }

            var root = NewUI(name, parent);
            root.sizeDelta = new Vector2(size, size);
            var back = root.gameObject.AddComponent<Image>();
            back.color = BadgeColor;
            back.raycastTarget = true;   // receives the hover for the tooltip
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;
            layout.flexibleWidth = layout.flexibleHeight = 0f;
            var tooltip = root.gameObject.AddComponent<IconTooltip>();

            var iconRect = NewUI("Icon", root);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            float inset = size * IconInset;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return new IconBadge(root.gameObject, icon, tooltip);
        }

        // A badge anchored to the left-middle of parent (a label), offset x from its left edge.
        public static IconBadge BadgeAtLeft(Transform parent, string name, float size)
        {
            IconBadge badge = Badge(parent, name, size);
            RectTransform rect = badge.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return badge;
        }

        // Puts the badge just after the label's text (left-aligned, single line).
        public static void PlaceAfterText(IconBadge badge, TMP_Text label, float gap)
        {
            float width = label.GetPreferredValues(label.text).x;
            badge.Rect.anchoredPosition = new Vector2(Mathf.Min(width + gap, label.rectTransform.rect.width), 0f);
        }

        // A container with the same placement as the label, as its next sibling.
        public static RectTransform Overlay(RectTransform label, string name)
        {
            var rect = NewUI(name, label.parent);
            rect.SetSiblingIndex(label.GetSiblingIndex() + 1);
            rect.anchorMin = label.anchorMin;
            rect.anchorMax = label.anchorMax;
            rect.pivot = label.pivot;
            rect.anchoredPosition = label.anchoredPosition;
            rect.sizeDelta = label.sizeDelta;
            return rect;
        }

        // A horizontal row, left-aligned and vertically centred, sized by its parent: index rows of height from the top
        // of parent (index 0 is the top row).
        public static RectTransform Row(Transform parent, string name, int index, float height, float spacing)
        {
            var rect = NewUI(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * height);
            rect.sizeDelta = new Vector2(0f, height);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        // A single-line text in a row, styled like the given label.
        public static TextMeshProUGUI Text(Transform parent, string name, TMP_Text style)
        {
            var rect = NewUI(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = style.font;
            text.fontSharedMaterial = style.fontSharedMaterial;
            text.fontSize = style.fontSize;
            text.color = style.color;
            text.richText = true;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // A badge then a text, added to a row, then an empty gap of gapAfter.
        public static IconText Pair(Transform row, string name, float iconSize, TMP_Text style, float gapAfter = 0f)
        {
            var pair = new IconText(Badge(row, name + " Icon", iconSize), Text(row, name + " Value", style));
            if (gapAfter > 0f) Spacer(row, name + " Gap", gapAfter);
            return pair;
        }

        // Empty space of the given width in a row.
        public static void Spacer(Transform row, string name, float width)
        {
            var layout = NewUI(name, row).gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
        }

        static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
