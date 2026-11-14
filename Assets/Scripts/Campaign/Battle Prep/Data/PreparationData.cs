using EndGameUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BattlePrep
{
    // A mission's pre-battle sheet. Its words come from story-vault notes: the briefing and court talk are Dialogue
    // Scripts, the loans are Loan Offers, each built from a markdown note (Assets/Leveraged Wars Story).
    [CreateAssetMenu(menuName = "Preparations/data")]
    public class PreparationData : ScriptableObject
    {
        [Tooltip("Picture on the war map.")]
        public Sprite image;

        // --- Tent hub (Docs/PreBattle/TentHub.md §6) ---
        [Header("Tent hub")]
        [Tooltip("Briefing played as dialogue when the tent opens, and shown as text on the war map. A Dialogue Script " +
                 "made from a story note. Empty: no briefing.")]
        public Cutscene.DialogueScript briefingFlow;
        [Tooltip("Loan offers pinned up in War Debts, in this order. Loan Offers are made from notes in the story " +
                 "vault's 3. Loans folder.")]
        public List<LoanOffer> loans = new();
        [Tooltip("The three court members: 0 Tactician (at the war table), 1 Captain (by the pole), 2 Dispatch-bearer (on the stool).")]
        public CourtMember[] court = new CourtMember[3];
        [Tooltip("Shows the glowing sealed dispatch on the dispatch-bearer.")]
        public bool questAvailable;

        // --- Deployment (SU-117) ---
        [Header("Deployment")]
        [Tooltip("Characters deployed automatically for this mission, free of charge, and locked: they cannot be undeployed.")]
        public List<CampaignCharacter> mandatoryCharacters = new();
        [Tooltip("Most characters the player may deploy to this mission (SU-119).")]
        [Min(1)] public int maxDeployed = 5;

        // --- Roster (SU-119) ---
        [Header("Roster")]
        [Tooltip("Characters who join the roster when this mission's tent opens, at level 1. They are not deployed " +
                 "unless they are also mandatory.")]
        public List<CampaignCharacter> newCharacters = new();

        // Whether this mission forces the character into the army.
        public bool IsMandatory(CampaignCharacter character) =>
            character != null && mandatoryCharacters != null && mandatoryCharacters.Contains(character);

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
        [Tooltip("Played when the player talks to this member: a Dialogue Script made from a story note. Empty: a " +
                 "built-in placeholder line.")]
        public Cutscene.DialogueScript dialogue;
    }
}
