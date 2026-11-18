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

    // Plays the saved level: both AIs form their spear chains, advance on each other and fight. Logs a timeline. Since
    // Movie_024 it also checks that each chain stands as one line (SU-178) and that the huskarls never stand inside their
    // own spearmen (SU-179).
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
        float widestJointBeforeContact = 0f; // SU-178: gap between neighbouring spearmen's end files before the fight
        int reserveInsideOwnSpears = 0;      // SU-179: seconds a huskarl's pivot stood inside its own side's spearmen
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
            Troop[] all = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None);
            foreach (Side side in both)
            {
                int team = side == Side.South ? 0 : 1;
                var spearsOfSide = new List<Troop>();
                foreach (Troop tr in all)
                    if (tr.TeamId == team && !tr.IsRouted && tr.FirstCharacter.name == "Spearman") spearsOfSide.Add(tr);
                spearsOfSide.Sort(CompareX);
                if (!engaged[(int)side] && now >= 3f)
                    for (int i = 0; i + 1 < spearsOfSide.Count; i++)
                    {
                        Extent(spearsOfSide[i], out _, out float maxX, out _, out _);
                        Extent(spearsOfSide[i + 1], out float minX, out _, out _, out _);
                        widestJointBeforeContact = Mathf.Max(widestJointBeforeContact, minX - maxX);
                    }
                foreach (Troop h in all)
                {
                    if (h.TeamId != team || h.IsRouted || h.FirstCharacter.name != "Huskarl") continue;
                    foreach (Troop sp in spearsOfSide)
                        if (AIApproach.Contains(TroopAIAdapter.For(sp).Footprint, h.transform.position, 0f))
                        {
                            reserveInsideOwnSpears++;
                            line.Append($" | {h.name} inside {sp.name}");
                            break;
                        }
                }
            }
            timeline.AppendLine(line.ToString());
        }

        int endSoldiers = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None).Sum(t => t.LivingSoldierCount);
        Debug.Log("[AIvsAI] timeline\n" + timeline);
        var summary = new StringBuilder($"[AIvsAI] over {battle.IsOver} won(south) {battle.Won}; soldiers {startSoldiers} -> {endSoldiers}; " +
                                        $"closest spear centres {closest:0.0}; widest joint before contact " +
                                        $"{widestJointBeforeContact:0.00}; huskarl inside own spearmen {reserveInsideOwnSpears} s");
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
        Assert.Less(widestJointBeforeContact, 2f * TroopConstants.LineSpacing,
            "each chain read as one line: its joints no wider than two files (SU-178)");
        Assert.AreEqual(0, reserveInsideOwnSpears, "the huskarls went round their own spearmen, not through them (SU-179)");
        Assert.Less(endSoldiers, startSoldiers * 0.8f, "a real fight");

        yield return new ExitPlayMode();
    }

    // Diagnostics (Movie_024): plays 150 s of the saved level and logs, every half second, each spearman's line,
    // links and soldier extent (and the gaps between neighbours), and each huskarl's order, whether its pivot stands inside
    // a friend's footprint and how many of its soldiers stand inside a friendly spearman troop; and every soldier's place at
    // 4, 12 and 24 s. DiagnosticsWithoutFixes runs the same with Chain Fill To Joints (SU-178) and Go Round Friends
    // (SU-179) off, for comparison. Run explicitly: -testFilter LevelAIvsAITests.Diagnostics
    [UnityTest, Explicit]
    public IEnumerator DiagnosticsWithFixes()
    {
        SessionState.SetBool("AIvsAI.GoRound", true);
        SessionState.SetFloat("AIvsAI.DiagSeconds", 150f);
        return RunDiagnostics();
    }

    [UnityTest, Explicit]
    public IEnumerator DiagnosticsWithoutFixes()
    {
        SessionState.SetBool("AIvsAI.GoRound", false);
        SessionState.SetFloat("AIvsAI.DiagSeconds", 150f);
        return RunDiagnostics();
    }

    IEnumerator RunDiagnostics()
    {
        EditorSceneManager.OpenScene(AIBattleSetup.ScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        Time.captureDeltaTime = 1f / 30f;
        yield return null;
        var log = new StringBuilder();
        AIDirector[] directors = Object.FindObjectsByType<AIDirector>(FindObjectsSortMode.None);
        bool goRound = SessionState.GetBool("AIvsAI.GoRound", true);
        foreach (AIDirector dir in directors)
        {
            var field = typeof(AIDirector).GetField("reflexes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ((TroopReflexSettings)field.GetValue(dir)).goRoundFriends = goRound;
        }
        var formationTuning = new SerializedObject(TroopTuning.Active.Formation);
        bool fillWas = formationTuning.FindProperty("_chainFillToJoints").boolValue;
        formationTuning.FindProperty("_chainFillToJoints").boolValue = goRound;
        formationTuning.ApplyModifiedPropertiesWithoutUndo();
        var positions = new StringBuilder();
        int pivotInsideFriend = 0;
        float until = SessionState.GetFloat("AIvsAI.DiagSeconds", 40f);
        for (float t = 0f; t < until; t += 0.5f)
        {
            yield return Seconds(0.5f);
            log.AppendLine($"t={t + 0.5f:0.0}");
            Troop[] all = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None);
            float now = t + 0.5f;
            if (Mathf.Abs(now - 4f) < 0.01f || Mathf.Abs(now - 12f) < 0.01f || Mathf.Abs(now - 24f) < 0.01f)
                foreach (Troop tr in all)
                    foreach (SoldierAgentState sa in tr.Soldiers)
                    {
                        if (!sa.IsAlive) continue;
                        Vector2 sp = tr.WorldPosition(sa);
                        positions.AppendLine($"{now:0},{tr.TeamId},{tr.name},{sp.x:0.000},{sp.y:0.000}");
                    }
            foreach (int team in new[] { 0, 1 })
            {
                var spears = new List<Troop>();
                var husk = new List<Troop>();
                foreach (Troop tr in all)
                {
                    if (tr.TeamId != team) continue;
                    if (tr.FirstCharacter.name == "Spearman") spears.Add(tr);
                    else if (tr.FirstCharacter.name == "Huskarl") husk.Add(tr);
                }
                spears.Sort(CompareX);
                float prevMax = float.NaN;
                foreach (Troop tr in spears)
                {
                    Extent(tr, out float minX, out float maxX, out float minY, out float maxY);
                    TroopFormationData f = tr.Formation;
                    string len = f != null ? $"{Vector2.Distance(f.Start, f.End):0.00}" : "-";
                    string order = tr.HasManualLineOrder ? $"{tr.LineOrder.Length:0.00}" : "-";
                    string gap = float.IsNaN(prevMax) ? "" : $" gap {minX - prevMax:0.00}";
                    log.AppendLine($"  T{team} {tr.name} pos ({tr.transform.position.x:0.00},{tr.transform.position.y:0.00}) " +
                                   $"{tr.FormationMode} line {len} order {order} L0 {Link(tr, 0)} L1 {Link(tr, 1)} " +
                                   $"soldiers x[{minX:0.00},{maxX:0.00}] y[{minY:0.00},{maxY:0.00}] alive {tr.LivingSoldierCount}" +
                                   $" moving {tr.IsMoving} melee {tr.IsEngaged}{gap}");
                    prevMax = maxX;
                }
                foreach (Troop h in husk)
                {
                    int inside = 0;
                    foreach (SoldierAgentState s in h.Soldiers)
                    {
                        if (!s.IsAlive) continue;
                        Vector2 p = h.WorldPosition(s);
                        foreach (Troop sp in spears)
                        {
                            Extent(sp, out float a, out float b, out float c, out float d);
                            if (p.x > a && p.x < b && p.y > c && p.y < d) { inside++; break; }
                        }
                    }
                    string agentOrder = "";
                    foreach (AIDirector dir in directors)
                    {
                        if (dir.Blackboard == null) continue;
                        foreach (AITroopAgent ag in dir.Blackboard.Agents)
                            if (ag.Troop is TroopAIAdapter ad && ad.Troop == h) agentOrder = $"{ag.Issued} reflex {ag.Reflex}";
                    }
                    string insideOf = "";
                    foreach (Troop friend in all)
                    {
                        if (friend == h || friend.TeamId != team || friend.IsRouted) continue;
                        AIFootprint fp = TroopAIAdapter.For(friend).Footprint;
                        if (AIApproach.Contains(fp, h.transform.position, 0f)) insideOf += " " + friend.name;
                    }
                    if (insideOf.Length > 0) pivotInsideFriend++;
                    log.AppendLine($"  T{team} {h.name} pos ({h.transform.position.x:0.00},{h.transform.position.y:0.00}) " +
                                   $"pivot inside friend [{insideOf}] " +
                                   $"pursuing {h.IsPursuing} -> {(h.PursuitTarget != null ? h.PursuitTarget.name : "-")} " +
                                   $"order [{agentOrder}] soldiers inside friendly spears {inside}");
                }
            }
        }
        Debug.Log($"[AIvsAI diag] go round {goRound}: huskarl pivot inside a friend's footprint in {pivotInsideFriend} samples\n" + log);
        Debug.Log($"[AIvsAI positions] fixes {goRound}\n" + positions + "[AIvsAI positions end]");
        formationTuning.FindProperty("_chainFillToJoints").boolValue = fillWas;
        formationTuning.ApplyModifiedPropertiesWithoutUndo();
        Time.captureDeltaTime = 0f;
        yield return new ExitPlayMode();
    }

    // Records the saved level for a replay video (Tools/Replay/render_replay.py): plays up to 150 s (or to the end) at
    // 30 frames a second and writes, every second frame, every living soldier's place, facing and state, plus the map's
    // sprites, as CSV files beside the test results. Nothing is asserted. Run explicitly:
    // -testFilter LevelAIvsAITests.RecordReplay
    [UnityTest, Explicit]
    public IEnumerator RecordReplay()
    {
        EditorSceneManager.OpenScene(AIBattleSetup.ScenePath, OpenSceneMode.Single);
        string folder = ReplayFolder();
        System.IO.Directory.CreateDirectory(folder);
        WriteMapLayout(System.IO.Path.Combine(folder, "replay_map.csv"));
        yield return new EnterPlayMode();

        Time.captureDeltaTime = 1f / 30f;
        yield return null;
        var battle = Battle.Instance;
        Troop[] troops = Object.FindObjectsByType<Troop>(FindObjectsSortMode.None);
        System.Array.Sort(troops, CompareName);
        var table = new StringBuilder("index,name,team,kind\n");
        for (int i = 0; i < troops.Length; i++)
            table.AppendLine($"{i},{troops[i].name},{troops[i].TeamId},{troops[i].FirstCharacter.name}");
        System.IO.File.WriteAllText(System.IO.Path.Combine(ReplayFolder(), "replay_troops.csv"), table.ToString());

        var frames = new StringBuilder("frame,time,troop,x,y,fx,fy,flags\n");
        var troopFrames = new StringBuilder("frame,time,troop,x,y,routed,engaged,alive\n");
        int frame = 0;
        int endFrames = -1;
        while (frame < 150 * 15 && endFrames != 0)
        {
            yield return null;
            yield return null;
            float time = frame / 15f;
            for (int i = 0; i < troops.Length; i++)
            {
                Troop tr = troops[i];
                if (tr == null) continue;
                troopFrames.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:0.000},{2},{3:0.000},{4:0.000},{5},{6},{7}", frame, time, i, tr.transform.position.x,
                    tr.transform.position.y, tr.IsRouted ? 1 : 0, tr.IsEngaged ? 1 : 0, tr.LivingSoldierCount));
                foreach (SoldierAgentState s in tr.Soldiers)
                {
                    if (!s.IsAlive) continue;
                    Vector2 p = tr.WorldPosition(s);
                    Vector2 f = s.Facing;
                    int flags = (s.IsCharging ? 1 : 0) | (s.IsClashColorPulsing ? 2 : 0) | (s.IsStunned ? 4 : 0) |
                                (s.IsFeinting ? 8 : 0);
                    frames.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1:0.000},{2},{3:0.000},{4:0.000},{5:0.000},{6:0.000},{7}", frame, time, i, p.x, p.y, f.x, f.y,
                        flags));
                }
            }
            frame++;
            if (endFrames > 0) endFrames--;
            else if (endFrames < 0 && battle != null && battle.IsOver) endFrames = 15 * 4; // four more seconds
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(ReplayFolder(), "replay_soldiers.csv"), frames.ToString());
        System.IO.File.WriteAllText(System.IO.Path.Combine(ReplayFolder(), "replay_troop_states.csv"), troopFrames.ToString());
        Debug.Log($"[AIvsAI replay] {frame} frames written to {ReplayFolder()}; over {battle != null && battle.IsOver}");
        Time.captureDeltaTime = 0f;
        yield return new ExitPlayMode();
    }

    // Beside the test results when run by the CLI runner (-testResults), otherwise the project's Temp folder.
    static string ReplayFolder()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-testResults") return System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[i + 1]));
        return System.IO.Path.GetFullPath("Temp/Replay");
    }

    static int CompareName(Troop a, Troop b) => string.CompareOrdinal(a.name, b.name);

    // Every sprite in the open scene that does not belong to a troop: its texture, the part of it drawn, where it stands.
    static void WriteMapLayout(string path)
    {
        var map = new StringBuilder("texture,rx,ry,rw,rh,ppu,pivotx,pivoty,x,y,z,rotation,scalex,scaley,flipx,flipy,r,g,b,a,layer,order,enabled\n");
        foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (sr.sprite == null || sr.GetComponentInParent<Troop>() != null) continue;
            Sprite sp = sr.sprite;
            Transform t = sr.transform;
            map.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "\"{0}\",{1},{2},{3},{4},{5},{6},{7},{8:0.0000},{9:0.0000},{10:0.0000},{11:0.000},{12:0.00000},{13:0.00000},{14},{15},{16:0.000},{17:0.000},{18:0.000},{19:0.000},{20},{21},{22}",
                AssetDatabase.GetAssetPath(sp.texture), sp.rect.x, sp.rect.y, sp.rect.width, sp.rect.height,
                sp.pixelsPerUnit, sp.pivot.x, sp.pivot.y, t.position.x, t.position.y, t.position.z, t.eulerAngles.z,
                t.lossyScale.x, t.lossyScale.y, sr.flipX ? 1 : 0, sr.flipY ? 1 : 0, sr.color.r, sr.color.g, sr.color.b,
                sr.color.a, SortingLayer.GetLayerValueFromID(sr.sortingLayerID), sr.sortingOrder,
                sr.enabled && sr.gameObject.activeInHierarchy ? 1 : 0));
        }
        System.IO.File.WriteAllText(path, map.ToString());
    }

    static int CompareX(Troop a, Troop b) => a.transform.position.x.CompareTo(b.transform.position.x);

    static string Link(Troop troop, int end)
    {
        TroopEndpointLink link = troop.GetLineLink(end);
        if (link == null) return "-";
        return $"{(link.Pending ? "P" : "A")}:{(link.Other != null ? link.Other.name : "?")}/{link.OtherEnd}";
    }

    static void Extent(Troop troop, out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = minY = float.PositiveInfinity;
        maxX = maxY = float.NegativeInfinity;
        foreach (SoldierAgentState s in troop.Soldiers)
        {
            if (!s.IsAlive) continue;
            Vector2 p = troop.WorldPosition(s);
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
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
