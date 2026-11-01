using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Converts character assets to the class and campaign data model.
// - Classes: every character (generic and campaign) whose class is still the old string gets a CharacterClass asset
//   instead. One asset is made per class name in Classes/ (or an existing class with that name is reused), with the
//   default caps. The old string is then cleared.
// - Campaign characters: every plain CharacterProfile in Characters/Campaign is switched to CampaignCharacter in place
//   (same file, same GUID, so troops keep their references). Its stats, equipment and sprites are kept; its growth
//   chances are set to the default (StatGrowths.DefaultChance).
// Runs once per editor session and from Troops > Migrate > Character Classes and Campaign Characters. It does nothing
// once the assets are converted.
[InitializeOnLoad]
public static class CampaignCharacterMigration
{
    public const string CampaignFolder = "Assets/Resources/Troop and Character Profiles/Characters/Campaign";
    public const string ClassFolder = "Assets/Resources/Troop and Character Profiles/Classes";
    const string CheckedKey = "CampaignCharacterMigration.Checked";

    static CampaignCharacterMigration()
    {
        if (SessionState.GetBool(CheckedKey, false)) return;
        EditorApplication.delayCall += AutoRun;
    }

    static void AutoRun()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        SessionState.SetBool(CheckedKey, true);
        Run();
    }

    [MenuItem("Troops/Migrate/Character Classes and Campaign Characters")]
    public static void RunFromMenu()
    {
        if (Run() == 0) Debug.Log("Campaign character migration: nothing to convert.");
    }

    // Returns the number of assets changed.
    public static int Run()
    {
        int classes = MigrateClasses(null, ClassFolder);
        int campaign = AssetDatabase.IsValidFolder(CampaignFolder) ? ConvertToCampaignCharacters(CampaignFolder) : 0;
        if (classes + campaign > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Campaign character migration: {classes} character(s) given a CharacterClass asset, {campaign} " +
                      "character(s) converted to CampaignCharacter.");
        }
        return classes + campaign;
    }

    // Replaces the legacy class string with a CharacterClass asset on every character in the folders (all folders when
    // null). Returns the number of characters changed.
    public static int MigrateClasses(string[] searchFolders, string classFolder)
    {
        int changed = 0;
        foreach (string path in CharacterPaths(searchFolders))
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            if (character == null) continue;
            var so = new SerializedObject(character);
            SerializedProperty classProperty = so.FindProperty("_class");
            SerializedProperty legacyProperty = so.FindProperty("_className");
            if (classProperty == null || legacyProperty == null) continue;
            string legacyName = legacyProperty.stringValue?.Trim();
            if (string.IsNullOrEmpty(legacyName)) continue;

            if (classProperty.objectReferenceValue == null)
                classProperty.objectReferenceValue = FindOrCreateClass(legacyName, classFolder);
            legacyProperty.stringValue = string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
            changed++;
        }
        return changed;
    }

    // Switches every plain CharacterProfile in the folder to CampaignCharacter in place and gives it the default growths.
    // Returns the number of characters converted.
    public static int ConvertToCampaignCharacters(string folder)
    {
        MonoScript campaignScript = ScriptFor(typeof(CampaignCharacter));
        if (campaignScript == null)
        {
            Debug.LogError("Campaign character migration: the CampaignCharacter script was not found.");
            return 0;
        }

        var pending = new List<string>();
        foreach (string path in CharacterPaths(new[] { folder }))
        {
            var character = AssetDatabase.LoadMainAssetAtPath(path) as CharacterProfile;
            if (character != null && character.GetType() == typeof(CharacterProfile)) pending.Add(path);
        }

        if (pending.Count == 0) return 0;
        // Referencing assets are reloaded from disk afterwards, so their unsaved edits must be on disk first.
        AssetDatabase.SaveAssets();

        var convertedPaths = new HashSet<string>();
        foreach (string path in pending)
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            var so = new SerializedObject(character);
            so.FindProperty("m_Script").objectReferenceValue = campaignScript;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (character != null) EditorUtility.SetDirty(character);
            AssetDatabase.SaveAssets();

            var campaign = AssetDatabase.LoadAssetAtPath<CampaignCharacter>(path);
            if (campaign == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                campaign = AssetDatabase.LoadAssetAtPath<CampaignCharacter>(path);
            }
            if (campaign == null)
            {
                Debug.LogError($"Campaign character migration: {path} could not be converted to CampaignCharacter.");
                continue;
            }
            campaign.ResetGrowthsToDefault();
            EditorUtility.SetDirty(campaign);
            convertedPaths.Add(path);
        }
        AssetDatabase.SaveAssets();
        ReloadAssetsReferencing(convertedPaths);
        return convertedPaths.Count;
    }

    // Switching an asset's script replaces its managed object. Loaded assets that reference it (troops) keep the right
    // reference in their serialized data, but their C# fields still hold the old, destroyed object (reads as null).
    // Unloading them makes the next load deserialize them again with the converted character. They were saved above.
    static void ReloadAssetsReferencing(HashSet<string> convertedPaths)
    {
        if (convertedPaths.Count == 0) return;
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/") || !path.EndsWith(".asset") || convertedPaths.Contains(path)) continue;
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
            {
                if (!convertedPaths.Contains(dependency)) continue;
                if (AssetDatabase.LoadMainAssetAtPath(path) is ScriptableObject referencing) Resources.UnloadAsset(referencing);
                break;
            }
        }
    }

    // The class asset with this display name (case-insensitive), or a new one in the folder.
    public static CharacterClass FindOrCreateClass(string className, string classFolder = ClassFolder)
    {
        className = className.Trim();
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CharacterClass)))
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterClass>(AssetDatabase.GUIDToAssetPath(guid));
            if (existing != null && string.Equals(existing.DisplayName, className, StringComparison.OrdinalIgnoreCase))
                return existing;
        }

        EnsureFolder(classFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{classFolder}/{FileNameFor(className)}.asset");
        CharacterClass created = CharacterClass.CreateRuntimeClass(className);
        AssetDatabase.CreateAsset(created, path);
        created.name = Path.GetFileNameWithoutExtension(path);
        EditorUtility.SetDirty(created);
        return created;
    }

    static IEnumerable<string> CharacterPaths(string[] searchFolders)
    {
        string[] guids = searchFolders == null
            ? AssetDatabase.FindAssets("t:" + nameof(CharacterProfile))
            : AssetDatabase.FindAssets("t:" + nameof(CharacterProfile), searchFolders);
        var seen = new HashSet<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (seen.Add(path)) yield return path;
        }
    }

    static MonoScript ScriptFor(Type type)
    {
        foreach (string guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
            if (script != null && script.GetClass() == type) return script;
        }
        return null;
    }

    static string FileNameFor(string className)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            className = className.Replace(invalid, '_');
        return className;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
