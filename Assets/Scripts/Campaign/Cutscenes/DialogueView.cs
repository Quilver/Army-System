using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutscene
{
    // Shows DialogueLines in a name plate, a text box and two portraits. Types each line at its words per minute and dims
    // the portrait that is not talking. Shared by CutScene and the tent's DialogueBox, which own the input.
    public class DialogueView
    {
        const float TintFadeSeconds = 0.15f;

        readonly TextMeshProUGUI _speakerName, _text;
        readonly GameObject _namePlate;
        readonly Image _left, _right;
        readonly float _defaultWordsPerMinute;
        readonly Color _dimColor;

        float _revealed, _charsPerSecond;
        int _total;
        bool _snapTint = true;
        Color _leftTarget = Color.white, _rightTarget = Color.white;

        public DialogueView(TextMeshProUGUI speakerName, GameObject namePlate, TextMeshProUGUI text, Image left, Image right,
                            float defaultWordsPerMinute, Color dimColor)
        {
            _speakerName = speakerName;
            _namePlate = namePlate;
            _text = text;
            _left = left;
            _right = right;
            _defaultWordsPerMinute = Mathf.Max(1f, defaultWordsPerMinute);
            _dimColor = dimColor;
        }

        public bool FullyShown => _revealed >= _total;

        // Call when a new conversation starts so the first line's portraits appear at their tint without fading.
        public void Begin() => _snapTint = true;

        public void Show(DialogueLine line)
        {
            string speaker = line.Speaker ?? "";
            if (_speakerName != null) _speakerName.text = speaker;
            if (_namePlate != null) _namePlate.SetActive(speaker.Length > 0);

            string text = line.Text ?? "";
            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate(true);
            var info = _text.textInfo;
            _total = info != null && info.characterCount > 0 ? info.characterCount : text.Length;
            int words = info != null && info.wordCount > 0 ? info.wordCount : CountWords(text);

            // Words per minute sets how long the whole box takes to type; characters are revealed evenly over that time.
            float wpm = line.WordsPerMinute > 0f ? line.WordsPerMinute : _defaultWordsPerMinute;
            float seconds = Mathf.Max(1, words) * 60f / wpm;
            _charsPerSecond = _total / seconds;
            _revealed = 0f;

            _leftTarget = SetPortrait(_left, line.LeftPortrait, line.SpeakerSide == DialogueSide.Left);
            _rightTarget = SetPortrait(_right, line.RightPortrait, line.SpeakerSide == DialogueSide.Right);
            _snapTint = false;
        }

        public void Tick(float deltaTime)
        {
            if (_revealed < _total)
            {
                _revealed = Mathf.Min(_total, _revealed + _charsPerSecond * deltaTime);
                _text.maxVisibleCharacters = _revealed >= _total ? int.MaxValue : Mathf.FloorToInt(_revealed);
            }
            Fade(_left, _leftTarget, deltaTime);
            Fade(_right, _rightTarget, deltaTime);
        }

        // Finish typing the current line at once.
        public void Complete()
        {
            _revealed = _total;
            _text.maxVisibleCharacters = int.MaxValue;
        }

        Color SetPortrait(Image image, Sprite sprite, bool speaking)
        {
            var target = speaking ? Color.white : _dimColor;
            if (image == null) return target;
            bool wasShown = image.enabled && image.sprite != null;
            image.enabled = sprite != null;
            if (sprite == null) return target;
            image.sprite = sprite;
            if (_snapTint || !wasShown) image.color = target;   // a portrait entering the stage starts at its tint
            return target;
        }

        static void Fade(Image image, Color target, float deltaTime)
        {
            if (image == null || !image.enabled) return;
            image.color = Vector4.MoveTowards(image.color, target, deltaTime / TintFadeSeconds);
        }

        static int CountWords(string text)
        {
            int count = 0;
            bool inWord = false;
            foreach (char c in text)
            {
                bool space = char.IsWhiteSpace(c);
                if (!space && !inWord) count++;
                inWord = !space;
            }
            return count;
        }
    }
}
