// Temporary: drops data left behind by deleted fields and classes. In every level scene it removes prefab-instance
// overrides whose property no longer exists (for example the old DataManager.data.Characters), then rewrites every
// prefab and asset under Assets/ so fields removed from scripts disappear from the files.
// Menu: Tools > Legacy Removal > 4. Remove unused overrides and stale serialized data. Log: Logs/LegacyOverrideCleanup.txt.
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyOverrideCleanup
{
    const string LogPath = "Logs/LegacyOverrideCleanup.txt";

    [MenuItem("Tools/Legacy Removal/4. Remove unused overrides and stale serialized data")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var log = new StringBuilder();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Resources/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.gameObject)
                    .Where(PrefabUtility.IsOutermostPrefabInstanceRoot)
                    .ToArray();
                if (roots.Length > 0)
                    PrefabUtility.RemoveUnusedOverrides(roots, InteractionMode.AutomatedAction);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine($"{path}: checked {roots.Length} prefab instance(s)");
            }

            string[] assets = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith("Assets/TextMesh Pro") && (p.EndsWith(".prefab") || p.EndsWith(".asset")))
                .Distinct().ToArray();
            AssetDatabase.ForceReserializeAssets(assets);
            log.AppendLine($"Reserialized {assets.Length} prefabs and assets.");
        }
        finally
        {
            File.WriteAllText(LogPath, log.ToString());
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        Debug.Log("[Legacy Removal] Override cleanup done. Log: " + Path.GetFullPath(LogPath));
    }
}
