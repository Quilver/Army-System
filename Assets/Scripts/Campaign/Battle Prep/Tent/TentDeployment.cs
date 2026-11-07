using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace BattlePrep.Tent
{
    // Deploying roster characters on the Map (spec TentHub.md §4.3, §5; SU-50, SU-52, SU-117). Deploying costs the
    // character's cost to field in gold; undeploying refunds it. At most LaunchRules.MaximumDeployed characters.
    // Mandatory characters (PreparationData.mandatoryCharacters) are deployed when the tent opens, cost nothing and
    // cannot be undeployed (SU-117).
    // Composite troops (SU-117): a deployed character dragged onto another deployed character serves under it, and the two
    // fight as one troop led by the character it was dropped on. At most RosterTroops.MaxCharactersPerTroop (2) per troop.
    public static class TentDeployment
    {
        public static List<RosterCharacter> Deployed => BattleReport.DeployedCharacters;
        // Follower -> the character it serves under.
        static Dictionary<RosterCharacter, RosterCharacter> Leaders => BattleReport.DeployedLeaders;

        // Goes up whenever the deployment or a pairing changes, so views can tell when to redraw.
        public static int Version { get; private set; }

        public static bool IsDeployed(RosterCharacter character) => character != null && Deployed.Contains(character);

        public static bool IsMandatory(RosterCharacter character)
        {
            var data = TentHubController.Data;
            return character != null && data != null && data.IsMandatory(character.character);
        }

        // Gold this character costs to deploy: nothing for a mandatory character.
        public static int CostOf(RosterCharacter character) =>
            character == null || IsMandatory(character) ? 0 : character.costToField;

        public static int GoldSpent
        {
            get
            {
                int spent = 0;
                foreach (var c in Deployed) spent += CostOf(c);
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
            if (CostOf(character) > CampaignDataManager.Data.Money)
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
                if (IsMandatory(character)) return; // locked in
                Unpair(character);
                Deployed.Remove(character);
                data.Money += CostOf(character);
            }
            else if (CanDeploy(character, out _))
            {
                Deployed.Add(character);
                data.Money -= CostOf(character);
            }
            Changed();
        }

        // Deploys this mission's mandatory characters, free (called when the tent opens). A mandatory character missing
        // from the roster is added to it at level 1.
        public static void DeployMandatory(PreparationData mission, int defaultCostToField)
        {
            if (mission == null || mission.mandatoryCharacters == null) return;
            var roster = CampaignDataManager.Data.Roster;
            foreach (CampaignCharacter character in mission.mandatoryCharacters)
            {
                if (character == null) continue;
                RosterCharacter entry = roster.Find(r => r != null && r.character == character);
                if (entry == null)
                {
                    entry = new RosterCharacter { character = character, costToField = defaultCostToField };
                    roster.Add(entry);
                    Debug.Log($"Tent: mandatory character {character.CharacterName} was not in the roster; added at level 1.");
                }
                if (!IsDeployed(entry)) Deployed.Add(entry);
            }
            if (Deployed.Count > LaunchRules.MaximumDeployed)
                Debug.LogWarning($"Tent: {mission.name} has more mandatory characters than the deploy cap " +
                                 $"({Deployed.Count}/{LaunchRules.MaximumDeployed}).");
            Changed();
        }

        // Adds this mission's new characters to the roster at level 1 (called when the tent opens, SU-119). Characters
        // already in the roster are left as they are. Returns how many joined.
        public static int AddNewCharacters(PreparationData mission, int defaultCostToField)
        {
            if (mission == null || mission.newCharacters == null) return 0;
            var roster = CampaignDataManager.Data.Roster;
            int added = 0;
            foreach (CampaignCharacter character in mission.newCharacters)
            {
                if (character == null || roster.Exists(r => r != null && r.character == character)) continue;
                roster.Add(new RosterCharacter { character = character, costToField = defaultCostToField });
                added++;
                Debug.Log($"Tent: {character.CharacterName} joins the roster ({mission.name}).");
            }
            return added;
        }

        // --- Composite troops ---

        // The character this one serves under, or null when it leads its own troop.
        public static RosterCharacter LeaderOf(RosterCharacter character) =>
            character != null && Leaders.TryGetValue(character, out var leader) && IsDeployed(leader) ? leader : null;

        // The characters serving under this one (deployed order).
        public static List<RosterCharacter> FollowersOf(RosterCharacter leader)
        {
            var result = new List<RosterCharacter>();
            if (leader == null) return result;
            foreach (var c in Deployed)
                if (c != null && LeaderOf(c) == leader) result.Add(c);
            return result;
        }

        public static bool IsFollower(RosterCharacter character) => LeaderOf(character) != null;
        public static bool IsComposite(RosterCharacter character) =>
            IsFollower(character) || FollowersOf(character).Count > 0;

        // Whether follower may be put under leader's command: both deployed, the leader is not serving under someone
        // else, and the troop stays within RosterTroops.MaxCharactersPerTroop characters. A follower that already serves
        // under another character moves.
        public static bool CanPair(RosterCharacter follower, RosterCharacter leader, out string reason)
        {
            reason = "";
            if (follower == null || leader == null || follower == leader) { reason = "Drop onto another troop"; return false; }
            if (!IsDeployed(follower) || !IsDeployed(leader)) { reason = "Both characters must be deployed"; return false; }
            if (LeaderOf(leader) != null) { reason = $"{leader.Name} already serves under {LeaderOf(leader).Name}"; return false; }
            if (FollowersOf(follower).Count > 0) { reason = $"{follower.Name} already leads a composite troop"; return false; }
            var followers = FollowersOf(leader);
            followers.Remove(follower);
            if (followers.Count + 2 > Deployment.RosterTroops.MaxCharactersPerTroop)
            {
                reason = $"A troop holds at most {Deployment.RosterTroops.MaxCharactersPerTroop} characters";
                return false;
            }
            return true;
        }

        // Puts follower under leader's command: they fight as one troop led by leader. Returns whether it was done.
        public static bool Pair(RosterCharacter follower, RosterCharacter leader)
        {
            if (!CanPair(follower, leader, out string reason))
            {
                Debug.Log($"Tent: cannot pair {follower?.Name} with {leader?.Name}: {reason}.");
                return false;
            }
            Leaders[follower] = leader;
            Changed();
            return true;
        }

        // Splits the composite troop this character is part of: it and anyone under it fight on their own again.
        public static void Unpair(RosterCharacter character)
        {
            if (character == null) return;
            bool changed = Leaders.Remove(character);
            var under = new List<RosterCharacter>();
            foreach (var pair in Leaders)
                if (pair.Value == character) under.Add(pair.Key);
            foreach (var follower in under) changed |= Leaders.Remove(follower);
            if (changed) Changed();
        }

        // Deployed characters in display order: each leader or solo character, then the characters serving under it.
        public static List<RosterCharacter> DisplayOrder()
        {
            var result = new List<RosterCharacter>();
            foreach (var c in Deployed)
            {
                if (c == null || IsFollower(c)) continue;
                result.Add(c);
                result.AddRange(FollowersOf(c));
            }
            return result;
        }

        static void Changed()
        {
            Version++;
            // The battle deploys troops: keep BattleReport.DeployedTroops in step with the characters and pairings.
            Deployment.RosterTroops.Sync();
        }
    }
}
