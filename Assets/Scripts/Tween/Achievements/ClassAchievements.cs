using System;
using System.Collections.Generic;
using UnityEngine;

namespace AchievementSystem
{
    // The mini quests of each character class (SU-114). Loaded from Resources/Achievement/Class Achievements. The ones every
    // character has are in AchievementTuning (SU-115); a character's own list (CampaignCharacter.Achievements) is added on top.
    // Kept in Tween because CharacterClass is battle data and the battle never references achievements.
    [CreateAssetMenu(menuName = "Achievements/Class Achievements")]
    public sealed class ClassAchievements : ScriptableObject
    {
        public const string ResourcePath = "Achievement/Class Achievements";

        [Serializable]
        public sealed class Entry
        {
            public CharacterClass characterClass;
            public List<CharacterAchievement> achievements = new();
        }

        [SerializeField]
        List<Entry> _classes = new();

        public IReadOnlyList<Entry> Classes => _classes;

        // The class's achievements (none when the class is null).
        public IEnumerable<CharacterAchievement> For(CharacterClass characterClass)
        {
            if (characterClass == null) yield break;
            foreach (var entry in _classes)
            {
                if (entry == null || entry.characterClass != characterClass) continue;
                foreach (var achievement in entry.achievements)
                    if (achievement != null) yield return achievement;
            }
        }

        static ClassAchievements _loaded;

        // The table in Resources, or null when there is none.
        public static ClassAchievements Load()
        {
            if (_loaded == null) _loaded = Resources.Load<ClassAchievements>(ResourcePath);
            return _loaded;
        }
    }
}
