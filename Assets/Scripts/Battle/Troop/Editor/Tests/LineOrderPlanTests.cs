using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Multi-troop line orders (TroopOrderPlanner.PlanLine, SU-160): the troops stand end to end from the press point and are
// all linked; one stretch for the whole line, clamp(drag / sum of L_nat, 0.7, 2.0), so each troop's length is in
// proportion to its frontage; a drag inside the facing dead zone keeps the selection's facing. Runs on the spec defaults.
public sealed class LineOrderPlanTests
{
    const float Tolerance = 1e-3f;
    static readonly int[] Frontages = { 6, 10, 14 };
    static readonly Vector2 Press = new(0f, -20f);

    readonly List<Object> _created = new();
    TroopTuning _tuning;
    TroopCombatWorld _world;

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
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop CreateTroop(string name, Vector2 position, int frontage)
    {
        MeleeWeaponProfile melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(melee);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 30, 5, CombatStatLimits.Default,
            new CharacterEquipment(melee, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15, null));
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(0, new List<CharacterProfile> { profile }, frontage);
        return troop;
    }

    // Three troops of frontage 6, 10 and 14 side by side along +x, in that order.
    List<Troop> ThreeTroops()
    {
        var troops = new List<Troop>();
        for (int i = 0; i < Frontages.Length; i++)
            troops.Add(CreateTroop("Troop " + Frontages[i], new Vector2(i * 12f, 0f), Frontages[i]));
        return troops;
    }

    static float NaturalTotal(List<Troop> troops)
    {
        float total = 0f;
        foreach (var troop in troops) total += troop.NaturalLineLength;
        return total;
    }

    static TroopOrderPlanner Plan(List<Troop> troops, Vector2 end)
    {
        var planner = new TroopOrderPlanner(new TroopSelection { PlayerTeamId = 0 });
        planner.PlanLine(new List<Troop>(troops), Press, end);
        return planner;
    }

    // dragOverNatural: the drag length as a multiple of the summed L_nat. expectedStretch: the clamped stretch.
    [TestCase(0.4f, 0.7f, TestName = "ShortDrag_GivesTheMinimumLine_PastTheCursor")]
    [TestCase(1.3f, 1.3f, TestName = "MediumDrag_SpansTheDrag")]
    [TestCase(6f, 2f, TestName = "VeryLongDrag_GivesTheMaximumLine_ShortOfTheCursor")]
    public void ThreeTroops_StandEndToEnd_InProportionToFrontage_AndAllLink(float dragOverNatural, float expectedStretch)
    {
        List<Troop> troops = ThreeTroops();
        float natural = NaturalTotal(troops);
        Vector2 end = Press + Vector2.right * (natural * dragOverNatural);
        TroopOrderPlanner planner = Plan(troops, end);

        var placements = planner.Placements;
        Assert.AreEqual(3, placements.Count);
        float total = 0f;
        for (int i = 0; i < placements.Count; i++)
        {
            Assert.AreSame(troops[i], placements[i].Troop, "kept in order along the drag");
            Assert.AreEqual(expectedStretch * troops[i].NaturalLineLength, placements[i].Length, Tolerance,
                "length in proportion to the troop's own L_nat");
            Assert.AreEqual(0f, Vector2.Distance(Vector2.up, placements[i].Facing), Tolerance, "front from the drag");
            total += placements[i].Length;
        }
        Assert.AreEqual(expectedStretch * natural, total, Tolerance, "total length clamped to [0.7, 2.0] x sum of L_nat");
        Assert.AreEqual(placements[1].Length / placements[0].Length, 10f / 6f, Tolerance);
        Assert.AreEqual(placements[2].Length / placements[0].Length, 14f / 6f, Tolerance);

        Vector2 direction = planner.LineDirection;
        TroopOrderPlanner.AdjacentEnds(placements[0], direction, out _, out int firstBack);
        Assert.AreEqual(0f, Vector2.Distance(Press, TroopOrderPlanner.PlannedLinePoint(placements[0], firstBack)), Tolerance,
            "the line starts at the press point");
        TroopOrderPlanner.AdjacentEnds(placements[2], direction, out int lastFront, out _);
        Assert.AreEqual(total, Vector2.Distance(Press, TroopOrderPlanner.PlannedLinePoint(placements[2], lastFront)), Tolerance);

        for (int i = 0; i + 1 < placements.Count; i++)
        {
            Assert.IsTrue(planner.JoinsNext(i), $"pair {i} linked");
            TroopOrderPlanner.AdjacentEnds(placements[i], direction, out int endA, out _);
            TroopOrderPlanner.AdjacentEnds(placements[i + 1], direction, out _, out int endB);
            float gap = Vector2.Distance(TroopOrderPlanner.PlannedLinePoint(placements[i], endA),
                TroopOrderPlanner.PlannedLinePoint(placements[i + 1], endB));
            float attach = ChainLinkRules.AttachDistance(troops[i].NaturalLineLength, troops[i + 1].NaturalLineLength,
                TroopTuning.Active.AttachScale);
            Assert.LessOrEqual(gap, Mathf.Min(attach, Tolerance), $"pair {i} line points coincide");
        }
    }

    // Inside the dead zone (a quarter of the minimum total width) the selection keeps its facing, and the minimum line
    // runs across it from the press point on the cursor's side; past it the drag sets the front.
    [Test]
    public void FacingDeadZone_KeepsTheSelectionsFacing_UntilAQuarterOfTheMinimumWidth()
    {
        List<Troop> troops = ThreeTroops();
        foreach (var troop in troops)
            troop.PlaceAt(troop.transform.position, Vector2.right, troop.NaturalLineLength, true);
        float natural = NaturalTotal(troops);
        float deadZone = natural * TroopTuning.Active.ChainMinStretchScale * TroopTuning.Active.LineOrderFacingDeadZone;
        Assert.AreEqual(0.25f, TroopTuning.Active.LineOrderFacingDeadZone, 1e-6f);

        foreach (float side in new[] { -1f, 1f })
        {
            // A small drag, mostly sideways (left or right): its own front would be up or down; the kept facing is right.
            Vector2 drag = new Vector2(side * 0.9f, 0.3f).normalized * (deadZone * 0.9f);
            TroopOrderPlanner planner = Plan(troops, Press + drag);
            Assert.AreEqual(3, planner.Placements.Count);
            // Facing right, the line runs up or down the screen, on the cursor's side (the drag's y is +).
            Assert.AreEqual(0f, Vector2.Distance(Vector2.up, planner.LineDirection), Tolerance, $"side {side}");
            float total = 0f;
            foreach (var placement in planner.Placements)
            {
                Assert.AreEqual(0f, Vector2.Distance(Vector2.right, placement.Facing), Tolerance, "kept facing");
                Assert.AreEqual(TroopTuning.Active.ChainMinStretchScale * placement.Troop.NaturalLineLength,
                    placement.Length, Tolerance, "minimum width");
                Assert.Greater(Vector2.Dot(placement.Center - Press, planner.LineDirection), 0f, "from the press point");
                total += placement.Length;
            }
            Assert.AreEqual(natural * TroopTuning.Active.ChainMinStretchScale, total, Tolerance);
        }

        // Downward drag inside the dead zone: the line runs down.
        TroopOrderPlanner down = Plan(troops, Press + Vector2.down * (deadZone * 0.5f));
        Assert.AreEqual(0f, Vector2.Distance(Vector2.down, down.LineDirection), Tolerance);
        Assert.AreEqual(0f, Vector2.Distance(Vector2.right, down.Placements[0].Facing), Tolerance);

        // Past the dead zone the drag's direction sets the front: a drag to the right faces up.
        TroopOrderPlanner past = Plan(troops, Press + Vector2.right * (deadZone * 1.01f));
        foreach (var placement in past.Placements)
            Assert.AreEqual(0f, Vector2.Distance(Vector2.up, placement.Facing), Tolerance, "front from the drag");
    }

    // A single troop's right-drag line is unchanged apart from the new maximum: it spans the drag, centred on its
    // midpoint, clamped to [0.7, 2.0] x L_nat.
    [Test]
    public void OneTroop_SpansTheDrag_ClampedToTwiceItsNaturalLength()
    {
        Troop troop = CreateTroop("Lone", Vector2.zero, 10);
        float natural = troop.NaturalLineLength;
        Assert.AreEqual(2f * natural, troop.MaximumLineLength, Tolerance);

        TroopOrderPlanner planner = Plan(new List<Troop> { troop }, Press + Vector2.right * (natural * 1.5f));
        Assert.AreEqual(1.5f * natural, planner.Placements[0].Length, Tolerance);
        Assert.AreEqual(0f, Vector2.Distance(Press + Vector2.right * (natural * 0.75f), planner.Placements[0].Center), Tolerance);

        planner = Plan(new List<Troop> { troop }, Press + Vector2.right * (natural * 5f));
        Assert.AreEqual(2f * natural, planner.Placements[0].Length, Tolerance);
    }
}
