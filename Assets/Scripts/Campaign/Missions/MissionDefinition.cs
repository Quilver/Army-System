using System.Collections.Generic;
using AchievementSystem;
using BattlePrep;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Campaign.Missions
{
    // One mission's sheet (Docs/Missions/README.md): what the pre-battle tent shows, what passes between the battle and
    // the campaign, and which battle scene is played. The MissionCatalog lists the missions in campaign order; a
    // mission's number is its place in that list. Made by Leveraged Wars > Missions > New Mission.
    [CreateAssetMenu(menuName = "Leveraged Wars/Mission", fileName = "Mission")]
    public class MissionDefinition : ScriptableObject
    {
        [Tooltip("The mission's name, shown in the tent next to its number.")]
        public string title;

        // --- Pre-battle ---
        [Tooltip("Briefing, loans, court, new and mandatory characters, and the deploy cap.")]
        public PreparationData preparation;

        // --- Tween ---
        [Tooltip("Global achievements of this mission's battle, on top of every battle's (AchievementTuning).")]
        public List<GlobalAchievement> achievements = new();
        [Tooltip("Character achievements every deployed character has in this mission only.")]
        public List<CharacterAchievement> characterAchievements = new();
        [Tooltip("The mission played after a victory. Empty: the campaign ends after this one.")]
        public MissionDefinition nextMission;

        // --- Battle ---
        // The battle scene, kept by GUID so it survives moves and renames; the path is refreshed from it in the editor
        // (OnValidate, MissionAssetWatcher) and is what the game loads at runtime.
        [SerializeField, HideInInspector] string battleSceneGuid;
        [SerializeField, HideInInspector] string battleScenePath;

        public string BattleScenePath => battleScenePath;
        public string BattleSceneGuid => battleSceneGuid;
        public bool HasBattleScene => !string.IsNullOrEmpty(battleScenePath);

        // This mission's number (1 = first), or 0 when it is not in the catalog.
        public int Number => MissionCatalog.NumberOf(this);

        // "Mission 2: The Danube Ambush", or "Mission 2" without a title.
        public string DisplayName
        {
            get
            {
                int number = Number;
                string head = number > 0 ? $"Mission {number}" : name;
                return string.IsNullOrEmpty(title) ? head : $"{head}: {title}";
            }
        }

        // Whether the scene at this path (Assets/...unity) is this mission's battle.
        public bool IsBattleScene(string scenePath) =>
            HasBattleScene && !string.IsNullOrEmpty(scenePath) &&
            string.Equals(battleScenePath, scenePath, System.StringComparison.OrdinalIgnoreCase);

        // Loads the battle scene. False (and an error) when it is not in Build Settings.
        public bool LoadBattle()
        {
            int index = HasBattleScene ? SceneUtility.GetBuildIndexByScenePath(battleScenePath) : -1;
            if (index < 0)
            {
                Debug.LogError($"{DisplayName}: battle scene '{battleScenePath}' is not in Build Settings " +
                               "(Leveraged Wars > Missions > Sync Build Settings).", this);
                return false;
            }
            SceneManager.LoadScene(index);
            return true;
        }

#if UNITY_EDITOR
        // Editor only: sets the battle scene (a SceneAsset's path).
        public void SetBattleScene(string scenePath)
        {
            battleScenePath = scenePath ?? "";
            battleSceneGuid = string.IsNullOrEmpty(scenePath) ? "" : UnityEditor.AssetDatabase.AssetPathToGUID(scenePath);
        }

        // Editor only: follows the scene's GUID to its current path. Returns whether the path changed.
        public bool RefreshBattleScenePath()
        {
            if (string.IsNullOrEmpty(battleSceneGuid)) return false;
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(battleSceneGuid);
            if (string.IsNullOrEmpty(path) || path == battleScenePath) return false;
            battleScenePath = path;
            return true;
        }

        void OnValidate()
        {
            if (RefreshBattleScenePath()) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
