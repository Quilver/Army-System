using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AchievementSystem;
using BattleEnd;
using LeveragedWars.Battle.AI;
using LeveragedWars.Battle.AI.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using M2 = LeveragedWars.Battle.AI.EditorTools.Mission2Setup;

// Mission 2 (Level 2): the setup tool's result on the battle scene, and the AI's behaviour in a played battle.
// Nothing is saved: the scene is opened, set up in memory and discarded.
[Category("Mission")]
public sealed class Mission2SetupTests
{
    static Scene OpenAndSetUp(out string report)
    {
        Scene scene = EditorSceneManager.OpenScene(M2.ScenePath, OpenSceneMode.Single);
        DeploymentSetup.SetUpScene(scene); // player troops deploy from the zone
        report = M2.Apply(scene);
        return scene;
    }

    // Leave an empty scene behind: the other troop tests simulate every Troop they can find.
    [TearDown]
    public void TearDown()
    {
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static IEnumerable<T> InScene<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));

    [Test]
    public void SetUpBuildsThePlan()
    {
        Scene scene = OpenAndSetUp(out string report);
        Debug.Log(report);
        M2.Apply(scene); // running it again adds nothing
        AssertBuilt(scene);
    }

    [Test]
    public void TheSavedSceneIsSetUp() =>
        AssertBuilt(EditorSceneManager.OpenScene(M2.ScenePath, OpenSceneMode.Single));

    static void AssertBuilt(Scene scene)
    {
        // ---- Enemy army: exactly the plan's troops
        var battle = InScene<Battle>(scene).Single();
        Troop[] enemy = battle.enemy.GetComponentsInChildren<Troop>(true);
        Assert.AreEqual(M2.Plan.Length, enemy.Length, "only the plan's troops");
        foreach (M2.TroopPlacement p in M2.Plan)
        {
            Troop troop = enemy.Single(t => t.name == p.Name);
            Assert.AreEqual(p.Character, troop.FirstCharacter.name, p.Name);
            Assert.AreEqual(1, troop.TeamId, $"{p.Name} on the enemy team");
            Assert.IsNotNull(troop.GetComponent<TroopAIAdapter>(), $"{p.Name} has an adapter");
        }
        Assert.AreEqual(4, enemy.Count(t => t.FirstCharacter.name == "Spearman"), "four spearmen");
        Assert.AreEqual(2, enemy.Count(t => t.FirstCharacter.name == "Cav"), "two cavalry");
        Assert.AreEqual(3, enemy.Count(t => t.FirstCharacter.name == "Archer"), "three archers");

        // ---- Objectives
        CapturePoint primary = M2.FindObjective(scene, M2.Primary, M2.PrimaryObjectName);
        CapturePoint secondary = M2.FindObjective(scene, M2.Secondary, M2.SecondaryObjectName);
        Assert.IsNotNull(primary);
        Assert.IsNotNull(secondary);
        Assert.AreNotSame(primary, secondary);
        Assert.AreEqual(2, InScene<CapturePoint>(scene).Count(), "two objectives");
        Assert.Less(primary.transform.position.x, -25f, "primary inside the walls, north-west");
        Assert.Greater(secondary.transform.position.x, 34f, "secondary in the south-east village");
        Assert.Less(secondary.transform.position.y, -20f, "secondary in the south-east village");

        var win = new SerializedObject(primary.GetComponent<CapturedPoint>());
        Assert.IsTrue(win.FindProperty("WinCondition").boolValue && win.FindProperty("playerControlled").boolValue,
            "the player wins by capturing the primary objective");
        Assert.IsNull(secondary.GetComponent<CapturedPoint>(), "the secondary objective does not end the battle");
        Assert.AreEqual(CapturePoint.PointController.Enemy, secondary.GetComponent<DestroyedOnCapture>().DestroyedBy,
            "the enemy destroys the secondary objective by taking it");

        AchievementController achievements = InScene<AchievementController>(scene).Single();
        var achievementsSO = new SerializedObject(achievements);
        SerializedProperty list = achievementsSO.FindProperty("_CustomGlobalAchievements");
        var matching = Enumerable.Range(0, list.arraySize).Select(list.GetArrayElementAtIndex)
            .Where(e => e.FindPropertyRelative("_title").stringValue == M2.SecondaryAchievementTitle).ToList();
        Assert.AreEqual(1, matching.Count, "one secondary objective achievement");
        SerializedProperty requirement = matching[0].FindPropertyRelative("_requirement");
        Assert.IsInstanceOf<CapturedVictoryPoint>(requirement.managedReferenceValue);
        Assert.AreSame(secondary, requirement.FindPropertyRelative("capturePoint").objectReferenceValue, "earned by holding the secondary objective");
        var gold = (AchievementSystem.Gold)matching[0].FindPropertyRelative("rewards").GetArrayElementAtIndex(0).managedReferenceValue;
        Assert.AreEqual(M2.SecondaryGold, gold.GoldFor(1));

        // ---- AI
        Assert.AreEqual(1, scene.GetRootGameObjects().Count(g => g.name == Mission1Setup.EnemyAIName), "one EnemyAI");
        GameObject ai = Mission1Setup.FindEnemyAI(scene);
        Transform wallGap = ai.transform.Find(Mission1Setup.MarkersName).Find(M2.WallGap);
        Assert.IsNotNull(wallGap, "WallGap marker");
        Assert.Greater(wallGap.position.y, 28f, "the wall gap faces north");

        var director = ai.GetComponent<AIDirector>();
        var directorSO = new SerializedObject(director);
        Assert.IsTrue(directorSO.FindProperty("beginOnStart").boolValue, "starts with the scene");
        Assert.AreEqual((int)AITeam.Enemy, directorSO.FindProperty("aiTeam").enumValueIndex);

        var mission = ai.GetComponent<AIMission>();
        var d = mission.Detachments.ToDictionary(x => x.id);
        Assert.AreEqual(7, d.Count);
        Assert.AreEqual(2, d[M2.FirstLine].troops.Count);
        Assert.AreEqual(1, d[M2.FirstCavalry].troops.Count);
        Assert.AreEqual(3, d[M2.Archers].troops.Count);
        Assert.AreEqual(2, d[M2.SecondLine].troops.Count);
        Assert.AreEqual(1, d[M2.SecondCavalry].troops.Count);
        Assert.AreEqual(1, d[M2.Bandit].troops.Count);
        Assert.AreEqual(1, d[M2.VillageGuard].troops.Count);
        foreach (DetachmentSetup setup in mission.Detachments)
            foreach (Component c in setup.troops)
                Assert.IsInstanceOf<TroopAIAdapter>(c, $"{setup.id} lists an adapter");

        Assert.IsInstanceOf<AdvanceDefinition>(d[M2.FirstLine].startingFormation);
        var cavalry = (HoldDefinition)d[M2.FirstCavalry].startingFormation;
        Assert.AreEqual(AnchorKind.Detachment, cavalry.anchor.kind);
        Assert.AreEqual(M2.FirstLine, cavalry.anchor.detachmentId);
        Assert.Greater(cavalry.anchor.sideOffset, 0f, "on the first line's right, which is west for a line facing south");
        var archers = (SkirmishDefinition)d[M2.Archers].startingFormation;
        Assert.AreEqual(M2.SecondLine, archers.fallbackPoint.detachmentId);
        Assert.Greater(archers.fallbackPoint.forwardOffset, 0f, "in front of the second line");
        Assert.IsInstanceOf<HoldDefinition>(d[M2.SecondLine].startingFormation);
        Assert.IsInstanceOf<HoldDefinition>(d[M2.SecondCavalry].startingFormation);
        Assert.AreEqual(M2.Secondary, ((CaptureDefinition)d[M2.Bandit].startingFormation).objectiveId);
        var guard = (GuardDefinition)d[M2.VillageGuard].startingFormation;
        Assert.AreEqual(AnchorKind.Objective, guard.guarded.kind);
        Assert.AreEqual(M2.Primary, guard.guarded.objectiveId);

        var triggers = mission.Triggers.ToDictionary(t => t.id);
        Assert.AreEqual(3, triggers.Count);
        Assert.AreEqual(M2.FirstCavalry, ((SetFormationAction)triggers[M2.FirstCavalryFlank].actions[0]).detachmentId);
        Assert.IsInstanceOf<AnyOf>(triggers[M2.SecondLineGo].condition);
        Assert.AreEqual(2, triggers[M2.SecondLineGo].actions.Count);
        var bypass = triggers[M2.Bypass];
        var nearGap = ((AllOf)bypass.condition).conditions.OfType<OpponentNearPoint>().Single();
        Assert.AreSame(wallGap, nearGap.point);
        Assert.AreSame(wallGap, ((HoldDefinition)((SetFormationAction)bypass.actions[0]).formation).anchor.transform);
        var disable = (EnableTriggerAction)bypass.actions[1];
        Assert.AreEqual(M2.SecondLineGo, disable.triggerId);
        Assert.IsFalse(disable.enabled);
    }

    // Plays Level 2 with the AI while the player's troops stand in their deployment zone (they only fight back).
    // Expects the first line to march on the player and engage, its cavalry to go round, the archers to come out and shoot
    // while the first line fights, the bandit to take and destroy the secondary objective, and the village guard to keep
    // to the wall gap. Run explicitly: -testFilter Mission2SetupTests.PlayedBattle
    [UnityTest, Explicit, Timeout(900000)]
    public IEnumerator PlayedBattle()
    {
        Scene scene = OpenAndSetUp(out _);
        // The briefing cutscene holds the game paused until it is read; nobody reads it here.
        foreach (Cutscene.CutScene cutscene in InScene<Cutscene.CutScene>(scene).ToList())
            Object.DestroyImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(cutscene.gameObject) ?? cutscene.gameObject);
        yield return new EnterPlayMode();

        Time.captureDeltaTime = 1f / 30f;
        yield return null;
        var battle = Battle.Instance;
        Assert.IsNotNull(battle, "battle in play mode");
        var director = Object.FindAnyObjectByType<AIDirector>();
        Assert.IsNotNull(director, "the AI in play mode");
        Assert.IsTrue(director.IsRunning, "AI running from the start");
        battle.StartBattle();

        // Everything the test watches lives in an object made after entering play mode: the iterator's state is
        // serialized across the play-mode domain reload, and a closure made before it would not survive.
        var watch = new PlayedBattleWatch(director.Blackboard);
        BattleEvents.VolleyFired += watch.OnVolley;
        for (int t = 0; t < 120; t++)
        {
            yield return Seconds(1f);
            watch.Sample();
            if (t % 10 == 9) Debug.Log("[Mission2Test] progress\n" + watch.Timeline);
        }
        BattleEvents.VolleyFired -= watch.OnVolley;
        Time.captureDeltaTime = 0f;
        Debug.Log("[Mission2Test] timeline\n" + watch.Timeline);
        Debug.Log("[Mission2Test] " + watch.Summary);

        Assert.Less(watch.FirstFurthestSouth, watch.FirstStart.y - 4f, "the first line marched on the player");
        Assert.Greater(watch.FirstEngagedAt, 0f, "the first line engaged the player");
        Assert.IsTrue(watch.Blackboard.TryGetTriggerFireTime(M2.FirstCavalryFlank, out _), "the first line's cavalry went round");
        Assert.Greater(watch.ArcherVolleysWhileFirstLineFights, 0, "the archers shot while the first line fought");
        Assert.Greater(watch.DestroyedAt, 0f, "the bandit took and destroyed the secondary objective");
        Assert.AreEqual(CapturePoint.PointController.Enemy, watch.Secondary.Controller, "a destroyed village stays the enemy's");
        Assert.LessOrEqual(watch.GuardFurthest, 3f, "the village guard kept to the wall gap");

        yield return new ExitPlayMode();
    }

    sealed class PlayedBattleWatch
    {
        public readonly AIBlackboard Blackboard;
        readonly Detachment first, firstCavalry, archers, second, bandit, guard;
        public readonly CapturePoint Secondary;
        readonly HashSet<Troop> archerTroops;
        public readonly Vector3 FirstStart;
        readonly Vector3 guardStart;
        public float FirstEngagedAt = -1f, FirstArcherVolleyAt = -1f, BanditArrivedAt = -1f, DestroyedAt = -1f;
        public int ArcherVolleys, ArcherVolleysWhileFirstLineFights;
        public float GuardFurthest, ArchersFurthestSouth, FirstFurthestSouth;
        public readonly StringBuilder Timeline = new StringBuilder();

        public PlayedBattleWatch(AIBlackboard bb)
        {
            Assert.IsNotNull(bb, "the AI running once the fighting begins");
            Blackboard = bb;
            first = Get(M2.FirstLine);
            firstCavalry = Get(M2.FirstCavalry);
            archers = Get(M2.Archers);
            second = Get(M2.SecondLine);
            bandit = Get(M2.Bandit);
            guard = Get(M2.VillageGuard);
            Secondary = Object.FindObjectsByType<CapturePointAIObjective>(FindObjectsSortMode.None)
                .Single(o => o.Id == M2.Secondary).GetComponent<CapturePoint>();
            archerTroops = new HashSet<Troop>(archers.Agents.Select(a => ((TroopAIAdapter)a.Troop).Troop));
            FirstStart = first.Centre;
            guardStart = guard.Centre;
            ArchersFurthestSouth = archers.Centre.y;
            FirstFurthestSouth = FirstStart.y;
        }

        Detachment Get(string id)
        {
            Detachment d = Blackboard.GetDetachment(id);
            Assert.IsNotNull(d, $"detachment {id}");
            return d;
        }

        public void OnVolley(Troop troop)
        {
            if (!archerTroops.Contains(troop)) return;
            ArcherVolleys++;
            if (FirstArcherVolleyAt < 0f) FirstArcherVolleyAt = Blackboard.Elapsed;
            if (first.State == DetachmentState.Engaged || first.IsInCombat) ArcherVolleysWhileFirstLineFights++;
        }

        public void Sample()
        {
            if (FirstEngagedAt < 0f && first.State == DetachmentState.Engaged) FirstEngagedAt = Blackboard.Elapsed;
            if (BanditArrivedAt < 0f && bandit.ActiveAgents.Count > 0 &&
                AIGeometry.FlatDistance(bandit.Centre, Secondary.transform.position) <= Secondary.Radius)
                BanditArrivedAt = Blackboard.Elapsed;
            if (DestroyedAt < 0f && Secondary.IsDestroyed) DestroyedAt = Blackboard.Elapsed;
            GuardFurthest = Mathf.Max(GuardFurthest, AIGeometry.FlatDistance(guard.Centre, guardStart));
            ArchersFurthestSouth = Mathf.Min(ArchersFurthestSouth, archers.Centre.y);
            if (first.ActiveAgents.Count > 0) FirstFurthestSouth = Mathf.Min(FirstFurthestSouth, first.Centre.y);
            Timeline.AppendLine($"{Blackboard.Elapsed,5:0}s first {Fmt(first.Centre)} {first.State} | cav {Fmt(firstCavalry.Centre)} " +
                                $"{firstCavalry.Formation?.Name} | archers {Fmt(archers.Centre)} {archers.State} volleys {ArcherVolleys} " +
                                $"| second {second.Formation?.Name} {second.State} | bandit {Fmt(bandit.Centre)} {bandit.State} " +
                                $"| village {Secondary.Controller}{(Secondary.IsDestroyed ? " destroyed" : "")} | guard {Fmt(guard.Centre)} {guard.State}");
        }

        public string Summary =>
            $"first line engaged {FirstEngagedAt:0.0}s, came as far south as y = {FirstFurthestSouth:0.0}; first archer volley " +
            $"{FirstArcherVolleyAt:0.0}s; archer volleys {ArcherVolleys} ({ArcherVolleysWhileFirstLineFights} while the first line " +
            $"fought); archers came as far south as y = {ArchersFurthestSouth:0.0}; bandit reached the village {BanditArrivedAt:0.0}s, " +
            $"destroyed it {DestroyedAt:0.0}s; guard strayed {GuardFurthest:0.0} from its post";
    }

    static IEnumerator Seconds(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end) yield return null;
    }

    static string Fmt(Vector3 v) => $"({v.x:0.0},{v.y:0.0})";
}
