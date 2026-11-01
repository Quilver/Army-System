using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Fire Emblem-style dialogue box for the tent: speaker name plate, text, one portrait on the left or right
    // (spec TentHub.md §1 Briefing, §4.5 Court). Plays a Cutscene.DialogueFlow or a single line.
    // Click / Space / Enter / gamepad South: finish the current line, then advance. Esc / right click / East: skip all.
    // Unlike Cutscene.CutScene it does not touch Time.timeScale or the battle camera.
    public class DialogueBox : MonoBehaviour
    {
        struct Line
        {
            public string Speaker, Text;
            public Sprite Portrait;
            public bool Right;
        }

        [SerializeField] GameObject _root;
        [SerializeField] TextMeshProUGUI _speakerName;
        [SerializeField] GameObject _namePlate;
        [SerializeField] TextMeshProUGUI _text;
        [SerializeField] Image _leftPortrait, _rightPortrait;
        [SerializeField, Min(1f), Tooltip("Characters revealed per second.")] float _revealSpeed = 70f;

        readonly List<Line> _lines = new();
        int _index;
        float _revealed;
        int _startFrame;
        Action _onComplete;

        public bool IsPlaying { get; private set; }

        void Awake()
        {
            if (!IsPlaying) _root.SetActive(false);
        }

        public void Play(Cutscene.DialogueFlow flow, Action onComplete)
        {
            _lines.Clear();
            if (flow != null)
            {
                flow.Start();
                for (var d = flow.GetNextScene(); d != null; d = flow.GetNextScene())
                    _lines.Add(new Line { Speaker = d.CharacterName, Text = d.dialogue, Portrait = d.Portrait, Right = d.leftOrRight });
            }
            Begin(onComplete);
        }

        public void PlayLine(string speaker, string text, Sprite portrait, bool right, Action onComplete)
        {
            _lines.Clear();
            if (!string.IsNullOrEmpty(text))
                _lines.Add(new Line { Speaker = speaker, Text = text, Portrait = portrait, Right = right });
            Begin(onComplete);
        }

        void Begin(Action onComplete)
        {
            _onComplete = onComplete;
            if (_lines.Count == 0)
            {
                Finish();
                return;
            }
            IsPlaying = true;
            _startFrame = Time.frameCount;
            _root.SetActive(true);
            ShowLine(0);
        }

        void ShowLine(int index)
        {
            _index = index;
            var line = _lines[index];
            _speakerName.text = line.Speaker;
            if (_namePlate != null) _namePlate.SetActive(!string.IsNullOrEmpty(line.Speaker));
            _text.text = line.Text;
            _revealed = 0f;
            _text.maxVisibleCharacters = 0;

            bool hasPortrait = line.Portrait != null;
            _leftPortrait.enabled = hasPortrait && !line.Right;
            _rightPortrait.enabled = hasPortrait && line.Right;
            if (hasPortrait) (line.Right ? _rightPortrait : _leftPortrait).sprite = line.Portrait;
        }

        void Update()
        {
            if (!IsPlaying) return;

            int total = _text.textInfo != null && _text.textInfo.characterCount > 0 ? _text.textInfo.characterCount : _text.text.Length;
            bool fullyShown = _revealed >= total;
            if (!fullyShown)
            {
                _revealed += _revealSpeed * Time.unscaledDeltaTime;
                _text.maxVisibleCharacters = Mathf.FloorToInt(_revealed);
            }

            // Ignore the click / key that started the dialogue.
            if (Time.frameCount == _startFrame) return;

            if (TentInput.CancelPressed)
            {
                Finish();
                return;
            }
            if (!TentInput.AdvancePressed) return;
            if (!fullyShown)
            {
                _revealed = total;
                _text.maxVisibleCharacters = int.MaxValue;
                return;
            }
            if (_index + 1 < _lines.Count) ShowLine(_index + 1);
            else Finish();
        }

        void Finish()
        {
            IsPlaying = false;
            _root.SetActive(false);
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }
    }
}
