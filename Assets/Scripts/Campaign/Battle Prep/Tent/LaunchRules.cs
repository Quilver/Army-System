using UnityEngine.SceneManagement;

namespace BattlePrep.Tent
{
    // When the army may march out (spec TentHub.md §5). Same rules and messages as the old ToBattleButton, plus the
    // deploy cap (SU-50; per mission since SU-119), enforced when a character is deployed (TentDeployment.CanDeploy).
    public static class LaunchRules
    {
        public const int MinimumDeployed = 2;
        // Deploy cap of a mission that does not set one (SU-50).
        public const int DefaultMaximumDeployed = 5;
        // At most this many characters can be deployed to this mission: PreparationData.maxDeployed (SU-119).
        public static int MaximumDeployed
        {
            get
            {
                var data = TentHubController.Data;
                return data != null && data.maxDeployed > 0 ? data.maxDeployed : DefaultMaximumDeployed;
            }
        }
        // Build indices before the first level scene (ToBattleButton.NextLevel). Only used without a MissionCatalog.
        const int EndOfNonLevelSceneIndexes = 2;

        public static int DeployedCount => BattleReport.DeployedCharacters?.Count ?? 0;

        public static bool CanLaunch(out string reason)
        {
            int deployed = DeployedCount;
            if (deployed >= MinimumDeployed)
            {
                reason = $"March out to battle ({deployed}/{MaximumDeployed} deployed)";
                return true;
            }
            if (Campaign.CampaignDataManager.Data.Money == 0) reason = "Needs money, get a loan";
            else if (deployed == 0) reason = "Select units for battle";
            else reason = "Need at least 2 units for battle";
            return false;
        }

        public static bool TryLaunch()
        {
            if (!CanLaunch(out _)) return false;
            // The current mission's battle scene (SU-119); without a mission catalog, the old build-index order.
            var mission = Campaign.Missions.MissionCatalog.Current;
            if (mission != null) return mission.LoadBattle();
            SceneManager.LoadScene(Campaign.CampaignDataManager.Data.CurrentLevel + EndOfNonLevelSceneIndexes);
            return true;
        }
    }
}
