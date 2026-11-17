using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleEnd;
using LeveragedWars.Battle.AI;
using LeveragedWars.Battle.AI.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Side = LeveragedWars.Battle.AI.EditorTools.AIBattleSetup.Side;

// The AI vs AI test level: the setup tool's result on Mission 1's map, the saved level, and a played battle between the
// two AIs. Nothing is saved: scenes are opened, built or played in memory and discarded.
// Named to run after the troop scenario tests: opening any battle scene earlier in the run (Level 1 Troop Test as well)
// makes ChainAndRangedScenarioTests.MovingOrEngagedArchers_DoNotFire fail, as Mission1SetupTests would.
[Category("Mission")]
public sealed class LevelAIvsAITests
{
    static Scene OpenAndBuild(out string report)
    {
        Scene scene = EditorSceneManager.OpenScene(Mission1Setup.ScenePath, OpenSceneMode.Single);
        report = AIBattleSetup.Apply(scene);
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
    public void TheToolBuildsTwoAIArmiesFacingEachOtherOnTheMission1Map()
    {
        Scene scene = OpenAndBuild(out string report);
        Debug.Log(report);
        AIBattleSetup.Apply(scene); // running it again rebuilds, it does not add a second set
        AssertBuilt(scene);
    }

    [Test]
    public void TheSavedLevelIsBuilt()
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(AIBattleSetup.ScenePath), "the level exists");
        AssertBuilt(EditorSceneManager.OpenScene(AIBattleSetup.ScenePath, OpenSceneMode.Single));
    }

    static void AssertBuilt(Scene scene)
    {
        var battle = InScene<Battle>(scene).Single();
        Assert.IsFalse(new SerializedObject(battle).FindProperty("_deployment").boolValue, "no deployment phase");
        Assert.IsEmpty(InScene<DeploymentZone>(scene), "no deployment zone");
        Assert.IsEmpty(InScene<Cutscene.CutScene>(scene), "no briefing");
        Assert.IsNull(scene.GetRootGameObjects().FirstOrDefault(g => g.name == Mission1Setup.EnemyAIName), "no Mission 1 AI");
        foreach (Rout rout in InScene<Rout>(scene)) Assert.AreEqual(1f, rout.RoutShare, "ends when one army is all out");
        foreach (TroopPlayerOrders controls in InScene<TroopPlayerOrders>(scene))
            Assert.AreEqual(AIBattleSetup.SpectatorTeamId, new SerializedObject(controls).FindProperty("_playerTeamId").intValue,
                "the player only watches");

        foreach (Side side in new[] { Side.South, Side.North })
        {
            Army army = side == Side.South ? battle.player : battle.enemy;
            Troop[] troops = army.GetComponentsInChildren<Troop>(true);
            Assert.AreEqual(8, troops.Length, $"{side}: eight troops");
            Assert.AreEqual(4, troops.Count(t => t.FirstCharacter.name == "Spearman"), $"{side}: four spearmen");
            Assert.AreEqual(2, troops.Count(t => t.FirstCharacter.name == "Huskarl"), $"{side}: two huskarls");
            Assert.AreEqual(2, troops.Count(t => t.FirstCharacter.name == "Skirmisher"), $"{side}: two skirmishers");
            int team = side == Side.South ? 0 : 1;
            float facingY = side == Side.South ? 1f : -1f;
            foreach (Troop t in troops)
            {
                Assert.AreEqual(team, t.TeamId, t.name);
                Assert.AreEqual(facingY, t.transform.right.y, 0.01f, $"{t.name} faces the other army");
                Assert.AreEqual(side == Side.South, t.transform.position.y < 0f, $"{t.name} on its own side");
                Assert.IsNotNull(t.GetComponent<TroopAIAdapter>());
                Assert.IsNotNull(t.GetComponentInChildren<TroopVisualiser>(true), $"{t.name} has a visualiser");
            }
        }

        GameObject root = scene.GetRootGameObjects().Single(g => g.name == AIBattleSetup.AIRootName);
        Assert.IsNotNull(root.GetComponent<BattleAIBattlefield>());
        AIDirector[] directors = root.GetComponentsInChildren<AIDirector>();
        Assert.AreEqual(2, directors.Length, "one AI per side");
        Assert.AreEqual(2, InScene<AIDirector>(scene).Count(), "and no others");
        var teams = directors.Select(d => (AITeam)new SerializedObject(d).FindProperty("aiTeam").enumValueIndex).ToList();
        CollectionAssert.AreEquivalent(new[] { AITeam.Player, AITeam.Enemy }, teams);

        foreach (AIDirector director in directors)
        {
            var so = new SerializedObject(director);
            AITeam team = (AITeam)so.FindProperty("aiTeam").enumValueIndex;
            Side side = team == AITeam.Player ? Side.South : Side.North;
            Army army = side == Side.South ? battle.player : battle.enemy;
            Assert.IsTrue(so.FindProperty("beginOnStart").boolValue);
            Assert.AreSame(root.GetComponent<BattleAIBattlefield>(), so.FindProperty("battlefield").objectReferenceValue);

            var mission = director.GetComponent<AIMission>();
            var d = mission.Detachments.ToDictionary(x => x.id);
            var spears = d[AIBattleSetup.Id(side, AIBattleSetup.Spears)];
            Assert.IsInstanceOf<ChainLineDefinition>(spears.startingFormation, $"{side} spears keep a chain");
            Assert.AreEqual(4, spears.troops.Count);
            Assert.AreEqual(2, d[AIBattleSetup.Id(side, AIBattleSetup.Skirmishers)].troops.Count);
            Assert.AreEqual(2, d[AIBattleSetup.Id(side, AIBattleSetup.Huskarls)].troops.Count);
            foreach (DetachmentSetup setup in mission.Detachments)
                foreach (Component c in setup.troops)
                {
                    Assert.IsInstanceOf<TroopAIAdapter>(c, $"{setup.id} lists an adapter");
                    Assert.IsTrue(c.transform.IsChildOf(army.transform), $"{setup.id}: {c.name} is on its own side");
                }
            Assert.AreEqual(2, mission.Triggers.Count, $"{side}: wrap and commit triggers");
            Assert.IsTrue(director.GetComponent<AIDebugOverlay>() != null);
        }
    }

    // Plays the saved level: both AIs form their spear chains, advance on each other and fight. Logs a timeline.
    // Run explicitly: -testFilter LevelAIvsAITests.PlayedBattle
    [UnityTest, Explicit]
    public IEnumerator PlayedBattle()
    {
        EditorSceneManager.OpenScene(AIBattleSetup.ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();

        Time.captureDeltaTime = 1f / 30f;
        yield return null;
        var battle = Battle.Instance;
        Assert.IsNotNull(battle, "battle in play mode");
        Assert.AreEqual(BattlePhase.Fighting, battle.Phase, "no deployment phase");
        AIDirector[] directors = Object.FindObjectsByType<AIDirector>(FindObjectsSortMode.None);
        Assert.AreEqual(2, directors.Length);
        Assert.IsTrue(directors.All(d => d.IsRunning), "both AIs running from the start");

        // Per side, indexed by (int)Side. No lambda may capture these locals: the closure object the compiler would make
        // for them is created before EnterPlayMode and does not survive the domain reload into play mode.
        var sides = new AIBlackboard[2];
        foreach (AIDirector d in directors)
        {
            Assert.IsNotNull(d.Blackboard, $"{d.name} blackboard");
            sides[(int)(d.Blackboard.AiTeam == AITeam.Player ? Side.South : Side.North)] = d.Blackboard;
        }
        Assert.IsTrue(sides.All(b => b != null), "one AI per side");

        var timeline = new StringBuilder();
        var formedBeforeContact = new bool[2];
        var engaged = new bool[2];
        var linksAtContact = new[] { -1, -1 };
        var linksAfterContact = new[] { -1, -1 };
        var contactTime = new[] { -1f, -1f };
        float closest = float.PositiveInfinity;
        int startSoldiers = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None).Sum(t => t.LivingSoldierCount);
        Side[] both = { Side.South, Side.North };

        for (float t = 0f; t < 150f && !battle.IsOver; t += 1f)
        {
            yield return Seconds(1f);
            float now = t + 1f;
            var line = new StringBuilder($"{now,5:0}s");
            var centres = new Vector3[2];
            foreach (Side side in both)
            {
                int s = (int)side;
                AIBlackboard bb = sides[s];
                Detachment spears = bb.GetDetachment(AIBattleSetup.Id(side, AIBattleSetup.Spears));
                Assert.IsNotNull(spears, $"{side} spears detachment");
                var chain = spears.Formation as ChainLineFormation;
                Assert.IsNotNull(chain, $"{side} spears in Chain Line, not {spears.Formation?.Name ?? "no formation"} ({spears.Agents.Count} troops)");
                int links = Links(chain);
                bool fighting = spears.State == DetachmentState.Engaged || spears.IsInCombat;
                if (!engaged[s] && chain.ChainOrder.Count == 4 && links == 3) formedBeforeContact[s] = true;
                if (fighting && !engaged[s])
                {
                    engaged[s] = true;
                    linksAtContact[s] = links;
                    contactTime[s] = now;
                }
                if (engaged[s] && now - contactTime[s] <= 5f) linksAfterContact[s] = links;
                centres[s] = spears.Centre;
                Detachment skirm = bb.GetDetachment(AIBattleSetup.Id(side, AIBattleSetup.Skirmishers));
                Detachment husk = bb.GetDetachment(AIBattleSetup.Id(side, AIBattleSetup.Huskarls));
                line.Append($" | {side}: spears {Fmt(spears.Centre)} {chain.Phase} {spears.State} links {links}/{chain.ChainOrder.Count - 1}" +
                            $" active {spears.ActiveAgents.Count} | skirm {skirm.Formation?.Name} {skirm.State} {skirm.ActiveAgents.Count}" +
                            $" | husk {husk.Formation?.Name} {husk.State} {husk.ActiveAgents.Count}");
            }
            closest = Mathf.Min(closest, AIGeometry.FlatDistance(centres[0], centres[1]));
            timeline.AppendLine(line.ToString());
        }

        int endSoldiers = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None).Sum(t => t.LivingSoldierCount);
        Debug.Log("[AIvsAI] timeline\n" + timeline);
        var summary = new StringBuilder($"[AIvsAI] over {battle.IsOver} won(south) {battle.Won}; soldiers {startSoldiers} -> {endSoldiers}; " +
                                        $"closest spear centres {closest:0.0}");
        foreach (Side side in both)
            summary.Append($"; {side}: formed {formedBeforeContact[(int)side]}, contact {contactTime[(int)side]}s, " +
                           $"links at contact {linksAtContact[(int)side]}, 5 s later {linksAfterContact[(int)side]}");
        Debug.Log(summary);
        Time.captureDeltaTime = 0f;

        foreach (Side side in both)
        {
            Assert.IsTrue(formedBeforeContact[(int)side], $"{side} spears stood as one linked chain before the fight");
            Assert.IsTrue(engaged[(int)side], $"{side} spears fought");
            Assert.GreaterOrEqual(linksAtContact[(int)side], 2, $"{side} spears met the enemy still chained");
        }
        Assert.Less(closest, 12f, "the armies met");
        Assert.Less(endSoldiers, startSoldiers * 0.8f, "a real fight");

        yield return new ExitPlayMode();
    }

    static int Links(ChainLineFormation chain)
    {
        if (chain == null) return 0;
        int links = 0;
        for (int i = 0; i + 1 < chain.ChainOrder.Count; i++)
            if (chain.ChainOrder[i].Troop is IAIChainTroop left && chain.ChainOrder[i + 1].Troop is IAIChainTroop right &&
                left.RightLinkTo(right) == AIChainLink.Linked) links++;
        return links;
    }

    static IEnumerator Seconds(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end) yield return null;
    }

    static string Fmt(Vector3 v) => $"({v.x:0.0},{v.y:0.0})";
}
