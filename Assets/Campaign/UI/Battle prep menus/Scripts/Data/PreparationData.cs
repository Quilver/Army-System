using EndGameUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    [CreateAssetMenu(menuName = "Preparations/data")]
    public class PreparationData : ScriptableObject
    {
        [SerializeField, TextArea(5, 15)]
        public string briefing;
        [SerializeField]
        public Sprite image;
        [SerializeField]
        public List<SellOrders> buyOrders;

        // --- Tent hub (Docs/PreBattle/TentHub.md §6) ---
        [Header("Tent hub")]
        [Tooltip("Briefing played as dialogue when the tent opens. Empty: the briefing text above is shown as one line.")]
        public Cutscene.DialogueFlow briefingFlow;
        [Tooltip("Speaker for the briefing text when there is no briefing flow.")]
        public string briefingSpeaker = "Briefing";
        [Tooltip("The three court members: 0 Tactician (at the war table), 1 Captain (by the pole), 2 Dispatch-bearer (on the stool).")]
        public CourtMember[] court = new CourtMember[3];
        [Tooltip("Shows the glowing sealed dispatch on the dispatch-bearer.")]
        public bool questAvailable;

        // The court member at index, or null.
        public CourtMember Court(int index) =>
            court != null && index >= 0 && index < court.Length ? court[index] : null;
    }

    // A court member for one mission (TentHub.md §4.5, PB-Q5). Relationships and quests are not built yet.
    [System.Serializable]
    public class CourtMember
    {
        public string name;
        public Sprite portrait;
        [Tooltip("Played when the player talks to this member. Empty: the default line is used.")]
        public Cutscene.DialogueFlow dialogue;
        [TextArea(1, 4)]
        public string defaultLine;
    }
}
