using System.Collections;
using System.Collections.Generic;
using EndGameUI;
using UnityEngine;
using UnityEngine.UI;

namespace CampaignUI
{
    // A character's XP as a row of notches, one per XP point of a level (RosterCharacter.XpPerLevel = 10): full notches
    // glow, empty ones are black (SU-115). Used by the roster ledger and the post-battle book; built at runtime over the
    // old XP bar, so the scenes need no rebuild. Colours: PostBattleTuning.
    public sealed class XpNotches : MonoBehaviour
    {
        readonly List<Image> _notches = new();
        int _filled;

        public int Count => _notches.Count;
        public int Filled => _filled;

        // count notches filling area, spacing apart.
        public static XpNotches Create(RectTransform area, int count, float spacing)
        {
            var root = new GameObject("XP Notches", typeof(RectTransform));
            root.layer = area.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(area, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var notches = root.AddComponent<XpNotches>();
            for (int i = 0; i < count; i++)
            {
                var notch = new GameObject("Notch " + (i + 1), typeof(RectTransform));
                notch.layer = root.layer;
                notch.transform.SetParent(rect, false);
                var image = notch.AddComponent<Image>();
                image.raycastTarget = false;
                notches._notches.Add(image);
            }
            notches.Set(0);
            return notches;
        }

        public void Set(int filled)
        {
            _filled = Mathf.Clamp(filled, 0, _notches.Count);
            var tuning = PostBattleTuning.Active;
            for (int i = 0; i < _notches.Count; i++)
            {
                bool full = i < _filled;
                _notches[i].color = full ? tuning.NotchFullColor : tuning.NotchEmptyColor;
                _notches[i].rectTransform.localScale = Vector3.one;
                UiGlow.Set(_notches[i], tuning.NotchGlowColor, full ? 1f : 0f);
            }
        }

        // Lights the next notch with a short pop.
        public IEnumerator Fill(int filled, UiSequence sequence, float seconds)
        {
            Set(filled);
            if (_filled <= 0) yield break;
            var notch = _notches[_filled - 1].rectTransform;
            yield return sequence.Tween(seconds, f => notch.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(f * Mathf.PI)));
            notch.localScale = Vector3.one;
        }

        // Every notch swells and brightens together, pulses times (a level-up).
        public IEnumerator Pulse(UiSequence sequence, int pulses, float seconds, float scale)
        {
            var tuning = PostBattleTuning.Active;
            for (int p = 0; p < pulses; p++)
                yield return sequence.Tween(seconds, f =>
                {
                    float wave = Mathf.Sin(f * Mathf.PI);
                    foreach (var notch in _notches)
                    {
                        notch.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, scale, wave);
                        notch.color = Color.Lerp(tuning.NotchFullColor, Color.white, wave * 0.6f);
                        UiGlow.Set(notch, tuning.NotchGlowColor, 0.5f + 0.5f * wave);
                    }
                });
            Set(_filled);
        }
    }
}
