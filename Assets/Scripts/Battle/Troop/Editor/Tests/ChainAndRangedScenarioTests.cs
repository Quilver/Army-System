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
        // Scenarios run on the spec defaults (a fresh TroopTuning), not the project's tuned Battle Tuning assets, so a
        // designer's tuning edit cannot change what these rules are checked against. UseTuning edits this instance.
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(777);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    // A 12-soldier character. Without a melee weapon, it carries a test sword with no damage or charge bonus.
    CharacterProfile Character(int morale = 5, int maxHealth = 15, MeleeWeaponProfile melee = null,
        RangedWeaponProfile ranged = null, List<WeaponSkill> skills = null)
    {
        if (melee == null)
        {
            melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
                null);
            _created.Add(melee);
        }
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 12, morale, CombatStatLimits.Default,
            new CharacterEquipment(melee, ranged, null), new SoldierStats(5, 5, 5, 5, 5, 5, maxHealth, skills));
        _created.Add(profile);
        return profile;
    }

    // Without a character, a default one is made with the given morale.
    Troop CreateTroop(string name, int team, Vector2 position, int baseMorale = 5, CharacterProfile character = null)
    {
        var troopCharacters = new List<CharacterProfile> { character ?? Character(baseMorale) };
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, troopCharacters, 10);
        return troop;
    }

    Troop LineTroop(string name, Vector2 position, Vector2 facing, int team = 0, int baseMorale = 5, CharacterProfile character = null)
    {
        Troop troop = CreateTroop(name, team, position, baseMorale, character);
        // The ordered line is the front rank (SU-161): order it half the rank depth ahead, so the pivot stays at position.
        troop.OrderLine(troop.LineCenterAhead(position, facing, troop.NaturalLineLength), facing, troop.NaturalLineLength);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // Steps exactly frames frames; returns the time stepped.
    float SimulateFrames(int frames)
    {
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
        return frames * FrameTime;
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

    void UseTuning(string property, bool value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
    }

    // SU-152: each active link adds the Chain Bonus Tuning's Defence and Morale; switched off, nothing.
    [Test]
    public void ChainedTroops_GetTheChainBonus_PerLink()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Troop c = LineTroop("C", new Vector2(0f, 18f), Vector2.right);
        Simulate(0.3f);
        Assert.AreEqual(1, a.ActiveLinkCount, "chain end");
        Assert.AreEqual(2, b.ActiveLinkCount, "middle of the chain");
        Assert.AreEqual(1, a.ChainDefenceBonus);
        Assert.AreEqual(2, b.ChainDefenceBonus);
        Assert.AreEqual((5 + 1) * a.LivingSoldierCount, a.MoraleState.EffectiveMorale, 1e-4f);
        Assert.AreEqual((5 + 2) * b.LivingSoldierCount, b.MoraleState.EffectiveMorale, 1e-4f);
        Assert.AreEqual(5 * b.LivingSoldierCount, b.MoraleState.InitialEffectiveMorale, 1e-4f, "rout threshold unchanged");

        UseTuning("_chainBonus", false);
        Assert.AreEqual(0, b.ChainDefenceBonus);
        Assert.AreEqual(5 * b.LivingSoldierCount, b.MoraleState.EffectiveMorale, 1e-4f);
        Assert.IsNotNull(c);
    }

    // SU-150: a troop in Line (an enemy within S_Distance) marches at 0.8 of its speed toward the enemy and 0.6 moving
    // back; with the toggles off it marches at full speed. Move 5 x 0.6 = 3 units/s. Moving back it wheels to its march
    // (SU-155) and is still more than 45° off its front after 1.2 s, so the turn slowdown counts as one of the toggles.
    [TestCase(true, true, 3f * 0.8f)]
    [TestCase(false, true, 3f * 0.6f)]
    [TestCase(true, false, 3f)]
    [TestCase(false, false, 3f)]
    public void LineMarch_IsSlower_AndSlowerStillMovingBack(bool forward, bool slowdown, float expectedSpeed)
    {
        UseTuning("_lineMarchSlowdown", slowdown);
        UseTuning("_lineBackwardSlowdown", slowdown);
        UseTuning("_lineTurnSlowdown", slowdown);
        Troop troop = CreateTroop("Marcher", 0, Vector2.zero);
        CreateTroop("Enemy", 1, new Vector2(24f, 0f));
        Simulate(0.5f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "the enemy within S_Distance puts it in Line");
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.right), 0.9f, "facing the enemy");

        troop.MoveTo(new Vector2(forward ? 8f : -8f, 0f));
        Simulate(0.2f);
        Vector2 start = troop.transform.position;
        Simulate(1f);
        float speed = Vector2.Distance(start, troop.transform.position);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode);
        Assert.AreEqual(expectedSpeed, speed, 0.15f);
    }

    // SU-154: a troop no longer turns toward an enemy by itself. An enemy within S_Distance on its flank puts it in Line,
    // but it keeps its own front (a new troop faces its transform's right, +x).
    [Test]
    public void Troop_DoesNotTurnToAnEnemyOnItsFlank()
    {
        Troop troop = CreateTroop("Watcher", 0, Vector2.zero);
        CreateTroop("Flanker", 1, new Vector2(0f, 20f));
        Simulate(2f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "the enemy within S_Distance puts it in Line");
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.right), 0.99f, "still facing its own front, not the enemy");
        Assert.AreEqual(1, troop.Engagements.Count);
        Assert.AreEqual(TroopFace.LeftFlank, troop.Engagements[0].Face, "the enemy is on its left flank");
    }

    // SU-154: a troop faces where it marches, and keeps that facing when it stops.
    [Test]
    public void Troop_FacesWhereItMarched_AndKeepsItWhenItStops()
    {
        Troop troop = CreateTroop("Marcher", 0, Vector2.zero);
        troop.MoveTo(new Vector2(0f, -6f));
        Simulate(1f);
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.down), 0.99f, "facing its march");
        Simulate(5f);
        Assert.IsFalse(troop.IsMoving, "arrived");
        Assert.AreEqual(FormationMode.Idle, troop.FormationMode);
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.down), 0.99f, "still facing the way it marched");
    }

    // SU-155: a troop in Line wheels to a new march at Line Wheel Rate (60°/s), marching as slowly as moving back (0.6)
    // while its march is more than Line Turn Angle (45°) off its front. With Line Wheel off it turns at once and marches
    // at the line speed (0.8). Move 5 x 0.6 = 3 units/s.
    [TestCase(true)]
    [TestCase(false)]
    public void LineTroop_WheelsToItsMarch(bool wheel)
    {
        UseTuning("_lineWheel", wheel);
        Troop troop = CreateTroop("Wheeler", 0, Vector2.zero);
        CreateTroop("Enemy", 1, new Vector2(24f, 0f));
        Simulate(0.5f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode);
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.right), 0.99f);

        troop.MoveTo(new Vector2(0f, 12f));
        Simulate(0.5f);
        float turned = Vector2.Angle(Vector2.right, troop.Facing);
        Vector2 start = troop.transform.position;
        float elapsed = SimulateFrames(6);
        float speed = Vector2.Distance(start, troop.transform.position) / elapsed;
        if (wheel)
        {
            Assert.That(turned, Is.InRange(15f, 35f), "wheeling at 60°/s");
            Assert.AreEqual(3f * 0.6f, speed, 0.15f, "more than 45° off its front: as slow as moving back");
        }
        else
        {
            Assert.Greater(turned, 85f, "turned at once");
            Assert.AreEqual(3f * 0.8f, speed, 0.15f, "marching forward in line");
        }

        Simulate(1.5f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode);
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.up), 0.99f, "facing its march");
    }

    // SU-155: in Line under a line order the troop keeps its ordered front, so marching sideways is as slow as moving back.
    [Test]
    public void LineOrder_KeepsItsFront_SidewaysIsAsSlowAsMovingBack()
    {
        Troop troop = CreateTroop("Sidestepper", 0, Vector2.zero);
        CreateTroop("Enemy", 1, new Vector2(24f, 0f));
        Simulate(0.5f);
        troop.OrderLine(new Vector2(0f, 8f), Vector2.right, troop.NaturalLineLength);
        Simulate(0.2f);
        Vector2 start = troop.transform.position;
        float elapsed = SimulateFrames(30);
        float speed = Vector2.Distance(start, troop.transform.position) / elapsed;
        Assert.Greater(Vector2.Dot(troop.Facing, Vector2.right), 0.99f, "keeps its ordered front");
        Assert.AreEqual(3f * 0.6f, speed, 0.15f);
    }

    // SU-156: a Face order (a line order at the troop's own place) turns a troop that is not in line at once; a troop
    // already in line wheels to it (SU-155).
    [Test]
    public void FaceOrder_TurnsAtOnce_OrWheelsWhenAlreadyInLine()
    {
        Troop idle = CreateTroop("Idle", 0, Vector2.zero);
        idle.OrderLine(Vector2.zero, Vector2.up, idle.NaturalLineLength);
        Troop lined = CreateTroop("Lined", 0, new Vector2(0f, -60f));
        CreateTroop("Enemy", 1, new Vector2(24f, -60f));
        Simulate(0.5f);
        Assert.Greater(Vector2.Dot(idle.Facing, Vector2.up), 0.99f, "not in line: turned at once");
        Assert.AreEqual(FormationMode.Line, idle.FormationMode, "holds its line");
        Assert.AreEqual(FormationMode.Line, lined.FormationMode);

        lined.OrderLine(lined.transform.position, Vector2.up, lined.NaturalLineLength);
        Simulate(0.5f);
        Assert.That(Vector2.Angle(Vector2.right, lined.Facing), Is.InRange(15f, 35f), "in line: wheeling at 60°/s");
        Simulate(1.5f);
        Assert.Greater(Vector2.Dot(lined.Facing, Vector2.up), 0.99f, "wheeled to its ordered front");
    }

    // SU-151: with limited ammunition a soldier shoots its weapon's Ammunition and stops; unlimited, it keeps shooting.
    [TestCase(true)]
    [TestCase(false)]
    public void Archers_StopShooting_WhenOutOfAmmunition(bool limited)
    {
        UseTuning("_limitedAmmunition", limited);
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10,
            ammunition: 3);
        _created.Add(bow);
        CharacterProfile archer = Character(5, 15, ranged: bow,
            skills: new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        LineTroop("Target", new Vector2(16f, 0f), Vector2.left, team: 1, character: Character(20, 40));

        Simulate(30f);
        if (limited)
        {
            Assert.AreEqual(12 * 3, archers.RangedFire.SingleShots, "12 archers, 3 shots each");
            foreach (var soldier in archers.Soldiers) Assert.AreEqual(0, soldier.AmmunitionLeft);
            Assert.AreEqual(0f, archers.RangedFire.MaxWeaponRange(), "no shooter left");
        }
        else
        {
            Assert.That(archers.RangedFire.SingleShots, Is.InRange(100, 132), "12 archers, one shot every 3 s each");
        }
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
        UseTuning("_chainBonus", false);         // the morale below is without the chain bonus (SU-152)
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
        chain[0].SetMeleeState(true);        // retreats only come from melee (SU-104)
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
        // Long bow range, accuracy and fire rate; damage 1 so the target does not break and walk out of range. Volley
        // fire (SU-105): the whole troop shoots in sync.
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10, volley: true);
        MeleeWeaponProfile knife = MeleeWeaponProfile.CreateRuntimeProfile("Knife", MeleeWeaponClass.Sword, 1, 0,
            null);
        _created.Add(bow);
        _created.Add(knife);
        CharacterProfile archer = Character(5, 15, knife, bow,
            new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        Troop target = LineTroop("Target", new Vector2(16f, 0f), Vector2.left, team: 1, character: Character(20, 40));

        Simulate(30f);
        MeleeMetrics.Summary shots = archers.MeleeMetrics.Snapshot(archers.LivingSoldierCount);
        Debug.Log($"Ranged: {archers.RangedFire.Volleys} volleys, {shots.Shots} shots, hit rate {shots.HitRate:0.00}.");
        Assert.IsFalse(archers.IsEngaged);
        Assert.That(archers.RangedFire.Volleys, Is.InRange(9, 11), "30 / FireRate 10 = one volley every 3 s");
        Assert.AreEqual(0.5f, shots.HitRate, 0.12f, "P(Hit) = (2 + 8) / (2 + 8 + 10)");
        Assert.Less(target.Soldiers.Count, 13);
    }

    // SU-105: without Volley each archer shoots on its own clock: the same number of shots over time, but spread out
    // rather than all at once.
    [Test]
    public void ArchersWithoutVolley_ShootOnTheirOwnTiming()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10);
        _created.Add(bow);
        Assert.IsFalse(bow.Volley, "Volley is off by default");
        CharacterProfile archer = Character(5, 15, ranged: bow,
            skills: new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        LineTroop("Target", new Vector2(16f, 0f), Vector2.left, team: 1, character: Character(20, 40));

        int frames = Mathf.CeilToInt(30f / FrameTime);
        int mostInOneFrame = 0, previous = 0;
        for (int i = 0; i < frames; i++)
        {
            _world.SimulateFrame(FrameTime);
            int shots = archers.RangedFire.SingleShots;
            mostInOneFrame = Mathf.Max(mostInOneFrame, shots - previous);
            previous = shots;
        }
        Assert.AreEqual(0, archers.RangedFire.Volleys, "No volleys");
        Assert.That(archers.RangedFire.SingleShots, Is.InRange(100, 132), "12 archers, one shot every 3 s each");
        Assert.LessOrEqual(mostInOneFrame, 4, "Shots are spread out, not 12 at once");

        // Combat log: every shot that landed is logged with its hit chance, roll and damage.
        int logged = 0;
        for (int i = 0; i < CombatLog.Count; i++)
        {
            CombatLogEntry entry = CombatLog.Get(i);
            if (entry.Kind != CombatLogKind.Shot) continue;
            logged++;
            Assert.AreEqual(0.5f, entry.HitChance, 1e-4f, "P(Hit) = (2 + 8) / (2 + 8 + 10)");
            Assert.AreEqual(entry.HitRoll < entry.HitChance, entry.Hit);
            if (entry.Hit) Assert.AreEqual(1, entry.Damage.Raw);
        }
        Assert.Greater(logged, 80, "Shots in flight at the end are not logged yet");
    }

    [Test]
    public void MovingOrEngagedArchers_DoNotFire()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 10, 20f, 2, 10);
        _created.Add(bow);
        CharacterProfile archer = Character(5, 15, ranged: bow, skills: new List<WeaponSkill> { new(WeaponClass.Bow, 8) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        LineTroop("Melee", new Vector2(5.5f, 0f), Vector2.left, team: 1, character: Character(20, 40));
        Simulate(6f);
        Assert.IsTrue(archers.IsEngaged);
        Assert.AreEqual(0, archers.RangedFire.Volleys, "No fire while engaged");
        Assert.AreEqual(0, archers.RangedFire.SingleShots, "No fire while engaged");
    }

    // SU-90: a Fire & Engage weapon fires one volley when its troop enters an engagement, then holds while engaged.
    [Test]
    public void FireAndEngageArchers_FireOnceWhenTheEngagementStarts()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Fire & Engage bow", RangedWeaponClass.Bow, 1,
            20f, 2, 10, new[] { new WeaponSpecialRuleEntry(WeaponSpecialRule.FireAndEngage) });
        _created.Add(bow);
        CharacterProfile archer = Character(20, 40, ranged: bow, skills: new List<WeaponSkill> { new(WeaponClass.Bow, 8) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        LineTroop("Melee", new Vector2(5.5f, 0f), Vector2.left, team: 1, character: Character(20, 40));
        Simulate(6f);
        Assert.IsTrue(archers.IsEngaged);
        Assert.AreEqual(1, archers.RangedFire.FireAndEngageVolleys, "One volley at the start of the engagement only");
    }

    RangedWeaponProfile LongBow()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10);
        _created.Add(bow);
        return bow;
    }

    CharacterProfile Squad(int numbers, RangedWeaponProfile ranged, int maxHealth = 15)
    {
        var sword = MeleeWeaponProfile.CreateRuntimeProfile("Sword", MeleeWeaponClass.Sword, 1, 0, null);
        _created.Add(sword);
        var profile = CharacterProfile.CreateRuntimeProfile("Squad", null, null, null, numbers, 20, CombatStatLimits.Default,
            new CharacterEquipment(sword, ranged, null),
            new SoldierStats(5, 5, 5, 5, 5, 5, maxHealth, new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) }));
        _created.Add(profile);
        return profile;
    }

    Troop TroopOf(string name, int team, Vector2 position, Vector2 facing, params CharacterProfile[] characters)
    {
        var troopCharacters = new List<CharacterProfile>(characters);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, troopCharacters, 10);
        troop.OrderLine(position, facing, troop.NaturalLineLength);
        return troop;
    }

    // D-54: reach is measured from each shooter, so the flank archers can hit an enemy straight ahead of them that lies
    // outside the front arc as seen from the pivot (the old Q-41 rule would not target it).
    [Test]
    public void FlankArchers_CanTargetAnEnemyOutsideThePivotArc()
    {
        UseTuning("_rangedFrontArcDeg", 10f);
        Troop archers = TroopOf("Archers", 0, Vector2.zero, Vector2.up, Squad(8, LongBow()));
        Simulate(4f);
        float flankX = float.NegativeInfinity;
        foreach (var soldier in archers.Soldiers) flankX = Mathf.Max(flankX, archers.WorldPosition(soldier).x);
        Vector2 enemyPosition = new(flankX, 12f);
        Assert.Greater(Vector2.Angle(Vector2.up, enemyPosition), 10f, "Outside the arc measured from the pivot");

        Troop enemy = TroopOf("Enemy", 1, enemyPosition, Vector2.down, Squad(1, null, 40));
        Simulate(8f);
        Assert.Greater(archers.RangedFire.Volleys + archers.RangedFire.SingleShots, 0, "The flank archers reach the enemy");
    }

    // --- Line of sight (SU-136) ---

    RangedWeaponProfile SightBow(bool arced)
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile(arced ? "Arced bow" : "Direct bow",
            RangedWeaponClass.Bow, 1, 20f, 2, 10, arcedShot: arced);
        _created.Add(bow);
        return bow;
    }

    // A troop in one rank (frontage = soldier count), so a single soldier stands on its pivot.
    Troop RankOf(string name, int team, Vector2 position, Vector2 facing, CharacterProfile character)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, character.Numbers);
        troop.OrderLine(position, facing, troop.NaturalLineLength);
        return troop;
    }

    // One archer at the origin facing +x and one enemy soldier 14 units ahead of it.
    Troop LoneArcher(bool arced) => RankOf("Archer", 0, Vector2.zero, Vector2.right, Squad(1, SightBow(arced)));
    Troop LoneEnemy(Vector2 position) => RankOf("Enemy", 1, position, Vector2.left, Squad(1, null, 40));
    // Five friendly soldiers in a row along y = rowY from x = 3.75 to 8.25 (facing up, so the line runs along x).
    Troop FriendlyRow(float rowY) => RankOf("Row", 0, new Vector2(6f, rowY), Vector2.up, Squad(5, null, 40));
    static int Shots(Troop troop) => troop.RangedFire.Volleys + troop.RangedFire.SingleShots;

    [Test]
    public void DirectFireArcher_HoldsWhenAnotherTroopStandsInTheLineOfFire()
    {
        Troop archer = LoneArcher(arced: false);
        LoneEnemy(new Vector2(14f, 0f));
        FriendlyRow(0f);
        Simulate(8f);
        Assert.AreEqual(0, Shots(archer), "The row of friends blocks the only line to the enemy");
        Assert.IsNull(archer.RangedFire.Target, "A troop the archer cannot see is not a target");
    }

    [Test]
    public void ArcedShotArcher_ShootsOverATroopInTheLineOfFire()
    {
        Troop archer = LoneArcher(arced: true);
        Troop enemy = LoneEnemy(new Vector2(14f, 0f));
        FriendlyRow(0f);
        Simulate(8f);
        Assert.Greater(Shots(archer), 0, "Arced shots ignore troops in the way");
        Assert.AreEqual(enemy, archer.RangedFire.Target);
    }

    [Test]
    public void DirectFireArcher_WithAClearLine_Fires()
    {
        Troop archer = LoneArcher(arced: false);
        Troop enemy = LoneEnemy(new Vector2(14f, 0f));
        FriendlyRow(5f);    // beside the line of fire, not on it
        Simulate(8f);
        Assert.Greater(Shots(archer), 0);
        Assert.AreEqual(enemy, archer.RangedFire.Target);
    }

    // The nearer enemy is hidden behind the row, so the archer shoots the further one it can see.
    [Test]
    public void DirectFireArcher_TargetsTheTroopItCanSee()
    {
        Troop archer = LoneArcher(arced: false);
        LoneEnemy(new Vector2(12f, 0f));
        Troop visible = LoneEnemy(new Vector2(14f, 7f));
        FriendlyRow(0f);
        Simulate(8f);
        Assert.AreEqual(visible, archer.RangedFire.Target);
        Assert.Greater(Shots(archer), 0);
    }

    // Sight-blocking terrain (default layers include "Terrain") blocks direct fire, not arced fire.
    [Test]
    public void TerrainInTheLineOfFire_BlocksDirectFire_NotArcedFire()
    {
        Assert.AreNotEqual(0, TroopTuning.Active.SightBlockingLayers.value & (1 << LayerMask.NameToLayer("Terrain")));
        var wall = new GameObject("Sight Wall") { layer = LayerMask.NameToLayer("Terrain") };
        wall.transform.position = new Vector3(7f, 0f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 3f);
        _created.Add(wall);
        Physics2D.SyncTransforms();

        Troop direct = LoneArcher(arced: false);
        Troop arced = RankOf("Arced Archer", 0, new Vector2(0f, -2f), Vector2.right, Squad(1, SightBow(arced: true)));
        LoneEnemy(new Vector2(14f, 0f));
        Simulate(8f);
        Assert.AreEqual(0, Shots(direct), "The wall blocks the direct line");
        Assert.Greater(Shots(arced), 0, "Arced shots go over the wall");
    }

    // D-55: in a composite troop the archers take the front rank when not engaged and fall back behind the melee soldiers
    // when engaged.
    // Melee jostles the ranks a lot, so the engaged lead is averaged over eight battle seeds. (A single seed passed only on
    // the random stream it happened to get from the soldier sprites earlier tests left in the scene; see TestScene.)
    [Test]
    public void CompositeTroop_ArchersLeadWhenFree_AndFallBackWhenEngaged()
    {
        float engaged = 0f;
        const int seeds = 8;
        for (int seed = 1; seed <= seeds; seed++)
        {
            TestScene.Destroy(_created);
            _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
            _world.SetSeed(seed * 1009);
            _world.Initialize();
            _created.Add(_world.gameObject);
            engaged += CompositeLeads();
        }
        engaged /= seeds;
        Debug.Log($"Composite rank lead (engaged, mean of {seeds} seeds): {engaged:0.00}");
        Assert.Less(engaged, -0.4f, "Archers fall back behind the melee soldiers");
    }

    // The free lead is checked here; returns the engaged lead.
    float CompositeLeads()
    {
        Troop mixed = TroopOf("Mixed", 0, Vector2.zero, Vector2.right, Squad(8, LongBow()), Squad(12, null));
        // The soldiers spawn in the blob; since troops are 1.5x larger (SU-131) the slowest seed takes about 20 s to
        // sort its ranks from there.
        Simulate(20f);
        float free = ShooterLead(mixed);
        Debug.Log($"Composite rank lead (free): {free:0.00}");
        Assert.Greater(free, 0.5f, "Archers stand in front of the melee soldiers");

        TroopOf("Enemy", 1, new Vector2(5.5f, 0f), Vector2.left, Squad(20, null, 40));
        for (int i = 0; i < 300 && !mixed.IsEngaged; i++) Simulate(FrameTime);
        Assert.IsTrue(mixed.IsEngaged, "The enemy engages the mixed troop");
        // Melee jostles the ranks, so the lead is averaged over seconds 12-16 of the engagement.
        Simulate(12f);
        float engaged = 0f;
        int samples = 0;
        for (; samples < 8 && mixed.IsEngaged; samples++)
        {
            Simulate(0.5f);
            engaged += ShooterLead(mixed);
        }
        engaged /= Mathf.Max(1, samples);
        Debug.Log($"Composite rank lead (engaged, mean of {samples} samples): {engaged:0.00}");
        Assert.Greater(samples, 4, "Still engaged");
        return engaged;
    }

    // D-55: a composite line spreads both kinds of soldier along its whole length, even when they start at opposite ends.
    [Test]
    public void CompositeTroop_SpreadsBothKindsAlongTheLine()
    {
        Troop mixed = TroopOf("Mixed", 0, Vector2.zero, Vector2.right, Squad(8, LongBow()), Squad(12, null));
        // Archers start at the top end of the line (+y), melee soldiers at the bottom end.
        int r = 0, m = 0;
        foreach (var soldier in mixed.Soldiers)
            soldier.Position = soldier.Character.RangedWeapon != null ? new Vector2(0f, 2f + 0.6f * r++) : new Vector2(0f, -2f - 0.6f * m++);
        Simulate(12f);

        Vector2 tangent = new(-mixed.Facing.y, mixed.Facing.x);
        Vector2 pivot = mixed.transform.position;
        float rangedMean = 0f, meleeMean = 0f, rangedMin = float.PositiveInfinity, rangedMax = float.NegativeInfinity;
        float meleeMin = float.PositiveInfinity, meleeMax = float.NegativeInfinity;
        foreach (var soldier in mixed.Soldiers)
        {
            float along = Vector2.Dot(mixed.WorldPosition(soldier) - pivot, tangent);
            if (soldier.Character.RangedWeapon != null)
            {
                rangedMean += along / 8f;
                rangedMin = Mathf.Min(rangedMin, along);
                rangedMax = Mathf.Max(rangedMax, along);
            }
            else
            {
                meleeMean += along / 12f;
                meleeMin = Mathf.Min(meleeMin, along);
                meleeMax = Mathf.Max(meleeMax, along);
            }
        }
        float length = mixed.NaturalLineLength;
        Debug.Log($"Composite spread: line {length:0.0}, ranged {rangedMin:0.0}..{rangedMax:0.0} (mean {rangedMean:0.0}), " +
                  $"melee {meleeMin:0.0}..{meleeMax:0.0} (mean {meleeMean:0.0}), lead {ShooterLead(mixed):0.00}");
        Assert.Less(Mathf.Abs(rangedMean - meleeMean), length * 0.15f, "Both kinds are centred on the line");
        Assert.Greater(rangedMax - rangedMin, length * 0.6f, "Archers span the line");
        Assert.Greater(meleeMax - meleeMin, length * 0.6f, "Melee soldiers span the line");
        Assert.Greater(ShooterLead(mixed), 0.5f, "Archers still in front");
    }

    // D-55: rank sorting only applies in a battle line. An idle composite troop (no line order) stays mixed.
    [Test]
    public void IdleCompositeTroop_StaysMixed()
    {
        var troopCharacters = new List<CharacterProfile> { Squad(8, LongBow()), Squad(12, null) };
        var root = new GameObject("Idle Mixed");
        _created.Add(root);

        var mixed = root.AddComponent<Troop>();
        mixed.Configure(0, troopCharacters, 10);
        Simulate(12f);
        Assert.IsFalse(mixed.FormationMode == FormationMode.Line || mixed.FormationMode == FormationMode.Chain);
        float lead = ShooterLead(mixed);
        Debug.Log($"Idle composite lead: {lead:0.00}");
        Assert.Less(Mathf.Abs(lead), 0.6f, "No front/back split while idle");
    }

    // Mean depth along the facing of the living ranged soldiers minus that of the melee soldiers.
    static float ShooterLead(Troop troop)
    {
        Vector2 facing = troop.Facing;
        Vector2 pivot = troop.transform.position;
        float ranged = 0f, melee = 0f;
        int rangedCount = 0, meleeCount = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            float depth = Vector2.Dot(troop.WorldPosition(soldier) - pivot, facing);
            if (soldier.Character.RangedWeapon != null) { ranged += depth; rangedCount++; }
            else { melee += depth; meleeCount++; }
        }
        return ranged / Mathf.Max(1, rangedCount) - melee / Mathf.Max(1, meleeCount);
    }
}
