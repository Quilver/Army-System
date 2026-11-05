using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LeveragedWars.Battle.AI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Moves the battle scenes onto the deployment system (SU-81 to SU-87): refactors the deployment zone prefab, adds the
// Start Battle panel to the battle canvas prefab, and in Level 1 and Level 2 puts the level's default troops on the
// scene DataManager's BattleReport (DeployedTroops), removes placed player troops (the zone spawns them), turns
// deployment on, clears the old Deploy event wiring, starts the enemy AI with the scene and removes the Troop Profile
// Sprite Defaults object (also from the troop test scene). Safe to run again.
public static class DeploymentSetup
{
    public const string ZonePrefabPath = "Assets/Prefabs/Battle/Deployment Zone.prefab";
    const string OldZonePrefabPath = "Assets/Prefabs/Battle/Deployement Zone.prefab";
    public const string CanvasPrefabPath = "Assets/Prefabs/Battle/Canvas.prefab";
    const string TroopPrefabPath = "Assets/Prefabs/Battle/Troop.prefab";
    const string CampaignTroops = "Assets/Resources/Troop and Character Profiles/Characters/Campaign/";

    const string SpriteDefaultsName = "Troop Profile Sprite Defaults";
    public const string TestScene = "Assets/Resources/Levels/Test/Level 1 Troop Test.unity";

    public static readonly string[] LevelScenes =
    {
        "Assets/Resources/Levels/Level 1/Level 1.unity",
        "Assets/Resources/Levels/Level 2/Level 2.unity",
    };

    // Default troops for a level with no player troops placed in it.
    static readonly string[] FallbackDefaults = { "Julian's Troop", "Franz's Troop", "Jagen's Troop", "Robin's Troop" };

    [MenuItem("Leveraged Wars/Battle/Set Up Deployment (Levels 1 and 2)")]
    static void SetUpFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = SceneManager.GetActiveScene().path;
        try
        {
            var log = new StringBuilder("[Deployment] set up:\n");
            log.Append(SetUpPrefabs());
            foreach (string path in LevelScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                log.Append(SetUpScene(scene));
                EditorSceneManager.SaveScene(scene);
            }
            Scene test = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
            string removed = RemoveSpriteDefaults(test);
            if (removed.Length > 0)
            {
                log.Append($"- {test.name}:\n").Append(removed);
                EditorSceneManager.SaveScene(test);
            }
            Debug.Log(log.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[Deployment] setup failed: {e.Message}\n{e}");
        }
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
    }

    // ---- Prefabs

    public static string SetUpPrefabs()
    {
        var log = new StringBuilder();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabPath) == null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(OldZonePrefabPath) != null)
        {
            string error = AssetDatabase.MoveAsset(OldZonePrefabPath, ZonePrefabPath);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            log.AppendLine("- renamed the prefab to Deployment Zone");
        }

        GameObject zone = PrefabUtility.LoadPrefabContents(ZonePrefabPath);
        try
        {
            zone.name = "Deployment Zone";
            // The old drag-and-drop deployment needed a collider and rigidbody; the area is now the object's unit square.
            foreach (var c in zone.GetComponents<Collider2D>()) Object.DestroyImmediate(c, true);
            foreach (var c in zone.GetComponents<Rigidbody2D>()) Object.DestroyImmediate(c, true);
            if (!zone.TryGetComponent(out DeploymentZone component)) component = zone.AddComponent<DeploymentZone>();
            var so = new SerializedObject(component);
            so.FindProperty("_troopPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Troop>(TroopPrefabPath);
            so.FindProperty("_areaRenderer").objectReferenceValue = zone.GetComponent<SpriteRenderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(zone, ZonePrefabPath);
            log.AppendLine("- Deployment Zone prefab: DeploymentZone, no collider or rigidbody");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(zone);
        }

        GameObject canvas = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        try
        {
            if (canvas.GetComponentInChildren<DeploymentPanel>(true) == null)
            {
                BuildDeploymentPanel(canvas.transform);
                PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPrefabPath);
                log.AppendLine("- Canvas prefab: Deployment Panel with the Start Battle button");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvas);
        }
        return log.ToString();
    }

    static void BuildDeploymentPanel(Transform canvas)
    {
        var panel = new GameObject("Deployment Panel", typeof(RectTransform));
        panel.transform.SetParent(canvas, false);
        Stretch((RectTransform)panel.transform);
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        Stretch((RectTransform)content.transform);

        var hint = new GameObject("Hint", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        hint.transform.SetParent(content.transform, false);
        Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 60f));
        hint.text = "Deployment: select troops and right-click or drag inside the zone to place them";
        hint.fontSize = 28;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = Color.white;
        hint.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) hint.font = TMP_Settings.defaultFontAsset;

        var buttonObject = new GameObject("Start Battle Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(content.transform, false);
        Place((RectTransform)buttonObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(280f, 72f));
        var image = buttonObject.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.93f, 0.85f, 0.62f, 1f);
        var button = buttonObject.GetComponent<Button>();

        var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(buttonObject.transform, false);
        Stretch(label.rectTransform);
        label.text = "Start Battle";
        label.fontSize = 34;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.2f, 0.12f, 0.05f, 1f);
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;

        var deploymentPanel = panel.AddComponent<DeploymentPanel>();
        var so = new SerializedObject(deploymentPanel);
        so.FindProperty("_content").objectReferenceValue = content;
        so.FindProperty("_startButton").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // ---- Level scenes

    public static string SetUpScene(Scene scene)
    {
        var log = new StringBuilder($"- {scene.name}:\n");
        global::Battle battle = FindInScene<global::Battle>(scene).FirstOrDefault()
            ?? throw new InvalidOperationException($"{scene.name} has no Battle.");
        if (battle.player == null) throw new InvalidOperationException($"{scene.name}: Battle has no player army.");

        var battleSO = new SerializedObject(battle);
        battleSO.FindProperty("_deployment").boolValue = true;
        battleSO.ApplyModifiedPropertiesWithoutUndo();

        DeploymentZone zone = FindInScene<DeploymentZone>(scene).FirstOrDefault()
            ?? throw new InvalidOperationException($"{scene.name} has no deployment zone.");
        zone.gameObject.name = "Deployment Zone";
        var zoneSO = new SerializedObject(zone);
        if (zoneSO.FindProperty("_army").objectReferenceValue == null)
            zoneSO.FindProperty("_army").objectReferenceValue = battle.player;
        zoneSO.ApplyModifiedPropertiesWithoutUndo();

        // The level's default troops live in the BattleReport on this scene's DataManager (BattleReport.DeployedTroops),
        // which a report coming from battle preparation replaces. Sources, first that has troops: the report itself,
        // the zone's old Default Troops (SU-84, kept as prefab overrides), the player troops placed in the scene,
        // FallbackDefaults.
        BattleReport report = FindInScene<BattleReport>(scene).FirstOrDefault()
            ?? throw new InvalidOperationException($"{scene.name} has no DataManager with a BattleReport.");
        var reportSO = new SerializedObject(report);
        SerializedProperty troops = reportSO.FindProperty("_deployedTroops");
        List<TroopProfile> legacy = TakeLegacyZoneDefaults(zone);
        Troop[] placed = battle.player.GetComponentsInChildren<Troop>(true).OrderBy(t => t.transform.position.x).ToArray();
        if (troops.arraySize == 0)
        {
            var profiles = legacy;
            if (profiles.Count == 0) profiles = placed.Select(t => t.Profile).Where(p => p != null).ToList();
            if (profiles.Count == 0)
                profiles = FallbackDefaults.Select(n => AssetDatabase.LoadAssetAtPath<TroopProfile>(CampaignTroops + n + ".asset"))
                    .Where(p => p != null).ToList();
            troops.arraySize = profiles.Count;
            for (int i = 0; i < profiles.Count; i++) troops.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            reportSO.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"  default troops on {report.name} (BattleReport.DeployedTroops): {string.Join(", ", profiles.Select(p => p.name))}");
        }
        foreach (Troop troop in placed)
        {
            log.AppendLine($"  removed placed player troop {troop.name} (now deployed from the zone)");
            Object.DestroyImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(troop.gameObject) ?? troop.gameObject);
        }

        log.Append(RemoveSpriteDefaults(scene));
        log.Append(CleanDeployEvent(battle, zone));

        foreach (AIDirector director in FindInScene<AIDirector>(scene))
        {
            var so = new SerializedObject(director);
            so.FindProperty("beginOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  enemy AI starts with the scene");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return log.ToString();
    }

    // The zone's Default Troops field is gone (SU-86); its values survive as overrides on the zone's prefab instance.
    // Returns them in order and removes those overrides.
    static List<TroopProfile> TakeLegacyZoneDefaults(DeploymentZone zone)
    {
        var result = new List<TroopProfile>();
        if (!PrefabUtility.IsPartOfPrefabInstance(zone)) return result;
        PropertyModification[] mods = PrefabUtility.GetPropertyModifications(zone.gameObject);
        if (mods == null) return result;
        const string prefix = "_defaultTroops.Array.data[";
        var entries = new SortedDictionary<int, TroopProfile>();
        foreach (var mod in mods)
        {
            if (mod.propertyPath == null || !mod.propertyPath.StartsWith(prefix)) continue;
            if (int.TryParse(mod.propertyPath.Substring(prefix.Length).TrimEnd(']'), out int index) && mod.objectReference is TroopProfile p)
                entries[index] = p;
        }
        result.AddRange(entries.Values);
        var kept = mods.Where(m => m.propertyPath == null || !m.propertyPath.StartsWith("_defaultTroops")).ToArray();
        if (kept.Length != mods.Length) PrefabUtility.SetPropertyModifications(zone.gameObject, kept);
        return result;
    }

    // Removes the Troop Profile Sprite Defaults object (its script is deleted, SU-87).
    public static string RemoveSpriteDefaults(Scene scene)
    {
        var log = new StringBuilder();
        foreach (Transform t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList())
        {
            if (t == null || t.name != SpriteDefaultsName) continue;
            Object.DestroyImmediate(t.gameObject);
            log.AppendLine($"  removed {SpriteDefaultsName}");
        }
        if (log.Length > 0) EditorSceneManager.MarkSceneDirty(scene);
        return log.ToString();
    }

    // Removes Deploy-event calls left from the old deployment: missing targets, the zone hiding itself (the zone hides
    // its area when the fighting begins) and AIDirector.BeginBattle (the AI starts with the scene).
    public static string CleanDeployEvent(global::Battle battle, DeploymentZone zone)
    {
        UnityEvent deploy = DeployEvent(battle);
        if (deploy == null) return "";
        int removed = 0;
        for (int i = deploy.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            Object target = deploy.GetPersistentTarget(i);
            string method = deploy.GetPersistentMethodName(i);
            bool stale = target == null
                         || (zone != null && target == zone.gameObject)
                         || (target is AIDirector && method == nameof(AIDirector.BeginBattle));
            if (!stale) continue;
            UnityEventTools.RemovePersistentListener(deploy, i);
            removed++;
        }
        if (removed == 0) return "";
        EditorUtility.SetDirty(battle);
        return $"  removed {removed} old Deploy event call(s)\n";
    }

    public static UnityEvent DeployEvent(global::Battle battle) =>
        typeof(global::Battle).GetField("_Deploy", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(battle) as UnityEvent;

    static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
}
