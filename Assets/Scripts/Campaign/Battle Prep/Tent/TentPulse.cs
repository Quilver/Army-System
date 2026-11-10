using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Pulses a graphic's alpha, e.g. the glow on the sealed dispatch when a quest is available.
    [RequireComponent(typeof(Graphic))]
    public class TentPulse : MonoBehaviour
    {
        [SerializeField] float _period = 1.6f;
        [SerializeField, Range(0f, 1f)] float _minAlpha = 0.25f, _maxAlpha = 1f;
        Graphic _graphic;

        void Awake() => _graphic = GetComponent<Graphic>();

        void Update()
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, _period));
            var c = _graphic.color;
            c.a = Mathf.Lerp(_minAlpha, _maxAlpha, t);
            _graphic.color = c;
        }
    }
}
