using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The marching column (spec §3.2, SU-120): lanes and rows from the soldier count and frontage (LineGeometry column
// helpers), the same geometry for the soldier field (SoldierFieldPlanner.Column) and the outline (TroopOutline.Column),
// always longer than wide, and soldiers free to fill a column that reaches past the troop radius
// (SoldierMotionConstraints column reach). The Mission 1 playtest showed small troops' columns many lanes wide and 1-2
// deep, with an outline about 3x the idle size that caught clicks far from the soldiers.
public sealed class ColumnGeometryTests
{
    // The spec defaults of Battle Tuning (FormationTuning), as in GeometryTests.
    const float NaturalLineRadiusScale = 1.8f;
    const float BlobRadiusScale = 0.85f;

    static int Frontage(int soldiers) => TroopFrontageRule.ForSoldierCount(soldiers);

    static float Radius(int soldiers) =>
        LineGeometry.TroopRadius(Frontage(soldiers), soldiers, NaturalLineRadiusScale, BlobRadiusScale);

    [TestCase(1, 1)]
    [TestCase(4, 1)]    // frontage 4 would give 2 x 2: single file instead, so it is longer than wide
    [TestCase(6, 2)]    // Prince Julian
    [TestCase(12, 2)]   // Laurice
    [TestCase(20, 3)]   // Jagen
    [TestCase(30, 3)]
    [TestCase(100, 3)]  // never more than MaxColumnLanes
    public void Lanes_FollowTheFrontage_UpToTheMaximum(int soldiers, int lanes)
    {
        Assert.AreEqual(lanes, LineGeometry.ColumnLanes(Frontage(soldiers), soldiers));
    }

    [Test]
    public void EveryTroopSize_MarchesLongerThanWide()
    {
        for (int soldiers = 1; soldiers <= 200; soldiers++)
        {
            int frontage = Frontage(soldiers);
            int lanes = LineGeometry.ColumnLanes(frontage, soldiers);
            int rows = LineGeometry.ColumnRows(frontage, soldiers);
            Assert.LessOrEqual(lanes, LineGeometry.MaxColumnLanes, $"{soldiers} soldiers");
            Assert.GreaterOrEqual(lanes * rows, soldiers, $"{soldiers} soldiers all have a place");
            Assert.Greater(LineGeometry.ColumnLength(frontage, soldiers),
                2f * LineGeometry.ColumnHalfWidth(frontage, soldiers), $"{soldiers} soldiers");
        }
    }

    // The Mission 1 troops (6, 12, 20 soldiers) and a larger one, with their real frontage and radius.
    [TestCase(6)]
    [TestCase(12)]
    [TestCase(20)]
    [TestCase(30)]
    public void FieldAndOutline_AreLongerThanWide_AndTheOutlineHugsTheLanes(int soldiers)
    {
        int frontage = Frontage(soldiers);
        float radius = Radius(soldiers);
        int lanes = LineGeometry.ColumnLanes(frontage, soldiers);

        FieldShape field = SoldierFieldPlanner.Column(Vector2.right, radius, frontage, soldiers);
        float fieldLength = Vector2.Distance(field.Start, field.End);
        Assert.Greater(fieldLength, 2f * field.HalfWidth, "the soldier field is longer than wide");
        Assert.AreEqual(-LineGeometry.ColumnTrail(radius), field.Start.x, 1e-4f, "starts just behind the pivot");
        Assert.Less(field.End.x, field.Start.x, "and trails it");
        Assert.AreEqual(lanes, field.ColumnLanes);

        // The outline's long axis is its Facing (the heading): HalfWidth runs along it, HalfLength (0) across it.
        TroopFootprint outline = TroopOutline.Column(Vector2.zero, Vector2.right, radius, frontage, soldiers);
        float outlineLength = 2f * outline.OuterHalfWidth;
        float outlineWidth = 2f * outline.OuterHalfLength;
        Assert.Greater(outlineLength, outlineWidth, "the outline is longer than wide");
        float lanesWidth = (lanes - 1) * TroopConstants.LineSpacing + 2f * TroopConstants.SoldierRadius;
        Assert.AreEqual(lanesWidth, outlineWidth, TroopConstants.SoldierRadius + 1e-4f, "the outline is as wide as the lanes");
        Assert.Less(outlineWidth, 2f * (radius * BlobRadiusScale + TroopConstants.SoldierRadius),
            "never wider than the idle blob");

        // Same geometry: the outline is the field's core padded by its half-width and one soldier radius.
        Assert.AreEqual((field.Start.x + field.End.x) * 0.5f, outline.Center.x, 1e-4f);
        Assert.AreEqual(fieldLength * 0.5f, outline.HalfWidth, 1e-4f);
        Assert.AreEqual(field.HalfWidth + TroopConstants.SoldierRadius, outline.Rounding, 1e-4f);
    }

    // The old 4-argument outline takes its frontage from the soldier count.
    [Test]
    public void ColumnOutline_WithoutFrontage_UsesTheFrontageRule()
    {
        TroopFootprint implicitFrontage = TroopOutline.Column(Vector2.zero, Vector2.up, 2.5f, 20);
        TroopFootprint explicitFrontage = TroopOutline.Column(Vector2.zero, Vector2.up, 2.5f, Frontage(20), 20);
        Assert.AreEqual(explicitFrontage.HalfWidth, implicitFrontage.HalfWidth, 1e-6f);
        Assert.AreEqual(explicitFrontage.Rounding, implicitFrontage.Rounding, 1e-6f);
        Assert.AreEqual(explicitFrontage.Center.y, implicitFrontage.Center.y, 1e-6f);
    }

    [Test]
    public void ColumnLength_ShrinksAsSoldiersDie()
    {
        int frontage = Frontage(30);
        Assert.Less(LineGeometry.ColumnLength(frontage, 15), LineGeometry.ColumnLength(frontage, 30));
        Assert.AreEqual(TroopConstants.LineSpacing, LineGeometry.ColumnLength(frontage, 1), 1e-5f, "never shorter than one spacing");
    }

    [Test]
    public void ColumnReach_GrowsAtOnce_AndShrinksAtTheShrinkSpeed()
    {
        Assert.AreEqual(8f, SoldierMotionConstraints.StepColumnReach(0f, 8f, 0.1f), 1e-5f, "grows at once");
        float shrunk = SoldierMotionConstraints.StepColumnReach(8f, 0f, 1f);
        Assert.AreEqual(8f - SoldierMotionConstraints.ColumnReachShrinkSpeed, shrunk, 1e-5f, "shrinks gradually");
        Assert.AreEqual(3f, SoldierMotionConstraints.StepColumnReach(3.5f, 3f, 10f), 1e-5f, "never below the target");
        Assert.AreEqual(5f, SoldierMotionConstraints.StepColumnReach(5f, 0f, 0f), 1e-5f, "nothing changes without time");
    }

    [Test]
    public void ColumnFill_SpansTheCoreEndToEnd_LineFillStaysInTheMiddleOfEachShare()
    {
        Assert.AreEqual(0f, SoldierSteering.FillPlace(0, 4, 2.7f, spanEnds: true), 1e-5f, "first row at the core's start");
        Assert.AreEqual(2.7f, SoldierSteering.FillPlace(3, 4, 2.7f, spanEnds: true), 1e-5f, "last row at its end");
        Assert.AreEqual(0.9f, SoldierSteering.FillPlace(1, 4, 2.7f, spanEnds: true), 1e-5f, "rows LineSpacing apart");
        Assert.AreEqual(0.45f, SoldierSteering.FillPlace(0, 1, 0.9f, spanEnds: true), 1e-5f, "a single row in the middle");
        Assert.AreEqual(1.25f, SoldierSteering.FillPlace(0, 4, 10f, spanEnds: false), 1e-5f, "line fill unchanged");
    }
}

// A marching troop in the simulation forms the column (SU-120). Stepped in Edit mode like TroopScenarioTests.
[Category("Scenario")]
public sealed class ColumnMarchScenarioTests
{
    const float FrameTime = 1f / 30f;
    const float MarchSeconds = 4f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(2468);
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

    Troop CreateTroop(int soldiers)
    {
        var melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(melee);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 5, CombatStatLimits.Default,
            new CharacterEquipment(melee, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15));
        _created.Add(profile);
        var root = new GameObject("Marcher");
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(0, new List<CharacterProfile> { profile }, TroopFrontageRule.ForSoldierCount(soldiers));
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    [TestCase(6)]
    [TestCase(20)]
    public void MarchingTroop_FormsAColumn_AndItsOutlineIsLongerThanWide(int soldiers)
    {
        Troop troop = CreateTroop(soldiers);
        Simulate(0.1f);
        troop.MoveTo(new Vector2(60f, 0f));
        Simulate(MarchSeconds);
        Assert.IsTrue(troop.IsMoving, "still marching");
        Assert.AreEqual(FormationMode.Column, troop.FormationMode);

        Vector2 pivot = troop.transform.position;
        float minAlong = float.PositiveInfinity, maxAlong = float.NegativeInfinity;
        float minAcross = float.PositiveInfinity, maxAcross = float.NegativeInfinity;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            Vector2 offset = (Vector2)troop.transform.TransformPoint(soldier.Position) - pivot;
            minAlong = Mathf.Min(minAlong, offset.x);
            maxAlong = Mathf.Max(maxAlong, offset.x);
            minAcross = Mathf.Min(minAcross, offset.y);
            maxAcross = Mathf.Max(maxAcross, offset.y);
        }
        Assert.Greater(maxAlong - minAlong, maxAcross - minAcross, "the soldiers are longer along the march than across it");
        Assert.Less(maxAcross - minAcross, 2f * LineGeometry.ColumnHalfWidth(troop.Frontage, soldiers) + TroopConstants.LineSpacing,
            "about as wide as the lanes");

        TroopFootprint outline = troop.Outline;
        Assert.AreEqual(FootprintKind.Capsule, outline.Kind);
        Assert.Greater(2f * outline.OuterHalfWidth, 2f * outline.OuterHalfLength, "the outline is longer than wide");
    }

    // 30 soldiers march 10 rows deep: the tail is past the troop radius, which no longer clamps it while marching.
    [Test]
    public void LongColumn_ReachesPastTheTroopRadius()
    {
        Troop troop = CreateTroop(30);
        Simulate(0.1f);
        troop.MoveTo(new Vector2(60f, 0f));
        Simulate(MarchSeconds);
        Assert.IsTrue(troop.IsMoving, "still marching");

        Vector2 pivot = troop.transform.position;
        float farthest = 0f;
        foreach (var soldier in troop.Soldiers)
            if (soldier.IsAlive)
                farthest = Mathf.Max(farthest, Vector2.Distance(troop.transform.TransformPoint(soldier.Position), pivot));
        Assert.Greater(farthest, troop.Radius + TroopConstants.LineSpacing, "the column's tail is beyond the troop radius");
    }
}
