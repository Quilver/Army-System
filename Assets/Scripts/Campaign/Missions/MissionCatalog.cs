using System.Collections.Generic;
using UnityEngine;

namespace Campaign.Missions
{
    // Every mission in campaign order (Docs/Missions/README.md). Lives at Resources/MissionCatalog.asset, the one
    // runtime lookup: the missions themselves are under Assets/Missions/. CampaignData.CurrentLevel is the current
    // mission's number (1 = the first entry).
    [CreateAssetMenu(menuName = "Leveraged Wars/Mission Catalog", fileName = "MissionCatalog")]
    public class MissionCatalog : ScriptableObject
    {
        public const string ResourcePath = "MissionCatalog";

        [Tooltip("The campaign's missions, first to last. A mission's number is its place here.")]
        public List<MissionDefinition> missions = new();

        static MissionCatalog _active;

        // The catalog in Resources, or null when there is none yet (missions not set up: callers fall back to the old
        // build-index flow).
        public static MissionCatalog Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<MissionCatalog>(ResourcePath);
                return _active;
            }
        }

        // --- Lookups on this catalog ---

        public MissionDefinition Get(int number) =>
            missions != null && number >= 1 && number <= missions.Count ? missions[number - 1] : null;

        public int IndexNumberOf(MissionDefinition mission) =>
            mission == null || missions == null ? 0 : missions.IndexOf(mission) + 1;

        public MissionDefinition WithScene(string scenePath)
        {
            if (missions == null || string.IsNullOrEmpty(scenePath)) return null;
            foreach (var mission in missions)
                if (mission != null && mission.IsBattleScene(scenePath)) return mission;
            return null;
        }

        // --- Lookups on the active catalog ---

        public static MissionDefinition ByNumber(int number) => Active != null ? Active.Get(number) : null;

        public static int NumberOf(MissionDefinition mission) => Active != null ? Active.IndexNumberOf(mission) : 0;

        // The mission whose battle is this scene (path Assets/...unity), or null.
        public static MissionDefinition ForScene(string scenePath) => Active != null ? Active.WithScene(scenePath) : null;

        // The campaign's current mission, or null without campaign data or catalog.
        public static MissionDefinition Current =>
            CampaignDataManager.instance != null ? ByNumber(CampaignDataManager.Data.CurrentLevel) : null;
    }
}
