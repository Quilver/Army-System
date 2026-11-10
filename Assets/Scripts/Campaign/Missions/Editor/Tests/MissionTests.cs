using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattlePrep;
using Campaign.Missions;
using Campaign.Missions.EditorTools;
using NavMeshPlus.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The mission workflow (SU-119, Docs/Missions/README.md): catalog lookups, names, the New Mission tool, and the
// project's missions as set up (each has its preparation data and a battle scene in Build Settings).
public sealed class MissionTests
{
    readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
    }

    T Make<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        _created.Add(asset);
        return asset;
    }

    [Test]
    public void Catalog_NumbersAreOneBasedPlacesInTheList()
    {
        var catalog = Make<MissionCatalog>();
        var first = Make<MissionDefinition>();
        var second = Make<MissionDefinition>();
        catalog.missions.AddRange(new[] { first, second });

        Assert.AreSame(first, catalog.Get(1));
        Assert.AreSame(second, catalog.Get(2));
        Assert.IsNull(catalog.Get(0));
        Assert.IsNull(catalog.Get(3));
        Assert.AreEqual(2, catalog.IndexNumberOf(second));
        Assert.AreEqual(0, catalog.IndexNumberOf(Make<MissionDefinition>()), "not listed");
        Assert.AreEqual(0, catalog.IndexNumberOf(null));
    }

    [Test]
    public void Catalog_FindsTheMissionOfABattleScene()
    {
        var catalog = Make<MissionCatalog>();
        var mission = Make<MissionDefinition>();
        mission.SetBattleScene("Assets/Missions/M01 - Test/Battle/M01 Battle.unity");
        catalog.missions.Add(Make<MissionDefinition>());
        catalog.missions.Add(mission);

        Assert.AreSame(mission, catalog.WithScene("Assets/Missions/M01 - Test/Battle/M01 Battle.unity"));
        Assert.AreSame(mission, catalog.WithScene("assets/missions/m01 - test/battle/m01 battle.unity"), "paths ignore case");
        Assert.IsNull(catalog.WithScene("Assets/Other.unity"));
        Assert.IsNull(catalog.WithScene(""));
    }

    [Test]
    public void Names_FolderAndCode()
    {
        Assert.AreEqual("M03", MissionTools.Code(3));
        Assert.AreEqual("M12", MissionTools.Code(12));
        Assert.AreEqual("Assets/Missions/M03 - The Danube", MissionTools.MissionFolder(3, "  The Danube "));
        Assert.AreEqual("Assets/Missions/M03", MissionTools.MissionFolder(3, ""));
        Assert.AreEqual("Ambush", MissionTools.CleanFileName("Amb:u*sh?"));
    }

    [Test]
    public void BattleScenePath_FallsBackToTheLegacyPath()
    {
        Assert.AreEqual("Assets/Legacy.unity", MissionTools.BattleScenePath(999, "Assets/Legacy.unity"));
    }

    [Test]
    public void PreparationData_DefaultsForNewFields()
    {
        var prep = Make<PreparationData>();
        Assert.AreEqual(5, prep.maxDeployed, "deploy cap of SU-50 unless the mission sets one");
        Assert.IsNotNull(prep.newCharacters);
        Assert.AreEqual(0, prep.newCharacters.Count);
    }

    // Makes a mission with the New Mission tool, copying Mission 2's (Level 2's) battle, then removes it again.
    [Test]
    public void NewMission_MakesFoldersSheetAndSceneWithItsOwnNavmesh_AndLinksThePreviousMission()
    {
        bool hadCatalog = MissionTools.LoadCatalog() != null;
        var catalogBefore = MissionTools.GetOrCreateCatalog();
        var scenesBefore = EditorBuildSettings.scenes;
        // Opening scenes unloads assets: keep paths, and load the assets again after each scene change.
        var previousObject = catalogBefore.missions.LastOrDefault(m => m != null);
        string previousPath = previousObject != null ? AssetDatabase.GetAssetPath(previousObject) : null;
        string previousNextPath = previousObject != null && previousObject.nextMission != null
            ? AssetDatabase.GetAssetPath(previousObject.nextMission) : null;

        var source = Make<MissionDefinition>();
        source.SetBattleScene(DeploymentSetup.LevelScenes[1]);
        string title = "Unit Test " + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        string missionPath = null, root = null;
        try
        {
            missionPath = AssetDatabase.GetAssetPath(MissionTools.CreateMission(title, source));
            var created = AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
            var catalog = MissionTools.LoadCatalog();
            int number = catalog.missions.IndexOf(created) + 1;
            root = MissionTools.MissionFolder(number, title);
            string code = MissionTools.Code(number);

            Assert.IsTrue(AssetDatabase.IsValidFolder($"{root}/PreBattle"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{root}/Battle/Map"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{root}/Tween"));
            Assert.AreEqual($"{root}/{code} Mission.asset", missionPath);
            Assert.IsNotNull(created.preparation);
            Assert.AreEqual(3, created.preparation.court.Length, "three court members");
            Assert.AreEqual($"{root}/Battle/{code} Battle.unity", created.BattleScenePath);
            Assert.IsFalse(string.IsNullOrEmpty(created.BattleSceneGuid));
            Assert.AreEqual(title, created.title);
            Assert.AreEqual(number, catalog.missions.Count, "added at the end");
            if (previousPath != null && previousNextPath == null)
                Assert.AreSame(created, AssetDatabase.LoadAssetAtPath<MissionDefinition>(previousPath).nextMission, "previous mission leads here");
            Assert.IsTrue(MissionTools.InBuildSettings(created.BattleScenePath), "in Build Settings");
            for (int i = 0; i < MissionTools.MenuScenes.Length; i++)
                Assert.AreEqual(MissionTools.MenuScenes[i], EditorBuildSettings.scenes[i].path, "menu scenes keep their indices");

            Scene scene = SceneManager.GetActiveScene();
            Assert.AreEqual(created.BattleScenePath, scene.path, "the new scene is open");
            var surfaces = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NavMeshSurface>(true))
                .Where(s => s.navMeshData != null).ToList();
            Assert.Greater(surfaces.Count, 0, "Level 2 has a baked navmesh to copy");
            foreach (var surface in surfaces)
                StringAssert.StartsWith($"{root}/Battle/", AssetDatabase.GetAssetPath(surface.navMeshData),
                    "the copy bakes into its own navmesh, not the source's");
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = MissionTools.LoadCatalog();
            if (catalog != null && missionPath != null)
            {
                catalog.missions.RemoveAll(m => m == null || AssetDatabase.GetAssetPath(m) == missionPath);
                EditorUtility.SetDirty(catalog);
            }
            if (previousPath != null)
            {
                var previous = AssetDatabase.LoadAssetAtPath<MissionDefinition>(previousPath);
                previous.nextMission = previousNextPath != null ? AssetDatabase.LoadAssetAtPath<MissionDefinition>(previousNextPath) : null;
                EditorUtility.SetDirty(previous);
            }
            AssetDatabase.SaveAssets();
            if (root != null && AssetDatabase.IsValidFolder(root)) AssetDatabase.DeleteAsset(root);
            if (!hadCatalog) AssetDatabase.DeleteAsset(MissionTools.CatalogPath);
            if (AssetDatabase.IsValidFolder(MissionTools.MissionsRoot) &&
                AssetDatabase.FindAssets("", new[] { MissionTools.MissionsRoot }).Length == 0)
                AssetDatabase.DeleteAsset(MissionTools.MissionsRoot);
            EditorBuildSettings.scenes = scenesBefore;
        }
    }

    // The project's own missions, once set up (Migrate Levels 1 and 2 / New Mission).
    [Test]
    public void ProjectMissions_AreComplete()
    {
        var catalog = MissionTools.LoadCatalog();
        if (catalog == null) Assert.Ignore("No Mission Catalog yet (Leveraged Wars > Missions > Migrate Levels 1 and 2).");
        Assert.IsTrue(catalog.missions.All(m => m != null), "no empty catalog entries");
        var seen = new HashSet<MissionDefinition>();
        foreach (var mission in catalog.missions)
        {
            Assert.IsTrue(seen.Add(mission), $"{mission.name} is listed once");
            Assert.IsNotNull(mission.preparation, $"{mission.name}: preparation data");
            Assert.IsTrue(mission.HasBattleScene && File.Exists(mission.BattleScenePath), $"{mission.name}: battle scene exists");
            Assert.IsTrue(MissionTools.InBuildSettings(mission.BattleScenePath), $"{mission.name}: battle scene in Build Settings");
            int mandatory = mission.preparation.mandatoryCharacters.Count(c => c != null);
            Assert.LessOrEqual(mandatory, mission.preparation.maxDeployed, $"{mission.name}: mandatory characters fit the deploy cap");
            if (mission.nextMission != null)
                Assert.Contains(mission.nextMission, catalog.missions, $"{mission.name}: next mission is in the catalog");
        }
    }
}

// Runs the Level 1 and 2 migration. Explicit: it moves real assets, so it runs only when asked for by name
// (the CLI runner's mirror is re-synced from the working tree before each run).
[Explicit("Moves Level 1 and Level 2 into Assets/Missions")]
public sealed class MissionMigrationTests
{
    [Test]
    public void Migrate_MovesTheLevelsIntoMissionFolders_KeepingReferences()
    {
        string level1Guid = AssetDatabase.AssetPathToGUID("Assets/Resources/Levels/Level 1/Level 1.unity");
        string prep1Guid = AssetDatabase.AssetPathToGUID("Assets/Resources/Levels/Level 1/Level 1.asset");
        Debug.Log(MissionMigration.Migrate());
        Debug.Log(MissionMigration.Migrate());   // again: skipped

        var catalog = MissionTools.LoadCatalog();
        Assert.IsNotNull(catalog);
        Assert.GreaterOrEqual(catalog.missions.Count, 2);
        var m1 = catalog.Get(1);
        var m2 = catalog.Get(2);
        Assert.AreEqual("Assets/Missions/M01 - Level 1/Battle/M01 Battle.unity", m1.BattleScenePath);
        Assert.AreEqual(level1Guid, m1.BattleSceneGuid, "the scene was moved, not copied");
        Assert.AreEqual(prep1Guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m1.preparation)));
        Assert.AreEqual("Assets/Missions/M01 - Level 1/PreBattle/M01 Prep.asset", AssetDatabase.GetAssetPath(m1.preparation));
        Assert.AreEqual("Assets/Missions/M02 - Level 2/Battle/M02 Battle.unity", m2.BattleScenePath);
        Assert.AreSame(m2, m1.nextMission, "Level 1 leads to Level 2");
        Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Resources/Levels/Level 1"), "old folder emptied and removed");
        Assert.AreEqual(m1.BattleScenePath, EditorBuildSettings.scenes[3].path);
        Assert.AreEqual(m2.BattleScenePath, EditorBuildSettings.scenes[4].path);
        Assert.AreEqual(m1.BattleScenePath, DeploymentSetup.LevelScenes[0], "setup tools follow the move");

        Scene scene = EditorSceneManager.OpenScene(m1.BattleScenePath, OpenSceneMode.Single);
        var surface = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NavMeshSurface>(true))
            .FirstOrDefault(s => s.navMeshData != null);
        if (surface != null)
            StringAssert.StartsWith("Assets/Missions/M01 - Level 1/Battle/M01 Battle/", AssetDatabase.GetAssetPath(surface.navMeshData));
    }
}
