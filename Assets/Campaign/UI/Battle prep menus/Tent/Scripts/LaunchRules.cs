using UnityEngine.SceneManagement;

namespace BattlePrep.Tent
{
    // When the army may march out (spec TentHub.md §5). Same rules and messages as the old ToBattleButton, plus the
    // deploy cap of 5 (SU-50), enforced when a character is deployed (TentDeployment.CanDeploy).
    public static class LaunchRules
    {
        public const int MinimumDeployed = 2;
        // At most this many characters can be deployed to a mission (SU-50).
        public const int MaximumDeployed = 5;
        // Build indices before the first level scene (ToBattleButton.NextLevel).
        const int EndOfNonLevelSceneIndexes = 2;

        public static int DeployedCount => BattleReport.DeployedRoster?.Count ?? 0;

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
            SceneManager.LoadScene(Campaign.CampaignDataManager.Data.CurrentLevel + EndOfNonLevelSceneIndexes);
            return true;
        }
    }
}
