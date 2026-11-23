using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Cutscene
{
    // A dialogue scene written as a markdown note (format in DialogueMarkdown). Every dialogue slot takes one: the
    // mission briefing, court members and the battle CutScene.
    // Story-vault notes with the property "dialogue: true" get one automatically, next to the note; for other .md files
    // right click > Create > Cutscenes > Dialogue Script from Markdown. The editor rebuilds it whenever the note or a
    // portrait it uses changes; the baked lines below are what the game plays.
    public class DialogueScript : MarkdownAsset
    {
        [SerializeField] List<DialogueLine> _lines = new();

        public IReadOnlyList<DialogueLine> GetLines() => _lines;

        public void SetLines(List<DialogueLine> lines, List<string> warnings)
        {
            _lines = lines ?? new List<DialogueLine>();
            SetWarnings(warnings);
        }

        // The scene's primary speaker: the first character who talks with a portrait on stage (narration and names
        // never given an image are skipped). The tent shows a court member as this character (SU-187).
        public bool TryGetPrimarySpeaker(out string speaker, out Sprite portrait)
        {
            foreach (var line in _lines)
            {
                if (line == null) continue;
                var sprite = line.SpeakerSide == DialogueSide.Left ? line.LeftPortrait
                           : line.SpeakerSide == DialogueSide.Right ? line.RightPortrait : null;
                if (sprite == null) continue;
                speaker = line.Speaker;
                portrait = sprite;
                return true;
            }
            speaker = null;
            portrait = null;
            return false;
        }

        // The scene as plain text, one paragraph per text box ("Speaker: text"); narration has no speaker.
        // Used where a scene is read rather than played, e.g. the briefing on the war map.
        public string AsText()
        {
            var sb = new StringBuilder();
            foreach (var line in _lines)
            {
                if (line == null || string.IsNullOrEmpty(line.Text)) continue;
                if (sb.Length > 0) sb.Append("\n\n");
                if (!string.IsNullOrEmpty(line.Speaker)) sb.Append("<b>").Append(line.Speaker).Append(":</b> ");
                sb.Append(line.Text);
            }
            return sb.ToString();
        }
    }
}
