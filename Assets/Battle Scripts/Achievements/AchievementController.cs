using System.Collections;
using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace AchievementSystem
{
    // Sets up the battle's achievements: the global ones listed here, and for each character deployed from the roster,
    // the achievements on its CampaignCharacter asset, tied to the troop that character leads.
    public class AchievementController : MonoBehaviour
    {
        public List<CharacterAchievement> _achievementsPC;
        public List<GlobalAchievement> _GlobalAchievements;
        public List<CustomGlobalAchievement> _CustomGlobalAchievements;
        public List<CustomCharacterAchievement> _CustomPcAchievements;

        void Start()
        {
            StartCoroutine(InitAchievements());
        }

        IEnumerator InitAchievements()
        {
            foreach (CustomCharacterAchievement achievement in _CustomPcAchievements)
                _achievementsPC.Add(CharacterAchievement.CreateCustom(achievement));
            foreach (CustomGlobalAchievement achievement in _CustomGlobalAchievements)
                _GlobalAchievements.Add(GlobalAchievement.CreateCustom(achievement));
            // Wait a frame so deployment has spawned the roster's troops.
            yield return null;

            if (BattleReport.Exists && Battle.Instance != null)
                foreach (RosterCharacter character in BattleReport.DeployedRoster)
                {
                    Troop troop = TroopOf(character);
                    if (troop == null || character.character == null) continue;
                    foreach (var achievement in character.character.Achievements)
                    {
                        if (achievement == null) continue;
                        CharacterAchievement instance = Instantiate(achievement);
                        instance.Initialise(troop, character);
                    }
                }
            foreach (var achievement in _GlobalAchievements)
                achievement.Initialise();
        }

        static Troop TroopOf(RosterCharacter character)
        {
            Army army = Battle.Instance.player;
            if (army == null || character == null || character.character == null) return null;
            foreach (var troop in army.Troops)
                if (troop.Profile != null)
                    foreach (var member in troop.Profile.Characters)
                        if (member == character.character) return troop;
            return null;
        }
    }
}
