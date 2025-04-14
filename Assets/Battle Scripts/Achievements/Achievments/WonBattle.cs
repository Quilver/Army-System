using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AchievementSystem
{
    [CreateAssetMenu(menuName = "Achievements/WonBattle")]
    public class WonBattle : Achievments
    {
        [SerializeField, Range(1, 50)]
        int experienceReward;

        public override string Description => "Won the battle";

        public override bool Achieved()
        {
            return true;
        }

        public override void Initialise()
        {
            Notifications.ArmyDestroyed += EndBattle;
        }
        void EndBattle(Army army)
        {
            Debug.Log("Battle over");
            if (army == null) return;
            else if (army.controller == Army.Controller.Player) {

            }
            else
            {

            }
        }
        public override void Reward()
        {
            

        }

    }
}