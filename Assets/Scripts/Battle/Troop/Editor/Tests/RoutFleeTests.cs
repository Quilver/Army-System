using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Spec §5.4 / Q-35: routed troops flee around terrain (RoutFleePlanner). The goal choice and the stuck check are tested
// on the plain planner with faked routes (a NavMesh cannot be baked in Edit mode); the last test checks that without a
// NavMesh the rout keeps its old straight-line flight away from the nearest enemy.
public sealed class RoutFleeTests
{
    const float FrameTime = 1f / 30f;
    const float EdgeInset = 1f;
    static readonly Rect Field = new(-50f, -50f, 100f, 100f);
    static readonly Vector2 Away = Vector2.right;
    static readonly List<Vector2> EnemyBehind = new() { new Vector2(-5f, 0f) };

    static bool Open(Vector2 from, Vector2 to, List<Vector2> corners)
    {
        corners.Clear();
        corners.Add(from);
        corners.Add(to);
        return true;
    }

    static bool Never(Vector2 from, Vector2 to, List<Vector2> corners)
    {
        corners.Clear();
        return false;
    }

    // No route in directions within halfAngle of blocked (a river or a wood ahead), straight routes elsewhere.
    static RoutFleePlanner.RouteFinder BlockedWithin(Vector2 blocked, float halfAngle) => (from, to, corners) =>
    {
        corners.Clear();
        if (Vector2.Angle(to - from, blocked) <= halfAngle) return false;
        return Open(from, to, corners);
    };

    static float AngleFromAway(RoutFleePlanner planner) => Vector2.Angle(planner.Goal, Away);

    [Test]
    public void OpenField_GoalIsStraightAwayOnTheBattlefieldEdge()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, Open);
        Assert.IsTrue(planner.HasGoal);
        Assert.Less(AngleFromAway(planner), 1f, "Straight away from the enemy");
        Assert.AreEqual(Field.xMax - EdgeInset, planner.Goal.x, 1e-3f, "On the inset battlefield edge");
    }

    [Test]
    public void WithoutABattlefieldEdge_GoalIsAtTheFleeDistance()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, null, EdgeInset, Open);
        Assert.IsTrue(planner.HasGoal);
        Assert.AreEqual(RoutFleePlanner.FleeGoalDistance, planner.Goal.magnitude, 1e-3f);
    }

    // The playtest's river: nothing straight ahead is reachable, so the troop runs off to one side instead.
    [Test]
    public void BlockedAhead_PicksAReachableDirectionWithin90Degrees()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, BlockedWithin(Away, 50f));
        Assert.IsTrue(planner.HasGoal);
        float angle = AngleFromAway(planner);
        Assert.Greater(angle, 50f, "Not into the blocked sector");
        Assert.LessOrEqual(angle, RoutFleePlanner.SearchHalfAngle + 0.1f, "Still away from the enemy");
    }

    // The playtest's dead-end pocket: nothing within ±90° of away is reachable, so the whole circle is searched.
    [Test]
    public void Cornered_SearchesTheWholeCircle()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, BlockedWithin(Away, 100f));
        Assert.IsTrue(planner.HasGoal, "Finds a way out");
        Assert.Greater(AngleFromAway(planner), 100f);
    }

    [Test]
    public void NoRouteAnywhere_HasNoGoal_SoTheRoutFallsBackToStraightFlight()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, Never);
        Assert.IsFalse(planner.HasGoal);
    }

    // A route straight away that first doubles back past the enemy loses to a slightly angled direct one.
    [Test]
    public void RouteThatDoublesBackPastTheEnemy_IsNotChosen()
    {
        RoutFleePlanner.RouteFinder route = (from, to, corners) =>
        {
            if (Vector2.Angle(to - from, Away) > 1f) return Open(from, to, corners);
            corners.Clear();
            corners.Add(from);
            corners.Add(new Vector2(-4f, 8f));
            corners.Add(to);
            return true;
        };
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Tick(0f, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, route);
        Assert.IsTrue(planner.HasGoal);
        Assert.Greater(AngleFromAway(planner), 1f);
        Assert.LessOrEqual(AngleFromAway(planner), RoutFleePlanner.CandidateStepDegrees + 0.1f);
    }

    // A pivot that does not move for StuckCheckSeconds picks a new goal away from the heading that failed.
    [Test]
    public void StuckPivot_PicksANewGoalAndAvoidsTheFailedHeading()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        float time = 0f;
        for (; time <= RoutFleePlanner.StuckCheckSeconds + FrameTime; time += FrameTime)
            planner.Tick(time, Vector2.zero, Away, EnemyBehind, Field, EdgeInset, Open);
        Assert.IsTrue(planner.HasGoal);
        Assert.AreEqual(1, planner.AvoidedCount, "The failed heading is remembered");
        Assert.Greater(AngleFromAway(planner), RoutFleePlanner.AvoidHalfAngle - 1f, "Turns off the failed heading");
        Assert.LessOrEqual(AngleFromAway(planner), RoutFleePlanner.SearchHalfAngle + 0.1f);
    }

    [Test]
    public void MovingPivot_KeepsItsGoal_AndAvoidedHeadingsExpire()
    {
        var planner = new RoutFleePlanner();
        planner.Reset(Vector2.zero, 0f);
        planner.Avoid(Vector2.up, 0f);
        Vector2 position = Vector2.zero;
        planner.Tick(0f, position, Away, EnemyBehind, Field, EdgeInset, Open);
        Vector2 firstGoal = planner.Goal;
        const float Speed = 3f;
        for (float time = FrameTime; time <= RoutFleePlanner.AvoidSeconds + 0.5f; time += FrameTime)
        {
            position += Away * (Speed * FrameTime);
            planner.Tick(time, position, Away, EnemyBehind, Field, EdgeInset, Open);
            Assert.Less((planner.Goal - firstGoal).magnitude, 1e-3f, $"Goal kept at t = {time:F2}");
        }
        Assert.AreEqual(0, planner.AvoidedCount, "Avoided headings are forgotten after AvoidSeconds");
    }

    [Test]
    public void IsAtEdge_OnlyNearTheInsetBattlefieldEdge()
    {
        Assert.IsFalse(RoutFleePlanner.IsAtEdge(Vector2.zero, Field, EdgeInset));
        Assert.IsTrue(RoutFleePlanner.IsAtEdge(new Vector2(Field.xMax - EdgeInset, 0f), Field, EdgeInset));
        Assert.IsTrue(RoutFleePlanner.IsAtEdge(new Vector2(0f, Field.yMin + EdgeInset + 0.4f), Field, EdgeInset));
        Assert.IsFalse(RoutFleePlanner.IsAtEdge(new Vector2(0f, Field.yMin + EdgeInset + 1f), Field, EdgeInset));
        Assert.IsTrue(RoutFleePlanner.IsAtEdge(new Vector2(Field.xMin - 3f, 0f), Field, EdgeInset), "Outside counts");
    }

    // Without a NavMesh (the test scene has none) the rout has no flee goal and runs straight away from the enemy.
    [Test, Category("Scenario")]
    public void WithoutNavMesh_RoutFleesStraightAwayFromTheNearestEnemy()
    {
        var created = new List<Object>();
        var tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(tuning);
        try
        {
            var world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
            world.SetSeed(7);
            world.Initialize();
            created.Add(world.gameObject);
            Troop enemy = CreateTroop(created, "Enemy", 1, new Vector2(-6f, 0f));
            Troop runner = CreateTroop(created, "Runner", 0, Vector2.zero);
            world.SimulateFrame(FrameTime);
            int killed = 0;
            foreach (var soldier in runner.Soldiers)
                if (killed++ < 7) soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
            runner.SetShockForTests(1000f);
            Assert.IsTrue(runner.IsRouted, "Few soldiers left and broken: the troop routs");
            for (int i = 0; i < Mathf.CeilToInt(1f / FrameTime); i++) world.SimulateFrame(FrameTime);
            if (runner == null) Assert.Pass("Despawned before the check");
            Assert.IsFalse(runner.TryGetRoutFleeGoal(out _), "No NavMesh: no flee goal");
            Vector2 away = (Vector2)runner.transform.position - (Vector2)enemy.transform.position;
            Assert.Less(Vector2.Angle(runner.RoutFleeDirection, away), 5f, "Straight away from the enemy");
            Assert.Greater(runner.transform.position.x, 1f, "And it has run");
        }
        finally
        {
            TroopObjects.FlushPending();
            TestScene.Destroy(created);
            TroopTuning.OverrideForTests(null);
            tuning.DestroySections();
        }
    }

    static Troop CreateTroop(List<Object> created, string name, int team, Vector2 position)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null, false);
        created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 10, 10,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 40));
        created.Add(character);
        var root = new GameObject(name);
        root.transform.position = position;
        created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, 8);
        return troop;
    }
}
