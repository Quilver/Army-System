using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Flush fronts (SU-161): the battle line is a troop's front rank and its ranks stand behind it, so troops of different
// depths placed on one line order stand with their front ranks on the ordered line. The pivot stays in the middle of the
// ranks, so a lone troop forming or dropping a line does not lurch. Stepped in Edit mode with fixed frame time and seed.
[Category("Scenario")]
public sealed class FlushFrontTests
{
    const float FrameTime = 1f / 30f;
    const int Frontage = 8;
    const float FrontTolerance = 0.1f;

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

    Troop CreateTroop(string name, int team, Vector2 position, int soldiers)
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
        troop.Configure(team, new List<CharacterProfile> { character }, Frontage);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // The troop's front rank: its SoldiersPerRank living soldiers furthest along front.
    static List<Vector2> FrontRank(Troop troop, Vector2 front)
    {
        var living = new List<Vector2>();
        foreach (var soldier in troop.Soldiers)
            if (soldier.IsAlive) living.Add(troop.WorldPosition(soldier));
        living.Sort((a, b) => Vector2.Dot(b, front).CompareTo(Vector2.Dot(a, front)));
        int perRank = LineGeometry.SoldiersPerRank(troop.Formation.Length);
        return living.GetRange(0, Mathf.Min(perRank, living.Count));
    }

    static float MeanAcross(List<Vector2> points, Vector2 linePoint, Vector2 front)
    {
        float sum = 0f;
        foreach (var point in points) sum += Vector2.Dot(point - linePoint, front);
        return sum / Mathf.Max(1, points.Count);
    }

    static Vector2 MeanLocal(Troop troop)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            sum += soldier.Position;
            count++;
        }
        return sum / Mathf.Max(1, count);
    }

    // One line order for three troops of 1, 2 and 4 ranks (8, 16 and 32 soldiers, frontage 8, at their natural length):
    // once settled, every troop's front rank stands on the ordered line, the links are active and the joints coincide;
    // the footprint's and the outline's front edges sit just ahead of the front rank.
    [Test]
    public void LineOrder_OfOneTwoAndFourRanks_StandsFlushOnTheOrderedLine()
    {
        int[] soldiers = { 8, 16, 32 };
        var troops = new List<Troop>();
        for (int i = 0; i < soldiers.Length; i++)
            troops.Add(CreateTroop("Ranks " + (1 << i), 0, new Vector2(i * 9f, -12f), soldiers[i]));

        float natural = 0f;
        foreach (var troop in troops) natural += troop.NaturalLineLength;
        Vector2 press = new(0f, 0f);
        Vector2 release = press + Vector2.right * natural;
        var planner = new TroopOrderPlanner(new TroopSelection { PlayerTeamId = 0 });
        planner.PlanLine(new List<Troop>(troops), press, release);
        var placements = planner.Placements;
        Assert.AreEqual(3, placements.Count);
        foreach (var placement in placements)
            placement.Troop.OrderLine(placement.Center, placement.Facing, placement.Length);
        for (int i = 0; i + 1 < placements.Count; i++)
        {
            TroopOrderPlanner.AdjacentEnds(placements[i], planner.LineDirection, out int endA, out _);
            TroopOrderPlanner.AdjacentEnds(placements[i + 1], planner.LineDirection, out _, out int endB);
            Assert.IsTrue(ChainLinker.LinkFromOrder(placements[i].Troop, endA, placements[i + 1].Troop, endB));
        }

        Simulate(25f);

        Vector2 front = Vector2.up;
        for (int i = 0; i < troops.Count; i++)
        {
            Troop troop = troops[i];
            Assert.IsFalse(troop.IsMoving, $"{troop.name} arrived");
            Assert.AreEqual(1 << i, LineGeometry.Ranks(troop.LivingSoldierCount, troop.Formation.Length), $"{troop.name} ranks");
            Assert.AreEqual(0f, Vector2.Distance(front, troop.Formation.Front), 1e-3f, $"{troop.name} faces up");
            Assert.AreEqual(press.y, troop.Formation.Start.y, 1e-3f, $"{troop.name} line on the ordered line");
            Assert.AreEqual(press.y, troop.Formation.End.y, 1e-3f, $"{troop.name} line on the ordered line");

            List<Vector2> rank = FrontRank(troop, front);
            float mean = MeanAcross(rank, press, front);
            TestContext.WriteLine($"{troop.name}: front rank mean {mean:F3}");
            Assert.AreEqual(0f, mean, FrontTolerance, $"{troop.name} front rank on the ordered line");

            // Footprint front edge: the front rank's furthest soldier plus SoldierRadius; ranks behind the line.
            TroopFootprint footprint = troop.Footprint;
            float footprintFront = Vector2.Dot(footprint.Center - press, front) + footprint.HalfWidth;
            float furthest = -float.MaxValue;
            foreach (var point in rank) furthest = Mathf.Max(furthest, Vector2.Dot(point - press, front));
            Assert.AreEqual(furthest + TroopConstants.SoldierRadius, footprintFront, 1e-3f, $"{troop.name} footprint front");
            Assert.Greater(footprint.RearHalfWidth, footprint.FrontHalfWidth, $"{troop.name} footprint lies behind its line");

            // Outline front edge: the front rank's band (RankThickness) plus one soldier radius of padding.
            TroopFootprint outline = TroopOutline.Line(troop.Formation.Start, troop.Formation.End, troop.Formation.Facing,
                troop.LivingSoldierCount);
            float outlineFront = Vector2.Dot(outline.Center - press, front) + outline.HalfWidth;
            Assert.AreEqual(mean + LineGeometry.RankThickness + TroopConstants.SoldierRadius, outlineFront, FrontTolerance,
                $"{troop.name} outline front");
            float outlineRear = Vector2.Dot(outline.Center - press, front) - outline.HalfWidth;
            float rearmost = float.MaxValue;
            foreach (var soldier in troop.Soldiers)
                if (soldier.IsAlive) rearmost = Mathf.Min(rearmost, Vector2.Dot(troop.WorldPosition(soldier) - press, front));
            Assert.LessOrEqual(outlineRear, rearmost - TroopConstants.SoldierRadius + FrontTolerance,
                $"{troop.name} outline holds the rear rank");
        }

        for (int i = 0; i + 1 < troops.Count; i++)
        {
            TroopOrderPlanner.AdjacentEnds(placements[i], planner.LineDirection, out int endA, out _);
            TroopOrderPlanner.AdjacentEnds(placements[i + 1], planner.LineDirection, out _, out int endB);
            Assert.IsTrue(troops[i].IsLinkActive(endA), $"link {i} active");
            Assert.IsTrue(troops[i + 1].IsLinkActive(endB), $"link {i} active");
            Assert.AreEqual(0f, Vector2.Distance(troops[i].SegmentEndpoint(endA), troops[i + 1].SegmentEndpoint(endB)), 1e-4f,
                $"joint {i} coincides");
        }
    }

    // A lone four-rank troop that forms a line because an enemy comes within S_Distance, and drops it again, keeps its
    // soldiers where they were: their mean place around the pivot barely moves (no half-depth lurch).
    [Test]
    public void LoneTroop_GainingAndLosingAThreat_DoesNotLurch()
    {
        const float LurchTolerance = 0.3f;
        Troop troop = CreateTroop("Lone", 0, Vector2.zero, 32);
        Assert.Greater(LineGeometry.FrontRankOffset(troop.NaturalLineLength, troop.LivingSoldierCount), 1f,
            "a half-depth lurch would be over a unit");
        Simulate(4f);
        Assert.IsNull(troop.Formation, "idle blob");
        Vector2 blob = MeanLocal(troop);

        float sDistance = TroopTuning.Active.SDistance;
        Troop enemy = CreateTroop("Enemy", 1, new Vector2(sDistance * 0.7f, 0f), 8);
        Simulate(6f);
        Assert.IsNotNull(troop.Formation, "threat line");
        Assert.IsFalse(troop.IsEngaged);
        Vector2 line = MeanLocal(troop);
        TestContext.WriteLine($"blob {blob} line {line}");
        Assert.Less(Vector2.Distance(blob, line), LurchTolerance, "forming the line");
        // The line's middle of ranks is the pivot; the line itself (front rank) is half the depth ahead.
        Vector2 pivot = troop.transform.position;
        Vector2 lineMiddle = (troop.Formation.Start + troop.Formation.End) * 0.5f;
        Assert.AreEqual(LineGeometry.FrontRankOffset(troop.Formation.Length, troop.LivingSoldierCount),
            Vector2.Dot(lineMiddle - pivot, troop.Formation.Front), 1e-3f);

        enemy.transform.position = new Vector3(sDistance * 20f, 0f, 0f);
        Simulate(6f);
        Assert.IsNull(troop.Formation, "line dropped");
        Vector2 after = MeanLocal(troop);
        TestContext.WriteLine($"after {after}");
        Assert.Less(Vector2.Distance(line, after), LurchTolerance, "dropping the line");
    }

    // Every rank of a held line stays inside the soldier radius clamp (SoldierMotionConstraints).
    [Test]
    public void FourRankLine_EveryRankInsideTheRadiusClamp()
    {
        Troop troop = CreateTroop("Deep", 0, Vector2.zero, 32);
        troop.PlaceAt(Vector2.zero, Vector2.right, troop.NaturalLineLength, true);
        Assert.AreEqual(-LineGeometry.FrontRankOffset(troop.NaturalLineLength, 32), troop.transform.position.x, 1e-4f,
            "placed with its front rank on the line, pivot in the middle of the ranks");
        Simulate(5f);
        float depth = LineGeometry.RankDepth(troop.Formation.Length, troop.LivingSoldierCount);
        float rearmost = float.MaxValue;
        float foremost = -float.MaxValue;
        foreach (var soldier in troop.Soldiers)
        {
            float x = troop.WorldPosition(soldier).x;
            rearmost = Mathf.Min(rearmost, x);
            foremost = Mathf.Max(foremost, x);
        }
        Assert.AreEqual(0f, foremost, 0.2f, "front rank on the placed line");
        Assert.AreEqual(-depth, rearmost, 0.2f, "rear rank a rank depth behind it");
    }
}
