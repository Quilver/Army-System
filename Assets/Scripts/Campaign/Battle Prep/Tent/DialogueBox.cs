using System;
using System.Collections.Generic;
using Cutscene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Fire Emblem-style dialogue box for the tent: speaker name plate, text, a portrait on the left and right
    // (spec TentHub.md §1 Briefing, §4.5 Court). Plays a Cutscene.DialogueFlow (including a DialogueScript written in
    // markdown) or a single line. The talking portrait is shown at full colour, the other one dimmed.
    // Click / Space / Enter / gamepad South: finish the current line, then advance. Esc / right click / East: skip all.
    // Unlike Cutscene.CutScene it does not touch Time.timeScale or the battle camera.
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] GameObject _root;
        [SerializeField] TextMeshProUGUI _speakerName;
        [SerializeField] GameObject _namePlate;
        [SerializeField] TextMeshProUGUI _text;
        [SerializeField] Image _leftPortrait, _rightPortrait;
        [SerializeField, Min(1f), Tooltip("Typing speed in words per minute for lines without a <wpm> tag.")]
        float _defaultWordsPerMinute = 250f;
        [SerializeField, Tooltip("Tint of the portrait that is not talking.")]
        Color _dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        readonly List<DialogueLine> _lines = new();
        DialogueView _view;
        int _index;
        int _startFrame;
        Action _onComplete;

        public bool IsPlaying { get; private set; }

        DialogueView View => _view ??= new DialogueView(_speakerName, _namePlate, _text, _leftPortrait, _rightPortrait,
                                                        _defaultWordsPerMinute, _dimColor);

        void Awake()
        {
            if (!IsPlaying) _root.SetActive(false);
        }

        public void Play(DialogueFlow flow, Action onComplete)
        {
            _lines.Clear();
            if (flow != null)
                foreach (var line in flow.GetLines())
                    if (line != null) _lines.Add(line);
            Begin(onComplete);
        }

        public void PlayLine(string speaker, string text, Sprite portrait, bool right, Action onComplete)
        {
            _lines.Clear();
            if (!string.IsNullOrEmpty(text))
                _lines.Add(DialogueLine.Single(speaker, text, portrait, right));
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
            View.Begin();
            ShowLine(0);
        }

        void ShowLine(int index)
        {
            _index = index;
            View.Show(_lines[index]);
        }

        void Update()
        {
            if (!IsPlaying) return;

            View.Tick(Time.unscaledDeltaTime);

            // Ignore the click / key that started the dialogue.
            if (Time.frameCount == _startFrame) return;

            if (TentInput.CancelPressed)
            {
                Finish();
                return;
            }
            if (!TentInput.AdvancePressed) return;
            if (!View.FullyShown)
            {
                View.Complete();
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
