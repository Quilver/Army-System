using System.Collections.Generic;
using UnityEngine;

namespace Cutscene
{
    // A ScriptableObject built from a markdown note: a note in the story vault (Assets/Leveraged Wars Story) or any .md
    // file. The editor rebuilds it whenever the note is saved; the game only reads what was built.
    // Kinds: DialogueScript (dialogue and cutscenes) and BattlePrep.LoanOffer (loan offers).
    public abstract class MarkdownAsset : ScriptableObject
    {
        [SerializeField, Tooltip("The markdown note this is built from.")]
        TextAsset _source;
        [SerializeField, HideInInspector]
        string _sourcePath;   // where the note was at the last build; finds it again after a move outside Unity
        [SerializeField] List<string> _warnings = new();

        public TextAsset Source { get => _source; set => _source = value; }
        public string SourcePath { get => _sourcePath; set => _sourcePath = value; }
        public IReadOnlyList<string> Warnings => _warnings;

        public void SetWarnings(List<string> warnings) => _warnings = warnings ?? new List<string>();
    }
}
