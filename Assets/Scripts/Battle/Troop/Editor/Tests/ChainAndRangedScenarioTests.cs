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
        foreach (var target in _created)
            if (target != null) Object.DestroyImmediate(target);
        _created.Clear();
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
                WeaponSpecialRule.None, false);
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
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile> { character ?? Character(baseMorale) }, 5f);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
        return troop;
    }

    Troop LineTroop(string name, Vector2 position, Vector2 facing, int team = 0, int baseMorale = 5, CharacterProfile character = null)
    {
        Troop troop = CreateTroop(name, team, position, baseMorale, character);
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
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10,
            false);
        MeleeWeaponProfile knife = MeleeWeaponProfile.CreateRuntimeProfile("Knife", MeleeWeaponClass.Sword, 1, 0,
            WeaponSpecialRule.None, false);
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

    [Test]
    public void MovingOrEngagedArchers_DoNotFire()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 10, 20f, 2, 10,
            false);
        _created.Add(bow);
        CharacterProfile archer = Character(5, 15, ranged: bow, skills: new List<WeaponSkill> { new(WeaponClass.Bow, 8) });
        Troop archers = LineTroop("Archers", Vector2.zero, Vector2.right, character: archer);
        LineTroop("Melee", new Vector2(5.5f, 0f), Vector2.left, team: 1, character: Character(20, 40));
        Simulate(6f);
        Assert.IsTrue(archers.IsEngaged);
        Assert.AreEqual(0, archers.RangedFire.Volleys, "No fire while engaged");
    }

    RangedWeaponProfile LongBow()
    {
        RangedWeaponProfile bow = RangedWeaponProfile.CreateRuntimeProfile("Long bow", RangedWeaponClass.Bow, 1, 20f, 2, 10, false);
        _created.Add(bow);
        return bow;
    }

    CharacterProfile Squad(int numbers, RangedWeaponProfile ranged, int maxHealth = 15)
    {
        var sword = MeleeWeaponProfile.CreateRuntimeProfile("Sword", MeleeWeaponClass.Sword, 1, 0, WeaponSpecialRule.None, false);
        _created.Add(sword);
        var profile = CharacterProfile.CreateRuntimeProfile("Squad", null, null, null, numbers, 20, CombatStatLimits.Default,
            new CharacterEquipment(sword, ranged, null),
            new SoldierStats(5, 5, 5, 5, 5, 5, maxHealth, new List<WeaponSkill> { new(WeaponClass.Bow, 8), new(WeaponClass.Sword, 3) }));
        _created.Add(profile);
        return profile;
    }

    Troop TroopOf(string name, int team, Vector2 position, Vector2 facing, params CharacterProfile[] characters)
    {
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile>(characters), 5f);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
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
        Assert.Greater(archers.RangedFire.Volleys, 0, "The flank archers reach the enemy");
    }

    // D-55: in a composite troop the archers take the front rank when not engaged and fall back behind the melee soldiers
    // when engaged.
    [Test]
    public void CompositeTroop_ArchersLeadWhenFree_AndFallBackWhenEngaged()
    {
        Troop mixed = TroopOf("Mixed", 0, Vector2.zero, Vector2.right, Squad(8, LongBow()), Squad(12, null));
        Simulate(10f);
        float free = ShooterLead(mixed);
        Debug.Log($"Composite rank lead (free): {free:0.00}");
        Assert.Greater(free, 0.5f, "Archers stand in front of the melee soldiers");

        TroopOf("Enemy", 1, new Vector2(5.5f, 0f), Vector2.left, Squad(20, null, 40));
        for (int i = 0; i < 300 && !mixed.IsEngaged; i++) Simulate(FrameTime);
        Assert.IsTrue(mixed.IsEngaged, "The enemy engages the mixed troop");
        // Melee jostles the ranks, so the lead is averaged over seconds 4-8 of the engagement.
        Simulate(4f);
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
        Assert.Less(engaged, -0.4f, "Archers fall back behind the melee soldiers");
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
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile> { Squad(8, LongBow()), Squad(12, null) }, 5f);
        _created.Add(profile);
        var root = new GameObject("Idle Mixed");
        _created.Add(root);
        var mixed = root.AddComponent<Troop>();
        mixed.Configure(0, profile);
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
