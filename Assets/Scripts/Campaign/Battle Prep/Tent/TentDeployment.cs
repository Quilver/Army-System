using System.Collections.Generic;
using Campaign;

namespace BattlePrep.Tent
{
    // Deploying roster characters on the Map (spec TentHub.md §4.3, §5; SU-50, SU-52). Deploying costs the character's
    // cost to field in gold; undeploying refunds it. At most LaunchRules.MaximumDeployed characters.
    public static class TentDeployment
    {
        public static List<RosterCharacter> Deployed => BattleReport.DeployedRoster;

        public static bool IsDeployed(RosterCharacter character) => Deployed.Contains(character);

        public static int GoldSpent
        {
            get
            {
                int spent = 0;
                foreach (var c in Deployed) spent += c.costToField;
                return spent;
            }
        }

        public static bool CanDeploy(RosterCharacter character, out string reason)
        {
            reason = "";
            if (IsDeployed(character)) return true;
            if (Deployed.Count >= LaunchRules.MaximumDeployed)
            {
                reason = $"Army is full ({LaunchRules.MaximumDeployed}/{LaunchRules.MaximumDeployed})";
                return false;
            }
            if (character.costToField > CampaignDataManager.Data.Money)
            {
                reason = "Not enough gold";
                return false;
            }
            return true;
        }

        public static void Toggle(RosterCharacter character)
        {
            if (character == null) return;
            var data = CampaignDataManager.Data;
            if (IsDeployed(character))
            {
                Deployed.Remove(character);
                data.Money += character.costToField;
            }
            else if (CanDeploy(character, out _))
            {
                Deployed.Add(character);
                data.Money -= character.costToField;
            }
        }
    }
}
