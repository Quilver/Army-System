using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattlePrep;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Campaign.Missions.EditorTools
{
    // Editor side of the mission workflow (Docs/Missions/README.md): the catalog, mission folders, new missions and
    // Build Settings. Leveraged Wars > Missions.
    public static class MissionTools
    {
        public const string MissionsRoot = "Assets/Missions";
        public const string CatalogPath = "Assets/Resources/" + MissionCatalog.ResourcePath + ".asset";
        public const string PreBattleFolder = "PreBattle";
        public const string BattleFolder = "Battle";
        public const string MapFolder = "Map";
        public const string TweenFolder = "Tween";

        // The menu scenes keep build indices 0, 1 and 2 (CampaignDataManager, BattleFlow and EndGameButton load them by
        // index); the missions' battle scenes follow in catalog order, then any other scene already listed.
        public static readonly string[] MenuScenes =
        {
            "Assets/Resources/Levels/Menu levels/Main menu.unity",
            "Assets/Resources/Levels/Menu levels/Battle Preparation.unity",
            "Assets/Resources/Levels/Menu levels/Post battle.unity",
        };

        // --- Names ---

        // "M03"
        public static string Code(int number) => $"M{number:00}";

        // "Assets/Missions/M03 - The Danube Ambush"
        public static string MissionFolder(int number, string title)
        {
            string clean = CleanFileName(title);
            return string.IsNullOrEmpty(clean) ? $"{MissionsRoot}/{Code(number)}" : $"{MissionsRoot}/{Code(number)} - {clean}";
        }

        public static string CleanFileName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var invalid = Path.GetInvalidFileNameChars();
            return new string(text.Trim().Where(c => !invalid.Contains(c)).ToArray()).Trim();
        }

        // --- Catalog ---

        public static MissionCatalog LoadCatalog() => AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);

        public static MissionCatalog GetOrCreateCatalog()
        {
            var catalog = LoadCatalog();
            if (catalog != null) return catalog;
            EnsureFolder(Path.GetDirectoryName(CatalogPath).Replace('\\', '/'));
            catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log($"[Missions] created {CatalogPath}.");
            return catalog;
        }

        // The battle scene of mission number, or legacyPath when there is no catalog or no such mission yet. Lets the
        // older setup tools (Mission1Setup, DeploymentSetup) work before and after the migration.
        public static string BattleScenePath(int number, string legacyPath)
        {
            var mission = LoadCatalog()?.Get(number);
            if (mission == null) return legacyPath;
            mission.RefreshBattleScenePath();
            return mission.HasBattleScene ? mission.BattleScenePath : legacyPath;
        }

        [MenuItem("Leveraged Wars/Missions/Select Mission Catalog", priority = 20)]
        static void SelectCatalog()
        {
            var catalog = GetOrCreateCatalog();
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        // --- Folders ---

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // Creates <root>/PreBattle, Battle/Map and Tween. Returns root.
        public static string CreateMissionFolders(string root)
        {
            EnsureFolder(root);
            EnsureFolder($"{root}/{PreBattleFolder}");
            EnsureFolder($"{root}/{BattleFolder}/{MapFolder}");
            EnsureFolder($"{root}/{TweenFolder}");
            return root;
        }

        // --- New mission ---

        // Makes mission number catalog.Count + 1: its folders, mission sheet, PreparationData and battle scene (a copy
        // of copySceneFrom's battle with its own navmesh, or an empty scene), links the previous last mission to it
        // when that one has no next mission, adds it to the catalog and Build Settings, and opens its scene.
        // Ask the user to save open scenes first: the new scene is opened in place of them.
        public static MissionDefinition CreateMission(string title, MissionDefinition copySceneFrom)
        {
            int number = GetOrCreateCatalog().missions.Count(m => m != null) + 1;
            string code = Code(number);
            string root = MissionFolder(number, title);
            if (AssetDatabase.IsValidFolder(root)) throw new System.InvalidOperationException($"{root} already exists.");
            string sourceScene = copySceneFrom != null && copySceneFrom.HasBattleScene ? copySceneFrom.BattleScenePath : null;
            string sourceName = copySceneFrom != null ? copySceneFrom.DisplayName : null;
            CreateMissionFolders(root);

            // The scene first: opening a scene unloads assets, so the catalog and new assets are loaded after it.
            string scenePath = $"{root}/{BattleFolder}/{code} Battle.unity";
            if (sourceScene != null)
            {
                if (!AssetDatabase.CopyAsset(sourceScene, scenePath))
                    throw new System.InvalidOperationException($"Could not copy {sourceScene}.");
                Scene copy = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                CopyNavMeshes(copy);
                EditorSceneManager.SaveScene(copy);
            }
            else
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            var prep = ScriptableObject.CreateInstance<PreparationData>();
            prep.court = new[] { new CourtMember(), new CourtMember(), new CourtMember() };
            AssetDatabase.CreateAsset(prep, $"{root}/{PreBattleFolder}/{code} Prep.asset");

            var mission = ScriptableObject.CreateInstance<MissionDefinition>();
            mission.title = title?.Trim() ?? "";
            mission.preparation = prep;
            mission.SetBattleScene(scenePath);
            AssetDatabase.CreateAsset(mission, $"{root}/{code} Mission.asset");

            var catalog = GetOrCreateCatalog();
            catalog.missions.RemoveAll(m => m == null);
            var last = catalog.missions.LastOrDefault();
            if (last != null && last.nextMission == null)
            {
                last.nextMission = mission;
                EditorUtility.SetDirty(last);
            }
            catalog.missions.Add(mission);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            SyncBuildSettings();
            Debug.Log($"[Missions] created {mission.DisplayName} in {root}" +
                      (sourceScene != null ? $" (battle scene copied from {sourceName})." : "."), mission);
            return mission;
        }

        // A copied scene still points at the source scene's navmesh assets, and rebaking would delete them (NavMeshPlus
        // deletes a surface's old asset). Give each surface its own copy, in the folder NavMeshPlus bakes into.
        static void CopyNavMeshes(Scene scene)
        {
            string folder = $"{Path.GetDirectoryName(scene.path).Replace('\\', '/')}/{scene.name}";
            foreach (var surface in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NavMeshSurface>(true)))
            {
                if (surface.navMeshData == null) continue;
                string source = AssetDatabase.GetAssetPath(surface.navMeshData);
                if (string.IsNullOrEmpty(source)) continue;
                EnsureFolder(folder);
                string target = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Path.GetFileName(source)}");
                if (!AssetDatabase.CopyAsset(source, target)) continue;
                surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(target);
                EditorUtility.SetDirty(surface);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // --- Build Settings ---

        [MenuItem("Leveraged Wars/Missions/Sync Build Settings", priority = 21)]
        static void SyncFromMenu()
        {
            SyncBuildSettings();
            Debug.Log("[Missions] Build Settings:\n" +
                      string.Join("\n", EditorBuildSettings.scenes.Select((s, i) => $"  {i}: {s.path}{(s.enabled ? "" : " (off)")}")));
        }

        // Menu scenes first (indices 0-2), then every mission's battle scene in catalog order, then the rest.
        public static void SyncBuildSettings()
        {
            var catalog = LoadCatalog();
            if (catalog == null) return;
            var current = EditorBuildSettings.scenes.ToList();
            var missionScenes = new List<string>();
            foreach (var mission in catalog.missions)
            {
                if (mission == null) continue;
                if (mission.RefreshBattleScenePath()) EditorUtility.SetDirty(mission);
                if (mission.HasBattleScene && !missionScenes.Contains(mission.BattleScenePath)) missionScenes.Add(mission.BattleScenePath);
            }

            var result = new List<EditorBuildSettingsScene>();
            foreach (string menu in MenuScenes)
                if (File.Exists(menu)) result.Add(new EditorBuildSettingsScene(menu, true));
            foreach (string path in missionScenes) result.Add(new EditorBuildSettingsScene(path, true));
            foreach (var scene in current)
                if (!result.Any(r => r.path == scene.path) && File.Exists(scene.path)) result.Add(scene);

            if (!result.Select(s => (s.path, s.enabled)).SequenceEqual(current.Select(s => (s.path, s.enabled))))
                EditorBuildSettings.scenes = result.ToArray();
        }

        public static bool InBuildSettings(string scenePath) =>
            !string.IsNullOrEmpty(scenePath) && EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scenePath);
    }

    // Keeps mission sheets pointing at their battle scenes when a scene is moved or renamed, and Build Settings in step.
    class MissionAssetWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!moved.Any(p => p.EndsWith(".unity")) && !deleted.Any(p => p.EndsWith(".unity"))) return;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(MissionDefinition)))
            {
                var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (mission != null && mission.RefreshBattleScenePath()) EditorUtility.SetDirty(mission);
            }
            AssetDatabase.SaveAssets();
            MissionTools.SyncBuildSettings();
        }
    }
}
