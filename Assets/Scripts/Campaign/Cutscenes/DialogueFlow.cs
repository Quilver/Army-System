using System.Collections.Generic;
using UnityEngine;
namespace Cutscene
{
    [CreateAssetMenu(menuName = "Cutscenes/Dialogue flow")]
    public class DialogueFlow : ScriptableObject
    {
        [SerializeField]
        List<Dialogue> dialogue;
        int sceneIndex = -1;
        public void Start()
        {
            sceneIndex= -1;
        }
        public Dialogue GetNextScene()
        {
            sceneIndex++;
            if (dialogue == null || sceneIndex >= dialogue.Count) { return null; }
            return dialogue[sceneIndex];
        }

        // Every text box in order. A DialogueScript (built from a markdown file) overrides this.
        public virtual IReadOnlyList<DialogueLine> GetLines()
        {
            var lines = new List<DialogueLine>();
            if (dialogue == null) return lines;
            foreach (var d in dialogue)
                if (d != null) lines.Add(DialogueLine.Single(d.CharacterName, d.dialogue, d.Portrait, d.leftOrRight));
            return lines;
        }
    }
}
