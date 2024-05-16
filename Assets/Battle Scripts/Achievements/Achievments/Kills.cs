using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/Kills")]
    public class Kills : Achievments
    {
        [SerializeField, Range(1, 10)]
        int XPperKill;

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