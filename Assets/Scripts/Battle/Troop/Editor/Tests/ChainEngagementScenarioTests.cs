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
// Also friendly outlines (SU-94: friends do not stand in or march through each other), the charge gate (SU-95:
// soldiers charge only while their troop is within the engagement gap) and deployment placement (SU-101: a placed
// troop, its outline and its soldiers show the same formation while time stands still).
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
        TestScene.Destroy(_created);
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

    Troop CreateTroop(string name, int team, Vector2 position, int frontage = 10, int soldiers = 12)
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

    Troop LineTroop(string name, Vector2 position, Vector2 facing, int team = 0, int frontage = 10, int soldiers = 12)
    {
        Troop troop = CreateTroop(name, team, position, frontage, soldiers);
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
        Troop enemy = CreateTroop("Enemy", 1, Vector2.zero, 6, 13);
        Troop fighter = LineTroop("Fighter", new Vector2(5.2f, 0.3f), Vector2.left, frontage: 8, soldiers: 22);
        Troop archers = LineTroop("Archers", new Vector2(4f, -6.5f), new Vector2(-4f, 6.5f).normalized, frontage: 5,
            soldiers: 8);
        Simulate(0.2f);
        Assert.IsTrue(ChainLinker.LinkFromOrder(fighter, TroopConstants.LeftEnd, archers, TroopConstants.RightEnd));
        Simulate(3f);
        Assert.IsTrue(fighter.IsLinkActive(TroopConstants.LeftEnd), "Fighter and archers chained by the line order");

        Troop attacker = CreateTroop("Attacker", 0, new Vector2(-8f, -8f), 6, 6);
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

    // --- SU-94: friendly outlines keep a gap ---

    static float OutlineGap(Troop a, Troop b) => FootprintGap.Gap(a.Outline, b.Outline);

    [Test]
    public void StandingFriends_OnTopOfEachOther_MoveApart_SharingTheMove()
    {
        Troop a = CreateTroop("A", 0, Vector2.zero);
        Troop b = CreateTroop("B", 0, new Vector2(3f, 0f));
        Simulate(0.1f);
        Assert.Less(OutlineGap(a, b), 0f, "the blobs overlap to begin with");

        Simulate(6f);
        Assert.GreaterOrEqual(OutlineGap(a, b), _tuning.FriendlyGap - 0.05f, "they stand clear of each other");
        float aMoved = -a.transform.position.x;
        float bMoved = b.transform.position.x - 3f;
        Assert.Greater(aMoved, 0.5f);
        Assert.AreEqual(aMoved, bMoved, 0.3f, "equals share the move");
    }

    [Test]
    public void MarchingTroop_HasRightOfWay_OverAStandingFriend()
    {
        Troop standing = CreateTroop("Standing", 0, new Vector2(0f, 2f));
        Troop marcher = CreateTroop("Marcher", 0, new Vector2(-15f, 0f));
        Simulate(0.1f);
        Vector2 standingStart = standing.transform.position;
        marcher.MoveTo(new Vector2(15f, 0f));

        float furthest = 0f;
        for (int i = 0; i < Mathf.CeilToInt(20f / FrameTime) && marcher.IsMoving; i++)
        {
            _world.SimulateFrame(FrameTime);
            furthest = Mathf.Max(furthest, Vector2.Distance(standingStart, standing.transform.position));
        }
        Assert.IsFalse(marcher.IsMoving, "the march is never held up for good");
        Assert.Less(Vector2.Distance(marcher.transform.position, new Vector2(15f, 0f)), 2f, "and arrives");
        Assert.Greater(furthest, 0.5f, "the standing friend gives way");
    }

    // An attacking troop (committed, SU-46) is never moved by a friend's outline; the friend gives way.
    [Test]
    public void AttackingTroop_IsNotMovedByAFriend()
    {
        Troop attacker = CreateTroop("Attacker", 0, Vector2.zero);
        Troop friend = CreateTroop("Friend", 0, new Vector2(0f, 5f));
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(60f, 0f));
        Simulate(0.1f);
        attacker.Pursue(enemy);
        Assert.IsTrue(attacker.IsCommitted && !friend.IsCommitted);
        Assert.Less(OutlineGap(attacker, friend), 0f, "the friend stands in the attacker's way");

        Vector2 attackerBefore = attacker.transform.position;
        Vector2 friendBefore = friend.transform.position;
        FriendlyOverlapResolver.SeparateOutlines(new List<Troop> { attacker, friend, enemy }, 0.5f);
        Assert.AreEqual(attackerBefore, (Vector2)attacker.transform.position, "the attacker is not moved");
        Assert.Greater(friend.transform.position.y - friendBefore.y, 0.1f, "the friend gives way, away from it");
        Assert.LessOrEqual(Vector2.Distance(friendBefore, friend.transform.position),
            friend.PivotSpeed * _tuning.FriendlyGiveWaySpeedScale * 0.5f + 1e-4f, "at most at its give-way speed");
    }

    [Test]
    public void LinkedFriends_AreNotPushedApart()
    {
        Troop a = LineTroop("A", new Vector2(0f, 0f), Vector2.right);
        Troop b = LineTroop("B", new Vector2(0f, 9f), Vector2.right);
        Simulate(0.5f);
        Assert.IsTrue(a.IsLinkActive(TroopConstants.LeftEnd), "linked into a chain");
        Assert.IsTrue(FriendlyOverlapResolver.AreLinked(a, b));
        Vector2 aBefore = a.transform.position, bBefore = b.transform.position;
        Simulate(2f);
        Assert.IsTrue(a.IsLinkActive(TroopConstants.LeftEnd), "still linked");
        Assert.Less(Vector2.Distance(aBefore, a.transform.position), 0.1f);
        Assert.Less(Vector2.Distance(bBefore, b.transform.position), 0.1f);
    }

    [Test]
    public void OverlappingFriends_DoNotMove_WhileTimeStandsStill()
    {
        Troop a = CreateTroop("A", 0, Vector2.zero);
        Troop b = CreateTroop("B", 0, new Vector2(4f, 0f));
        for (int i = 0; i < 10; i++) _world.SimulateFrame(0f);
        Assert.Less(OutlineGap(a, b), 0f);
        Assert.AreEqual(Vector2.zero, (Vector2)a.transform.position, "paused or deploying: nothing gives way yet");
        Assert.AreEqual(new Vector2(4f, 0f), (Vector2)b.transform.position);
    }

    // --- SU-95: charges need the troop within the engagement gap ---

    [Test]
    public void Charges_StartOnlyWithinTheEngagementGap()
    {
        // Code defaults: engaged at A_Distance 5.2, charges within EngagementGap 2 (+ 0.25). The lines form apart, then
        // one closes to about 4 (engaged, beyond the engagement gap) and holds there.
        Troop far = LineTroop("Far", Vector2.zero, Vector2.right);
        Troop farEnemy = LineTroop("Far Enemy", new Vector2(20f, 0f), Vector2.left, team: 1);
        Simulate(2f);
        farEnemy.OrderLine(new Vector2(5.5f, 0f), Vector2.left, farEnemy.NaturalLineLength);
        Simulate(6f);
        float gap = FootprintGap.Gap(far.Footprint, farEnemy.Footprint);
        Assert.IsTrue(far.IsEngaged, $"engaged at a gap of {gap:0.00}");
        Assert.Greater(gap, _tuning.ChargeGap, "but beyond the engagement gap");
        Assert.IsFalse(ChargeCoordinator.CanStartCharges(far));
        far.ResetMeleeMetrics();
        farEnemy.ResetMeleeMetrics();
        Simulate(5f);
        Assert.AreEqual(0, far.MeleeMetrics.Snapshot(far.LivingSoldierCount).Charges
            + farEnemy.MeleeMetrics.Snapshot(farEnemy.LivingSoldierCount).Charges, "no charges outside the engagement gap");

        Troop near = LineTroop("Near", new Vector2(0f, 40f), Vector2.right);
        Troop nearEnemy = LineTroop("Near Enemy", new Vector2(3f, 40f), Vector2.left, team: 1);
        Simulate(2f);
        Assert.IsTrue(ChargeCoordinator.CanStartCharges(near), "within the engagement gap");
        near.ResetMeleeMetrics();
        Simulate(5f);
        Assert.Greater(near.MeleeMetrics.Snapshot(near.LivingSoldierCount).Charges, 0, "soldiers charge");
    }

    [Test]
    public void ChargeReach_IsADistanceBetweenSoldierEdges()
    {
        Assert.AreEqual(_tuning.ADistance + 2f * TroopConstants.SoldierRadius, _tuning.ChargeRange, 1e-5f);
        Assert.AreEqual(_tuning.EngagementGap + _tuning.ChargeGapTolerance, _tuning.ChargeGap, 1e-5f);
    }

    // --- SU-101: deployment placement ---

    static void StandStill(TroopCombatWorld world, int frames = 3)
    {
        for (int i = 0; i < frames; i++) world.SimulateFrame(0f);
    }

    [Test]
    public void Placement_IdleShowsTheBlob_ALineShowsTheLine_WhileTimeStandsStill()
    {
        Troop troop = CreateTroop("Placed", 0, Vector2.zero);
        troop.PlaceAt(Vector2.zero, Vector2.right, troop.NaturalLineLength, holdLine: false);
        StandStill(_world);
        Assert.AreEqual(FormationMode.Idle, troop.FormationMode, "a right-click placement stands Idle");
        Assert.IsFalse(troop.HasManualLineOrder);
        Assert.AreEqual(FootprintKind.Circle, troop.Outline.Kind, "and its outline is the blob");

        Vector2 spot = new(10f, 5f);
        troop.PlaceAt(spot, Vector2.up, troop.NaturalLineLength, holdLine: true);
        StandStill(_world);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "a line placement holds the line");
        TroopFootprint line = troop.Outline;
        Assert.AreEqual(FootprintKind.Rect, line.Kind);
        Assert.Less(Vector2.Distance(spot, line.Center), 0.01f, "the outline is on the troop");
        Assert.Less(Vector2.Angle(Vector2.up, line.Facing), 0.5f, "facing the placed way");
        foreach (var soldier in troop.Soldiers)
            Assert.LessOrEqual(line.SignedDistance(troop.WorldPosition(soldier)), 1e-3f, "the soldiers stand in the line");

        troop.PlaceAt(Vector2.zero, Vector2.right, troop.NaturalLineLength, holdLine: false);
        StandStill(_world);
        Assert.AreEqual(FormationMode.Idle, troop.FormationMode);
        Assert.AreEqual(FootprintKind.Circle, troop.Outline.Kind);
        float blob = troop.Radius * _tuning.BlobRadiusScale + 1e-3f;
        foreach (var soldier in troop.Soldiers)
            Assert.LessOrEqual(Vector2.Distance(troop.WorldPosition(soldier), Vector2.zero), blob, "back in the blob");
    }

    // Two troops side by side in a line, linked at the joint where A's right end meets B's left end.
    void LinkedPair(out Troop a, out Troop b)
    {
        a = LineTroop("A", Vector2.zero, Vector2.up);
        float length = a.NaturalLineLength;
        b = LineTroop("B", new Vector2(length, 0f), Vector2.up);
        Simulate(0.2f);
        Assert.IsTrue(ChainLinker.LinkFromOrder(a, TroopConstants.RightEnd, b, TroopConstants.LeftEnd));
        Simulate(1f);
        Assert.IsTrue(b.IsLinkActive(TroopConstants.LeftEnd), "the pair is chained");
    }

    // SU-130 (Q-22 option 2): a detached troop that stays inside a neighbour's attach radius stays unlinked; suppression
    // ends only after it has been outside every attach radius for ChainSuppressCooldownSeconds.
    [Test]
    public void ChainSuppressed_EndsOnlyAfterLeavingTheAttachRadiusForThreeSeconds()
    {
        LinkedPair(out Troop a, out Troop b);
        ChainLinker.BreakLink(b, TroopConstants.LeftEnd, ChainBreakReason.PlayerOrder);
        b.SetChainSuppressed(true);
        Simulate(5f);
        Assert.IsTrue(b.ChainSuppressed, "still suppressed while it stands inside A's attach radius");
        Assert.IsFalse(b.IsLinkActive(TroopConstants.LeftEnd), "and it has not relinked");

        b.MoveTo(new Vector2(b.transform.position.x + 40f, -30f));
        Simulate(12f);
        Assert.IsFalse(b.ChainSuppressed, "suppression ends once it has been outside every attach radius for 3 s");
    }

    // SU-129: a single click selects just the troop; a double click selects its whole chain.
    [Test]
    public void ClickSelectsTheTroop_DoubleClickSelectsItsChain()
    {
        LinkedPair(out Troop a, out Troop b);
        Troop c = LineTroop("C", new Vector2(2f * a.NaturalLineLength, 0f), Vector2.up);
        Simulate(0.2f);
        Assert.IsTrue(ChainLinker.LinkFromOrder(b, TroopConstants.RightEnd, c, TroopConstants.LeftEnd));
        Simulate(1f);
        Troop loner = CreateTroop("Loner", 0, new Vector2(0f, -30f));

        CollectionAssert.AreEquivalent(new[] { a, b, c }, TroopSelection.ChainMembers(a));
        CollectionAssert.AreEquivalent(new[] { loner }, TroopSelection.ChainMembers(loner));

        var selection = new TroopSelection { PlayerTeamId = 0 };
        selection.SelectTroop(b, wholeChain: false, toggle: false);
        CollectionAssert.AreEquivalent(new[] { b }, selection.Selected, "a single click selects only that troop");
        selection.SelectTroop(c, wholeChain: true, toggle: false);
        CollectionAssert.AreEquivalent(new[] { a, b, c }, selection.Selected, "a double click selects the whole chain");
        selection.SelectTroop(loner, wholeChain: false, toggle: true);
        Assert.AreEqual(4, selection.Selected.Count, "Shift adds to the selection");
    }
}
