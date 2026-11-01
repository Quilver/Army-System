// Temporary: removes the legacy Unit/Model objects from the scenes and gives Level 1 and Level 2 troops instead.
// Menu: Tools > Legacy Removal > 3. Clean scenes and rebuild Level 1 and 2 with troops. Writes a log to
// Logs/LegacySceneCleanup.txt. Delete with the rest of this folder when the removal is finished.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LegacySceneCleanup
{
    const string LogPath = "Logs/LegacySceneCleanup.txt";
    const string Levels = "Assets/Resources/Levels/";
    const string TroopTest = Levels + "Test/Level 1 Troop Test.unity";
    const string Level1 = Levels + "Level 1/Level 1.unity";
    const string Level2 = Levels + "Level 2/Level 2.unity";
    const string Preparation = Levels + "Menu levels/Battle Preparation.unity";
    const string Profiles = "Assets/Resources/Troop and Character Profiles/";
    const string VisualiserPrefab = Profiles + "Troop Visualiser.prefab";
    const string SoundPrefab = "Assets/AudioSystem/Battle scripts/Model SFX.prefab";
    const string LegacyPlayerController = "Assets/Battle Scripts/Player IO/Player controller.prefab";
    const string BattleCanvasPrefab = "Assets/Battle Scripts/Player IO/BattleUI/Canvas.prefab";
    const int EnemyTeam = 1;
    // Removed from the battle canvas prefab: the old deployment phase button and an unused result text.
    static readonly string[] CanvasLeftovers = { "Deploy", "Victory and Defeat" };
    // Removed from Level 1: a physics circle on the old Unit layer.
    static readonly string[] Level1Leftovers = { "Dynamic Obstacle" };

    static readonly string[] LegacyUnitPrefabs =
    {
        "Assets/Resources/Battle prefabs/Unit Prefabs/BattleReadyUnit.prefab",
        "Assets/Resources/Battle prefabs/Unit Prefabs/DeploymentUnit.prefab",
    };

    // Old unit name -> troop profile, per scene (from the NPC data each old unit used).
    static readonly Dictionary<string, Dictionary<string, string>> EnemyProfiles = new()
    {
        [Level1] = new() { ["Alpha"] = "Bandit Troop", ["Beta"] = "Bandit Troop", ["Archer"] = "Archer Troop", ["Cav"] = "Cav Troop" },
        [Level2] = new(),
    };
    const string DefaultEnemyProfile = "Bandit Troop";

    static readonly StringBuilder Log = new();

    [MenuItem("Tools/Legacy Removal/3. Clean scenes and rebuild Level 1 and 2 with troops")]
    static void Run()
    {
        if (!EditorUtility.DisplayDialog("Legacy scene cleanup",
                "This edits and saves the battle Canvas prefab, Level 1, Level 2, Level 1 Troop Test and Battle Preparation:\n\n" +
                "- removes old units, old player controllers, the shooting cursor, missing AI prefabs and the old prep menu canvas\n" +
                "- removes components whose script is missing\n" +
                "- gives Level 1 and 2 a troop combat world, troop player orders, battle sounds and enemy troops where the old units stood\n" +
                "- removes the Deploy button and 'Victory and Defeat' text (battle canvas) and Level 1's 'Dynamic Obstacle'\n\n" +
                "Everything is on the legacy-removal branch, so it can be reverted with git.", "Run", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Log.Clear();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            CleanBattleCanvasPrefab();
            CleanPreparation();
            CleanTroopTest();
            RebuildLevel(Level1);
            RebuildLevel(Level2);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            File.WriteAllText(LogPath, Log.ToString());
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        Debug.Log("[Legacy Removal] Scene cleanup done. Log: " + System.IO.Path.GetFullPath(LogPath));
    }

    // --- Battle canvas prefab ---

    static void CleanBattleCanvasPrefab()
    {
        Log.AppendLine().AppendLine("=== " + BattleCanvasPrefab);
        GameObject root = PrefabUtility.LoadPrefabContents(BattleCanvasPrefab);
        try
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
                if (t != null && CanvasLeftovers.Contains(t.name))
                {
                    Log.AppendLine("  removed " + PathOf(t));
                    Object.DestroyImmediate(t.gameObject);
                }
            PrefabUtility.SaveAsPrefabAsset(root, BattleCanvasPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Scene objects with these names that are not inside a prefab instance (added in the scene).
    static void RemoveNamed(Scene scene, IEnumerable<string> names, string why)
    {
        var set = new HashSet<string>(names);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true).ToArray())
                if (t != null && set.Contains(t.name)) Remove(t.gameObject, why);
    }

    // TroopPlayerOrders shows the canvas's "Paused" text while paused.
    static void WirePausedIndicator(Scene scene)
    {
        var orders = FindInScene<TroopPlayerOrders>(scene);
        if (orders == null) return;
        Transform paused = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == "Paused" && t.GetComponentInParent<Canvas>(true) != null);
        if (paused == null)
        {
            Log.AppendLine("  ! no 'Paused' text found for TroopPlayerOrders");
            return;
        }
        var so = new SerializedObject(orders);
        so.FindProperty("_pausedIndicator").objectReferenceValue = paused.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        paused.gameObject.SetActive(false);
        Log.AppendLine("  TroopPlayerOrders shows " + PathOf(paused) + " while paused");
    }

    // --- Battle Preparation: the old tabbed menu canvas (inactive, its scripts deleted). ---

    static void CleanPreparation()
    {
        Scene scene = EditorSceneManager.OpenScene(Preparation, OpenSceneMode.Single);
        Section(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Canvas" && !root.activeSelf && HasMissingScripts(root))
                Remove(root, "old prep menu canvas");
        RemoveMissingScripts(scene);
        Save(scene);
    }

    // --- Level 1 Troop Test: unpack the player controller, drop the shooting cursor. ---

    static void CleanTroopTest()
    {
        Scene scene = EditorSceneManager.OpenScene(TroopTest, OpenSceneMode.Single);
        Section(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (IsInstanceOf(root, LegacyPlayerController) && root.GetComponent<TroopPlayerOrders>() != null)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Log.AppendLine("  unpacked " + root.name + " (keeps TroopPlayerOrders)");
                if (root.TryGetComponent<SpriteRenderer>(out var cursorSprite))
                {
                    Object.DestroyImmediate(cursorSprite);
                    Log.AppendLine("  removed the old cursor SpriteRenderer from " + root.name);
                }
            }
        }
        RemoveLegacyObjects(scene, out _);
        RemoveNamed(scene, CanvasLeftovers, "unused battle canvas object");
        RemoveMissingScripts(scene);
        EnsureSound(scene);
        WirePausedIndicator(scene);
        Save(scene);
    }

    // --- Level 1 and Level 2 ---

    static void RebuildLevel(string path)
    {
        // Settings to copy come from the troop test scene.
        Scene source = EditorSceneManager.OpenScene(TroopTest, OpenSceneMode.Single);
        var world = Object.FindFirstObjectByType<TroopCombatWorld>();
        var overlay = world != null ? world.GetComponent<TroopDebugOverlay>() : null;
        var spriteDefaults = Object.FindFirstObjectByType<TroopProfileSpriteDefaults>();
        var orders = Object.FindFirstObjectByType<TroopPlayerOrders>();

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        Section(scene);

        RemoveLegacyObjects(scene, out List<OldUnit> oldUnits);
        RemoveNamed(scene, CanvasLeftovers, "unused battle canvas object");
        if (path == Level1) RemoveNamed(scene, Level1Leftovers, "old physics obstacle");
        RemoveMissingScripts(scene);

        if (FindInScene<TroopCombatWorld>(scene) == null && world != null)
        {
            var go = NewRoot(scene, "Troop Combat World");
            Copy(world, go.AddComponent<TroopCombatWorld>());
            if (overlay != null) Copy(overlay, go.AddComponent<TroopDebugOverlay>());
            Log.AppendLine("  added Troop Combat World");
        }
        if (FindInScene<TroopProfileSpriteDefaults>(scene) == null && spriteDefaults != null)
        {
            Copy(spriteDefaults, NewRoot(scene, "Troop Profile Sprite Defaults").AddComponent<TroopProfileSpriteDefaults>());
            Log.AppendLine("  added Troop Profile Sprite Defaults");
        }
        if (FindInScene<TroopPlayerOrders>(scene) == null && orders != null)
        {
            Copy(orders, NewRoot(scene, "Player controller").AddComponent<TroopPlayerOrders>());
            Log.AppendLine("  added Player controller (TroopPlayerOrders)");
        }
        EnsureSound(scene);
        WirePausedIndicator(scene);

        Army enemy = Armies(scene).enemy;
        foreach (OldUnit unit in oldUnits.Where(u => u.Enemy))
            AddEnemyTroop(path, enemy, unit);

        EditorSceneManager.CloseScene(source, true);
        Save(scene);
    }

    struct OldUnit
    {
        public string Name;
        public Vector3 Position;
        public bool Enemy;
    }

    static void AddEnemyTroop(string scenePath, Army enemy, OldUnit unit)
    {
        if (enemy == null)
        {
            Log.AppendLine("  ! no enemy Army on the Battle; skipped troop for " + unit.Name);
            return;
        }
        string profileName = EnemyProfiles.TryGetValue(scenePath, out var map) && map.TryGetValue(unit.Name, out var p)
            ? p : DefaultEnemyProfile;
        var profile = AssetDatabase.FindAssets("t:TroopProfile " + profileName, new[] { Profiles })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TroopProfile>)
            .FirstOrDefault(t => t != null && t.name == profileName);
        if (profile == null)
        {
            Log.AppendLine("  ! troop profile not found: " + profileName);
            return;
        }

        var go = new GameObject(unit.Name);
        Undo.RegisterCreatedObjectUndo(go, "Add troop");
        go.transform.SetParent(enemy.transform, false);
        // Enemies stand north of the player and face south (a troop faces its transform.right).
        go.transform.SetPositionAndRotation(unit.Position, Quaternion.Euler(0f, 0f, -90f));
        var troop = go.AddComponent<Troop>();
        var so = new SerializedObject(troop);
        so.FindProperty("_teamId").intValue = EnemyTeam;
        so.FindProperty("_profile").objectReferenceValue = profile;
        so.ApplyModifiedPropertiesWithoutUndo();
        var visualiser = AssetDatabase.LoadAssetAtPath<GameObject>(VisualiserPrefab);
        if (visualiser != null) PrefabUtility.InstantiatePrefab(visualiser, go.transform);
        Log.AppendLine($"  added enemy troop {unit.Name} ({profile.name}) at {unit.Position}");
    }

    // --- Shared ---

    // Old unit prefab instances, old player controllers without TroopPlayerOrders, the shooting cursor and prefab
    // instances whose prefab no longer exists (the old AI squads).
    static void RemoveLegacyObjects(Scene scene, out List<OldUnit> oldUnits)
    {
        oldUnits = new List<OldUnit>();
        Army enemy = Armies(scene).enemy;
        var doomed = new List<(GameObject go, string why)>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (doomed.Any(d => go.transform.IsChildOf(d.go.transform))) continue;
                if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && LegacyUnitPrefabs.Any(p => IsInstanceOf(go, p)))
                {
                    oldUnits.Add(new OldUnit
                    {
                        Name = go.name, Position = go.transform.position,
                        Enemy = enemy != null && go.transform.IsChildOf(enemy.transform),
                    });
                    doomed.Add((go, "old unit"));
                }
                else if (PrefabUtility.IsPrefabAssetMissing(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go)
                         && go.GetComponent<TroopPlayerOrders>() == null)
                    doomed.Add((go, "instance of a deleted prefab"));
                else if (IsInstanceOf(go, LegacyPlayerController) && PrefabUtility.IsOutermostPrefabInstanceRoot(go)
                         && go.GetComponent<TroopPlayerOrders>() == null)
                    doomed.Add((go, "old player controller"));
                else if (go.GetComponent<TroopPlayerOrders>() == null && (go.name == "ShootingCursor" ||
                         (go.name == "Player controller" && go.GetComponent<SpriteRenderer>() != null && HasMissingScripts(go, false))))
                    doomed.Add((go, "old shooting cursor"));
            }
        foreach (var (go, why) in doomed)
            if (go != null) Remove(go, why);
    }

    static void RemoveMissingScripts(Scene scene)
    {
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count == 0) continue;
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                {
                    Log.AppendLine($"  ! {PathOf(t)} has {count} missing script(s) inside a prefab instance; fix the prefab");
                    continue;
                }
                Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove missing scripts");
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                Log.AppendLine($"  removed {count} missing script(s) from {PathOf(t)}");
            }
        if (removed == 0) Log.AppendLine("  no missing scripts left");
    }

    // The scene's player and enemy armies: Battle.player / Battle.enemy, or the Army objects named PLAYER / ENEMY ARMY,
    // which are then assigned to the Battle.
    static (Army player, Army enemy) Armies(Scene scene)
    {
        var battle = FindInScene<Battle>(scene);
        Army player = battle != null ? battle.player : null;
        Army enemy = battle != null ? battle.enemy : null;
        var armies = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Army>(true)).ToList();
        if (player == null) player = armies.FirstOrDefault(a => a.name.ToUpperInvariant().Contains("PLAYER"));
        if (enemy == null) enemy = armies.FirstOrDefault(a => a.name.ToUpperInvariant().Contains("ENEMY"));
        if (battle != null && (battle.player != player || battle.enemy != enemy))
        {
            var so = new SerializedObject(battle);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("enemy").objectReferenceValue = enemy;
            so.ApplyModifiedPropertiesWithoutUndo();
            Log.AppendLine($"  Battle armies set: player {player?.name}, enemy {enemy?.name}");
        }
        return (player, enemy);
    }

    static void EnsureSound(Scene scene)
    {
        if (FindInScene<AudioSystem.ModelSoundEffects>(scene) != null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoundPrefab);
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        go.name = "Battle Sounds";
        Log.AppendLine("  added Battle Sounds (Model SFX prefab)");
    }

    static bool HasMissingScripts(GameObject go, bool children = true) =>
        (children ? go.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject) : new[] { go })
        .Any(g => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g) > 0);

    static bool IsInstanceOf(GameObject go, string prefabPath) =>
        PrefabUtility.IsPartOfPrefabInstance(go) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == prefabPath;

    static T FindInScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();

    static GameObject NewRoot(Scene scene, string name)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    static void Copy(Component from, Component to) => EditorUtility.CopySerialized(from, to);

    static void Remove(GameObject go, string why)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
        {
            Log.AppendLine($"  ! {PathOf(go.transform)} ({why}) is inside a prefab instance; remove it by hand");
            return;
        }
        Log.AppendLine($"  removed {PathOf(go.transform)} ({why})");
        Object.DestroyImmediate(go);
    }

    static void Section(Scene scene) => Log.AppendLine().AppendLine("=== " + scene.path);

    static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
