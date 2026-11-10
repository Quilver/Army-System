using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndGameUI
{
    public class EndGameButton : MonoBehaviour
    {
        public void Click() => Continue();

        // Leaves the post-battle screen for the next mission's preparation (also the post-battle book's Continue button).
        public static void Continue()
        {
            // Rewards of achievements whose cards had not appeared yet.
            AchievementSystem.Achievement.GrantAll(BattleReport.Achievements);
            // With a mission catalog, the finished mission's next mission (SU-119).
            var data = Campaign.CampaignDataManager.Data;
            var finished = Campaign.Missions.MissionCatalog.ByNumber(data.CurrentLevel);
            if (finished != null)
            {
                int next = Campaign.Missions.MissionCatalog.NumberOf(finished.nextMission);
                BattleReport.Reset();
                if (next > 0)
                {
                    data.CurrentLevel = next;
                    SceneManager.LoadScene(1);
                }
                else Debug.Log($"Game over: {finished.DisplayName} has no next mission.");
                return;
            }
            Campaign.CampaignDataManager.Data.CurrentLevel++;
            BattleReport.Reset();
            if (SceneManager.sceneCountInBuildSettings + 3 > Campaign.CampaignDataManager.Data.CurrentLevel)
                SceneManager.LoadScene(1);
            else
                Debug.Log("Game over");
        }
    }
}