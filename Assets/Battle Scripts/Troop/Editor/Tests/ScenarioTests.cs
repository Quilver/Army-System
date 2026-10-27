using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// End-to-end scenarios. The troop code lives in Assembly-CSharp (asmdef test assemblies cannot reference it), so these
// run in Edit mode and step the battle with a fixed frame time through TroopCombatWorld.SimulateFrame: the world's
// Update, each troop's Update, then the world's LateUpdate, exactly as in Play mode. Seeds are fixed, so every run is
// the same. Spec §5 (breaks and rout), §4.3 (charge rate), §4.1 (facing lock), §3.4.1 (chains).
[Category("Scenario")]
public sealed class TroopScenarioTests
{
    const float FrameTime = 1f / 30f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(12345);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
        TroopTuning.OverrideForTests(null);
        if (_tuning != null) Object.DestroyImmediate(_tuning);
    }

    SoldierProfile Soldier(int aggression = 5, int maxHealth = 15, int strength = 5)
    {
        var profile = SoldierProfile.CreateCharacterProfile("Test", null, 5, 5, maxHealth, aggression, strength, 5, 5,
            new List<WeaponProfile>(), new List<WeaponSkill>(), null);
        _created.Add(profile);
        return profile;
    }

    Troop CreateTroop(string name, int team, Vector2 position, float radius = 5f, int soldiers = 12, int baseMorale = 5,
        SoldierProfile soldier = null)
    {
        var profile = TroopProfile.CreateRuntimeProfile(baseMorale,
            new List<TroopProfileCount> { new() { Profile = soldier ?? Soldier(), Count = soldiers } }, radius);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
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

    static void Kill(Troop troop, int count)
    {
        int killed = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (killed >= count) break;
            if (!soldier.IsAlive) continue;
            soldier.ApplyMitigation(new MitigationResult(0, 0, 100, 0));
            killed++;
        }
    }

    [Test]
    public void Rout_RaisesTheDestroyedEventOnce_AndDespawnsLater()
    {
        UseTuning("_routDespawnSeconds", 1f);
        Troop troop = CreateTroop("Routing", 0, Vector2.zero);
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(60f, 0f));
        int destroyedEvents = 0;
        int routedEvents = 0;
        void OnDestroyed(IUnit unit) { if (unit == troop) destroyedEvents++; }
        IUnit.OnUnitDestroyed += OnDestroyed;
        try
        {
            troop.Routed += _ => routedEvents++;
            Simulate(0.1f);

            Kill(troop, 7);                    // EM = 5 x 5 = 25 < 0.5 x 60
            troop.SetShockForTests(26f);
            Assert.IsTrue(troop.IsRouted);
            Simulate(0.2f);
            Assert.AreEqual(1, destroyedEvents, "Destroyed event raised at rout");
            Assert.AreEqual(1, routedEvents);
            Assert.IsTrue(troop != null, "Not destroyed before RoutDespawnSeconds");
            Assert.AreEqual(FormationMode.Rout, troop.FormationMode);

            Vector2 before = troop.transform.position;
            troop.MoveTo(new Vector2(-50f, 0f));
            Assert.IsFalse(troop.IsMoving, "A routed troop ignores orders");
            Simulate(0.3f);
            Assert.Less(troop.transform.position.x, before.x, "Flees away from the nearest enemy");

            Simulate(1f);
            Assert.IsTrue(troop == null, "Destroyed after RoutDespawnSeconds");
            Assert.AreEqual(1, destroyedEvents, "Despawn raises nothing");
        }
        finally
        {
            IUnit.OnUnitDestroyed -= OnDestroyed;
        }
        Assert.IsTrue(enemy != null);
    }

    [Test]
    public void Retreat_ResetsShock_CostsFive_AndCannotReBreakDuringTheRetreat()
    {
        Troop troop = CreateTroop("Retreating", 0, Vector2.zero);
        Simulate(0.1f);

        Kill(troop, 2);                        // EM 50, initial 60
        Assert.AreEqual(50f, troop.EffectiveMorale);
        Vector2 before = troop.transform.position;
        troop.SetShockForTests(51f);
        Assert.IsTrue(troop.IsRetreating);
        Assert.IsFalse(troop.IsRouted);
        Assert.AreEqual(0f, troop.Shock);
        Assert.AreEqual(1, troop.MoraleState.RetreatCount);
        Assert.AreEqual(45f, troop.EffectiveMorale);

        troop.SetShockForTests(100f);
        Assert.AreEqual(1, troop.MoraleState.RetreatCount, "No second break while retreating (Q-34)");

        Simulate(1f);
        float moved = Vector2.Distance(before, troop.transform.position);
        Assert.AreEqual(TroopTuning.Active.RetreatDistance, moved, 0.05f, "Retreat distance");
    }

    [Test]
    public void SurroundedBreak_DoesNotRetreat_AndCostsTwenty()
    {
        Troop troop = CreateTroop("Surrounded", 0, Vector2.zero);
        troop.OrderLine(Vector2.zero, Vector2.right, troop.NaturalLineLength);
        CreateTroop("Behind", 1, new Vector2(-7f, 0f));
        Simulate(0.5f);

        Assert.Less(Vector2.Angle(Vector2.right, troop.Footprint.Facing), 0.1f, "Holds the ordered facing");
        Assert.IsTrue(troop.IsSurrounded(), "An enemy footprint behind the rear face blocks the retreat path");
        Kill(troop, 2);
        Vector2 before = troop.transform.position;
        troop.SetShockForTests(51f);
        Assert.IsFalse(troop.IsRetreating);
        Assert.IsFalse(troop.IsRouted);
        Assert.AreEqual(1, troop.MoraleState.SurroundedBreakCount);
        Assert.AreEqual(30f, troop.EffectiveMorale);  // 50 - 20
        Assert.AreEqual(0f, troop.Shock);
        Simulate(0.5f);
        Assert.Less(Vector2.Distance(before, troop.transform.position), 0.5f, "No retreat motion");
    }

    [Test]
    public void FlankedTroop_TakesTripleShock()
    {
        Troop troop = CreateTroop("Defender", 0, Vector2.zero);
        troop.OrderLine(Vector2.zero, Vector2.right, troop.NaturalLineLength);
        Troop front = CreateTroop("Front", 1, new Vector2(8f, 0f));
        Troop flank = CreateTroop("Flank", 1, new Vector2(0f, 9f));
        Simulate(0.3f);
        Assert.AreEqual(TroopFace.Front, troop.FaceAttackedBy(front));
        Assert.AreEqual(TroopFace.LeftFlank, troop.FaceAttackedBy(flank));

        troop.MoraleState.SetShockForTests(0f);
        SoldierAgentState target = troop.Soldiers[0];
        // 4 points of unarmoured bleed: x1 Shock from the front, x3 from the flank.
        troop.ApplyDamage(target, 4, front.transform.position, troop.IsFlankedBy(front));
        Assert.AreEqual(4f, troop.Shock, 1e-3f);
        troop.ApplyDamage(troop.Soldiers[1], 4, flank.transform.position, troop.IsFlankedBy(flank));
        Assert.AreEqual(16f, troop.Shock, 1e-3f);
    }

    [Test]
    public void EngagedTroop_FacingIsLocked()
    {
        Troop left = CreateTroop("Left", 0, new Vector2(-4f, 0f), radius: 2f);
        Troop right = CreateTroop("Right", 1, new Vector2(4f, 0f), radius: 2f);
        Simulate(0.5f);
        Assert.IsTrue(left.IsEngaged, "Footprints within A_Distance");
        Vector2 facing = left.NaturalFormation.Facing;
        for (int i = 0; i < 60; i++)
        {
            right.transform.position += new Vector3(0f, 0.05f, 0f);   // the enemy slides sideways
            _world.SimulateFrame(FrameTime);
            if (!left.IsEngaged) break;
            Assert.Less(Vector2.Angle(facing, left.NaturalFormation.Facing), 0.01f, "Facing locked while engaged");
        }
    }

    [Test]
    public void ChargeRate_Over30Seconds_IsAboutAggression()
    {
        const int aggression = 10;
        SoldierProfile tough = Soldier(aggression, maxHealth: 40, strength: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), radius: 1.5f, baseMorale: 20, soldier: tough);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), radius: 1.5f, baseMorale: 20, soldier: tough);
        Simulate(0.2f);
        Assert.IsTrue(left.IsEngaged && right.IsEngaged);
        left.ResetMeleeMetrics();
        right.ResetMeleeMetrics();

        // Soldier-seconds in which a soldier could roll for a charge (engaged, not broken, not already committed).
        float eligible = 0f;
        int frames = Mathf.CeilToInt(30f / FrameTime);
        for (int i = 0; i < frames; i++)
        {
            eligible += EligibleSoldiers(left) * FrameTime + EligibleSoldiers(right) * FrameTime;
            _world.SimulateFrame(FrameTime);
        }
        int charges = left.MeleeMetrics.Snapshot(left.LivingSoldierCount).Charges
            + right.MeleeMetrics.Snapshot(right.LivingSoldierCount).Charges;
        float perWindow = charges / eligible * TroopTuning.Active.ActionWindowSeconds;
        Debug.Log($"Charge rate: {perWindow:0.00} per soldier per 30 s (Aggression {aggression}), {charges} charges.");
        Assert.AreEqual(aggression, perWindow, aggression * 0.15f);
        Assert.Greater(left.MeleeMetrics.Snapshot(left.LivingSoldierCount).Clashes, 0, "Charges lead to clashes");
    }

    static int EligibleSoldiers(Troop troop)
    {
        if (troop == null || !troop.IsEngaged || troop.IsRouted || troop.IsFaultSlipping) return 0;
        int count = 0;
        foreach (var soldier in troop.Soldiers)
            if (soldier.IsAlive && !soldier.IsCommitted) count++;
        return count;
    }

    [Test]
    public void MidChainRout_SplitsTheChain_AndBrokenEndsRelax()
    {
        var troops = new List<Troop>();
        for (int i = 0; i < 5; i++)
        {
            Troop troop = CreateTroop("Chain " + (i + 1), 0, new Vector2(0f, i * 9f));
            troop.OrderLine(troop.transform.position, Vector2.right, troop.NaturalLineLength);
            troops.Add(troop);
        }
        Simulate(0.1f);
        for (int i = 0; i + 1 < troops.Count; i++)
            Assert.IsTrue(ChainLinker.LinkFromOrder(troops[i], TroopConstants.LeftEnd, troops[i + 1], TroopConstants.RightEnd));
        Simulate(0.2f);
        for (int i = 0; i < troops.Count; i++)
            Assert.AreEqual(FormationMode.Chain, troops[i].FormationMode, troops[i].name);

        Kill(troops[2], 7);
        troops[2].SetShockForTests(26f);
        Assert.IsTrue(troops[2].IsRouted);
        Simulate(0.1f);

        Assert.IsTrue(troops[0].IsLinkActive(TroopConstants.LeftEnd), "1-2 linked");
        Assert.IsNull(troops[1].GetLineLink(TroopConstants.LeftEnd), "2-3 severed");
        Assert.IsNull(troops[3].GetLineLink(TroopConstants.RightEnd), "3-4 severed");
        Assert.IsTrue(troops[3].IsLinkActive(TroopConstants.LeftEnd), "4-5 linked");
        Assert.IsNull(troops[2].GetLineLink(0));
        Assert.IsNull(troops[2].GetLineLink(1));
    }

    [Test]
    public void Links_AreAcyclic_AndNeverCrossTeams()
    {
        Troop a = CreateTroop("A", 0, new Vector2(0f, 0f));
        Troop b = CreateTroop("B", 0, new Vector2(0f, 9f));
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(0f, 18f));
        foreach (var troop in new[] { a, b })
            troop.OrderLine(troop.transform.position, Vector2.right, troop.NaturalLineLength);
        enemy.OrderLine(enemy.transform.position, Vector2.right, enemy.NaturalLineLength);
        Simulate(0.3f);
        Assert.IsTrue(a.IsLinkActive(TroopConstants.LeftEnd), "Touching friendly line points auto-attach");
        Assert.IsNull(b.GetLineLink(TroopConstants.LeftEnd), "No link to an enemy troop");
        Assert.IsFalse(ChainLinker.LinkFromOrder(b, TroopConstants.LeftEnd, enemy, TroopConstants.RightEnd));
        Assert.IsFalse(ChainLinker.LinkFromOrder(b, TroopConstants.LeftEnd, a, TroopConstants.RightEnd), "Would close a loop");
    }

    [Test]
    public void PinnedAnchor_EngagedEndDoesNotMove_LinkSnapsWhenOverStretched()
    {
        Troop anchor = CreateTroop("Anchor", 0, new Vector2(0f, 0f));
        Troop wing = CreateTroop("Wing", 0, new Vector2(0f, 9f));
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(7f, -6f));
        anchor.OrderLine(anchor.transform.position, Vector2.right, anchor.NaturalLineLength);
        wing.OrderLine(wing.transform.position, Vector2.right, wing.NaturalLineLength);
        enemy.OrderLine(enemy.transform.position, Vector2.left, enemy.NaturalLineLength);
        Simulate(0.3f);
        Assert.IsTrue(anchor.IsEngaged);
        Assert.IsFalse(wing.IsEngaged);
        Assert.IsTrue(anchor.IsLinkActive(TroopConstants.LeftEnd));

        Vector2 anchorStart = anchor.transform.position;
        // The wing is pulled far away: the engaged anchor is never dragged, and the link snaps at 3 x L_nat.
        for (int i = 0; i < 60 && anchor.IsLinkActive(TroopConstants.LeftEnd); i++)
        {
            wing.ShiftPivot(new Vector2(0f, 1f));
            _world.SimulateFrame(FrameTime);
        }
        Assert.IsFalse(anchor.IsLinkActive(TroopConstants.LeftEnd), "Link snapped");
        Assert.Less(Mathf.Abs(anchor.transform.position.y - anchorStart.y), 0.5f, "Engaged anchor not dragged along the line");
    }
}

// The relaxation of a stretched line point after its link breaks (spec §3.4.1): 2 units/s back to natural.
public sealed class EndpointRelaxTests
{
    [Test]
    public void StretchedEndpoint_RelaxesAtTwoUnitsPerSecond()
    {
        var links = new TroopLineLinks();
        links.StartRelax(1, new Vector2(4f, 0f));
        links.TickRelax(1f, 2f);
        Assert.AreEqual(2f, links.RelaxOffset(1).magnitude, 1e-4f);
        links.TickRelax(1.5f, 2f);
        Assert.AreEqual(Vector2.zero, links.RelaxOffset(1));
    }
}
