using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Retreat through friends (SU-181), stepped in Edit mode with TroopCombatWorld.SimulateFrame
// as in TroopScenarioTests, on the spec defaults (a fresh TroopTuning) with a fixed seed.
[Category("Scenario")]
public sealed class RetreatAndDazeTests
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

    CharacterProfile Character(int numbers = 12, int morale = 5, int aggression = 5, int maxHealth = 15, int strength = 5)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(weapon);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, numbers, morale,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null),
            new SoldierStats(5, 5, strength, 5, 5, aggression, maxHealth));
        _created.Add(profile);
        return profile;
    }

    Troop CreateTroop(string name, int team, Vector2 position, int frontage = 10, CharacterProfile character = null)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character ?? Character() }, frontage);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    void UseTuning(string property, float value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TroopTuning.OverrideForTests(_tuning);
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
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
            TroopTuning.OverrideForTests(_tuning);
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
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

    // Breaks the troop as if in melee: it retreats.
    static void BreakInMelee(Troop troop)
    {
        troop.SetMeleeState(true);
        troop.SetShockForTests(troop.EffectiveMorale + 1f);
    }

    void SimulateUntilRetreatEnds(Troop troop)
    {
        for (int i = 0; i < 400 && troop.IsFaultSlipping; i++) _world.SimulateFrame(FrameTime);
        Assert.IsFalse(troop.IsFaultSlipping, "The retreat ends");
    }

    // --- Retreat through friends (SU-181) ---

    // A front line breaks with a reserve standing where its retreat would end: it falls back through the reserve and
    // ends FriendlyGap beyond it, none of its soldiers left inside it, and the reserve is not pushed aside.
    [Test]
    public void RetreatEndingInsideAFriend_GoesThroughItToTheFarSide()
    {
        Troop front = CreateTroop("Front", 0, Vector2.zero);
        Troop reserve = CreateTroop("Reserve", 0, new Vector2(-5f, 0f));
        CreateTroop("Enemy", 1, new Vector2(14f, 0f));
        Simulate(1f);

        Vector2 back = -front.Facing;
        float retreat = TroopTuning.Active.RetreatDistance;
        Assert.Less(FootprintGap.Gap(front.Outline.Translated(back * retreat), reserve.Outline), 0f,
            "Setup: a plain retreat would end inside the reserve");
        Assert.IsFalse(FriendlyOverlapResolver.AreLinked(front, reserve), "Setup: not chained");
        Vector2 frontBefore = front.transform.position;
        Vector2 reserveBefore = reserve.transform.position;

        Kill(front, 2);
        BreakInMelee(front);
        Assert.IsTrue(front.IsRetreating);
        SimulateUntilRetreatEnds(front);
        Simulate(1f);

        float moved = Vector2.Dot((Vector2)front.transform.position - frontBefore, back);
        Assert.Greater(moved, retreat + 1f, "The retreat went further than RetreatDistance");
        Assert.Greater(Vector2.Dot(front.Outline.Center - reserve.Outline.Center, back), 0f,
            "It ended on the far side of the reserve");
        Assert.GreaterOrEqual(FootprintGap.Gap(front.Outline, reserve.Outline), TroopTuning.Active.FriendlyGap - 0.05f,
            "Clear of the reserve");
        foreach (var soldier in front.Soldiers)
            if (soldier.IsAlive)
                Assert.IsFalse(reserve.Outline.Contains(front.WorldPosition(soldier)), "No soldier left inside the reserve");
        Assert.GreaterOrEqual(Vector2.Distance(front.transform.position, reserve.transform.position),
            FriendlyOverlapResolver.MinimumSpacing(front, reserve) - 0.05f, "Pivots the friendly spacing apart");
        Assert.Less(Vector2.Distance(reserveBefore, reserve.transform.position), 0.1f, "The reserve was not pushed aside");
    }

    // RetreatThroughFriends off: the old behaviour. The retreat is not lengthened and does not pass the reserve; the two
    // troops shove each other apart instead.
    [Test]
    public void RetreatThroughFriendsOff_TheRetreatStaysThisSideOfTheReserve_AndShovesIt()
    {
        UseTuning("_retreatThroughFriends", false);
        Troop front = CreateTroop("Front", 0, Vector2.zero);
        Troop reserve = CreateTroop("Reserve", 0, new Vector2(-5f, 0f));
        CreateTroop("Enemy", 1, new Vector2(14f, 0f));
        Simulate(1f);
        Vector2 back = -front.Facing;
        Vector2 before = front.transform.position;
        Vector2 reserveBefore = reserve.transform.position;

        Kill(front, 2);
        BreakInMelee(front);
        SimulateUntilRetreatEnds(front);
        float moved = Vector2.Dot((Vector2)front.transform.position - before, back);
        Assert.LessOrEqual(moved, TroopTuning.Active.RetreatDistance + 0.05f, "Not lengthened");
        Assert.Less(Vector2.Dot((Vector2)front.transform.position - (Vector2)reserve.transform.position, back), 0f,
            "Still this side of the reserve");
        Assert.Greater(Vector2.Distance(reserveBefore, reserve.transform.position), 0.1f, "The reserve was shoved");
    }

    // With nothing in the way the retreat is RetreatDistance, as before.
    [Test]
    public void RetreatWithAFriendWellBehind_IsUnchanged()
    {
        Troop front = CreateTroop("Front", 0, Vector2.zero);
        CreateTroop("Reserve", 0, new Vector2(-14f, 0f));
        CreateTroop("Enemy", 1, new Vector2(14f, 0f));
        Simulate(1f);
        Vector2 back = -front.Facing;
        Vector2 before = front.transform.position;

        Kill(front, 2);
        BreakInMelee(front);
        Simulate(0.5f);
        float moved = Vector2.Dot((Vector2)front.transform.position - before, back);
        Assert.AreEqual(TroopTuning.Active.RetreatDistance, moved, 0.1f);
    }
}
