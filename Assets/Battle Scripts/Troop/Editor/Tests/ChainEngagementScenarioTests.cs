using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Allied troops must not stop each other closing with the same enemy ("chain engagement lock", SU-45 to SU-48):
//   SU-45  engagement hold: an engaged pair stays engaged until the gap passes A_Distance + EngagementHoldMargin;
//   SU-46  engaged or attacking troops are never moved by chain tension, shared joints, shared retreat or pivot
//          separation; free friends give way;
//   SU-47  auto-links need facings within 45 degrees, and an automatic link breaks past 60;
//   SU-48  a troop with an attack order does not auto-link.
// Stepped in Edit mode like TroopScenarioTests, with fixed frame time and seed.
[Category("Scenario")]
public sealed class ChainEngagementScenarioTests
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
        _world.SetSeed(4242);
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

    void UseTuning(string property, float value)
    {
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

    // The project's Formation Tuning: lines form at 5, engage at 2, and the barrier holds them 2 apart.
    void UseProjectDistances()
    {
        UseTuning("_sDistance", 5f);
        UseTuning("_aDistance", 2f);
        UseTuning("_engagementGap", 2f);
    }

    Troop CreateTroop(string name, int team, Vector2 position, float radius = 5f, int soldiers = 12)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
            WeaponSpecialRule.None, false);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 10,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 40));
        _created.Add(character);
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile> { character }, radius);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
        return troop;
    }

    Troop LineTroop(string name, Vector2 position, Vector2 facing, int team = 0, float radius = 5f, int soldiers = 12)
    {
        Troop troop = CreateTroop(name, team, position, radius, soldiers);
        troop.OrderLine(position, facing, troop.NaturalLineLength);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    static Vector2 Direction(float degrees) =>
        new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

    static bool HasAnyLink(Troop troop) => troop.GetLineLink(0) != null || troop.GetLineLink(1) != null;

    // SU-45: engagement starts at A_Distance and is held until A_Distance + EngagementHoldMargin (1.0).
    [Test]
    public void EngagedPair_StaysEngagedUntilTheHoldMargin_AndReEngagesOnlyAtADistance()
    {
        UseProjectDistances();
        Troop ours = LineTroop("Ours", Vector2.zero, Vector2.right);
        Troop enemy = LineTroop("Enemy", new Vector2(3.2f, 0f), Vector2.left, team: 1);
        Simulate(0.5f);
        Assert.IsTrue(ours.IsEngaged && enemy.IsEngaged, "Lines start engaged");

        // Pull the enemy away a little each frame and record the gap at which 'engaged' ends.
        float releasedAt = float.NaN;
        for (int i = 0; i < 200 && float.IsNaN(releasedAt); i++)
        {
            enemy.ShiftPivot(new Vector2(0.05f, 0f));
            _world.SimulateFrame(FrameTime);
            float gap = FootprintGap.Gap(ours.Footprint, enemy.Footprint);
            if (!ours.IsEngaged) releasedAt = gap;
            else Assert.LessOrEqual(gap, 3f + 0.06f, "Still engaged past the hold edge");
        }
        Assert.IsFalse(float.IsNaN(releasedAt), "Engagement ends");
        Assert.Greater(releasedAt, 3f - 0.06f, "Held until A_Distance + 1.0");
        Assert.IsFalse(enemy.IsEngaged, "Both sides release together");

        // Coming back: no hold for a pair that is not engaged, so it re-engages only at A_Distance.
        float engagedAt = float.NaN;
        for (int i = 0; i < 200 && float.IsNaN(engagedAt); i++)
        {
            enemy.ShiftPivot(new Vector2(-0.05f, 0f));
            _world.SimulateFrame(FrameTime);
            float gap = FootprintGap.Gap(ours.Footprint, enemy.Footprint);
            if (ours.IsEngaged) engagedAt = gap;
        }
        Assert.IsFalse(float.IsNaN(engagedAt), "Engages again");
        Assert.LessOrEqual(engagedAt, 2f + 0.06f, "Re-engages at A_Distance, not inside the hold band");
    }

    // The clip: an engaged troop chained to a free one, and a third troop attacking the same enemy past the chain.
    // Before SU-45/46/48 the engaged troop flickered out of A_Distance, was dragged about 6 units along the chain, and the
    // attacker snapped onto the chain end.
    [Test]
    public void ChainedFighter_IsNotDraggedAway_AndAnAttackerPassingTheChainReachesTheEnemy()
    {
        UseProjectDistances();
        Troop enemy = CreateTroop("Enemy", 1, Vector2.zero, 3f, 13);
        Troop fighter = LineTroop("Fighter", new Vector2(5.2f, 0.3f), Vector2.left, radius: 4f, soldiers: 22);
        Troop archers = LineTroop("Archers", new Vector2(4f, -6.5f), new Vector2(-4f, 6.5f).normalized, radius: 2.5f,
            soldiers: 8);
        Simulate(0.2f);
        Assert.IsTrue(ChainLinker.LinkFromOrder(fighter, TroopConstants.LeftEnd, archers, TroopConstants.RightEnd));
        Simulate(3f);
        Assert.IsTrue(fighter.IsLinkActive(TroopConstants.LeftEnd), "Fighter and archers chained by the line order");

        Troop attacker = CreateTroop("Attacker", 0, new Vector2(-8f, -8f), 3f, 6);
        attacker.Pursue(enemy);
        Vector2 fighterStart = fighter.transform.position;
        int frames = Mathf.CeilToInt(12f / FrameTime);
        int fighterEngaged = 0, attackerEngagedLate = 0, lateFrames = 0;
        float fighterDrift = 0f;
        for (int i = 0; i < frames; i++)
        {
            _world.SimulateFrame(FrameTime);
            if (fighter.IsEngaged) fighterEngaged++;
            fighterDrift = Mathf.Max(fighterDrift, Vector2.Distance(fighter.transform.position, fighterStart));
            Assert.IsFalse(HasAnyLink(attacker), $"Attacker auto-linked at frame {i}");
            if (i >= frames / 2)
            {
                lateFrames++;
                if (attacker.IsEngaged) attackerEngagedLate++;
            }
        }

        Assert.Less(fighterDrift, 1f, "The engaged fighter is not dragged along the chain");
        Assert.GreaterOrEqual(fighterEngaged, frames * 0.95f, $"Fighter engaged {fighterEngaged} of {frames} frames");
        Assert.GreaterOrEqual(attackerEngagedLate, lateFrames * 0.9f,
            $"Attacker engaged {attackerEngagedLate} of the last {lateFrames} frames");
    }

    // SU-48 (issue 02, diagram 23): an attacker passing a free chain end, facing the same way, does not link.
    [Test]
    public void AttackingTroop_DoesNotAutoLink_UntilItsAttackEnds()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(28f, 15f));
        Simulate(0.2f);
        Assert.AreEqual(b, a.GetLineLink(TroopConstants.LeftEnd)?.Other, "A-B chain");

        // C's right line point starts on B's free left line point, facing within 10 degrees of B.
        Vector2 start = new(1f, 18f);
        Troop c = CreateTroop("C", 0, start);
        c.Pursue(enemy);
        Simulate(0.5f);
        Assert.IsFalse(HasAnyLink(c), "An attacking troop does not auto-link");
        Assert.IsNull(b.GetLineLink(TroopConstants.LeftEnd));

        // A line order ends the attack: back at the chain end it links as before.
        c.OrderLine(start, Vector2.right, c.NaturalLineLength);
        Simulate(2f);
        Assert.AreEqual(c, b.GetLineLink(TroopConstants.LeftEnd)?.Other, "Links once the attack order has ended");
    }

    // SU-47: auto-links need facings within 45 degrees and break past 60; links from a line order keep the old rules.
    [Test]
    public void AutoLink_NeedsFacingsWithin45Degrees_AndBreaksPast60_OrderedLinksDoNot()
    {
        // Each pair: A faces right; B's right line point is placed on A's left line point (0, 4.5), turned by degrees.
        Troop PairB(Vector2 offset, float degrees, out Troop a)
        {
            a = LineTroop("A " + degrees, offset, Vector2.right);
            Vector2 tangent = new(-Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));
            Vector2 centre = offset + new Vector2(0f, 4.5f) + tangent * 4.5f;
            return LineTroop("B " + degrees, centre, Direction(degrees));
        }

        Troop b30 = PairB(new Vector2(0f, 0f), 30f, out Troop a30);
        Troop b50 = PairB(new Vector2(100f, 0f), 50f, out Troop a50);
        Troop b70 = PairB(new Vector2(200f, 0f), 70f, out Troop a70);
        Simulate(0.1f);
        Assert.IsTrue(ChainLinker.LinkFromOrder(a70, TroopConstants.LeftEnd, b70, TroopConstants.RightEnd));
        Simulate(0.5f);

        Assert.AreEqual(b30, a30.GetLineLink(TroopConstants.LeftEnd)?.Other, "30 degrees apart: links");
        Assert.IsNull(a50.GetLineLink(TroopConstants.LeftEnd), "50 degrees apart: no auto-link");
        Assert.IsTrue(a70.IsLinkActive(TroopConstants.LeftEnd), "A line-ordered link ignores the facing limit");

        // Turn the 30 degree pair to 70, about B's linked line point so the hinge stays wide: the automatic link breaks.
        Vector2 tangent70 = new(-Mathf.Sin(70f * Mathf.Deg2Rad), Mathf.Cos(70f * Mathf.Deg2Rad));
        Vector2 turned = new Vector2(0f, 4.5f) + tangent70 * 4.5f;
        b30.ShiftPivot(turned - (Vector2)b30.transform.position);
        b30.OrderLine(turned, Direction(70f), b30.NaturalLineLength);
        Simulate(0.2f);
        Assert.IsNull(a30.GetLineLink(TroopConstants.LeftEnd), "An automatic link breaks past 60 degrees");
        Assert.IsTrue(a70.IsLinkActive(TroopConstants.LeftEnd), "The ordered link at 70 degrees holds");
    }

    // SU-46: a linked troop with an attack order closes at full speed; tension never holds it back.
    [Test]
    public void LinkedAttacker_IsNotHeldBackByTension()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(25f, 0f));
        Simulate(0.2f);
        Assert.IsTrue(a.IsLinkActive(TroopConstants.LeftEnd));

        a.Pursue(enemy);
        float start = a.transform.position.x;
        Simulate(2f);
        Assert.IsFalse(a.IsEngaged);
        Assert.GreaterOrEqual(a.transform.position.x - start, 0.9f * a.PivotSpeed * 2f, "Full pursuit speed");
    }

    // SU-46: pivot separation moves the free friend, not the attacker.
    [Test]
    public void FreeFriend_GivesWayToAnAttacker()
    {
        Troop attacker = CreateTroop("Attacker", 0, Vector2.zero);
        Troop idle = CreateTroop("Idle", 0, new Vector2(0.5f, 0f));
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(40f, 0f));
        attacker.Pursue(enemy);
        Assert.IsTrue(attacker.IsCommitted && !idle.IsCommitted);

        FriendlyOverlapResolver.Resolve(new List<Troop> { attacker, idle, enemy });
        Assert.AreEqual(Vector2.zero, (Vector2)attacker.transform.position, "The attacker is not pushed");
        Assert.GreaterOrEqual(Vector2.Distance(attacker.transform.position, idle.transform.position),
            FriendlyOverlapResolver.MinimumSpacing(attacker, idle) - 1e-3f, "The idle friend moves aside");
    }
}
