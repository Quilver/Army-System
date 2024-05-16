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

        public override string Description => throw new System.NotImplementedException();

        public override bool Achieved()
        {
            throw new System.NotImplementedException();
        }

        public override void Initialise()
        {
            throw new System.NotImplementedException();
        }

        public override void Reward()
        {
            throw new System.NotImplementedException();
        }
    }
}