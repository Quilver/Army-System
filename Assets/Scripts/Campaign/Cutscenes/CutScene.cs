using PlayerControls;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutscene
{
    // Plays a DialogueFlow (or a DialogueScript written in markdown) over a paused battle.
    // Any key finishes typing the current line, then advances.
    public class CutScene : MonoBehaviour
    {
        [SerializeField]
        DialogueFlow dialogue;
        [SerializeField]
        TextMeshProUGUI dialogueText, speakerName;
        [SerializeField]
        Image leftPortrait, rightPortrait;
        [SerializeField]
        CameraControls cameraControls;
        [SerializeField, Tooltip("Optional. Hidden while a line has no speaker (narration).")]
        GameObject namePlate;
        [SerializeField, Min(1f), Tooltip("Typing speed in words per minute for lines without a <wpm> tag.")]
        float defaultWordsPerMinute = 250f;
        [SerializeField, Tooltip("Tint of the portrait that is not talking.")]
        Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        IReadOnlyList<DialogueLine> _lines = Array.Empty<DialogueLine>();
        DialogueView _view;
        int _index = -1;
        int _startFrame;

        void Start()
        {
            _view = new DialogueView(speakerName, namePlate, dialogueText, leftPortrait, rightPortrait, defaultWordsPerMinute, dimColor);
            _lines = dialogue != null ? dialogue.GetLines() : Array.Empty<DialogueLine>();
            GamePause.Hold(this);
            if (cameraControls != null) cameraControls.enabled = false;
            _startFrame = Time.frameCount;
            _index = -1;
            _view.Begin();
            Next();
        }

        void Update()
        {
            if (_view == null || _index < 0 || _index >= _lines.Count) return;
            _view.Tick(Time.unscaledDeltaTime);
            if (Time.frameCount == _startFrame || !Input.anyKeyDown) return;
            if (!_view.FullyShown) _view.Complete();
            else Next();
        }

        void Next()
        {
            _index++;
            if (_index >= _lines.Count)
            {
                EndCutscene();
                return;
            }
            _view.Show(_lines[_index]);
        }

        void EndCutscene()
        {
            gameObject.SetActive(false);
            if (cameraControls != null) cameraControls.enabled = true;
            GamePause.Release(this);
        }

        void OnDestroy() => GamePause.Release(this);
    }
}
