using System.Collections;
using AchievementSystem;
using BattlePrep.Tent;
using CampaignUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndGameUI
{
    // An earned achievement stamped onto a post-battle page (SU-115): it comes down large and faint, lands with a thump
    // and glows, then the glow fades. Hovering the icon shows the achievement's description and rewards.
    public sealed class AchievementStamp : MonoBehaviour
    {
        [SerializeField] RectTransform _body;
        [SerializeField] CanvasGroup _group;
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _title, _reward;

        public void Set(Achievement achievement, string reward)
        {
            _icon.sprite = achievement.icon;
            _icon.enabled = achievement.icon != null;
            _title.text = achievement.Level > 1 ? $"{achievement.Title} x{achievement.Level}" : achievement.Title;
            _reward.text = reward;
            if (_icon.TryGetComponent(out IconTooltip tooltip)) tooltip.Text = achievement.HoverText;
            Land();
        }

        // Shown as already stamped, without animation.
        public void Land()
        {
            _body.localScale = Vector3.one;
            _group.alpha = 1f;
            UiGlow.Clear(_icon);
        }

        public IEnumerator Play(UiSequence sequence, PostBattleTuning tuning, AudioSource audio)
        {
            _body.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-tuning.StampMaxTilt, tuning.StampMaxTilt));
            _group.alpha = 0f;
            UiGlow.Set(_icon, tuning.StampGlowColor, 1f);
            yield return sequence.Tween(tuning.StampDropSeconds, f =>
            {
                float eased = f * f;   // speeds up as it comes down
                _body.localScale = Vector3.one * Mathf.Lerp(tuning.StampStartScale, 1f, eased);
                _group.alpha = Mathf.Clamp01(f * 2.5f);
            });
            if (!sequence.Skipping && audio != null && tuning.StampSound != null) audio.PlayOneShot(tuning.StampSound);
            yield return sequence.Tween(0.1f, f => _body.localScale = Vector3.one * (1f - 0.08f * Mathf.Sin(f * Mathf.PI)));
            yield return sequence.Tween(tuning.StampGlowSeconds, f => UiGlow.Set(_icon, tuning.StampGlowColor, 1f - f));
            Land();
        }
    }
}
