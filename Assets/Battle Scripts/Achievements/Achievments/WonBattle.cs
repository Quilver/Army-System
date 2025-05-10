using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            throw new System.NotImplementedException();
            //Notifications.ArmyDestroyed += EndBattle;
        }
        void EndBattle(ArmyData army)
        {
            Debug.Log("Battle over: "+army.gameObject.name);
            if (army == null) return;
            else if (army.controller == Army.Controller.Player) {
                Debug.Log("Lost");
                SceneManager.LoadScene(0);
            }
            else
            {
                Debug.Log("Victory");
                BattleReport.Achievements.Add(this);
                BattleReport.timeTaken = Time.time - BattleReport.timeTaken;
                SceneManager.LoadScene(2);
            }
        }
        public override void Reward()
        {

            foreach (var character in BattleReport.DeployedCharacters)
            {
                GiveExperience(character.statBase, experienceReward);
                character.statBase.AddXP(experienceReward);
            }
        }

    }
}