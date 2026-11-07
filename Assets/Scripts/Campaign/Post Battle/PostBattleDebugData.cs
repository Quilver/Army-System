using System.Collections.Generic;
using AchievementSystem;
using Campaign;
using UnityEngine;

namespace EndGameUI
{
    // Editor debugging of the post-battle book (SU-116): when the Post battle scene opens with no deployed characters in
    // the BattleReport (played directly, or nothing came from the battle), it is filled with a sample battle:
    // Julian and Lyra deployed, both survived, Julian also earned Ten Kills, Routed Foe, Against the Odds and Shock Assault
    // (enough XP to level up), and the battle was won (Victory, Mission Complete). Figures the battle did not set are made up.
    public static class PostBattleDebugData
    {
        static readonly string[] JulianExtras = { "Ten Kills", "Routed Foe", "Against the Odds", "Shock Assault" };
        static readonly string[] Won = { "Victory", "Mission Complete" };

        // Fills the BattleReport; false when there was nothing to do (characters were deployed) or no report.
        public static bool FillIfEmpty()
        {
            if (!BattleReport.Exists) return false;
            if (BattleReport.DeployedCharacters != null && BattleReport.DeployedCharacters.Count > 0) return false;

            RosterCharacter julian = Roster("Julian");
            RosterCharacter lyra = Roster("Lyra");
            var deployed = new List<RosterCharacter>();
            if (julian != null) deployed.Add(julian);
            if (lyra != null) deployed.Add(lyra);
            BattleReport.DeployedCharacters = deployed;

            if (BattleReport.timeTaken <= 0f)
            {
                BattleReport.ResetBattleStats();
                BattleReport.timeTaken = 247f;
                BattleReport.kills = 23;
                BattleReport.deaths = 7;
                BattleReport.EnemyTroopsRouted = 3;
                BattleReport.PlayerTroopsRouted = 0;
            }
            SetRecord(julian, 15, 3);
            SetRecord(lyra, 8, 4);

            var achievements = new List<Achievement>();
            foreach (string name in Won) Add(achievements, name, new AchievementContext(null));
            foreach (var character in deployed) Add(achievements, "Survived", new AchievementContext(null, character));
            if (julian != null)
                foreach (string name in JulianExtras) Add(achievements, name, new AchievementContext(null, julian));
            BattleReport.Achievements = achievements;

            Debug.Log("Post-battle: no deployed characters were passed, so the book shows debug data " +
                      "(Julian and Lyra; Julian levels up). Turn it off on the Post Battle Book.");
            return true;
        }

        // The character's roster entry when the campaign has one, otherwise a new level 1 entry.
        static RosterCharacter Roster(string characterName)
        {
            CampaignData campaign = CampaignDataManager.instance != null ? CampaignDataManager.Data : null;
            if (campaign?.Roster != null)
                foreach (var entry in campaign.Roster)
                    if (entry?.character != null && (entry.character.name == characterName || entry.Name == characterName))
                        return entry;
            foreach (var character in Resources.LoadAll<CampaignCharacter>(RosterCharacter.CharacterFolder))
                if (character != null && (character.name == characterName || character.CharacterName == characterName))
                    return new RosterCharacter { character = character };
            Debug.LogWarning($"Post-battle debug: campaign character '{characterName}' not found.");
            return null;
        }

        static void SetRecord(RosterCharacter character, int kills, int casualties)
        {
            var record = BattleReport.RecordFor(character);
            if (record == null) return;
            record.Kills = kills;
            record.Casualties = casualties;
        }

        // An earned copy of the achievement asset in Resources/Achievement.
        static void Add(List<Achievement> achievements, string assetName, AchievementContext context)
        {
            var asset = Resources.Load<Achievement>("Achievement/" + assetName);
            if (asset == null)
            {
                Debug.LogWarning($"Post-battle debug: achievement 'Achievement/{assetName}' not found.");
                return;
            }
            Achievement copy = Object.Instantiate(asset);
            copy.name = asset.name;
            copy.ForceEarned(context);
            achievements.Add(copy);
        }
    }
}
