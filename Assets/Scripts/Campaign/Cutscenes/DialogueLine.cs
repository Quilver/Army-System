using System;
using UnityEngine;

namespace Cutscene
{
    // One text box, with who is on stage while it shows. Both dialogue players (CutScene, the tent's DialogueBox) play these.
    // The speaker's portrait is shown at full colour; the other portrait on stage is dimmed.
    [Serializable]
    public class DialogueLine
    {
        public string Speaker;
        [TextArea(2, 5)] public string Text;
        [Tooltip("Typing speed for this box in words per minute. 0 = the player's default.")]
        public float WordsPerMinute;
        [Tooltip("Which portrait is talking. None = narration (both portraits dimmed).")]
        public DialogueSide SpeakerSide;
        public string LeftName, RightName;
        public Sprite LeftPortrait, RightPortrait;

        // A line from the old one-portrait Dialogue asset: only the speaker is on stage.
        public static DialogueLine Single(string speaker, string text, Sprite portrait, bool right)
        {
            var line = new DialogueLine { Speaker = speaker, Text = text };
            if (portrait == null) return line;
            line.SpeakerSide = right ? DialogueSide.Right : DialogueSide.Left;
            if (right) { line.RightName = speaker; line.RightPortrait = portrait; }
            else { line.LeftName = speaker; line.LeftPortrait = portrait; }
            return line;
        }
    }
}
