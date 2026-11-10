using System.Collections.Generic;
using UnityEngine;

namespace Cutscene
{
    // A dialogue scene written as a markdown file (see DialogueMarkdown for the format).
    // It is a DialogueFlow, so it drops into any DialogueFlow slot (mission briefing, court members, CutScene).
    // Create one with right click on a .md file > Create > Cutscenes > Dialogue Script from Markdown. The editor rebuilds
    // it whenever the .md file or a portrait it uses changes; the baked lines below are what the game plays.
    [CreateAssetMenu(menuName = "Cutscenes/Dialogue Script (empty)")]
    public class DialogueScript : DialogueFlow
    {
        [SerializeField, Tooltip("The markdown file this scene is built from.")]
        TextAsset _source;
        [SerializeField] List<DialogueLine> _lines = new();
        [SerializeField] List<string> _warnings = new();

        public TextAsset Source { get => _source; set => _source = value; }
        public IReadOnlyList<string> Warnings => _warnings;

        public override IReadOnlyList<DialogueLine> GetLines() => _lines;

        public void SetLines(List<DialogueLine> lines, List<string> warnings)
        {
            _lines = lines ?? new List<DialogueLine>();
            _warnings = warnings ?? new List<string>();
        }
    }
}
