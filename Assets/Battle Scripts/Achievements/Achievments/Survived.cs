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
        List<StatSystem.RegimentStats> characters;
        public override string Description { 
            get
            {
                string description = "Survived: ";
                foreach (var unit in characters)
                {
                    description += unit.UnitName;
                }
                return description;
            } }

        public override bool Achieved()
        {
            return characters.Count > 0;    
        }

        public override void Initialise()
        {
            
        }

        public override void Reward()
        {
            
        }
    }
}