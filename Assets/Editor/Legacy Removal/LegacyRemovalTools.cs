// Temporary tools for removing the legacy Unit/Model system (Docs/Battle/Plan/11-legacy-removal.md).
// Delete this folder once the removal is finished.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacyRemovalTools
{
    const string Menu = "Tools/Legacy Removal/";
    const string ReportPath = "Logs/LegacyRemovalReport.txt";

    [MenuItem(Menu + "1. Switch to Force Text and reserialize all assets")]
    static void SwitchToForceText()
    {
        if (!EditorUtility.DisplayDialog("Force Text",
                "Switch Asset Serialization to Force Text and rewrite every asset, scene and prefab as text?\n\n" +
                "This touches most files under Assets/ (one large commit). Save your open scenes first.",
                "Switch and reserialize", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSettings.serializationMode = SerializationMode.ForceText;
        AssetDatabase.SaveAssets();
        AssetDatabase.ForceReserializeAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Legacy Removal] Asset serialization is now Force Text and all assets were reserialized.");
    }

    [MenuItem(Menu + "2. Write scene and prefab report")]
    static void WriteReport()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var sb = new StringBuilder();
        sb.AppendLine("Legacy removal report " + System.DateTime.Now.ToString("u"));
        sb.AppendLine("Format: path [components]  (MISSING = missing script)");
        int missingTotal = 0;
        try
        {
            string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Resources", "Assets/Campaign", "Assets/Battle Scripts" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Legacy removal report", scenes[i], (float)i / scenes.Length);
                Scene scene = EditorSceneManager.OpenScene(scenes[i], OpenSceneMode.Single);
                sb.AppendLine().AppendLine("=== SCENE " + scenes[i]);
                foreach (GameObject root in scene.GetRootGameObjects())
                    missingTotal += Describe(root.transform, 0, sb);
            }
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources", "Assets/Campaign", "Assets/Battle Scripts", "Assets/AudioSystem" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            foreach (string path in prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                sb.AppendLine().AppendLine("=== PREFAB " + path);
                missingTotal += Describe(root.transform, 0, sb);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        sb.Insert(0, "Missing scripts in total: " + missingTotal + "\n");
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[Legacy Removal] Report written to " + Path.GetFullPath(ReportPath) + " (" + missingTotal + " missing scripts).");
    }

    static int Describe(Transform t, int depth, StringBuilder sb)
    {
        int missing = 0;
        var names = new List<string>();
        foreach (Component c in t.GetComponents<Component>())
        {
            if (c == null) { names.Add("MISSING"); missing++; continue; }
            if (c is Transform) continue;
            names.Add(c.GetType().Name);
        }
        string prefab = PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
            ? " <prefab " + AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) + ">" : "";
        sb.Append(' ', depth * 2).Append(t.name).Append(t.gameObject.activeSelf ? "" : " (inactive)")
          .Append(" [").Append(string.Join(", ", names)).Append(']').AppendLine(prefab);
        foreach (Transform child in t) missing += Describe(child, depth + 1, sb);
        return missing;
    }
}
