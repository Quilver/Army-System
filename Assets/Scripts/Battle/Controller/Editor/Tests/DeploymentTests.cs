using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LeveragedWars.Battle.AI;
using LeveragedWars.Battle.AI.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// The deployment phase: the shared pause, the deployment zone's area and roster, the level setup, and a played Level 1
// deployment. Scenes are set up in memory and never saved.
[Category("Deployment")]
public sealed class DeploymentTests
{
    readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var o in _created)
            if (o != null) Object.DestroyImmediate(o);
        _created.Clear();
        DeploymentZone.TroopSource = null;
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void GamePause_RunsAgainOnlyWhenEveryHolderReleases()
    {
        object deployment = new object(), cutscene = new object();
        GamePause.Hold(deployment);
        GamePause.Hold(cutscene);
        GamePause.Release(cutscene);
        Assert.IsTrue(GamePause.IsPaused, "a cutscene ending does not end deployment's pause");
        Assert.AreEqual(0f, Time.timeScale);
        GamePause.Release(deployment);
        Assert.IsFalse(GamePause.IsPaused);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void Zone_ClampsIntoItsArea_AndDeploysTheReportsTroops()
    {
        var go = new GameObject("Zone");
        _created.Add(go);
        go.transform.position = new Vector3(10f, -5f, 0f);
        go.transform.localScale = new Vector3(20f, 10f, 1f);
        var zone = go.AddComponent<DeploymentZone>(); // Awake does not run in Edit mode

        Assert.IsTrue(zone.Contains(new Vector2(10f, -5f)));
        Assert.IsFalse(zone.Contains(new Vector2(25f, -5f)));
        Vector2 clamped = zone.Clamp(new Vector2(40f, 20f), inset: 2f);
        Assert.AreEqual(18f, clamped.x, 1e-3f, "right edge 20 minus inset 2");
        Assert.AreEqual(-2f, clamped.y, 1e-3f, "top edge 0 minus inset 2");

        DeploymentZone.TroopSource = null;
        Assert.AreEqual(0, zone.TroopsToDeploy().Count, "no report: nothing to deploy");
        var character = ScriptableObject.CreateInstance<CharacterProfile>();
        _created.Add(character);
        IReadOnlyList<CharacterProfile> troop = new List<CharacterProfile> { character };
        DeploymentZone.TroopSource = () => new List<IReadOnlyList<CharacterProfile>> { troop };
        Assert.AreSame(troop, zone.TroopsToDeploy().Single(), "deploys the report's troops, each as its characters");
    }

    [Test]
    public void Zone_AreaAndBorderShowOnlyWhileDeploying()
    {
        var go = new GameObject("Zone");
        _created.Add(go);
        var area = go.AddComponent<SpriteRenderer>();
        var border = new GameObject("Border");
        border.transform.SetParent(go.transform, false);
        var borderRenderer = border.AddComponent<SpriteRenderer>();
        var zone = go.AddComponent<DeploymentZone>(); // Awake does not run in Edit mode

        zone.ApplyPhase(BattlePhase.Deployment);
        Assert.IsTrue(area.enabled && borderRenderer.enabled, "area and border shown while deploying");
        Assert.IsTrue(zone.IsShown);

        zone.ApplyPhase(BattlePhase.Fighting);
        Assert.IsFalse(area.enabled, "area hidden once fighting");
        Assert.IsFalse(borderRenderer.enabled, "border hidden once fighting (it used to stay on screen)");
        Assert.IsFalse(zone.IsShown);
    }

    static int ReportTroopCount(out BattleReport report)
    {
        report = Object.FindAnyObjectByType<BattleReport>(FindObjectsInactive.Include);
        Assert.IsNotNull(report, "the level's DataManager has a BattleReport");
        var list = new SerializedObject(report).FindProperty("_deployedTroops");
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty characters = list.GetArrayElementAtIndex(i).FindPropertyRelative("_characters");
            Assert.Greater(characters.arraySize, 0, $"default troop {i} has characters");
            for (int j = 0; j < characters.arraySize; j++)
                Assert.IsNotNull(characters.GetArrayElementAtIndex(j).objectReferenceValue, $"default troop {i} character {j}");
        }
        return list.arraySize;
    }

    static void AssertNoSpriteDefaults() =>
        Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(t => t.name == "Troop Profile Sprite Defaults"), "Troop Profile Sprite Defaults removed");

    [Test]
    public void LevelSetUp_Level1_DefaultsOnTheDataManagersReport()
    {
        Scene scene = EditorSceneManager.OpenScene(DeploymentSetup.LevelScenes[0], OpenSceneMode.Single);
        Debug.Log(DeploymentSetup.SetUpScene(scene));
        Debug.Log(DeploymentSetup.SetUpScene(scene)); // again: no change

        Battle battle = Object.FindAnyObjectByType<Battle>();
        DeploymentZone zone = Object.FindAnyObjectByType<DeploymentZone>();
        Assert.IsTrue(new SerializedObject(battle).FindProperty("_deployment").boolValue);
        Assert.AreEqual(battle.player, zone.Army);
        Assert.AreEqual(3, ReportTroopCount(out _), "Level 1's three default troops");
        var mods = PrefabUtility.GetPropertyModifications(zone.gameObject) ?? new PropertyModification[0];
        Assert.IsFalse(mods.Any(m => m.propertyPath.StartsWith("_defaultTroops")), "old zone defaults moved off the zone");
        Assert.AreEqual(0, battle.player.GetComponentsInChildren<Troop>(true).Length, "placed player troops removed");
        AssertDeployEventClean(battle, zone);
        AssertNoSpriteDefaults();
        foreach (var director in Object.FindObjectsByType<AIDirector>(FindObjectsSortMode.None))
            Assert.IsTrue(new SerializedObject(director).FindProperty("beginOnStart").boolValue, "AI starts with the scene");
    }

    [Test]
    public void LevelSetUp_Level2_DefaultsOnTheDataManagersReport()
    {
        Scene scene = EditorSceneManager.OpenScene(DeploymentSetup.LevelScenes[1], OpenSceneMode.Single);
        Debug.Log(DeploymentSetup.SetUpScene(scene));
        Assert.Greater(ReportTroopCount(out _), 0);
        AssertDeployEventClean(Object.FindAnyObjectByType<Battle>(), Object.FindAnyObjectByType<DeploymentZone>());
        AssertNoSpriteDefaults();
    }

    static void AssertDeployEventClean(Battle battle, DeploymentZone zone)
    {
        var deploy = DeploymentSetup.DeployEvent(battle);
        for (int i = 0; i < deploy.GetPersistentEventCount(); i++)
        {
            Object target = deploy.GetPersistentTarget(i);
            Assert.IsNotNull(target, "no missing Deploy targets");
            Assert.AreNotEqual(zone.gameObject, target, "the zone hides itself");
            Assert.AreNotEqual(nameof(AIDirector.BeginBattle), deploy.GetPersistentMethodName(i));
        }
    }

    // Level 1 played: paused while deploying with the AI already running, troops spawned inside the zone, placement
    // clamped to the zone, and Start Battle runs time again. Run explicitly: -testFilter DeploymentTests.PlayedDeployment
    [UnityTest, Explicit]
    public IEnumerator PlayedDeployment()
    {
        Scene scene = EditorSceneManager.OpenScene(DeploymentSetup.LevelScenes[0], OpenSceneMode.Single);
        DeploymentSetup.SetUpScene(scene);
        Mission1Setup.Apply(scene);
        yield return new EnterPlayMode();
        yield return null;
        yield return null;

        Battle battle = Battle.Instance;
        DeploymentZone zone = DeploymentZone.For(battle.player);
        var director = Object.FindAnyObjectByType<AIDirector>();
        var orders = Object.FindAnyObjectByType<TroopPlayerOrders>();

        Assert.AreEqual(BattlePhase.Deployment, battle.Phase);
        Assert.IsTrue(GamePause.IsPaused);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(director.IsRunning, "the AI starts at once");
        if (orders != null) Assert.AreSame(zone, orders.Placement, "player orders place troops while deploying");

        Troop[] mine = battle.player.GetComponentsInChildren<Troop>();
        Assert.IsTrue(BattleReport.Exists, "the level's own DataManager");
        Assert.AreEqual(BattleReport.DeployedTroops.Count, mine.Length, "one troop per BattleReport.DeployedTroops entry");
        foreach (Troop troop in mine)
            Assert.IsTrue(zone.Contains(troop.transform.position, 0f), $"{troop.name} starts inside the zone");

        // Placing outside the zone keeps the troop inside it, with no march.
        Troop moved = mine[0];
        Assert.IsTrue(zone.Place(moved, new Vector2(60f, 60f), Vector2.up, moved.NaturalLineLength, holdLine: false));
        Assert.IsTrue(zone.Contains(moved.transform.position, 0f), "placement clamped into the zone");
        Assert.IsFalse(moved.IsMoving, "placed, not marching");

        // Nothing moves while deploying.
        Troop archer = battle.enemy.GetComponentsInChildren<Troop>().First(t => t.name == "Archer");
        Vector3 archerStart = archer.transform.position;
        for (int i = 0; i < 30; i++) yield return null;
        Assert.AreEqual(archerStart, archer.transform.position, "the enemy waits while the player deploys");

        battle.StartBattle();
        Assert.AreEqual(BattlePhase.Fighting, battle.Phase);
        Assert.IsFalse(GamePause.IsPaused);
        Assert.AreEqual(1f, Time.timeScale);
        if (orders != null) Assert.IsNull(orders.Placement, "orders are orders again");
        Assert.IsFalse(zone.IsShown, "the zone's area and border are hidden once fighting");
        Assert.IsFalse(zone.Place(moved, moved.transform.position, Vector2.up, moved.NaturalLineLength, holdLine: true), "no placing once fighting");

        Time.captureDeltaTime = 1f / 30f;
        float end = Time.time + 5f;
        while (Time.time < end) yield return null;
        Time.captureDeltaTime = 0f;
        Assert.Greater(Vector3.Distance(archerStart, archer.transform.position), 2f, "the AI acts once the fighting begins");

        yield return new ExitPlayMode();
    }
    // Runs the prefab part of the setup tool too, so it changes project assets: run it only in a throwaway copy of the
    // project (the Unity CLI runner's mirror). -testFilter DeploymentTests.FullSetUp_StartBattleButton
    [UnityTest, Explicit]
    public IEnumerator FullSetUp_StartBattleButton()
    {
        Debug.Log(DeploymentSetup.SetUpPrefabs());
        var zonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeploymentSetup.ZonePrefabPath);
        Assert.IsNotNull(zonePrefab, "renamed prefab");
        Assert.IsNotNull(zonePrefab.GetComponent<DeploymentZone>());
        Assert.IsNull(zonePrefab.GetComponent<Collider2D>(), "no collider");
        Assert.IsNull(zonePrefab.GetComponent<Rigidbody2D>(), "no rigidbody");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(DeploymentSetup.CanvasPrefabPath)
            .GetComponentInChildren<DeploymentPanel>(true), "Start Battle panel on the canvas");

        Scene scene = EditorSceneManager.OpenScene(DeploymentSetup.LevelScenes[0], OpenSceneMode.Single);
        DeploymentSetup.SetUpScene(scene);
        Mission1Setup.Apply(scene);
        yield return new EnterPlayMode();
        yield return null;

        var panel = Object.FindAnyObjectByType<DeploymentPanel>(FindObjectsInactive.Include);
        Assert.IsNotNull(panel, "panel in Level 1");
        var button = panel.GetComponentInChildren<UnityEngine.UI.Button>(true);
        Assert.IsTrue(button.gameObject.activeInHierarchy, "Start Battle shown while deploying");
        var zoneRenderer = DeploymentZone.For(Battle.Instance.player).GetComponent<SpriteRenderer>();
        Assert.IsTrue(zoneRenderer.enabled, "zone shown while deploying");

        button.onClick.Invoke();
        yield return null;
        Assert.AreEqual(BattlePhase.Fighting, Battle.Instance.Phase);
        Assert.IsFalse(button.gameObject.activeInHierarchy, "Start Battle hidden once fighting");
        Assert.IsFalse(zoneRenderer.enabled, "zone hidden once fighting");
        yield return new ExitPlayMode();
    }
}
