using System.IO;
using System.Linq;
using System.Text;
using BattlePrep;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Campaign.Missions.EditorTools
{
    // Moves Level 1 and Level 2 out of Assets/Resources/Levels into mission folders (SU-119):
    //   Assets/Missions/M01 - Level 1/
    //     M01 Mission.asset                  the new mission sheet
    //     PreBattle/M01 Prep.asset           the level's PreparationData, plus its dialogue assets
    //     Battle/M01 Battle.unity            the level scene, Battle/M01 Battle/ its navmesh, Battle/Map/ its images
    //     Tween/
    // and lists both in the MissionCatalog (Level 1 -> Level 2) and Build Settings. Assets are moved with the
    // AssetDatabase, so every reference (GUID) survives. Safe to run again: a level already in the catalog is skipped.
    public static class MissionMigration
    {
        readonly struct Legacy
        {
            public readonly int Number;
            public readonly string Title, Folder, Scene, Prep;
            public Legacy(int number, string title, string folder, string scene, string prep)
            {
                Number = number; Title = title; Folder = folder; Scene = scene; Prep = prep;
            }
        }

        static readonly Legacy[] Levels =
        {
            new(1, "Level 1", "Assets/Resources/Levels/Level 1", "Level 1.unity", "Level 1.asset"),
            new(2, "Level 2", "Assets/Resources/Levels/Level 2", "Level 2.unity", "Level 2 prep.asset"),
        };

        static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".psd", ".tga" };

        [MenuItem("Leveraged Wars/Missions/Migrate Levels 1 and 2", priority = 40)]
        static void MigrateFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Migrate Levels 1 and 2",
                    "Move Level 1 and Level 2 from Assets/Resources/Levels into Assets/Missions, make their mission " +
                    "sheets and the Mission Catalog, and update Build Settings?\n\nReferences are kept. Commit first if " +
                    "you want an easy way back.", "Migrate", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Moving an open scene is allowed, but start from an empty one so nothing half-saved is moved.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string report = Migrate();
            Debug.Log(report);
            var catalog = MissionTools.LoadCatalog();
            if (catalog != null) Selection.activeObject = catalog;
        }

        public static string Migrate()
        {
            var log = new StringBuilder("[Missions] migration:\n");
            var catalog = MissionTools.GetOrCreateCatalog();
            catalog.missions.RemoveAll(m => m == null);

            foreach (var level in Levels)
            {
                string legacyScene = $"{level.Folder}/{level.Scene}";
                string sceneGuid = AssetDatabase.AssetPathToGUID(legacyScene);
                if (catalog.missions.Any(m => m.BattleSceneGuid == sceneGuid && !string.IsNullOrEmpty(sceneGuid)))
                {
                    log.AppendLine($"- {level.Title}: already in the catalog, skipped.");
                    continue;
                }
                if (!AssetDatabase.IsValidFolder(level.Folder) || !File.Exists(legacyScene))
                {
                    log.AppendLine($"- {level.Title}: {legacyScene} not found (already migrated?), skipped.");
                    continue;
                }

                string code = MissionTools.Code(level.Number);
                string root = MissionTools.CreateMissionFolders(MissionTools.MissionFolder(level.Number, level.Title));
                string battle = $"{root}/{MissionTools.BattleFolder}";
                string map = $"{battle}/{MissionTools.MapFolder}";
                string preBattle = $"{root}/{MissionTools.PreBattleFolder}";
                string sceneName = $"{code} Battle";
                string sceneFolderName = Path.GetFileNameWithoutExtension(level.Scene);

                log.AppendLine($"- {level.Title} -> {root}:");
                foreach (string entry in Entries(level.Folder))
                {
                    string name = Path.GetFileName(entry);
                    bool folder = AssetDatabase.IsValidFolder(entry);
                    if (folder && name == "Map")
                    {
                        foreach (string image in Entries(entry)) Move(image, $"{map}/{Path.GetFileName(image)}", log);
                        DeleteIfEmpty(entry, log);
                    }
                    else if (folder && name == sceneFolderName) Move(entry, $"{battle}/{sceneName}", log);   // the navmesh
                    else if (name == level.Scene) Move(entry, $"{battle}/{sceneName}.unity", log);
                    else if (name == level.Prep) Move(entry, $"{preBattle}/{code} Prep.asset", log);
                    else if (ImageExtensions.Contains(Path.GetExtension(name).ToLowerInvariant())) Move(entry, $"{map}/{name}", log);
                    else if (name.EndsWith(".asset")) Move(entry, $"{preBattle}/{name}", log);   // dialogue
                    else Move(entry, $"{root}/{name}", log);
                }
                DeleteIfEmpty(level.Folder, log);

                string scenePath = $"{battle}/{sceneName}.unity";
                var mission = ScriptableObject.CreateInstance<MissionDefinition>();
                mission.title = level.Title;
                mission.preparation = AssetDatabase.LoadAssetAtPath<PreparationData>($"{preBattle}/{code} Prep.asset");
                mission.SetBattleScene(scenePath);
                AssetDatabase.CreateAsset(mission, $"{root}/{code} Mission.asset");
                if (mission.preparation == null) log.AppendLine($"  ! no PreparationData found ({level.Prep}); set it on the mission sheet.");

                int index = Mathf.Clamp(level.Number - 1, 0, catalog.missions.Count);
                catalog.missions.Insert(index, mission);
                log.AppendLine($"  mission sheet {root}/{code} Mission.asset, catalog number {index + 1}.");
            }

            // One link: each mission leads to the one after it, unless it already names one.
            for (int i = 0; i + 1 < catalog.missions.Count; i++)
                if (catalog.missions[i].nextMission == null)
                {
                    catalog.missions[i].nextMission = catalog.missions[i + 1];
                    EditorUtility.SetDirty(catalog.missions[i]);
                }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            MissionTools.SyncBuildSettings();
            log.AppendLine("Build Settings: " + string.Join(", ", EditorBuildSettings.scenes.Select((s, i) => $"{i} {Path.GetFileNameWithoutExtension(s.path)}")));
            return log.ToString();
        }

        static string[] Entries(string folder) =>
            Directory.GetFileSystemEntries(folder)
                .Where(e => !e.EndsWith(".meta"))
                .Select(e => e.Replace('\\', '/'))
                .OrderBy(e => e)
                .ToArray();

        static void Move(string from, string to, StringBuilder log)
        {
            to = AssetDatabase.GenerateUniqueAssetPath(to);
            string error = AssetDatabase.MoveAsset(from, to);
            log.AppendLine(string.IsNullOrEmpty(error) ? $"  {from} -> {to}" : $"  ! {from}: {error}");
        }

        static void DeleteIfEmpty(string folder, StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(folder) || Entries(folder).Length > 0) return;
            AssetDatabase.DeleteAsset(folder);
            log.AppendLine($"  removed empty {folder}");
        }
    }
}
