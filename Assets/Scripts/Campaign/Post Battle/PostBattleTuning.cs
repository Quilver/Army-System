using UnityEngine;

namespace EndGameUI
{
    // Campaign Tuning: timings, colours and sounds of the post-battle book (SU-115).
    // Asset: Assets/Resources/Campaign Tuning/Post Battle Tuning.asset. Read through PostBattleTuning.Active; without the
    // asset the defaults below are used. Times are real seconds (they ignore the battle speed).
    [CreateAssetMenu(menuName = "Campaign Tuning/Post Battle Tuning")]
    public sealed class PostBattleTuning : ScriptableObject
    {
        public const string AssetName = "Post Battle Tuning";
        public const string ResourcePath = "Campaign Tuning/" + AssetName;

        [Header("Pages")]
        [SerializeField, Min(0f), Tooltip("Pause after a spread opens before its animations start.")]
        float _pageIntroSeconds = 0.35f;
        [SerializeField, Min(0f), Tooltip("Pause between rows of figures.")]
        float _rowIntervalSeconds = 0.15f;

        [Header("Counting figures (gold, kills, ...)")]
        [SerializeField, Min(0f), Tooltip("Time a figure takes to count up to its total.")]
        float _countUpSeconds = 0.9f;
        [SerializeField, Min(1f), Tooltip("Figures below this count one step at a time, each taking Count Up Seconds / " +
            "value; larger figures take Count Up Seconds in all.")]
        int _countUpStepsLimit = 12;

        [Header("Achievement stamps")]
        [SerializeField, Min(0f), Tooltip("Pause between one stamp landing and the next starting.")]
        float _stampIntervalSeconds = 0.45f;
        [SerializeField, Min(0.01f), Tooltip("Time a stamp takes to come down onto the page.")]
        float _stampDropSeconds = 0.28f;
        [SerializeField, Min(1f), Tooltip("Size of the stamp when it starts coming down.")]
        float _stampStartScale = 2.4f;
        [SerializeField, Range(0f, 30f), Tooltip("Largest random tilt of a stamp, in degrees.")]
        float _stampMaxTilt = 7f;
        [SerializeField, Min(0f), Tooltip("Time the glow takes to fade after the stamp lands.")]
        float _stampGlowSeconds = 0.9f;
        [SerializeField] Color _stampGlowColor = new(1f, 0.82f, 0.35f, 1f);

        [Header("XP notches")]
        [SerializeField, Min(0f), Tooltip("Time to light each XP notch.")]
        float _notchFillSeconds = 0.16f;
        [SerializeField] Color _notchFullColor = new(1f, 0.76f, 0.22f, 1f);
        [SerializeField] Color _notchGlowColor = new(1f, 0.9f, 0.5f, 0.85f);
        [SerializeField] Color _notchEmptyColor = new(0.05f, 0.04f, 0.03f, 1f);

        [Header("Level-up")]
        [SerializeField, Min(1), Tooltip("Times all the XP notches pulse together on a level-up.")]
        int _levelUpPulses = 3;
        [SerializeField, Min(0.01f), Tooltip("Length of one pulse.")]
        float _levelUpPulseSeconds = 0.3f;
        [SerializeField, Min(1f), Tooltip("How much the notches grow at the height of a pulse.")]
        float _levelUpPulseScale = 1.4f;
        [SerializeField, Min(0f), Tooltip("Time each stat glows before its result shows.")]
        float _statGlowSeconds = 0.22f;
        [SerializeField, Min(0f), Tooltip("Time the result (+1, or back to normal) shows before the next stat.")]
        float _statResultSeconds = 0.18f;
        [SerializeField] Color _statGlowColor = new(1f, 0.78f, 0.25f, 1f);
        [SerializeField] Color _statGainColor = new(0.25f, 0.55f, 0.1f, 1f);

        [Header("Sounds (optional)")]
        [SerializeField] AudioClip _tickSound;
        [SerializeField] AudioClip _stampSound;
        [SerializeField] AudioClip _levelUpSound;
        [SerializeField] AudioClip _statGainSound;

        public float PageIntroSeconds => _pageIntroSeconds;
        public float RowIntervalSeconds => _rowIntervalSeconds;
        public float CountUpSeconds => _countUpSeconds;
        public int CountUpStepsLimit => _countUpStepsLimit;
        public float StampIntervalSeconds => _stampIntervalSeconds;
        public float StampDropSeconds => _stampDropSeconds;
        public float StampStartScale => _stampStartScale;
        public float StampMaxTilt => _stampMaxTilt;
        public float StampGlowSeconds => _stampGlowSeconds;
        public Color StampGlowColor => _stampGlowColor;
        public float NotchFillSeconds => _notchFillSeconds;
        public Color NotchFullColor => _notchFullColor;
        public Color NotchGlowColor => _notchGlowColor;
        public Color NotchEmptyColor => _notchEmptyColor;
        public int LevelUpPulses => _levelUpPulses;
        public float LevelUpPulseSeconds => _levelUpPulseSeconds;
        public float LevelUpPulseScale => _levelUpPulseScale;
        public float StatGlowSeconds => _statGlowSeconds;
        public float StatResultSeconds => _statResultSeconds;
        public Color StatGlowColor => _statGlowColor;
        public Color StatGainColor => _statGainColor;
        public AudioClip TickSound => _tickSound;
        public AudioClip StampSound => _stampSound;
        public AudioClip LevelUpSound => _levelUpSound;
        public AudioClip StatGainSound => _statGainSound;

        // Time one figure takes to count up to value: one step per CountUpSeconds / value for small figures.
        public float CountUpDuration(int value) =>
            value <= 0 ? 0f : _countUpSeconds * Mathf.Min(1f, value / (float)Mathf.Max(1, _countUpStepsLimit));

        static PostBattleTuning _active;

        public static PostBattleTuning Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<PostBattleTuning>(ResourcePath);
                if (_active == null)
                {
                    _active = CreateInstance<PostBattleTuning>();
                    _active.hideFlags = HideFlags.DontSave;
                }
                return _active;
            }
        }
    }
}
