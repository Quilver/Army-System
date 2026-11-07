using System;
using UnityEngine;

namespace AchievementSystem
{
    // An achievement for the whole battle (SU-114): victory, side objectives. Gold and prestige go to the campaign; XP goes
    // to every roster character.
    [CreateAssetMenu(menuName = "Achievements/Global Achievement")]
    public class GlobalAchievement : Achievement
    {
        [SerializeReference, SubclassSelector]
        GlobalRequirement _requirement;

        public override Requirement Requirement => _requirement;

        public static GlobalAchievement CreateCustom(CustomGlobalAchievement data)
        {
            var achievement = CreateInstance<GlobalAchievement>();
            achievement.name = string.IsNullOrEmpty(data._title) ? "Custom global achievement" : data._title;
            achievement.SetData(data._icon, data._title, data._description, data.rewards);
            achievement._requirement = data._requirement;
            return achievement;
        }
    }

    [Serializable]
    public class CustomGlobalAchievement : CustomAchievementData
    {
        [SerializeReference, SubclassSelector]
        public GlobalRequirement _requirement;
    }
}
