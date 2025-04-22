using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Survived")]
    public class Survived : Achievments
    {
        [SerializeField, Range(1, 10)]
        int XpForSurviving;
        List<Campaign.StatWrapper> characters;
        public override string Description { 
            get
            {
                string description = "Survived: ";
                foreach (var unit in characters)
                {
                    description += unit.statBase.UnitName+", ";
                }
                return description;
            } }

        public override bool Achieved()
        {
            return characters.Count > 0;    
        }

        public override void Initialise()
        {
            characters = Campaign.CampaignDataManager.instance.deployedCharacters;
        }

        public override void Reward()
        {
            foreach (var character in characters)
            {
                GiveExperience(character.statBase, XpForSurviving);
            }
        }
    }
}