using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Hover order paths (TroopOrderPathView data), the engagement view (EngagementFrontage, EngagementHeatStrip,
// TroopEngagementView) and the Battle Tuning assets. Stepped in Edit mode like TroopScenarioTests.
[Category("Scenario")]
public sealed class BattleViewTests
{
    const float FrameTime = 1f / 30f;

    readonly List<Object> _created = new();
    readonly List<Vector2> _points = new();
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
        // Soldier sprites are root objects removed by Troop.OnDestroy, which does not run in Edit mode. Remove them here
        // so this fixture leaves the scene as it found it: other scenario tests seed their random streams from the
        // troop's place in the hierarchy (Troop.StreamId), so leftover objects would change their results.
        foreach (var view in Object.FindObjectsByType<SoldierView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (view != null) Object.DestroyImmediate(view.gameObject);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    CharacterProfile Character()
    {
        var melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0,
            WeaponSpecialRule.None, false);
        _created.Add(melee);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 12, 5, CombatStatLimits.Default,
            new CharacterEquipment(melee, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15, null));
        _created.Add(profile);
        return profile;
    }

    Troop CreateTroop(string name, int team, Vector2 position)
    {
        var profile = TroopProfile.CreateRuntimeProfile(new List<CharacterProfile> { Character() }, 5f);
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, profile);
        return troop;
    }

    Troop LineTroop(string name, int team, Vector2 position, Vector2 facing)
    {
        Troop troop = CreateTroop(name, team, position);
        troop.OrderLine(position, facing, troop.NaturalLineLength);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // Two lines facing each other close enough to engage at once.
    (Troop first, Troop second) EngagedLines()
    {
        Troop a = LineTroop("A", 0, Vector2.zero, Vector2.up);
        Troop b = LineTroop("B", 1, new Vector2(0f, 4.5f), Vector2.down);
        Simulate(1f);
        Assert.IsTrue(a.IsEngaged && b.IsEngaged, "Both lines engaged");
        return (a, b);
    }

    // --- Order paths ---

    [Test]
    public void IdleTroop_HasNoOrderPath()
    {
        Troop troop = CreateTroop("Idle", 0, Vector2.zero);
        Assert.AreEqual(TroopOrderKind.None, troop.CollectOrderPath(_points));
        Assert.AreEqual(0, _points.Count);
    }

    [Test]
    public void MoveOrder_PathRunsFromPivotToDestination()
    {
        Troop troop = CreateTroop("Mover", 0, Vector2.zero);
        troop.MoveTo(new Vector2(20f, 0f));
        Simulate(0.2f);
        Assert.AreEqual(TroopOrderKind.Move, troop.CollectOrderPath(_points));
        Assert.GreaterOrEqual(_points.Count, 2);
        Assert.AreEqual((Vector2)troop.transform.position, _points[0], "Starts at the pivot");
        Assert.Less(Vector2.Distance(new Vector2(20f, 0f), _points[_points.Count - 1]), 0.01f, "Ends at the waypoint");
    }

    [Test]
    public void AttackOrder_PathEndsAtTarget()
    {
        Troop attacker = CreateTroop("Attacker", 0, Vector2.zero);
        Troop target = CreateTroop("Target", 1, new Vector2(40f, 10f));
        attacker.Pursue(target);
        Assert.AreEqual(TroopOrderKind.Attack, attacker.CollectOrderPath(_points));
        Assert.Less(Vector2.Distance(target.transform.position, _points[_points.Count - 1]), 0.01f);
    }

    [Test]
    public void LineOrder_ShowsWhileMarching_ThenNothingOnceHeld()
    {
        Troop troop = CreateTroop("Liner", 0, Vector2.zero);
        troop.OrderLine(new Vector2(4f, 0f), Vector2.up, troop.NaturalLineLength);
        Assert.AreEqual(TroopOrderKind.Line, troop.CurrentOrder);
        Assert.IsTrue(troop.TryGetLineOrder(out Vector2 centre, out _, out _));
        Assert.AreEqual(new Vector2(4f, 0f), centre);
        Simulate(5f);
        Assert.AreEqual(TroopOrderKind.None, troop.CurrentOrder, "Holding the line is not an order to show");
    }

    // --- Engagement frontage ---

    [Test]
    public void EngagedLines_AreOnePair_WithLinksWithinReach()
    {
        var (a, b) = EngagedLines();
        var pairs = new List<(Troop, Troop)>();
        EngagementFrontage.CollectEngagedPairs(new[] { a, b }, pairs);
        Assert.AreEqual(1, pairs.Count, "One pair, not one per troop");

        float reach = EngagementFrontage.Reach(TroopTuning.Active.EngagementGap, 0.45f);
        var links = new List<EngagementFrontage.Link>();
        EngagementFrontage.Measure(a, b, reach, links, out int aEngaged, out int bEngaged);
        Assert.Greater(aEngaged, 0);
        Assert.Greater(bEngaged, 0);
        Assert.AreEqual(aEngaged + bEngaged, links.Count);
        Assert.LessOrEqual(aEngaged, a.LivingSoldierCount);
        foreach (var link in links)
            Assert.LessOrEqual(Vector2.Distance(link.From, link.To), reach + 1e-4f);
    }

    [Test]
    public void FarApartTroops_AreNotAPair()
    {
        Troop a = LineTroop("A", 0, Vector2.zero, Vector2.up);
        Troop b = LineTroop("B", 1, new Vector2(0f, 40f), Vector2.down);
        Simulate(0.2f);
        var pairs = new List<(Troop, Troop)>();
        EngagementFrontage.CollectEngagedPairs(new[] { a, b }, pairs);
        Assert.AreEqual(0, pairs.Count);
    }

    // --- Heat strip ---

    const float Gap = 3f;
    const int FrontSoldiers = 10;
    static float FrontLength => (FrontSoldiers - 1) * TroopConstants.LineSpacing;

    static EngagementHeatStrip.Settings HeatSettings() => new()
    {
        CellWidth = 0.25f,
        EndFade = 2f
    };

    // Two lines facing each other Gap apart (first at y = 0, second at y = Gap), with an engaged soldier pair at every
    // LineSpacing except the missing ones.
    static List<EngagementFrontage.Link> Front(params int[] missing)
    {
        var links = new List<EngagementFrontage.Link>();
        for (int i = 0; i < FrontSoldiers; i++)
        {
            if (System.Array.IndexOf(missing, i) >= 0) continue;
            var near = new Vector2(i * TroopConstants.LineSpacing, 0.5f);
            var far = new Vector2(i * TroopConstants.LineSpacing, Gap - 0.5f);
            links.Add(new EngagementFrontage.Link(near, far, true));
            links.Add(new EngagementFrontage.Link(far, near, false));
        }
        return links;
    }

    static List<EngagementHeatStrip.Column> BuildFront(List<EngagementFrontage.Link> links,
        out Vector2 origin, out Vector2 along, out Vector2 across)
    {
        var columns = new List<EngagementHeatStrip.Column>();
        Assert.IsTrue(EngagementHeatStrip.Build(Vector2.zero, new Vector2(FrontLength, 0f), new Vector2(FrontLength, Gap),
            new Vector2(0f, Gap), links, HeatSettings(), columns, out origin, out along, out across));
        return columns;
    }

    static EngagementHeatStrip.Column Middle(List<EngagementHeatStrip.Column> columns)
    {
        EngagementHeatStrip.Column best = columns[0];
        foreach (var column in columns)
            if (Mathf.Abs(column.Along) < Mathf.Abs(best.Along)) best = column;
        return best;
    }

    [Test]
    public void HeatStrip_SpansTheGapBetweenTheTroopLines()
    {
        var columns = BuildFront(Front(), out Vector2 origin, out Vector2 along, out Vector2 across);
        Assert.AreEqual(1f, across.y, 1e-4f, "Across points from the first troop's line to the second's");
        Assert.AreEqual(Gap * 0.5f, origin.y, 1e-4f, "Origin is midway between the lines");
        float first = float.PositiveInfinity, last = float.NegativeInfinity;
        foreach (var column in columns)
        {
            Assert.AreEqual(-Gap * 0.5f, column.SideA, 1e-4f, "Band starts on the first troop's line");
            Assert.AreEqual(Gap * 0.5f, column.SideB, 1e-4f, "Band ends on the second troop's line");
            first = Mathf.Min(first, column.Along);
            last = Mathf.Max(last, column.Along);
        }
        Assert.AreEqual(FrontLength, last - first, 1e-3f, "Band covers the lines' overlap");
    }

    [Test]
    public void HeatStrip_PeaksMidGap_AndFadesToTheLinesAndTheEnds()
    {
        var columns = BuildFront(Front(), out _, out _, out _);
        EngagementHeatStrip.Column middle = Middle(columns);
        Assert.AreEqual(1f, EngagementHeatStrip.Intensity(middle, 0.5f), 1e-4f, "Full in the middle of the gap");
        Assert.AreEqual(0f, EngagementHeatStrip.Intensity(middle, 0f), 1e-4f, "Nothing on the first troop's line");
        Assert.AreEqual(0f, EngagementHeatStrip.Intensity(middle, 1f), 1e-4f, "Nothing on the second troop's line");
        Assert.Less(EngagementHeatStrip.Intensity(middle, 0.2f), EngagementHeatStrip.Intensity(middle, 0.4f));
        Assert.AreEqual(0f, columns[0].Weight, 1e-4f, "Nothing at one end of the engagement");
        Assert.AreEqual(0f, columns[columns.Count - 1].Weight, 1e-4f, "Nothing at the other end");
        Assert.Less(columns[1].Weight, middle.Weight, "Lighter toward the ends");
        for (int i = 1; i < columns.Count; i++)
        {
            float step = Mathf.Abs(columns[i].Weight - columns[i - 1].Weight);
            Assert.LessOrEqual(step, 0.25f, "A smooth gradient along the front");
        }
    }

    [Test]
    public void HeatStrip_IgnoresHowManySoldiersFight()
    {
        var full = BuildFront(Front(), out _, out _, out _);
        var gap = BuildFront(Front(4, 5), out _, out _, out _);
        var none = BuildFront(new List<EngagementFrontage.Link>(), out _, out _, out _);
        Assert.AreEqual(full.Count, gap.Count);
        Assert.AreEqual(full.Count, none.Count);
        for (int i = 0; i < full.Count; i++)
        {
            Assert.AreEqual(full[i].Weight, gap[i].Weight, 1e-5f, "Same heat with soldiers missing from the fight");
            Assert.AreEqual(full[i].Weight, none[i].Weight, 1e-5f, "Same heat with no soldier links");
        }
    }

    [Test]
    public void HeatStrip_FlankAttack_FallsBackToTheEngagedSoldiers()
    {
        // The second troop's line is perpendicular to the first's (a flank attack), so the band follows the soldiers.
        var links = Front();
        var columns = new List<EngagementHeatStrip.Column>();
        Assert.IsTrue(EngagementHeatStrip.Build(Vector2.zero, new Vector2(FrontLength, 0f), new Vector2(-1f, -5f),
            new Vector2(-1f, 5f), links, HeatSettings(), columns, out _, out _, out Vector2 across));
        Assert.AreEqual(1f, across.y, 1e-4f, "Across follows the engaged soldiers");
        EngagementHeatStrip.Column middle = Middle(columns);
        Assert.AreEqual(Gap - 1f, middle.SideB - middle.SideA, 1e-3f, "From one side's engaged soldiers to the other's");
        foreach (var column in columns)
            Assert.AreEqual(middle.SideA, column.SideA, 1e-4f, "A straight band");
    }

    // --- View ---

    static TroopEngagementView.Style Style() => new()
    {
        HeatLight = new Color(1f, 0.35f, 0.3f, 1f),
        HeatDark = new Color(0.45f, 0f, 0f, 1f),
        MaxHeatAlpha = 0.75f,
        EndFade = 2f,
        ReachSlack = 0.45f
    };

    [Test]
    public void EngagementView_BuildsHeatMap_AndHidesWhenOff()
    {
        var (a, b) = EngagedLines();
        var troops = new[] { a, b };
        var view = new TroopEngagementView();
        try
        {
            view.Update(troops, EngagementViewMode.HeatMap, Style());
            Assert.IsTrue(view.IsVisible, "Heat map shown");
            Assert.Greater(view.VertexCount, 0);
            Assert.AreEqual(0, view.VertexCount % (EngagementHeatStrip.Rows + 1), "Rows + 1 vertices per column");

            view.Update(troops, EngagementViewMode.Off, Style());
            Assert.IsFalse(view.IsVisible, "Off hides the view");
        }
        finally
        {
            view.Destroy();
        }
    }

    [Test]
    public void HeatColour_DarkerAndMoreOpaque_WithMoreHeat()
    {
        Color low = TroopEngagementView.HeatColour(0.2f, Style());
        Color high = TroopEngagementView.HeatColour(1f, Style());
        Assert.Greater(high.a, low.a);
        Assert.Less(high.r + high.g + high.b, low.r + low.g + low.b);
        Assert.AreEqual(0f, TroopEngagementView.HeatColour(0f, Style()).a, "Transparent where there is no heat");
    }

    [Test]
    public void F5_TogglesHeatMap()
    {
        Assert.AreEqual(EngagementViewMode.Off, TroopPlayerOrders.NextEngagementView(EngagementViewMode.HeatMap));
        Assert.AreEqual(EngagementViewMode.HeatMap, TroopPlayerOrders.NextEngagementView(EngagementViewMode.Off));
    }

    [Test]
    public void OverlayShader_Compiles()
    {
        Shader shader = Resources.Load<Shader>("Shaders/TroopOverlayColor");
        Assert.IsNotNull(shader);
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "TroopOverlayColor has compile errors");
    }

    // --- Battle Tuning assets ---

    [Test]
    public void BattleTuningAssets_LoadFromResources()
    {
        TroopTuning.OverrideForTests(null);
        TroopTuning tuning = TroopTuning.Active;
        Assert.AreEqual(7, tuning.Sections.Count);
        foreach (var section in tuning.Sections)
        {
            Assert.IsNotNull(section);
            Assert.IsTrue(EditorUtility.IsPersistent(section), $"{section.GetType().Name} is loaded from its asset, not defaults");
            StringAssert.StartsWith(TroopTuning.AssetFolder + "/", AssetDatabase.GetAssetPath(section));
        }
        TroopTuning.OverrideForTests(_tuning);
    }
}
