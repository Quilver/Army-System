using System;
using System.Collections;
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
        public override string Description => _pcs.statBase.UnitName + " " + _description;

        //Specific to instance
        Campaign.PCWrapper _pcs;
        public void Initialise(IUnit _units, Campaign.PCWrapper pcs)
        {
            _pcs = pcs;
            name = _pcs.statBase.name + " " + name; 
            _requirement.Setup(_pcs, _units);
            if(_requirement.Valid())
                BattleReport.Achievements.Add(this);
        }
        public override bool Achieved() => _requirement.Succeeded() > 0;
        public override void Reward()
        {
            foreach (var reward in rewards)
                reward.GiveReward(_pcs, _requirement.Succeeded());
        }
        public static CharacterAchievement CreateCustom(CustomCharacterAchievement data)
        {
            CharacterAchievement globalAchievement = ScriptableObject.CreateInstance<CharacterAchievement>();
            globalAchievement.icon = data._icon;
            globalAchievement._description = data._description;
            globalAchievement._requirement = data._requirement;
            globalAchievement.rewards = data.rewards;
            return globalAchievement;
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