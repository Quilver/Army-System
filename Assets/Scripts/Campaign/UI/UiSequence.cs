using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampaignUI
{
    // Waits, tweens and counting figures for menu animations, in real time (they ignore Time.timeScale, which the battle
    // speed may have left changed). Setting Skipping finishes every remaining wait and tween at once (SU-115).
    public sealed class UiSequence
    {
        public bool Skipping;

        public IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !Skipping; t += Time.unscaledDeltaTime) yield return null;
        }

        // Calls step with 0..1 over seconds, and always with 1 at the end.
        public IEnumerator Tween(float seconds, Action<float> step)
        {
            for (float t = 0f; t < seconds && !Skipping; t += Time.unscaledDeltaTime)
            {
                step(Mathf.Clamp01(t / seconds));
                yield return null;
            }
            step(1f);
        }

        // Counts text from one value to another; onStep runs each time the shown number changes (a tick sound).
        public IEnumerator CountUp(TMP_Text text, int from, int to, float seconds, Func<int, string> format,
            Action onStep = null)
        {
            int shown = from;
            text.text = format(from);
            yield return Tween(seconds, f =>
            {
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, f));
                if (value == shown) return;
                shown = value;
                text.text = format(value);
                if (!Skipping) onStep?.Invoke();
            });
            text.text = format(to);
        }
    }

    // A soft glow around a graphic, drawn with an Outline effect (SU-115).
    public static class UiGlow
    {
        const float Distance = 3f;

        public static void Set(Graphic graphic, Color color, float strength)
        {
            if (graphic == null) return;
            if (!graphic.TryGetComponent(out Outline outline))
            {
                outline = graphic.gameObject.AddComponent<Outline>();
                outline.useGraphicAlpha = false;
            }
            outline.effectDistance = new Vector2(Distance, -Distance);
            color.a *= Mathf.Clamp01(strength);
            outline.effectColor = color;
            outline.enabled = strength > 0f;
        }

        public static void Clear(Graphic graphic) => Set(graphic, Color.clear, 0f);
    }
}
