using System.Collections;
using System.Linq;
using System.Text;
using BattleEnd;
using LeveragedWars.Battle.AI;
using LeveragedWars.Battle.AI.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Mission 1 (Level 1) enemy AI: the setup tool's result, and the AI's behaviour in a short played battle.
// Nothing is saved: the scene is opened, set up in memory and discarded.
[Category("Mission")]
public sealed class Mission1SetupTests
{
    static Scene OpenAndSetUp(out string report)
    {
        Scene scene = EditorSceneManager.OpenScene(Mission1Setup.ScenePath, OpenSceneMode.Single);
        DeploymentSetup.SetUpScene(scene); // player troops deploy from the zone
        report = Mission1Setup.Apply(scene);
        return scene;
    }

    // Leave an empty scene behind: the other troop tests simulate every Troop they can find.
    [TearDown]
    public void TearDown()
    {
        if (!Application.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [Test]
    public void SetUpBuildsDetachmentsTriggersAndWiring()
    {
        Scene scene = OpenAndSetUp(out string report);
        Debug.Log(report);
        // Running it again changes nothing structural.
        Mission1Setup.Apply(scene);

        GameObject ai = Mission1Setup.FindEnemyAI(scene);
        Assert.IsNotNull(ai);
        Assert.AreEqual(1, scene.GetRootGameObjects().Count(g => g.name == Mission1Setup.EnemyAIName), "one EnemyAI");
        Assert.AreEqual(4, ai.transform.Find(Mission1Setup.MarkersName).childCount, "four markers");

        var mission = ai.GetComponent<AIMission>();
        Assert.IsNotNull(ai.GetComponent<BattleAIBattlefield>());
        Assert.IsNotNull(ai.GetComponent<AIDebugOverlay>());
        var director = ai.GetComponent<AIDirector>();
        Assert.IsTrue(new SerializedObject(director).FindProperty("beginOnStart").boolValue, "starts with the scene");

        var d = mission.Detachments.ToDictionary(x => x.id);
        Assert.AreEqual(1, d[Mission1Setup.Archers].troops.Count);
        Assert.AreEqual(2, d[Mission1Setup.Guards].troops.Count);
        Assert.AreEqual(1, d[Mission1Setup.Cavalry].troops.Count);
        Assert.IsInstanceOf<SkirmishDefinition>(d[Mission1Setup.Archers].startingFormation);
        Assert.IsInstanceOf<GuardDefinition>(d[Mission1Setup.Guards].startingFormation);
        Assert.IsInstanceOf<HoldDefinition>(d[Mission1Setup.Cavalry].startingFormation);
        foreach (var setup in mission.Detachments)
            foreach (Component c in setup.troops)
                Assert.IsInstanceOf<TroopAIAdapter>(c, $"{setup.id} lists an adapter");

        var skirmish = (SkirmishDefinition)d[Mission1Setup.Archers].startingFormation;
        Assert.IsNotNull(skirmish.fallbackPoint.transform);

        var triggers = mission.Triggers.ToDictionary(t => t.id);
        var loop = triggers[Mission1Setup.CavalryLoop];
        Assert.IsNotNull(((OpponentCrossesLine)loop.condition).line);
        var flank = (FlankDefinition)((SetFormationAction)loop.actions[0]).formation;
        Assert.AreEqual(2, flank.route.Count(r => r != null));
        Assert.IsInstanceOf<DetachmentStatus>(triggers[Mission1Setup.GuardsAdvance].condition);

        var battle = Object.FindObjectsByType<Battle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var deploy = (UnityEvent)typeof(Battle).GetField("_Deploy", System.Reflection.BindingFlags.Instance |
                                                          System.Reflection.BindingFlags.NonPublic).GetValue(battle);
        int wired = Enumerable.Range(0, deploy.GetPersistentEventCount())
            .Count(i => deploy.GetPersistentTarget(i) == director && deploy.GetPersistentMethodName(i) == "BeginBattle");
        Assert.AreEqual(0, wired, "the AI does not wait for Deploy");

        var enemyRout = Object.FindObjectsByType<Rout>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(r => new SerializedObject(r).FindProperty("_army").objectReferenceValue == battle.enemy);
        Assert.AreEqual(Mission1Setup.EnemyRoutShare, enemyRout.RoutShare);
    }

    // Plays Level 1 with the AI: the player stays put for a while, then one player troop marches up the middle after the
    // archer. Expects the archer to harass and fall back, the lure line to send the cavalry round the river, and the
    // cavalry to charge. Run explicitly: -testFilter Mission1SetupTests.PlayedBattle
    [UnityTest, Explicit]
    public IEnumerator PlayedBattle()
    {
        OpenAndSetUp(out _);
        yield return new EnterPlayMode();

        Time.captureDeltaTime = 1f / 30f;
        yield return null;
        var battle = Battle.Instance;
        Assert.IsNotNull(battle, "battle in play mode");
        var director = Object.FindAnyObjectByType<AIDirector>();
        Assert.IsTrue(director.IsRunning, "AI running from the start, while deploying");
        battle.StartBattle();
        Assert.AreEqual(BattlePhase.Fighting, battle.Phase);

        AIBlackboard bb = director.Blackboard;
        Detachment archers = bb.GetDetachment(Mission1Setup.Archers);
        Detachment guards = bb.GetDetachment(Mission1Setup.Guards);
        Detachment cavalry = bb.GetDetachment(Mission1Setup.Cavalry);
        Vector3 archerStart = archers.Centre;
        Vector3 cavalryStart = cavalry.Centre;
        var timeline = new StringBuilder();
        bool guardsLeftPost = false, archerShot = false, archerFellBack = false, guardsEngaged = false, cavalryCharged = false;
        Vector3 cavalryLowest = cavalryStart;

        void Sample(string phase)
        {
            AITroopAgent archer = archers.Agents[0];
            AITroopAgent cav = cavalry.Agents[0];
            if (archer.Issued.Kind == AIOrderKind.Attack) archerShot = true;
            if (archer.Issued.Kind == AIOrderKind.FallBack) archerFellBack = true;
            if (guards.State == DetachmentState.Engaged) guardsEngaged = true;
            if (phase == "wait" && guards.State != DetachmentState.Waiting) guardsLeftPost = true;
            if (cav.Issued.Kind == AIOrderKind.Charge || (cav.Issued.Kind == AIOrderKind.Attack && cav.Troop.IsInMelee)) cavalryCharged = true;
            if (cav.Position.y < cavalryLowest.y) cavalryLowest = cav.Position;
            timeline.AppendLine($"{bb.Elapsed,5:0.0}s {phase} | archer {Fmt(archer.Position)} {archer.Issued.Kind} {archer.Reflex} " +
                                $"| guards {guards.Formation?.Name} {guards.State} | cav {Fmt(cav.Position)} {cavalry.Formation?.Name} {cav.Issued.Kind}");
        }

        // Phase 1: the player waits. The archer should come forward and shoot; the cavalry stays put.
        for (float t = 0; t < 20f; t += 1f)
        {
            yield return Seconds(1f);
            Sample("wait");
        }
        Assert.IsFalse(bb.TryGetTriggerFireTime(Mission1Setup.CavalryLoop, out _), "cavalry waits while the player holds");
        Assert.Less(archers.Centre.y, archerStart.y - 3f, "archer came forward to harass");
        Assert.IsFalse(guardsLeftPost, "guards hold their post while the archer harasses");

        // Phase 2: the middle player troop chases the archer up the field.
        Troop chaser = battle.player.GetComponentsInChildren<Troop>().OrderBy(t => Mathf.Abs(t.transform.position.x)).First();
        for (float t = 0; t < 60f; t += 1f)
        {
            if (chaser != null && !chaser.IsDefeated && !chaser.IsEngaged)
            {
                AITroopAgent archer = archers.Agents[0];
                Vector2 goal = archer.IsActive ? (Vector2)archer.Position : new Vector2(-2.5f, 14f);
                chaser.MoveTo(goal);
            }
            yield return Seconds(1f);
            Sample("chase");
        }

        Debug.Log("[Mission1Test] timeline\n" + timeline);
        Time.captureDeltaTime = 0f;

        Assert.IsTrue(archerShot, "archer shot at the player");
        Assert.IsTrue(archerFellBack, "archer fell back when chased");
        Assert.IsTrue(bb.TryGetTriggerFireTime(Mission1Setup.CavalryLoop, out float fired), "lure line fired");
        Debug.Log($"[Mission1Test] CavalryLoop fired at {fired - bb.BattleStartTime:0.0}s; guards engaged {guardsEngaged}; cavalry lowest point {Fmt(cavalryLowest)}");
        Assert.Less(cavalryLowest.y, -8f, "cavalry went south round the river");
        Assert.IsTrue(guardsEngaged, "guards intercepted the chaser");
        Assert.IsTrue(cavalryCharged, "cavalry charged");

        yield return new ExitPlayMode();
    }

    static IEnumerator Seconds(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end) yield return null;
    }

    static string Fmt(Vector3 v) => $"({v.x:0.0},{v.y:0.0})";
}
