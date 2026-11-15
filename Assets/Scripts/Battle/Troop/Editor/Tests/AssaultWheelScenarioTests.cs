using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// SU-163: the assault wheel. A pursuer engaged with its target wheels until its front is parallel to the face it attacks,
// about the end of its front rank nearest the enemy; the face is held with hysteresis; two troops pursuing each other
// meet front to front; an engaged pursuer within the charge gap holds instead of marching into the barrier; a troop under
// a line order, and a troop that is attacked, keep their facing. Stepped in Edit mode with fixed frame time and seed and
// the project's distances (S_Distance 5, A_Distance 2, EngagementGap 1.5).
[Category("Scenario")]
public sealed class AssaultWheelScenarioTests
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
        _world.SetSeed(1631);
        _world.Initialize();
        _created.Add(_world.gameObject);
        UseTuning("_sDistance", 5f);
        UseTuning("_aDistance", 2f);
        UseTuning("_engagementGap", 1.5f);
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

    SerializedProperty Find(string property, out SerializedObject serialized)
    {
        serialized = null;
        foreach (var section in _tuning.Sections)
        {
            serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found != null) return found;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
        return null;
    }

    void UseTuning(string property, float value)
    {
        Find(property, out SerializedObject serialized).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        TroopTuning.OverrideForTests(_tuning);
    }

    void UseToggle(string property, bool value)
    {
        Find(property, out SerializedObject serialized).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        TroopTuning.OverrideForTests(_tuning);
    }

    Troop CreateTroop(string name, int team, Vector2 position, int frontage = 10, int soldiers = 12)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 10,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 40));
        _created.Add(character);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, frontage);
        return troop;
    }

    // A troop standing in a placed line whose front rank is centred on lineCenter (SU-101, SU-161).
    Troop PlacedLine(string name, int team, Vector2 lineCenter, Vector2 facing, int frontage = 10, int soldiers = 12)
    {
        Troop troop = CreateTroop(name, team, lineCenter, frontage, soldiers);
        troop.PlaceAt(lineCenter, facing, troop.NaturalLineLength, holdLine: true);
        return troop;
    }

    static Vector2 Direction(float degrees) =>
        new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

    static int SoldiersInReach(Troop attacker, Troop defender)
    {
        // The engagement view's reach (TroopEngagementView): A_Distance between soldier edges plus its 0.45 slack.
        float reach = EngagementFrontage.Reach(TroopTuning.Active.ADistance, 0.45f);
        var links = new List<EngagementFrontage.Link>();
        EngagementFrontage.Measure(attacker, defender, reach, links, out int attackerEngaged, out _);
        return attackerEngaged;
    }

    // The defender (frontage 10) stands facing -x with its front rank on x = 0. The attacker (frontage 6) meets it on a
    // 45-degree diagonal, the leading end of its front rank (End, the left wing) just off the defender's front, the rest
    // angled away: corner contact.
    (Troop attacker, Troop defender) DiagonalAssault()
    {
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left);
        Vector2 heading = Direction(-45f);
        Vector2 tangent = new(-heading.y, heading.x);
        float length = LineGeometry.NaturalLength(6);
        Vector2 leadingEnd = new(-1.75f, 1.5f);
        Troop attacker = PlacedLine("Attacker", 0, leadingEnd - tangent * (length * 0.5f), heading, 6, 8);
        attacker.Pursue(defender);
        return (attacker, defender);
    }

    // A pursuer that met a standing line corner-on wheels about its engaged end until it is parallel to the front it
    // attacks, and most of its front rank comes into reach. The defender's facing does not change.
    [Test]
    public void DiagonalPursuer_WheelsParallelToTheFaceItAttacks_AndBringsItsFrontIntoContact()
    {
        var (attacker, defender) = DiagonalAssault();
        Vector2 defenderFacing = defender.Facing;
        float tolerance = TroopTuning.Active.AssaultWheelToleranceDeg;

        float engagedAt = float.NaN;
        int before = -1;
        Vector2 leadingEndAtContact = Vector2.zero;
        for (int i = 0; i < 60 && float.IsNaN(engagedAt); i++)
        {
            _world.SimulateFrame(FrameTime);
            if (!attacker.WasEngagedWith(defender)) continue;
            engagedAt = i * FrameTime;
            before = SoldiersInReach(attacker, defender);
            leadingEndAtContact = attacker.Formation.End;
        }
        Assert.IsFalse(float.IsNaN(engagedAt), "The pursuer engages the line");
        Assert.AreEqual(TroopFace.Front, defender.FaceAttackedBy(attacker), "It attacks the front");
        float startAngle = Vector2.Angle(attacker.Facing, -defenderFacing);
        Assert.Greater(startAngle, 30f, $"Meets the line at an angle ({startAngle:F1} degrees)");

        float squaredAfter = float.NaN;
        for (int i = 0; i < Mathf.CeilToInt(6f / FrameTime) && float.IsNaN(squaredAfter); i++)
        {
            _world.SimulateFrame(FrameTime);
            if (Vector2.Angle(attacker.Facing, -defenderFacing) <= tolerance) squaredAfter = (i + 1) * FrameTime;
        }
        Assert.IsFalse(float.IsNaN(squaredAfter), "The pursuer squares up within 6 s of engaging");
        float endMoved = Vector2.Distance(attacker.Formation.End, leadingEndAtContact);
        Simulate(2f);   // the soldiers walk into the wheeled line

        int after = SoldiersInReach(attacker, defender);
        int perRank = Mathf.CeilToInt(attacker.LivingSoldierCount /
                                      (float)LineGeometry.Ranks(attacker.LivingSoldierCount, attacker.NaturalLineLength));
        TestContext.WriteLine($"Squared {squaredAfter:F2} s after engaging (from {startAngle:F1} degrees); soldiers in " +
                              $"reach {before} -> {after} of {perRank} per rank; leading end moved {endMoved:F2}");
        Assert.LessOrEqual(Vector2.Angle(attacker.Facing, -defenderFacing), tolerance, "Stays square");
        Assert.LessOrEqual(before, 3, "Corner contact to begin with");
        Assert.GreaterOrEqual(after, Mathf.CeilToInt(perRank * 0.6f), "Most of the front rank is in reach");
        Assert.Less(Vector2.Angle(defender.Facing, defenderFacing), 0.5f, "The defender's facing does not change");
        Assert.AreEqual(TroopFace.Front, defender.FaceAttackedBy(attacker), "Still the front");
    }

    // With the assault wheel off the pursuer eases about its pivot as before (SU-75): kept to compare the numbers.
    [Test]
    public void DiagonalPursuer_WithTheAssaultWheelOff_StillTurnsAsBefore()
    {
        UseToggle("_assaultWheel", false);
        var (attacker, defender) = DiagonalAssault();
        float tolerance = TroopTuning.Active.AssaultWheelToleranceDeg;
        float squaredAt = float.NaN;
        int before = -1;
        for (int i = 0; i < Mathf.CeilToInt(4f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            if (before < 0 && attacker.WasEngagedWith(defender)) before = SoldiersInReach(attacker, defender);
            if (float.IsNaN(squaredAt) && Vector2.Angle(attacker.Facing, -defender.Facing) <= tolerance)
                squaredAt = (i + 1) * FrameTime;
        }
        int after = SoldiersInReach(attacker, defender);
        TestContext.WriteLine($"Wheel off: square after {squaredAt:F2} s; soldiers in reach {before} -> {after} after 4 s");
        Assert.IsTrue(attacker.WasEngagedWith(defender));
    }

    // Two troops pursuing each other from an oblique start meet front to front and neither heading swings back and forth.
    [Test]
    public void MutualPursuers_MeetFrontToFront_WithoutOscillating()
    {
        Troop first = PlacedLine("First", 0, new Vector2(-6f, -3f), Direction(20f));
        Troop second = PlacedLine("Second", 1, new Vector2(6f, 2f), Direction(250f));
        first.Pursue(second);
        second.Pursue(first);

        float firstLast = Mathf.Atan2(first.Facing.y, first.Facing.x) * Mathf.Rad2Deg;
        float secondLast = Mathf.Atan2(second.Facing.y, second.Facing.x) * Mathf.Rad2Deg;
        int firstSign = 0, secondSign = 0, firstReversals = 0, secondReversals = 0;
        bool engaged = false;
        for (int i = 0; i < Mathf.CeilToInt(12f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            engaged |= first.WasEngagedWith(second);
            Count(first, ref firstLast, ref firstSign, ref firstReversals);
            Count(second, ref secondLast, ref secondSign, ref secondReversals);
        }

        float antiParallel = Vector2.Angle(first.Facing, -second.Facing);
        TestContext.WriteLine($"Mutual: {antiParallel:F1} degrees from front to front; heading reversals " +
                              $"{firstReversals} and {secondReversals}");
        Assert.IsTrue(engaged, "They meet");
        Assert.IsTrue(first.WasEngagedWith(second), "Still engaged");
        Assert.AreEqual(TroopFace.Front, second.FaceAttackedBy(first), "First meets Second's front");
        Assert.AreEqual(TroopFace.Front, first.FaceAttackedBy(second), "Second meets First's front");
        Assert.Less(antiParallel, TroopTuning.Active.AssaultWheelToleranceDeg * 2f + 1f, "Fronts parallel");
        Assert.LessOrEqual(firstReversals, 2, "First's heading does not oscillate");
        Assert.LessOrEqual(secondReversals, 2, "Second's heading does not oscillate");
    }

    static void Count(Troop troop, ref float last, ref int sign, ref int reversals)
    {
        float angle = Mathf.Atan2(troop.Facing.y, troop.Facing.x) * Mathf.Rad2Deg;
        float step = Mathf.DeltaAngle(last, angle);
        last = angle;
        if (Mathf.Abs(step) < 0.05f) return;
        int now = step > 0f ? 1 : -1;
        if (sign != 0 && now != sign) reversals++;
        sign = now;
    }

    // Runs a head-on pursuit into a standing line and, over 3 s from the frame it is first within the charge gap, counts
    // how often the sign of the pursuer's pivot velocity along the line to the target flipped, how many of those flips
    // came within 3 frames of the previous one (rocking), the largest march speed while holding, and on how many frames
    // within the charge gap it was not holding (closing in again after the gap opened) or marched.
    (int flips, int quickFlips, float maxMarch, int marchingInGap, Troop attacker, Troop defender) HeadOnPursuit()
    {
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left);
        Troop attacker = PlacedLine("Attacker", 0, new Vector2(-6f, 0.5f), Vector2.right);
        attacker.Pursue(defender);
        float chargeGap = TroopTuning.Active.ChargeGap;
        bool within = false;
        for (int i = 0; i < Mathf.CeilToInt(10f / FrameTime) && !within; i++)
        {
            _world.SimulateFrame(FrameTime);
            within = attacker.TryGetEngagedGap(defender, out float gap) && gap <= chargeGap;
        }
        Assert.IsTrue(within, "The pursuer closes to the charge gap");

        int flips = 0, quickFlips = 0, sign = 0, lastFlip = -100, marchingInGap = 0;
        float maxMarch = 0f;
        for (int i = 0; i < Mathf.CeilToInt(3f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            Vector2 axis = ((Vector2)defender.transform.position - (Vector2)attacker.transform.position).normalized;
            float along = Vector2.Dot(attacker.PivotVelocity, axis);
            if (attacker.IsHoldingAgainstPursuitTarget) maxMarch = Mathf.Max(maxMarch, attacker.MarchVelocity.magnitude);
            if (attacker.TryGetEngagedGap(defender, out float gap) && gap <= chargeGap
                && attacker.MarchVelocity.sqrMagnitude > 0f)
                marchingInGap++;
            if (Mathf.Abs(along) < 1e-3f) continue;
            int now = along > 0f ? 1 : -1;
            if (sign != 0 && now != sign)
            {
                flips++;
                if (i - lastFlip <= 3) quickFlips++;
                lastFlip = i;
            }
            sign = now;
        }
        return (flips, quickFlips, maxMarch, marchingInGap, attacker, defender);
    }

    // An engaged pursuer within the charge gap does not march at its target, so its pivot does not rock; it marches
    // again when the target is moved away.
    [Test]
    public void EngagedPursuer_HoldsWithinTheChargeGap_AndResumesWhenTheGapOpens()
    {
        var (flips, quickFlips, maxMarch, marchingInGap, attacker, defender) = HeadOnPursuit();
        TestContext.WriteLine($"Hold on: {flips} sign flips of the pivot velocity ({quickFlips} within 3 frames), " +
                              $"max march while holding {maxMarch:F3}, {marchingInGap} of 90 frames marching within the gap");
        Assert.AreEqual(0f, maxMarch, 1e-5f, "No march while holding");
        Assert.LessOrEqual(marchingInGap, 9, "It holds on (nearly) every frame within the charge gap");
        Assert.AreEqual(0, quickFlips, "The pivot does not rock toward and away from the target frame to frame");
        Assert.LessOrEqual(flips, 2, "At most a settle after arriving");

        defender.ShiftPivot(new Vector2(4f, 0f));
        _world.SimulateFrame(FrameTime);
        _world.SimulateFrame(FrameTime);
        Vector2 toTarget = ((Vector2)defender.transform.position - (Vector2)attacker.transform.position).normalized;
        Assert.Greater(Vector2.Dot(attacker.MarchVelocity, toTarget), 0.1f, "Marches after the target again");
        Assert.IsTrue(attacker.IsPursuing, "Still pursuing");
    }

    // The rocking seen in play: with the hold off the pursuer keeps marching into the barrier.
    [Test]
    public void EngagedPursuer_WithTheHoldOff_MarchesIntoTheBarrier()
    {
        UseToggle("_assaultHold", false);
        var (flips, quickFlips, _, marchingInGap, _, _) = HeadOnPursuit();
        TestContext.WriteLine($"Hold off: {flips} sign flips of the pivot velocity ({quickFlips} within 3 frames), " +
                              $"{marchingInGap} of 90 frames marching within the gap");
        Assert.Greater(marchingInGap, 45, "Without the hold it keeps marching within the charge gap");
    }

    // A troop under a line order that gets engaged keeps its ordered front; only the attacker wheels.
    [Test]
    public void LineOrderTroop_ThatGetsEngaged_DoesNotWheel()
    {
        Troop holder = PlacedLine("Holder", 0, Vector2.zero, Vector2.right);
        Troop attacker = PlacedLine("Attacker", 1, new Vector2(3f, 1f), Direction(200f));
        attacker.Pursue(holder);

        bool engaged = false;
        for (int i = 0; i < Mathf.CeilToInt(5f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            engaged |= holder.IsEngaged;
            Assert.IsFalse(holder.IsAssaultWheeling, "The line-order troop never wheels");
        }
        Assert.IsTrue(engaged, "The holder gets engaged");
        Assert.Less(Vector2.Angle(holder.Facing, Vector2.right), 0.5f, "It keeps its ordered front");
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // --- AssaultWheelRules ---

    [Test]
    public void KeepFace_HoldsTheFaceUntilTheBearingIsWellPastTheBoundary()
    {
        Assert.AreEqual(TroopFace.Front, AssaultWheelRules.KeepFace(TroopFace.Front, 60f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.LeftFlank, AssaultWheelRules.KeepFace(TroopFace.Front, 66f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.RightFlank, AssaultWheelRules.KeepFace(TroopFace.RightFlank, -30f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.Front, AssaultWheelRules.KeepFace(TroopFace.RightFlank, -20f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.Rear, AssaultWheelRules.KeepFace(TroopFace.Rear, 120f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.LeftFlank, AssaultWheelRules.KeepFace(TroopFace.Rear, 110f, 45f, 135f, 20f));
        Assert.AreEqual(TroopFace.RightFlank, AssaultWheelRules.KeepFace(TroopFace.LeftFlank, -60f, 45f, 135f, 20f),
            "The other flank is not held");
        Assert.AreEqual(TroopFace.LeftFlank, AssaultWheelRules.KeepFace(TroopFace.Front, 50f, 45f, 135f, 0f),
            "No hysteresis: the face at the bearing");
    }

    [Test]
    public void PivotStepKeepingEnd_KeepsTheLeadingEndOfTheFrontRankInPlace()
    {
        Vector2 pivot = new(3f, -2f);
        Vector2 from = Direction(-45f);
        Vector2 to = Direction(-30f);
        int side = AssaultWheelRules.LeadingEnd(pivot, from, 9f, 0.45f, Vector2.right);
        Assert.AreEqual(1, side, "The left wing (End) leads toward +x when facing down and right");
        Vector2 end = AssaultWheelRules.EndPoint(pivot, from, 9f, 0.45f, side);
        Vector2 moved = pivot + AssaultWheelRules.PivotStepKeepingEnd(from, to, 9f, 0.45f, side);
        Assert.Less(Vector2.Distance(end, AssaultWheelRules.EndPoint(moved, to, 9f, 0.45f, side)), 1e-4f);
        Vector2 far = AssaultWheelRules.EndPoint(pivot, from, 9f, 0.45f, -side);
        Vector2 farAfter = AssaultWheelRules.EndPoint(moved, to, 9f, 0.45f, -side);
        Assert.Greater(farAfter.x, far.x, "The far end swings toward the face");
    }
}
