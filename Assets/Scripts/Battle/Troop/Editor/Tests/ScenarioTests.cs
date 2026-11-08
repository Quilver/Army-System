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
        // Scenarios run on the spec defaults (a fresh TroopTuning), not the project's tuned Battle Tuning assets, so a
        // designer's tuning edit cannot change what these rules are checked against. UseTuning edits this instance.
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(12345);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    // Every character needs a melee weapon. This one adds no damage or charge bonus and the test soldiers are unskilled
    // with it, so it fights as the old weaponless test character did.
    MeleeWeaponProfile TestMeleeWeapon()
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
            null);
        _created.Add(weapon);
        return weapon;
    }

    CharacterProfile Character(int numbers = 12, int morale = 5, int aggression = 5, int maxHealth = 15, int strength = 5)
    {
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, numbers, morale,
            CombatStatLimits.Default, new CharacterEquipment(TestMeleeWeapon(), null, null),
            new SoldierStats(5, 5, strength, 5, 5, aggression, maxHealth));
        _created.Add(profile);
        return profile;
    }

    // Without a character, a default one is made with the given soldier count and morale.
    Troop CreateTroop(string name, int team, Vector2 position, int frontage = 10, int soldiers = 12, int baseMorale = 5,
        CharacterProfile character = null)
    {
        var troopCharacters = new List<CharacterProfile> { character ?? Character(soldiers, baseMorale) };
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

    void UseTuning(string property, float value)
    {
        _tuning ??= TroopTuning.CreateDefaults();
        // The field lives on one of the Battle Tuning sections; find the one that has it.
        SerializedObject serialized = null;
        SerializedProperty found = null;
        foreach (var section in _tuning.Sections)
        {
            serialized = new SerializedObject(section);
            found = serialized.FindProperty(property);
            if (found != null) break;
        }
        Assert.IsNotNull(found, $"No Battle Tuning section has the field {property}");
        found.floatValue = value;
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
    public void CharacterWithoutMeleeWeapon_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new CharacterEquipment(null, null, null));
        Assert.Throws<MissingMeleeWeaponException>(() => CharacterProfile.CreateRuntimeProfile("No Melee", null, null,
            null, 12, 5, CombatStatLimits.Default, new CharacterEquipment(), null));

        // A character asset made in the Inspector with the melee slot left empty: deploying it throws.
        var unarmed = ScriptableObject.CreateInstance<CharacterProfile>();
        _created.Add(unarmed);
        Assert.Throws<MissingMeleeWeaponException>(() => _ = unarmed.MeleeWeapon);
        var troopCharacters = new List<CharacterProfile> { unarmed };
        var root = new GameObject("Unarmed");
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        Assert.Throws<MissingMeleeWeaponException>(() => troop.Configure(0, troopCharacters, 10));
    }

    [Test]
    public void Rout_RaisesTheDestroyedEventOnce_AndDespawnsLater()
    {
        UseTuning("_routDespawnSeconds", 1f);
        Troop troop = CreateTroop("Routing", 0, Vector2.zero);
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(60f, 0f));
        int destroyedEvents = 0;
        int routedEvents = 0;
        void OnDestroyed(Troop defeated) { if (defeated == troop) destroyedEvents++; }
        BattleEvents.TroopDefeated += OnDestroyed;
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
            BattleEvents.TroopDefeated -= OnDestroyed;
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
        troop.SetMeleeState(true);             // retreats only come from melee (SU-104)
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

    // SU-14: the rear ranks start first and move faster; the front ranks start later and move slower (accordion).
    [Test]
    public void Retreat_RearRanksFallBackFirstAndFaster()
    {
        Troop troop = CreateTroop("Accordion", 0, Vector2.zero, soldiers: 30);
        troop.OrderLine(Vector2.zero, Vector2.right, troop.NaturalLineLength);
        Simulate(2f);

        var living = new List<SoldierAgentState>();
        foreach (var soldier in troop.Soldiers)
            if (soldier.IsAlive) living.Add(soldier);
        // No attacker: the retreat runs along -Facing (-x), so the rear is the lowest x and the front the highest.
        living.Sort((a, b) => World(troop, a).x.CompareTo(World(troop, b).x));
        int third = living.Count / 3;
        var start = new Dictionary<SoldierAgentState, float>();
        foreach (var soldier in living) start[soldier] = World(troop, soldier).x;

        troop.SetMeleeState(true);             // retreats only come from melee (SU-104)
        troop.SetShockForTests(troop.EffectiveMorale + 1f);
        Assert.IsTrue(troop.IsRetreating);
        Simulate(0.8f);

        float MeanFallBack(int from, int to)
        {
            float sum = 0f;
            for (int i = from; i < to; i++) sum += start[living[i]] - World(troop, living[i]).x;
            return sum / (to - from);
        }
        float rear = MeanFallBack(0, third);
        float front = MeanFallBack(living.Count - third, living.Count);
        Assert.Greater(rear, 2f, "Rear third has fallen back most of the way");
        Assert.Less(front, 1f, "Front third has barely started");
        Assert.Greater(rear - front, 1.5f, "The troop stretches out (accordion)");
    }

    static Vector2 World(Troop troop, SoldierAgentState soldier) => troop.transform.TransformPoint(soldier.Position);

    // SU-104: a break out of melee does not retreat. The troop is disordered: it stands still, refuses orders for
    // DisorderSeconds, and pays the retreat penalty; then it can move again.
    [Test]
    public void BreakOutOfMelee_Disorders_NoRetreat_NoMovementForTenSeconds()
    {
        Troop troop = CreateTroop("Shot At", 0, Vector2.zero);
        Simulate(0.1f);
        troop.MoveTo(new Vector2(30f, 0f));
        Simulate(0.5f);
        Kill(troop, 2);                        // EM 50, initial 60
        Assert.IsFalse(troop.InMelee);
        troop.SetShockForTests(51f);

        Assert.IsFalse(troop.IsRetreating, "Out of melee a break does not retreat");
        Assert.IsTrue(troop.IsDisordered);
        Assert.IsFalse(troop.IsMoving, "The march is cancelled");
        Assert.AreEqual(0f, troop.Shock);
        Assert.AreEqual(1, troop.MoraleState.RetreatCount);
        Assert.AreEqual(45f, troop.EffectiveMorale, "Same penalty as a retreat");

        Vector2 held = troop.transform.position;
        troop.MoveTo(new Vector2(30f, 0f));
        Assert.IsFalse(troop.IsMoving, "Orders are refused while disordered");
        Simulate(9.5f);
        Assert.IsTrue(troop.IsDisordered);
        Assert.Less(Vector2.Distance(held, troop.transform.position), 0.05f, "Does not move while disordered");

        Simulate(0.6f);
        Assert.IsFalse(troop.IsDisordered, "Disorder ends after DisorderSeconds");
        troop.MoveTo(new Vector2(30f, 0f));
        Simulate(1f);
        Assert.Greater(troop.transform.position.x, held.x + 0.5f, "Moves again");
    }

    // SU-104: a troop in melee still retreats, and a retreat ends any disorder.
    [Test]
    public void BreakInMelee_StillRetreats()
    {
        Troop troop = CreateTroop("Fighting", 0, Vector2.zero);
        Simulate(0.1f);
        Kill(troop, 2);
        troop.SetShockForTests(51f);
        Assert.IsTrue(troop.IsDisordered);
        troop.SetMeleeState(true);
        troop.SetShockForTests(troop.EffectiveMorale + 1f);
        Assert.IsTrue(troop.IsRetreating);
        Assert.IsFalse(troop.IsDisordered);
        Assert.AreEqual(2, troop.MoraleState.RetreatCount);
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
        Troop left = CreateTroop("Left", 0, new Vector2(-4f, 0f), frontage: 4);
        Troop right = CreateTroop("Right", 1, new Vector2(4f, 0f), frontage: 4);
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
    public void ChargeRate_IsAboutAggressionPerChargeWindow()
    {
        // Six to a rank and 90 s, so enough charges are rolled for the 15% tolerance now that only the front rank charges
        // (SU-106) at Aggression per 45 s (SU-112).
        const int aggression = 10;
        CharacterProfile tough = Character(morale: 20, aggression: aggression, maxHealth: 40, strength: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), frontage: 6, character: tough);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), frontage: 6, character: tough);
        Simulate(0.2f);
        Assert.IsTrue(left.IsEngaged && right.IsEngaged);
        left.ResetMeleeMetrics();
        right.ResetMeleeMetrics();

        // Soldier-seconds in which a soldier could roll for a charge (troop within the engagement gap, not broken, not
        // already committed).
        float eligible = 0f;
        int frames = Mathf.CeilToInt(90f / FrameTime);
        for (int i = 0; i < frames; i++)
        {
            eligible += EligibleSoldiers(left) * FrameTime + EligibleSoldiers(right) * FrameTime;
            _world.SimulateFrame(FrameTime);
        }
        int charges = left.MeleeMetrics.Snapshot(left.LivingSoldierCount).Charges
            + right.MeleeMetrics.Snapshot(right.LivingSoldierCount).Charges;
        float perWindow = charges / eligible * TroopTuning.Active.ChargeWindowSeconds;
        Debug.Log($"Charge rate: {perWindow:0.00} per soldier per {TroopTuning.Active.ChargeWindowSeconds:0} s " +
            $"(Aggression {aggression}), {charges} charges.");
        Assert.AreEqual(aggression, perWindow, aggression * 0.15f);
        Assert.Greater(left.MeleeMetrics.Snapshot(left.LivingSoldierCount).Clashes, 0, "Charges lead to clashes");
    }

    // Soldiers that may roll for a charge this frame: within the gate, alive, not committed, returning or stunned, and in
    // the front rank (SU-106, SU-107).
    static int EligibleSoldiers(Troop troop)
    {
        var chargeable = new HashSet<Troop>();
        if (troop == null || !ChargeCoordinator.CollectChargeable(troop, TroopTuning.Active.ChargeGap, chargeable)) return 0;
        var front = new HashSet<SoldierAgentState>();
        if (TroopTuning.Active.FrontRankChargesOnly)
            ChargeCoordinator.CollectFrontRank(troop, chargeable, front, new List<(SoldierAgentState, float)>());
        int count = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning || soldier.IsStunned) continue;
            if (TroopTuning.Active.FrontRankChargesOnly && !front.Contains(soldier)) continue;
            count++;
        }
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

    void UseRule(string property, bool value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TroopTuning.OverrideForTests(_tuning);
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
    }

    // --- Stunned soldiers (SU-107) ---

    // 0 HP with at least half of it Bruise: stunned, not killed. It stays alive (and counts for morale), heals its
    // bruises from 0 HP and recovers at StunRecoverHp (5).
    [Test]
    public void ZeroHpMostlyBruise_Stuns_AndRecoversAtFiveHp()
    {
        Troop troop = CreateTroop("Stunned", 0, Vector2.zero);
        Simulate(0.1f);
        SoldierAgentState soldier = troop.Soldiers[0];
        soldier.ApplyMitigation(new MitigationResult(0, 8, 7, 0));      // MaxHP 15: 8 bruise >= 7 bleed

        Assert.AreEqual(0, soldier.CurrentHp);
        Assert.IsTrue(soldier.IsStunned);
        Assert.IsTrue(soldier.IsAlive);
        Assert.IsFalse(soldier.CanSupport);
        Assert.AreEqual(12, troop.LivingSoldierCount, "A stunned soldier still counts");

        soldier.ApplyMitigation(new MitigationResult(0, 0, 5, 0));
        Assert.IsTrue(soldier.IsStunned, "No HP left to take: a hit on 0 HP does nothing");

        Simulate(4.5f);
        Assert.AreEqual(4, soldier.CurrentHp);
        Assert.IsTrue(soldier.IsStunned, "Still stunned at 4 HP");
        Simulate(1f);
        Assert.AreEqual(5, soldier.CurrentHp);
        Assert.IsFalse(soldier.IsStunned, "Recovers at 5 HP");
        Assert.AreEqual(SoldierLifeState.Fatigued, soldier.LifeState);
    }

    [Test]
    public void ZeroHpMostlyBleed_Kills_AndStunsCanBeSwitchedOff()
    {
        Troop troop = CreateTroop("Wounded", 0, Vector2.zero);
        Simulate(0.1f);
        troop.Soldiers[0].ApplyMitigation(new MitigationResult(0, 7, 8, 0));
        Assert.IsFalse(troop.Soldiers[0].IsAlive, "Bleed over half: dead");

        UseRule("_stunAtZeroHp", false);
        troop.Soldiers[1].ApplyMitigation(new MitigationResult(0, 15, 0, 0));
        Assert.IsFalse(troop.Soldiers[1].IsAlive, "Stuns off: 0 HP kills");
    }

    // A stunned soldier neither charges nor follows.
    [Test]
    public void StunnedSoldiers_DoNotCharge()
    {
        CharacterProfile keen = Character(morale: 20, aggression: 20, maxHealth: 40, strength: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), frontage: 4, character: keen);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), frontage: 4, character: keen);
        Simulate(0.2f);
        Assert.IsTrue(left.IsEngaged);
        foreach (var soldier in left.Soldiers) soldier.ApplyMitigation(new MitigationResult(0, 40, 0, 0));
        int charging = 0;
        for (int i = 0; i < Mathf.CeilToInt(3f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            foreach (var soldier in left.Soldiers)
                if (soldier.IsCommitted) charging++;
        }
        Assert.AreEqual(0, charging, "Stunned soldiers neither charge nor follow");
        Assert.IsTrue(right != null);
    }

    // SU-111: a stunned soldier that wins its clash deals no damage; when it loses it takes the hit as usual (none on 0 HP).
    [Test]
    public void StunnedWinner_DealsNoDamage()
    {
        Troop left = CreateTroop("Stunned Side", 0, new Vector2(-2f, 0f), frontage: 4);
        Troop right = CreateTroop("Healthy Side", 1, new Vector2(2f, 0f), frontage: 4,
            character: Character(morale: 20, maxHealth: 40));
        Simulate(0.1f);
        SoldierAgentState stunned = left.Soldiers[0];
        stunned.ApplyMitigation(new MitigationResult(0, 15, 0, 0));
        Assert.IsTrue(stunned.IsStunned);
        SoldierAgentState enemy = right.Soldiers[0];

        var index = new SoldierContactIndex();
        index.Rebuild(new List<Troop> { left, right });
        var duel = new DuelResolver(new SupportCalculator(index));
        int stunnedWins = 0, enemyWins = 0;
        for (int i = 0; i < 60; i++)
        {
            int enemyHp = enemy.CurrentHp;
            duel.Resolve(new SoldierContact(left, stunned, left.WorldPosition(stunned)),
                new SoldierContact(right, enemy, right.WorldPosition(enemy)));
            CombatLogEntry entry = CombatLog.Get(CombatLog.Count - 1);
            bool stunnedWon = entry.Winner.SoldierId == stunned.Id && entry.Winner.TeamId == left.TeamId;
            if (stunnedWon)
            {
                stunnedWins++;
                Assert.AreEqual(enemyHp, enemy.CurrentHp, "A stunned winner deals no damage");
                Assert.IsFalse(entry.Damage.Dealt);
                Assert.AreEqual("winner stunned", entry.Damage.NoDamageReason);
                StringAssert.Contains("(stunned)", CombatLog.Format(entry));
            }
            else enemyWins++;
            Assert.IsTrue(stunned.IsStunned, "Still down: nothing left to take on 0 HP");
        }
        Assert.Greater(stunnedWins, 5, "The stunned soldier won some clashes");
        Assert.Greater(enemyWins, 5);
    }

    // A troop that retreats from melee leaves its stunned soldiers behind, and they die. A break out of melee (disorder)
    // does not.
    [Test]
    public void RetreatFromMelee_KillsTheStunned_DisorderDoesNot()
    {
        Troop troop = CreateTroop("Breaking", 0, Vector2.zero);
        Simulate(0.1f);
        Kill(troop, 2);                                                    // EM 50 of 60
        int stunned = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive || stunned >= 2) continue;
            soldier.ApplyMitigation(new MitigationResult(0, 15, 0, 0));
            stunned++;
        }
        int died = 0;
        troop.SoldierDied += _ => died++;

        troop.SetShockForTests(51f);
        Assert.IsTrue(troop.IsDisordered);
        Assert.AreEqual(10, troop.LivingSoldierCount, "Disorder leaves the stunned alive");

        troop.SetMeleeState(true);
        troop.SetShockForTests(troop.EffectiveMorale + 1f);
        Assert.IsTrue(troop.IsRetreating);
        Assert.AreEqual(8, troop.LivingSoldierCount, "Retreating from melee kills the stunned");
        Assert.AreEqual(2, died);
    }

    // --- Front rank charges (SU-106) ---

    // Every charge is started by a soldier of the front rank: within half a rank of the FrontRankSize-th most forward
    // soldier toward the enemy, measured just before the frame it charged.
    [Test]
    public void OnlyTheFrontRank_StartsCharges()
    {
        CharacterProfile keen = Character(morale: 20, aggression: 15, maxHealth: 40, strength: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), frontage: 4, character: keen);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), frontage: 4, character: keen);
        Simulate(0.5f);
        Assert.IsTrue(left.IsEngaged && right.IsEngaged);
        Assert.AreEqual(4, ChargeCoordinator.FrontRankSize(left), "Frontage 4: four to a rank");

        int charges = 0, outOfRank = 0;
        var forward = new Dictionary<SoldierAgentState, float>();
        var front = new List<float>();
        for (int i = 0; i < Mathf.CeilToInt(30f / FrameTime) && left != null && right != null; i++)
        {
            Vector2 toward = ((Vector2)right.transform.position - (Vector2)left.transform.position).normalized;
            forward.Clear();
            front.Clear();
            foreach (var soldier in left.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                float f = Vector2.Dot(left.WorldPosition(soldier) - (Vector2)left.transform.position, toward);
                forward[soldier] = f;
                front.Add(f);
            }
            front.Sort((a, b) => b.CompareTo(a));
            float cutoff = front[Mathf.Min(ChargeCoordinator.FrontRankSize(left), front.Count) - 1] - TroopConstants.LineSpacing * 0.5f;
            var wasCharging = new HashSet<SoldierAgentState>();
            foreach (var soldier in left.Soldiers) if (soldier.IsCharging) wasCharging.Add(soldier);

            _world.SimulateFrame(FrameTime);
            if (left == null) break;
            foreach (var soldier in left.Soldiers)
            {
                if (!soldier.IsCharging || wasCharging.Contains(soldier)) continue;
                charges++;
                if (forward.TryGetValue(soldier, out float f) && f < cutoff) outOfRank++;
            }
        }
        Assert.Greater(charges, 10, "The front rank charges");
        Assert.AreEqual(0, outOfRank, "Nobody behind the front rank starts a charge");
    }

    // --- Combat log ---

    // Every clash is logged with both sides' Combat Power parts, the chance and roll, and the damage through armour.
    [Test]
    public void CombatLog_RecordsEveryClash_WithItsBonusesAndDamage()
    {
        CharacterProfile keen = Character(morale: 20, aggression: 15, maxHealth: 40);
        CreateTroop("Left", 0, new Vector2(-2f, 0f), frontage: 4, character: keen);
        CreateTroop("Right", 1, new Vector2(2f, 0f), frontage: 4, character: keen);
        Assert.AreEqual(0, CombatLog.Count, "A new battle starts an empty log");
        Simulate(10f);

        int clashes = 0;
        for (int i = 0; i < CombatLog.Count; i++)
        {
            CombatLogEntry entry = CombatLog.Get(i);
            if (entry.Kind != CombatLogKind.Clash) continue;
            clashes++;
            int a = entry.AttackerPower.Total, b = entry.DefenderPower.Total;
            Assert.AreEqual(a / (float)(a + b), entry.ChanceA, 1e-4f);
            Assert.AreEqual(entry.Roll <= entry.ChanceA * 100f, entry.AttackerWins);
            CombatLogDamage d = entry.Damage;
            if (!d.Dealt) continue;
            Assert.AreEqual(d.Strength + d.WeaponDamage + d.Domination, d.Raw);
            Assert.AreEqual(d.Raw, d.Deflected + d.Bruised + d.Bled + d.Overflow, "Every point of the hit is accounted for");
            StringAssert.Contains("Clash", CombatLog.Format(entry));
        }
        Assert.Greater(clashes, 5);
        Assert.AreEqual(CombatLog.TotalRecorded, CombatLog.Count);
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
