using System.Collections.Generic;
using Campaign;
using UnityEngine;

namespace Deployment
{
    // Turns the characters deployed in battle preparation into the troops the battle deploys (Tween system), and hands
    // BattleReport.DeployedTroops to the battle's deployment zone as one list of characters per troop.
    // Each deployed character fights as its own troop, unless it was put under another deployed character's command in
    // the tent (BattleReport.DeployedLeaders, SU-117): then the two fight as one composite troop, leader first. The troop
    // sets its frontage from its total soldier count when the battle starts (TroopFrontageRule, SU-117).
    public static class RosterTroops
    {
        // Most characters in one composite troop (SU-117).
        public const int MaxCharactersPerTroop = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            DeploymentZone.TroopSource = () => BattleReport.Exists ? WithGains(BattleReport.DeployedTroopCharacters()) : null;
        }

        // Every character that belongs to a deployed roster character fights with its level-up gains (SU-132): it is
        // swapped for RosterCharacter.BattleCharacter(). Characters with no roster entry (a level's default troops played
        // on their own, enemies) are unchanged.
        static List<IReadOnlyList<CharacterProfile>> WithGains(List<IReadOnlyList<CharacterProfile>> troops)
        {
            if (troops == null) return null;
            var result = new List<IReadOnlyList<CharacterProfile>>();
            foreach (var troop in troops)
            {
                var characters = new List<CharacterProfile>();
                foreach (CharacterProfile member in troop)
                {
                    RosterCharacter roster = CharacterOf(member);
                    CharacterProfile fighting = roster != null ? roster.BattleCharacter() : null;
                    characters.Add(fighting != null ? fighting : member);
                }
                result.Add(characters);
            }
            return result;
        }

        // Rebuilds BattleReport.DeployedTroops from BattleReport.DeployedCharacters and DeployedLeaders: one troop per
        // character that leads or stands alone, holding the leader and the characters under it.
        public static void Sync()
        {
            if (!BattleReport.Exists) return;
            var deployed = BattleReport.DeployedCharacters;
            var leaders = BattleReport.DeployedLeaders;
            var troops = new List<DeployedTroop>();
            var members = new List<RosterCharacter>();
            foreach (RosterCharacter character in deployed)
            {
                if (character == null) continue;
                if (leaders.TryGetValue(character, out RosterCharacter leader) && leader != null && deployed.Contains(leader))
                    continue; // fights in its leader's troop
                members.Clear();
                members.Add(character);
                foreach (RosterCharacter other in deployed)
                    if (other != null && other != character && members.Count < MaxCharactersPerTroop &&
                        leaders.TryGetValue(other, out RosterCharacter otherLeader) && otherLeader == character)
                        members.Add(other);
                DeployedTroop troop = For(members);
                if (troop != null) troops.Add(troop);
            }
            BattleReport.DeployedTroops = troops;
        }

        // A level played on its own has no characters from the tent: the campaign characters in the player's troops become
        // the deployed characters (their roster entries when the campaign has them, otherwise new level 1 entries), so
        // achievements, XP and the post-battle book still have them (SU-116). Does nothing when characters were deployed.
        public static void EnsureDeployedCharacters(Army player)
        {
            if (!BattleReport.Exists || player == null) return;
            BattleReport.DeployedCharacters ??= new List<RosterCharacter>();
            var deployed = BattleReport.DeployedCharacters;
            if (deployed.Count > 0) return;

            CampaignData campaign = CampaignDataManager.instance != null ? CampaignDataManager.Data : null;
            foreach (var troop in player.Troops)
            {
                if (troop == null) continue;
                foreach (var member in troop.Characters)
                {
                    if (member == null || member.Source is not CampaignCharacter character ||
                        deployed.Exists(r => r.character == character)) continue;
                    RosterCharacter entry = campaign?.Roster?.Find(r => r != null && r.character == character)
                                            ?? new RosterCharacter { character = character };
                    deployed.Add(entry);
                }
            }
            if (deployed.Count > 0)
                Debug.Log($"Battle report: no characters came from the tent; deployed the campaign characters in the " +
                          $"player's troops: {string.Join(", ", deployed.ConvertAll(r => r.Name))}.");
        }

        // The deployed character who leads this troop (the first of the troop's characters that was deployed), or null.
        public static RosterCharacter CharacterOf(Troop troop)
        {
            if (troop == null || !BattleReport.Exists) return null;
            foreach (var member in troop.Characters)
            {
                RosterCharacter character = CharacterOf(member);
                if (character != null) return character;
            }
            return null;
        }

        // Every deployed character in this troop, leader first (two in a composite troop).
        public static List<RosterCharacter> CharactersOf(Troop troop)
        {
            var result = new List<RosterCharacter>();
            if (troop == null || !BattleReport.Exists) return result;
            foreach (var member in troop.Characters)
            {
                RosterCharacter character = CharacterOf(member);
                if (character != null && !result.Contains(character)) result.Add(character);
            }
            return result;
        }

        // The deployed character made from this character profile (or from the asset a battle copy was made from), or null.
        public static RosterCharacter CharacterOf(CharacterProfile profile)
        {
            if (profile == null || !BattleReport.Exists) return null;
            CharacterProfile source = profile.Source;
            foreach (RosterCharacter character in BattleReport.DeployedCharacters)
                if (character != null && character.character == source) return character;
            return null;
        }

        // The troop a roster character fights as on its own, or null without a character.
        public static DeployedTroop For(RosterCharacter character) =>
            character != null ? For(new[] { character }) : null;

        // The troop these roster characters fight as together: their characters, the first one leading. Null when none
        // has a character. The troop sets its frontage from its total soldier count in battle (TroopFrontageRule, SU-117).
        public static DeployedTroop For(IReadOnlyList<RosterCharacter> characters)
        {
            var profiles = new List<CharacterProfile>();
            if (characters != null)
                foreach (RosterCharacter character in characters)
                    if (character != null && character.character != null) profiles.Add(character.character);
            return profiles.Count > 0 ? new DeployedTroop(profiles) : null;
        }
    }
}
