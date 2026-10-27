using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Chain edge cases (spec §3.4.1) and ranged fire (spec §4.7), stepped in Edit mode like TroopScenarioTests.
[Category("Scenario")]
public sealed class ChainAndRangedScenarioTests
{
    const float FrameTime = 1f / 30f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(777);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        TroopObjects.FlushPending();
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
        TroopTuning.OverrideForTests(null);
        if (_tuning != null) Object.DestroyImmediate(_tuning);
    }

    SoldierProfile Soldier(int maxHealth = 15, List<WeaponProfile> weapons = null, List<WeaponSkill> skills = null)
    {
        var profile = SoldierProfile.CreateCharacterProfile("Test", null, 5, 5, maxHealth, 5, 5, 5, 5,
            weapons ?? new List<WeaponProfile>(), skills ?? new List<WeaponSkill>(), null);
        _created.Add(profile);
        return profile;
    }

    Troop CreateTroop(string name, int team, Vector2 position, int baseMorale = 5, SoldierProfile soldier = null)
    {
        var profile = TroopProfile.CreateRuntimeProfile(baseMorale,
            new List<TroopProfileCount> { new() { Profile = soldier ?? Soldier(), Count = 12 } }, 5f);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
        return troop;
    }

    Troop LineTroop(string name, Vector2 position, Vector2 facing, int team = 0, int baseMorale = 5, SoldierProfile soldier = null)
    {
        Troop troop = CreateTroop(name, team, position, baseMorale, soldier);
        troop.OrderLine(position, facing, troop.NaturalLineLength);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    void UseTuning(string property, float value)
    {
        if (_tuning == null) _tuning = ScriptableObject.CreateInstance<TroopTuning>();
        var serialized = new SerializedObject(_tuning);
        serialized.FindProperty(property).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        TroopTuning.OverrideForTests(_tuning);
    }

    [Test]
    public void LinkedLinePoint_IsNeverStolen()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Simulate(0.2f);
        Assert.AreEqual(b, a.GetLineLink(TroopConstants.LeftEnd)?.Other);

        // A third troop marches its right line point onto the joint.
        Troop c = LineTroop("C", new Vector2(-1.5f, 9f), Vector2.right);
        Simulate(0.5f);
        Assert.AreEqual(b, a.GetLineLink(TroopConstants.LeftEnd)?.Other, "A-B link kept");
        Assert.AreEqual(a, b.GetLineLink(TroopConstants.RightEnd)?.Other, "A-B link kept");
        Assert.AreNotEqual(a, c.GetLineLink(TroopConstants.RightEnd)?.Other, "C never takes A's linked point");
    }

    [Test]
    public void ObstructionAcrossTheJoint_BreaksTheLink()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Simulate(0.2f);
        Assert.IsTrue(a.IsLinkActive(TroopConstants.LeftEnd));

        var wall = new GameObject("Wall") { layer = LayerMask.NameToLayer("Terrain") };
        wall.transform.position = new Vector3(0f, 4.5f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 0.4f);
        _created.Add(wall);
        Physics2D.SyncTransforms();
        _world.SimulateFrame(FrameTime);
        Assert.IsFalse(a.IsLinkActive(TroopConstants.LeftEnd), "Impassable terrain at the joint severs the link");
        Assert.IsNull(b.GetLineLink(TroopConstants.RightEnd));
        Simulate(0.5f);
        Assert.IsFalse(a.IsLinkActive(TroopConstants.LeftEnd), "No re-attach through the obstruction");
    }

    [Test]
    public void SharedRetreat_FallsOffSeventyPercentPerHop()
    {
        UseTuning("_retreatDistance", 10f);      // troop 1 retreats 10 units
        var chain = new List<Troop>();
        for (int i = 0; i < 3; i++) chain.Add(LineTroop("Chain " + (i + 1), new Vector2(0f, i * 9f), Vector2.right));
        Simulate(0.2f);
        Assert.IsTrue(chain[0].IsLinkActive(TroopConstants.LeftEnd) && chain[1].IsLinkActive(TroopConstants.LeftEnd));

        var start = new List<float>();
        foreach (var troop in chain) start.Add(troop.transform.position.x);
        foreach (var soldier in chain[0].Soldiers)
        {
            if (soldier.IsAlive && chain[0].LivingSoldierCount > 10) soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
        }
        chain[0].SetShockForTests(51f);      // retreat (EM 50 of 60), away from its facing
        Assert.IsTrue(chain[0].IsRetreating);
        Simulate(0.3f);

        Assert.AreEqual(-10f, chain[0].transform.position.x - start[0], 0.5f, "Troop 1 retreats 10");
        Assert.AreEqual(-7f, chain[1].transform.position.x - start[1], 0.5f, "Troop 2 follows 7");
        Assert.AreEqual(-4.9f, chain[2].transform.position.x - start[2], 0.5f, "Troop 3 follows 4.9");
        Assert.AreEqual(0, chain[1].MoraleState.RetreatCount, "A sympathetic retreat costs no morale");
    }

    [Test]
    public void ArcherTroop_FiresAVolleyEveryThreeSeconds_AndHitsAboutHalf()
    {
        // Long bow range, accuracy and fire rate; damage 1 so the target does not break and walk out of range.
        WeaponProfile bow = WeaponProfile.CreateRuntimeProfile("Long bow", WeaponClass.Bow, 1, 0, 20f, 2, 10,
            WeaponSpecialRule.None, false);
        WeaponProfile knife = WeaponProfile.CreateRuntimeProfile("Knife", WeaponClass.Sword, 1, 0, 0f, 0, 0,
            WeaponSpecialRule.None, false);
        _created.Add(bow);
        _created.Add(knife);
        SoldierProfile archer = Soldier(15, new List<WeaponProfile> { bow, knife },
            new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, soldier: archer);
        Troop target = LineTroop("Target", new Vector2(16f, 0f), Vector2.left, team: 1, baseMorale: 20, soldier: Soldier(40));

        Simulate(30f);
        MeleeMetrics.Summary shots = archers.MeleeMetrics.Snapshot(archers.LivingSoldierCount);
        Debug.Log($"Ranged: {archers.RangedFire.Volleys} volleys, {shots.Shots} shots, hit rate {shots.HitRate:0.00}.");
        Assert.IsFalse(archers.IsEngaged);
        Assert.That(archers.RangedFire.Volleys, Is.InRange(9, 11), "30 / FireRate 10 = one volley every 3 s");
        Assert.AreEqual(0.5f, shots.HitRate, 0.12f, "P(Hit) = (2 + 8) / (2 + 8 + 10)");
        Assert.Less(target.Soldiers.Count, 13);
    }

    [Test]
    public void MovingOrEngagedArchers_DoNotFire()
    {
        WeaponProfile bow = WeaponProfile.CreateRuntimeProfile("Long bow", WeaponClass.Bow, 10, 0, 20f, 2, 10,
            WeaponSpecialRule.None, false);
        _created.Add(bow);
        SoldierProfile archer = Soldier(15, new List<WeaponProfile> { bow }, new List<WeaponSkill> { new(WeaponClass.Bow, 8) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, soldier: archer);
        LineTroop("Melee", new Vector2(5.5f, 0f), Vector2.left, team: 1, baseMorale: 20, soldier: Soldier(40));
        Simulate(6f);
        Assert.IsTrue(archers.IsEngaged);
        Assert.AreEqual(0, archers.RangedFire.Volleys, "No fire while engaged");
    }
}
