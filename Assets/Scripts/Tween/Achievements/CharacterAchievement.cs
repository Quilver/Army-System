using System;
using System.Collections.Generic;
using UnityEngine;

namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Character Achievement")]
    public class CharacterAchievement : Achievement
    {
        [SerializeReference, SubclassSelector]
        RequirementsPCs _requirement;
        [SerializeReference, SubclassSelector]
        List<Reward> rewards;
        [SerializeField, TextArea]
        string _description;

        public override string Description => (_character != null ? _character.Name + " " : "") + _description;

        // Specific to instance
        Campaign.RosterCharacter _character;

        public void Initialise(Troop troop, Campaign.RosterCharacter character)
        {
            _character = character;
            name = character.Name + " " + name;
            _requirement.Setup(character, troop);
            if (_requirement.Valid())
                BattleReport.Achievements.Add(this);
        }

        public override bool Achieved() => _requirement.Succeeded() > 0;

        public override void Reward()
        {
            foreach (var reward in rewards)
                reward.GiveReward(_character, _requirement.Succeeded());
        }

        public static CharacterAchievement CreateCustom(CustomCharacterAchievement data)
        {
            CharacterAchievement achievement = ScriptableObject.CreateInstance<CharacterAchievement>();
            achievement.icon = data._icon;
            achievement._description = data._description;
            achievement._requirement = data._requirement;
            achievement.rewards = data.rewards;
            return achievement;
        }
    }

    [Serializable]
    public class CustomCharacterAchievement
    {
        [SerializeField]
        public Sprite _icon;
        [SerializeReference, SubclassSelector]
        public RequirementsPCs _requirement;
        [SerializeReference, SubclassSelector]
        public List<Reward> rewards;
        [SerializeField, TextArea]
        public string _description;
    }
}
