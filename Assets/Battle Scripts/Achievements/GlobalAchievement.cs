using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Global Achievement")]
    public class GlobalAchievement : Achievement
    {
        [SerializeReference, SubclassSelector]
        RequirementGlobal _requirement;
        [SerializeReference, SubclassSelector]
        List<Reward> rewards;
        [SerializeField, TextArea]
        string _description;
        public override string Description => _description;
        public static GlobalAchievement CreateCustom(CustomGlobalAchievement data)
        {
            GlobalAchievement globalAchievement = new();
            globalAchievement.icon = data._icon;
            globalAchievement._description = data._description;
            globalAchievement._requirement = data._requirement;
            globalAchievement.rewards = data.rewards;
            return globalAchievement;
        }
        public void Initialise()
        {
            _requirement.Setup();
            BattleReport.Achievements.Add(this);
        }
        public override bool Achieved() => _requirement.Succeeded() > 0;
        public override void Reward()
        {
            foreach (var reward in rewards)
                reward.GiveReward(_requirement.Succeeded());
        }
    }
    [Serializable]
    public class CustomGlobalAchievement
    {
        [SerializeField]
        public Sprite _icon;
        [SerializeReference, SubclassSelector]
        public RequirementGlobal _requirement;
        [SerializeReference, SubclassSelector]
        public List<Reward> rewards;
        [SerializeField, TextArea]
        public string _description;
    }
}