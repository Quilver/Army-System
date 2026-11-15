using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The troop border (TroopBorder, SU-162): one rounded box for every formation, fitted to the troop's soldiers with
// OutlinePadding, quick to grow and slow to shrink, and the shape picking tests (Contains) agrees with the drawn polygon.
public sealed class TroopBorderTests
{
    const float Frame = 1f / 60f;
    const float Padding = 0.65f;

    static List<Vector2> LineSoldiers(float halfLength, float depth)
    {
        var soldiers = new List<Vector2>();
        for (float x = -halfLength; x <= halfLength + 1e-4f; x += TroopConstants.LineSpacing)
        for (float y = 0f; y >= -depth - 1e-4f; y -= TroopConstants.LineSpacing)
            soldiers.Add(new Vector2(x, y));
        return soldiers;
    }

    static TroopFootprint Run(TroopBorder border, TroopFootprint formation, List<Vector2> soldiers, float seconds)
    {
        TroopFootprint result = border.At(Vector2.zero);
        for (float t = 0f; t < seconds; t += Frame) result = border.Step(formation, Vector2.zero, soldiers, Padding, Frame);
        return result;
    }

    static float Top(in TroopFootprint footprint) => footprint.ClosestPoint(footprint.Center + Vector2.up * 1000f).y;

    [Test]
    public void SettledLine_LeavesThePaddingBeyondEverySoldier()
    {
        List<Vector2> soldiers = LineSoldiers(4.5f, 1.8f);
        TroopFootprint line = TroopBorder.Line(new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f), Vector2.up, soldiers.Count, Padding);
        var border = new TroopBorder();
        TroopFootprint result = Run(border, line, soldiers, 2f);
        foreach (Vector2 soldier in soldiers)
            Assert.LessOrEqual(result.SignedDistance(soldier), -Padding + 1e-3f, $"soldier at {soldier}");
        Assert.Greater(result.Rounding, 0f, "rounded corners");
    }

    [Test]
    public void NoTimeStep_Snaps()
    {
        List<Vector2> soldiers = LineSoldiers(4.5f, 0.9f);
        var border = new TroopBorder();
        border.Step(TroopBorder.Blob(Vector2.zero, 3f, Vector2.up, Padding), Vector2.zero, soldiers, Padding, Frame);
        Vector2 placed = new(20f, 10f);
        var moved = new List<Vector2>(soldiers);
        TroopFootprint target = TroopBorder.Line(placed + new Vector2(-4.5f, 0f), placed + new Vector2(4.5f, 0f), Vector2.up,
            soldiers.Count, Padding);
        TroopFootprint snapped = border.Step(target, placed, moved, Padding, 0f);
        var fresh = new TroopBorder();
        TroopFootprint expected = fresh.Step(target, placed, moved, Padding, Frame);
        Assert.AreEqual(expected.Center.x, snapped.Center.x, 1e-4f);
        Assert.AreEqual(expected.Center.y, snapped.Center.y, 1e-4f);
        Assert.AreEqual(expected.OuterHalfLength, snapped.OuterHalfLength, 1e-4f);
        Assert.AreEqual(expected.OuterHalfWidth, snapped.OuterHalfWidth, 1e-4f);
    }

    [Test]
    public void GrowsQuickly_ShrinksSlowly()
    {
        var soldiers = new List<Vector2> { Vector2.zero };
        TroopFootprint small = TroopBorder.Blob(Vector2.zero, 1f, Vector2.up, Padding);
        TroopFootprint large = TroopBorder.Blob(Vector2.zero, 5f, Vector2.up, Padding);
        // A lone soldier: the border reaches the formation only within FormationReach, so give it soldiers at the rim.
        var rim = new List<Vector2> { new(0f, 5f), new(0f, -5f), new(5f, 0f), new(-5f, 0f) };

        var border = new TroopBorder();
        float from = Top(Run(border, small, soldiers, 1f));
        float to = Top(new TroopBorder().Step(large, Vector2.zero, rim, Padding, Frame));
        float grown = Top(Run(border, large, rim, 0.3f));
        Assert.Greater((grown - from) / (to - from), 0.9f, "most of the way out in 0.3 s");

        float shrunk = Top(Run(border, small, soldiers, 0.3f));
        float closed = (to - shrunk) / (to - from);
        Assert.Less(closed, 0.6f, "less than 60% of the way in after 0.3 s");
        Assert.Greater(closed, 0.2f, "but on its way");
    }

    [Test]
    public void ColumnTarget_IsNotShownBeforeItsSoldiersGetThere()
    {
        // Soldiers still in a line across the march; the column trails far behind the pivot.
        List<Vector2> soldiers = LineSoldiers(4.5f, 0f);
        TroopFootprint column = TroopBorder.Column(Vector2.zero, Vector2.up, 40f, 10, soldiers.Count, Padding);
        float columnRear = column.Center.y - column.OuterHalfWidth;
        Assert.Less(columnRear, -4f, "the column reaches well behind the line");
        TroopFootprint result = Run(new TroopBorder(), column, soldiers, 2f);
        float rear = result.ClosestPoint(result.Center + Vector2.down * 1000f).y;
        Assert.Greater(rear, -(Padding + TroopBorder.FormationReach) - 1e-3f, "the border stays with the soldiers");
    }

    [Test]
    public void Contains_AgreesWithTheDrawnPolygon()
    {
        var points = new List<Vector2>();
        var shapes = new[]
        {
            new TroopFootprint(FootprintKind.Rect, new Vector2(1f, 2f), new Vector2(1f, 1f), 4f, 1.2f, 0.65f),
            new TroopFootprint(FootprintKind.Capsule, new Vector2(-3f, 0f), new Vector2(0.3f, -1f), 0f, 3f, 1.5f),
            new TroopFootprint(FootprintKind.Circle, Vector2.zero, Vector2.right, 0f, 0f, 3.5f),
            new TroopFootprint(FootprintKind.Rect, new Vector2(2f, -1f), new Vector2(-1f, 0.2f), 2.5f, 0.3f, 1.7f),
        };
        var random = new System.Random(7);
        foreach (TroopFootprint shape in shapes)
        {
            TroopOutlinePolygon.Build(shape, points, 16);
            float reach = shape.OuterHalfLength + shape.OuterHalfWidth + 1f;
            for (int i = 0; i < 2000; i++)
            {
                Vector2 point = shape.Center + new Vector2((float)(random.NextDouble() * 2 - 1) * reach,
                    (float)(random.NextDouble() * 2 - 1) * reach);
                if (Mathf.Abs(shape.SignedDistance(point)) < 0.02f) continue;
                Assert.AreEqual(InsidePolygon(points, point), shape.Contains(point), $"{shape.Kind} at {point}");
            }
        }
    }

    static bool InsidePolygon(List<Vector2> polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }
}

// The border of a troop in the simulation through formation changes (SU-162). Stepped in Edit mode like
// ColumnMarchScenarioTests.
[Category("Scenario")]
public sealed class TroopBorderScenarioTests
{
    const float FrameTime = 1f / 30f;
    // The most the border's bounds may move in one frame (pivot motion included).
    const float MaxFrameStep = 0.35f;

    readonly List<Object> _created = new();
    readonly List<Vector2> _polygon = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;
    float _worstStep;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(1357);
        _world.Initialize();
        _created.Add(_world.gameObject);
        _worstStep = 0f;
    }

    [TearDown]
    public void TearDown()
    {
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop CreateTroop(int soldiers)
    {
        var melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(melee);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 5, CombatStatLimits.Default,
            new CharacterEquipment(melee, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15));
        _created.Add(profile);
        var root = new GameObject("Bordered");
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(0, new List<CharacterProfile> { profile }, TroopFrontageRule.ForSoldierCount(soldiers));
        return troop;
    }

    Rect Bounds(Troop troop)
    {
        TroopOutlinePolygon.Build(troop.Border, _polygon);
        Vector2 min = _polygon[0], max = _polygon[0];
        foreach (Vector2 point in _polygon)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Steps the world, checking every frame that the border holds every soldier it should and moves smoothly.
    void Simulate(Troop troop, float seconds, string phase)
    {
        Rect previous = Bounds(troop);
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++)
        {
            _world.SimulateFrame(FrameTime);
            TroopFootprint border = troop.Border;
            foreach (var soldier in troop.Soldiers)
            {
                if (!TroopBorder.Holds(soldier)) continue;
                Vector2 position = troop.WorldPosition(soldier);
                Assert.IsTrue(border.Contains(position), $"{phase}, frame {i}: soldier {soldier.Id} at {position} is outside");
            }
            Rect bounds = Bounds(troop);
            float step = Mathf.Max(Mathf.Max(Mathf.Abs(bounds.xMin - previous.xMin), Mathf.Abs(bounds.xMax - previous.xMax)),
                Mathf.Max(Mathf.Abs(bounds.yMin - previous.yMin), Mathf.Abs(bounds.yMax - previous.yMax)));
            _worstStep = Mathf.Max(_worstStep, step);
            Assert.LessOrEqual(step, MaxFrameStep, $"{phase}, frame {i}: the border jumped {step:F3}");
            previous = bounds;
        }
    }

    void AssertPadded(Troop troop, string phase)
    {
        TroopFootprint border = troop.Border;
        float padding = TroopTuning.Active.OutlinePadding;
        foreach (var soldier in troop.Soldiers)
        {
            if (!TroopBorder.Holds(soldier)) continue;
            float distance = border.SignedDistance(troop.WorldPosition(soldier));
            Assert.LessOrEqual(distance, -padding + 0.1f, $"{phase}: soldier {soldier.Id} is only {-distance:F3} inside");
        }
    }

    [Test]
    public void FormationChanges_HoldTheSoldiers_WithoutJumps()
    {
        Troop troop = CreateTroop(20);
        _world.SimulateFrame(FrameTime); // the first frame seeds the border
        Simulate(troop, 0.5f, "idle");

        // Idle -> Line: a line facing up on the spot.
        Vector2 pivot = troop.transform.position;
        float length = LineGeometry.NaturalLength(troop.Frontage);
        troop.OrderLine(pivot + Vector2.up * troop.FrontRankOffset(length), Vector2.up, length);
        Simulate(troop, 5f, "idle to line");
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "formed a line");
        AssertPadded(troop, "settled line");

        // Line -> Column -> Idle: a march to the right, and a stop.
        troop.MoveTo((Vector2)troop.transform.position + new Vector2(12f, 0f));
        Simulate(troop, 2f, "line to column");
        Assert.AreEqual(FormationMode.Column, troop.FormationMode, "marching in column");
        Simulate(troop, 8f, "column to idle");
        Assert.AreEqual(FormationMode.Idle, troop.FormationMode, "stopped in the blob");
        AssertPadded(troop, "settled blob");

        // Idle -> Line again, facing left.
        pivot = troop.transform.position;
        troop.OrderLine(pivot + Vector2.left * troop.FrontRankOffset(length), Vector2.left, length);
        Simulate(troop, 5f, "idle to line facing left");
        Assert.AreEqual(FormationMode.Line, troop.FormationMode);
        AssertPadded(troop, "settled second line");
        Assert.Less(_worstStep, MaxFrameStep);
    }

    [Test]
    public void ChargingSoldier_DoesNotChangeTheBorder()
    {
        Troop troop = CreateTroop(20);
        _world.SimulateFrame(FrameTime); // the first frame seeds the border
        Simulate(troop, 0.5f, "idle");
        Vector2 pivot = troop.transform.position;
        float length = LineGeometry.NaturalLength(troop.Frontage);
        troop.OrderLine(pivot + Vector2.up * troop.FrontRankOffset(length), Vector2.up, length);
        Simulate(troop, 5f, "idle to line");

        for (int i = 0; i < 30; i++) troop.UpdateOutline(FrameTime);
        TroopFootprint before = troop.Border;

        SoldierAgentState charger = troop.Soldiers[0];
        charger.BeginCharge(troop.Soldiers[1], null, 3f, 0f);
        Assert.IsTrue(charger.IsCharging);
        charger.Position = troop.transform.InverseTransformPoint((Vector2)troop.transform.position + Vector2.up * 8f);
        for (int i = 0; i < 30; i++) troop.UpdateOutline(FrameTime);
        TroopFootprint after = troop.Border;

        Assert.AreEqual(before.Center.x, after.Center.x, 0.02f);
        Assert.AreEqual(before.Center.y, after.Center.y, 0.02f);
        Assert.AreEqual(before.OuterHalfLength, after.OuterHalfLength, 0.02f);
        Assert.AreEqual(before.OuterHalfWidth, after.OuterHalfWidth, 0.02f);
        Assert.IsFalse(after.Contains(troop.WorldPosition(charger)), "the charger is outside the border");
    }
}
