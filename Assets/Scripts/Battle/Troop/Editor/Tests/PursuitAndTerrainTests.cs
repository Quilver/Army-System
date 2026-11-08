using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// SU-73 to SU-75: pivots stopped by terrain, kept on the battlefield, and pursuers squaring up to the face they attack.
// SU-139: when a Pursue order ends. Stepped in Edit mode like TroopScenarioTests, with fixed frame time and seed. There
// is no NavMesh in the test scene, so paths are straight lines and the terrain clip is what stops the pivot; tests that
// need a battlefield edge set one with BattlefieldBounds.OverrideForTests.
[Category("Scenario")]
public sealed class PursuitAndTerrainTests
{
    const float FrameTime = 1f / 30f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(7);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        BattlefieldBounds.OverrideForTests(null);
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop CreateTroop(string name, int team, Vector2 position, int frontage = 8, int soldiers = 10)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
            null);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 10,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 40));
        _created.Add(character);
        var troopCharacters = new List<CharacterProfile> { character };
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, troopCharacters, frontage);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // Kills 7 of 10 soldiers and breaks the troop: too few left to retreat, so it routs (spec §5.4).
    static void Rout(Troop troop)
    {
        int killed = 0;
        foreach (var soldier in troop.Soldiers)
            if (killed++ < 7) soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
        troop.SetShockForTests(1000f);
        Assert.IsTrue(troop.IsRouted, "Few soldiers left and broken: the troop routs");
    }

    // Kills every living soldier through the damage path, as melee would.
    static void WipeOut(Troop troop, Troop attacker)
    {
        var soldiers = new List<SoldierAgentState>(troop.Soldiers);
        foreach (var soldier in soldiers)
            if (soldier.IsAlive) troop.ApplyDamage(soldier, 1000, attacker.transform.position, false, attacker);
        Assert.AreEqual(0, troop.LivingSoldierCount);
    }

    GameObject Wall(Vector2 center, Vector2 size)
    {
        var wall = new GameObject("Test Wall") { layer = LayerMask.NameToLayer("Terrain") };
        wall.transform.position = center;
        wall.AddComponent<BoxCollider2D>().size = size;
        _created.Add(wall);
        Physics2D.SyncTransforms();
        return wall;
    }

    // SU-75: a line attacked on its front is met parallel, whatever angle the pursuer comes in from.
    [Test]
    public void Pursuer_SquaresUpToTheFrontOfALineTarget()
    {
        Troop enemy = CreateTroop("Enemy", 1, Vector2.zero);
        enemy.OrderLine(Vector2.zero, Vector2.left, enemy.NaturalLineLength);
        Troop attacker = CreateTroop("Attacker", 0, new Vector2(-14f, 5f), 6, 8);
        Simulate(0.1f);
        attacker.Pursue(enemy);

        bool engaged = false;
        for (int i = 0; i < Mathf.CeilToInt(15f / FrameTime) && !engaged; i++)
        {
            _world.SimulateFrame(FrameTime);
            engaged = attacker.WasEngagedWith(enemy);
        }
        Assert.IsTrue(engaged, "The pursuer reaches the line");
        Simulate(3f);

        Assert.AreEqual(TroopFace.Front, enemy.FaceAttackedBy(attacker), "Attacks the front");
        float angle = Vector2.Angle(attacker.Facing, -enemy.Facing);
        Assert.Less(angle, 8f, $"Pursuer line is {angle:F1} degrees off parallel to the target's front");
    }

    // SU-75: the pursuit heading is pivot to pivot at S_Distance and squared to the attacked face at A_Distance.
    [Test]
    public void PursuitHeading_BlendsFromPivotDirectionToTheAttackedFace()
    {
        TroopFootprint target = TroopFootprint.Rect(new Vector2(0f, -3f), new Vector2(0f, 3f), Vector2.left, 0.6f);
        Vector2 ownPivot = new(-6f, 4f);
        TroopFootprint own = TroopFootprint.Circle(ownPivot, 2f, Vector2.right);

        Vector2 far = TroopEngagementSystem.PursuitHeading(target, Vector2.zero, own, ownPivot, 30f, 30f, 5f, 45f, 135f);
        Vector2 near = TroopEngagementSystem.PursuitHeading(target, Vector2.zero, own, ownPivot, 5f, 30f, 5f, 45f, 135f);
        Assert.Less(Vector2.Angle(far, -ownPivot), 0.1f, "Far: toward the target's pivot");
        Assert.Less(Vector2.Angle(near, Vector2.right), 0.1f, "Near: square to the front, facing against it");

        Vector2 flank = TroopEngagementSystem.PursuitHeading(target, Vector2.zero,
            TroopFootprint.Circle(new Vector2(1f, 8f), 2f, Vector2.right), new Vector2(1f, 8f), 5f, 30f, 5f, 45f, 135f);
        Assert.Less(Vector2.Angle(flank, Vector2.down), 0.1f, "From above a left-facing line: its right flank, faced down");
    }

    // SU-74: a march straight into a wall stops at the wall instead of carrying the troop through it.
    [Test]
    public void MarchingPivot_DoesNotCrossTerrain()
    {
        Wall(new Vector2(6f, 0f), new Vector2(1f, 40f));
        Troop troop = CreateTroop("Marcher", 0, Vector2.zero);
        troop.MoveTo(new Vector2(14f, 0f));
        Simulate(8f);
        Assert.Less(troop.transform.position.x, 5.5f, "The pivot stops on the near side of the wall");
    }

    // SU-74: a routed troop runs along a wall rather than through it.
    [Test]
    public void FleeingPivot_SlidesAlongTerrain()
    {
        Wall(new Vector2(-6f, 0f), new Vector2(1f, 60f));
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(4f, -3f));
        Troop runner = CreateTroop("Runner", 0, Vector2.zero);
        Simulate(0.1f);
        int killed = 0;
        foreach (var soldier in runner.Soldiers)
            if (killed++ < 7) soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
        runner.SetShockForTests(1000f);
        Assert.IsTrue(runner.IsRouted, "Few soldiers left and broken: the troop routs");
        Simulate(4f);
        if (runner == null) Assert.Pass("Despawned before the check");
        Assert.Greater(runner.transform.position.x, -5.5f, "Never crosses the wall");
        Assert.Greater(runner.transform.position.y, 1f, "Slides along the wall, away from the enemy");
    }

    // SU-139: a Pursue does not end when the target routs; the pursuer keeps chasing it.
    [Test]
    public void Pursuer_KeepsItsPursueOrder_AfterTheTargetRouts()
    {
        Troop target = CreateTroop("Target", 1, new Vector2(12f, 0f));
        Troop pursuer = CreateTroop("Pursuer", 0, Vector2.zero);
        Simulate(0.1f);
        pursuer.Pursue(target);
        Simulate(0.5f);
        Rout(target);
        Simulate(2f);

        Assert.IsTrue(target != null, "The routed target is still on the field (no battlefield edge here)");
        Assert.IsTrue(pursuer.IsPursuing, "Still pursuing the routed target");
        Assert.AreSame(target, pursuer.PursuitTarget);
        Assert.AreEqual(TroopOrderKind.Attack, pursuer.CurrentOrder);
        Assert.Greater(pursuer.PivotVelocity.x, 0f, "Running after it");
    }

    // SU-139: the order ends when the target is wiped out, routed or not.
    [Test]
    public void Pursuit_EndsWhenTheRoutedTargetIsWipedOut()
    {
        Troop target = CreateTroop("Target", 1, new Vector2(12f, 0f));
        Troop pursuer = CreateTroop("Pursuer", 0, Vector2.zero);
        Simulate(0.1f);
        pursuer.Pursue(target);
        Simulate(0.5f);
        Rout(target);
        Simulate(1f);
        Assert.IsTrue(pursuer.IsPursuing);

        WipeOut(target, pursuer);
        Simulate(0.1f);
        Assert.IsTrue(target == null, "A wiped-out troop is removed");
        Assert.IsFalse(pursuer.IsPursuing, "The order ends with the target");
        Assert.AreEqual(TroopOrderKind.None, pursuer.CurrentOrder);
        Assert.AreEqual(TroopState.Idle, pursuer.State);
    }

    // SU-139: the order ends when the pursuer itself breaks into a (Case B) retreat, and does not come back after it.
    [Test]
    public void Pursuit_EndsWhenThePursuerBreaksIntoARetreat()
    {
        Troop target = CreateTroop("Target", 1, new Vector2(10f, 0f));
        Troop pursuer = CreateTroop("Pursuer", 0, Vector2.zero);
        Simulate(0.1f);
        pursuer.Pursue(target);
        bool inMelee = false;
        for (int i = 0; i < Mathf.CeilToInt(15f / FrameTime) && !inMelee; i++)
        {
            _world.SimulateFrame(FrameTime);
            inMelee = pursuer.InMelee;
        }
        Assert.IsTrue(inMelee, "The pursuer reaches the target");
        Assert.IsTrue(pursuer.IsPursuing);

        pursuer.SetShockForTests(1000f);
        Assert.IsTrue(pursuer.IsRetreating, "At full strength in melee the break is a retreat");
        Assert.IsFalse(pursuer.IsPursuing, "The retreat ends the Pursue");
        Simulate(5f);
        Assert.IsFalse(pursuer.IsPursuing, "It is not taken up again after the retreat");
    }

    // SU-138, SU-139: a routed target that reaches the battlefield edge leaves the field, and its pursuer's order ends.
    [Test]
    public void Pursuit_EndsWhenTheRoutedTargetLeavesTheFieldAtTheEdge()
    {
        var field = new Rect(-40f, -40f, 80f, 80f);
        BattlefieldBounds.OverrideForTests(field);
        Troop target = CreateTroop("Target", 1, new Vector2(10f, 0f));
        Troop pursuer = CreateTroop("Pursuer", 0, Vector2.zero);
        Simulate(0.1f);
        pursuer.Pursue(target);
        Rout(target);

        float seconds = 0f;
        while (target != null && seconds < 30f)
        {
            _world.SimulateFrame(FrameTime);
            seconds += FrameTime;
        }
        Assert.IsTrue(target == null, "The routed target left the field at the edge");
        Assert.Less(seconds, _tuning.RoutDespawnSeconds, "Before the despawn time");
        _world.SimulateFrame(FrameTime);
        Assert.IsFalse(pursuer.IsPursuing, "The order ends when the target leaves the field");
        Assert.AreEqual(TroopState.Idle, pursuer.State);
    }
}
