using System;
using UnityEngine;

namespace AchievementSystem
{
    // A character's mini quest (SU-114): measured on the troop the deployed character leads, rewards (XP) go to that
    // character. Listed on a character class (ClassAchievements), a campaign character, or the battle's controller.
    [CreateAssetMenu(menuName = "Achievements/Character Achievement")]
    public class CharacterAchievement : Achievement
    {
        [SerializeReference, SubclassSelector]
        CharacterRequirement _requirement;

        public override Requirement Requirement => _requirement;

        public override string Description
        {
            get
            {
                string description = base.Description ?? "";
                if (Character == null) return description.Replace("{name}", "This character");
                return description.Contains("{name}")
                    ? description.Replace("{name}", Character.Name)
                    : Character.Name + " " + description;
            }
        }

        public static CharacterAchievement CreateCustom(CustomCharacterAchievement data)
        {
            var achievement = CreateInstance<CharacterAchievement>();
            achievement.name = string.IsNullOrEmpty(data._title) ? "Custom character achievement" : data._title;
            achievement.SetData(data._icon, data._title, data._description, data.rewards);
            achievement._requirement = data._requirement;
            return achievement;
        }
    }

    [Serializable]
    public class CustomCharacterAchievement : CustomAchievementData
    {
        [SerializeReference, SubclassSelector]
        public CharacterRequirement _requirement;
    }
}
