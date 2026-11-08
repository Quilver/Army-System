using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Converts assets made before the melee/ranged weapon split (Decisions D-52).
// - Weapon assets: the old combined WeaponProfile script GUID now belongs to MeleeWeaponProfile, so every old weapon
//   loads as melee. Each one whose class is Bow, Crossbow or Javelin is switched to RangedWeaponProfile in place (same
//   file, same GUID, so references to it are kept) and its range, accuracy and fire rate are copied across.
// - Character assets: the old weapon list is moved into the melee slot (first melee weapon) and the ranged slot
//   (first ranged weapon). Any other listed weapon is dropped with a warning. A character left without a melee weapon
//   is logged as an error.
// Runs once per editor session, before entering Play mode, and from Troops > Migrate > Split Weapon Profiles. It does
// nothing once the assets are converted. The Phase 01 generator and the CLI smoke test run it first as well.
[InitializeOnLoad]
public static class WeaponProfileMigration
{
    const string CheckedKey = "WeaponProfileMigration.Checked";

    static WeaponProfileMigration()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (SessionState.GetBool(CheckedKey, false)) return;
        EditorApplication.delayCall += AutoRun;
    }

    // Unconverted characters throw when deployed, so convert before Play mode even if the delayed run has not happened.
    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) Run();
    }

    static void AutoRun()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        SessionState.SetBool(CheckedKey, true);
        Run();
    }

    [MenuItem("Troops/Migrate/Split Weapon Profiles")]
    public static void RunFromMenu()
    {
        if (Run() == 0) Debug.Log("Weapon migration: nothing to convert.");
    }

    // Returns the number of assets changed.
    public static int Run()
    {
        int weapons = ConvertRangedWeapons();
        int characters = MigrateCharacters();
        if (weapons + characters > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Weapon migration: {weapons} weapon(s) converted to ranged, {characters} character(s) moved to the " +
                      "melee/ranged slots.");
        }
        return weapons + characters;
    }

    struct LegacyRangedValues
    {
        public string DisplayName;
        public int WeaponClass;
        public int Damage;
        public float Range;
        public int Accuracy;
        public int FireRate;
    }

    static int ConvertRangedWeapons()
    {
        var pending = new List<(string Path, LegacyRangedValues Values)>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(MeleeWeaponProfile)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var melee = AssetDatabase.LoadAssetAtPath<MeleeWeaponProfile>(path);
            if (melee == null) continue;
            var so = new SerializedObject(melee);
            int weaponClass = so.FindProperty("_weaponClass").intValue;
            if (!((WeaponClass)weaponClass).IsRanged()) continue;
            pending.Add((path, new LegacyRangedValues
            {
                DisplayName = so.FindProperty("_displayName").stringValue,
                WeaponClass = weaponClass,
                Damage = so.FindProperty("_damage").intValue,
                Range = so.FindProperty("_range").floatValue,
                Accuracy = so.FindProperty("_accuracy").intValue,
                FireRate = so.FindProperty("_fireRate").intValue,
            }));
        }
        if (pending.Count == 0) return 0;

        MonoScript rangedScript = RangedScript();
        int converted = 0;
        foreach (var (path, values) in pending)
        {
            RangedWeaponProfile ranged = SwitchScript(path, rangedScript) ?? ReplaceAsset(path);
            if (ranged == null)
            {
                Debug.LogError($"Weapon migration: could not convert {path} to a ranged weapon. Recreate it with " +
                               "Create > Troops > Ranged Weapon.");
                continue;
            }
            WriteRangedValues(ranged, values);
            converted++;
        }
        return converted;
    }

    static MonoScript RangedScript()
    {
        var temp = ScriptableObject.CreateInstance<RangedWeaponProfile>();
        MonoScript script = MonoScript.FromScriptableObject(temp);
        Object.DestroyImmediate(temp);
        return script;
    }

    // Changes the asset's script in place, as the Inspector does in Debug mode. Keeps the file and its GUID.
    static RangedWeaponProfile SwitchScript(string path, MonoScript rangedScript)
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (asset == null || rangedScript == null) return null;
        var so = new SerializedObject(asset);
        SerializedProperty script = so.FindProperty("m_Script");
        if (script == null) return null;
        script.objectReferenceValue = rangedScript;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<RangedWeaponProfile>(path);
    }

    // Fallback: a new ranged asset replaces the old one at the same path. The GUID changes, so character weapon lists
    // that pointed at the old asset are repointed first.
    static RangedWeaponProfile ReplaceAsset(string path)
    {
        Object old = AssetDatabase.LoadAssetAtPath<Object>(path);
        string tempPath = AssetDatabase.GenerateUniqueAssetPath(path);
        var fresh = ScriptableObject.CreateInstance<RangedWeaponProfile>();
        AssetDatabase.CreateAsset(fresh, tempPath);

        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CharacterProfile)))
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(AssetDatabase.GUIDToAssetPath(guid));
            if (character == null) continue;
            var so = new SerializedObject(character);
            SerializedProperty list = so.FindProperty("_equipment._weapons");
            if (list == null) continue;
            bool changed = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (old != null && element.objectReferenceValue == old)
                {
                    element.objectReferenceValue = fresh;
                    changed = true;
                }
            }
            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(character);
            }
        }
        AssetDatabase.SaveAssets();

        if (!AssetDatabase.DeleteAsset(path)) return null;
        string error = AssetDatabase.MoveAsset(tempPath, path);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Weapon migration: {error}");
            return null;
        }
        Debug.LogWarning($"Weapon migration: {path} was recreated as a ranged weapon with a new GUID. Character " +
                         "references were updated; check any other object that referenced it.");
        return AssetDatabase.LoadAssetAtPath<RangedWeaponProfile>(path);
    }

    static void WriteRangedValues(RangedWeaponProfile ranged, LegacyRangedValues values)
    {
        var so = new SerializedObject(ranged);
        so.FindProperty("_displayName").stringValue = values.DisplayName;
        so.FindProperty("_weaponClass").intValue = values.WeaponClass;
        so.FindProperty("_damage").intValue = values.Damage;
        so.FindProperty("_range").floatValue = values.Range;
        so.FindProperty("_accuracy").intValue = values.Accuracy;
        so.FindProperty("_fireRate").intValue = values.FireRate;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ranged);
    }

    static int MigrateCharacters()
    {
        int migrated = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CharacterProfile)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(path);
            if (character == null) continue;
            var so = new SerializedObject(character);
            SerializedProperty list = so.FindProperty("_equipment._weapons");
            if (list == null || list.arraySize == 0) continue;
            SerializedProperty meleeSlot = so.FindProperty("_equipment._meleeWeapon");
            SerializedProperty rangedSlot = so.FindProperty("_equipment._rangedWeapon");

            for (int i = 0; i < list.arraySize; i++)
            {
                Object listed = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (listed == null) continue;
                string weaponPath = AssetDatabase.GetAssetPath(listed);
                var melee = AssetDatabase.LoadAssetAtPath<MeleeWeaponProfile>(weaponPath);
                var ranged = AssetDatabase.LoadAssetAtPath<RangedWeaponProfile>(weaponPath);
                if (melee != null && meleeSlot.objectReferenceValue == null) meleeSlot.objectReferenceValue = melee;
                else if (ranged != null && rangedSlot.objectReferenceValue == null) rangedSlot.objectReferenceValue = ranged;
                else Debug.LogWarning($"Weapon migration: {path}: {listed.name} dropped. A character carries one melee " +
                                      "weapon and at most one ranged weapon.", character);
            }
            list.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
            migrated++;

            if (meleeSlot.objectReferenceValue == null)
                Debug.LogError($"Weapon migration: {path} has no melee weapon. Assign one; deploying it will throw.",
                    character);
        }
        return migrated;
    }
}
