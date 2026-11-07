using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// False "surrounded" breaks (SU-122, SU-123): the surround test (spec §4.9, §5.3, §5.6, Q-33) and the Line / Chain footprint
// (spec §3.3). In a Mission 1 playtest a troop of six fighting one enemy line, with open ground behind it, took a
// Surrounded break: the enemy's line was pressed up to the contact line with its soldiers behind it, and its footprint,
// mirrored about that line, reached over the thin troop into the box behind it. Troops are placed with PlaceAt and their
// footprints built directly; no frames are simulated, so every soldier stands exactly where the test puts it.
[Category("Scenario")]
public sealed class SurroundAndFootprintTests
{
    const int ThinTroopSoldiers = 6;
    const int ThinTroopFrontage = 6;
    const int EnemySoldiers = 12;
    const int EnemyFrontage = 6;
    const int BaseMorale = 5;
    // The enemy line stands here (its two ranks 0.45 either side), facing our troop at the origin...
    const float EnemyX = 3.5f;
    // ...and its line is then pressed up to the contact line, just in front of our troop, its soldiers left behind.
    const float PressedEnemyLineX = 1.2f;
    // A second enemy line standing in the box behind our rear face (our footprint ends 0.3 behind the origin).
    const float BehindEnemyX = -2.5f;
    static readonly Vector2 WallCenter = new(-3f, 0f);
    static readonly Vector2 WallSize = new(1f, 4f);
    // Kills that drop the enemy's EffectiveMorale (5 x 12 = 60) to 5 x 5 = 25, under half, so its next break is a rout.
    const int KillsToRout = 7;
    const float RoutShock = 26f;
    const float Tolerance = 1e-3f;

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
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop CreateTroop(string name, int team, int soldiers, int frontage)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null, false);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, BaseMorale,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15));
        _created.Add(character);
        var root = new GameObject(name);
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, frontage);
        return troop;
    }

    // A troop standing in a formed line at x, facing the given way, its soldiers in their ranks.
    Troop PlaceLine(string name, int team, int soldiers, int frontage, float x, Vector2 facing)
    {
        Troop troop = CreateTroop(name, team, soldiers, frontage);
        troop.PlaceAt(new Vector2(x, 0f), facing, troop.NaturalLineLength, holdLine: true);
        return troop;
    }

    // Our thin troop at the origin facing the enemy (+x), with the enemy line in front of it pressed up to the contact
    // line: the enemy's soldiers stand at EnemyX, its line at PressedEnemyLineX.
    void PlacePlaytestCase(out Troop ours, out Troop enemy)
    {
        ours = PlaceLine("Thin Troop", 0, ThinTroopSoldiers, ThinTroopFrontage, 0f, Vector2.right);
        enemy = PlaceLine("Enemy Line", 1, EnemySoldiers, EnemyFrontage, EnemyX, Vector2.left);
        enemy.Formation = TroopFormationState.Build(new Vector2(PressedEnemyLineX, 0f), Vector2.left,
            enemy.NaturalLineLength, true);
    }

    void Rebuild(params Troop[] troops)
    {
        _world.RefreshTroopsNow();
        foreach (var troop in troops) troop.SetFootprint(TroopFootprintBuilder.Build(troop));
    }

    static TroopFootprint RetreatPath(Troop troop) =>
        SurroundTest.RetreatPath(troop.Footprint, TroopTuning.Active.RetreatDistance);

    // A living soldier of troop whose body overlaps box (the rule Troop.IsSurrounded uses).
    static bool AnyLivingSoldierIn(Troop troop, in TroopFootprint box)
    {
        foreach (var soldier in troop.Soldiers)
            if (soldier.IsAlive && box.SignedDistance(troop.WorldPosition(soldier)) < TroopConstants.SoldierRadius) return true;
        return false;
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

    // The playtest case: open ground behind, so a break is a normal retreat. The old mirrored footprint of the pressed
    // enemy line reached into the box behind our troop, which is what gave the false surround.
    [Test]
    public void ThinTroop_FacingAPressedEnemyLine_WithOpenGroundBehind_IsNotSurrounded()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Rebuild(ours, enemy);
        TroopFootprint path = RetreatPath(ours);

        TroopFootprint mirrored = TroopFootprint.Rect(enemy.Formation.Start, enemy.Formation.End, enemy.Formation.Facing,
            TroopFootprintBuilder.LineHalfWidth(enemy, enemy.Formation));
        Assert.IsTrue(SurroundTest.BlockedBy(path, mirrored), "Setup: the old mirrored footprint reached the retreat path");
        Assert.IsFalse(AnyLivingSoldierIn(enemy, path), "Setup: no enemy soldier stands behind our troop");

        Assert.IsFalse(SurroundTest.BlockedBy(path, enemy.Footprint), "The enemy footprint stays over its soldiers");
        Assert.IsFalse(ours.IsSurrounded(), "Open ground behind: not surrounded");
    }

    // Even an enemy footprint that does reach the box (a line pushed right over our troop) does not block it on its own:
    // only soldiers standing in the box do.
    [Test]
    public void EnemyFootprintInTheBox_WithoutSoldiersThere_DoesNotSurround()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        enemy.Formation = TroopFormationState.Build(new Vector2(BehindEnemyX, 0f), Vector2.left, enemy.NaturalLineLength,
            true);
        Rebuild(ours, enemy);
        TroopFootprint path = RetreatPath(ours);

        Assert.IsTrue(SurroundTest.BlockedBy(path, enemy.Footprint), "Setup: the enemy footprint overlaps the path");
        Assert.IsFalse(ours.IsSurrounded());
    }

    [Test]
    public void EnemySoldiersInTheBoxBehind_Surround()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Troop behind = PlaceLine("Behind", 1, EnemySoldiers, EnemyFrontage, BehindEnemyX, Vector2.right);
        Rebuild(ours, enemy, behind);

        Assert.IsTrue(AnyLivingSoldierIn(behind, RetreatPath(ours)), "Setup: enemy soldiers stand behind our troop");
        Assert.IsTrue(ours.IsSurrounded());
    }

    [Test]
    public void FriendlySoldiersInTheBoxBehind_DoNotSurround()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Troop friend = PlaceLine("Friend", 0, EnemySoldiers, EnemyFrontage, BehindEnemyX, Vector2.right);
        Rebuild(ours, enemy, friend);

        Assert.IsTrue(AnyLivingSoldierIn(friend, RetreatPath(ours)), "Setup: friendly soldiers stand behind our troop");
        Assert.IsFalse(ours.IsSurrounded(), "Friendly troops never block (Q-33)");
    }

    [Test]
    public void TerrainBehind_Surrounds()
    {
        int terrain = LayerMask.NameToLayer("Terrain");
        Assert.GreaterOrEqual(terrain, 0, "Setup: the project has a Terrain layer");
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Rebuild(ours, enemy);
        Assert.IsFalse(ours.IsSurrounded(), "Setup: open ground before the wall is built");

        var wall = new GameObject("Test Wall") { layer = terrain };
        wall.transform.position = WallCenter;
        wall.AddComponent<BoxCollider2D>().size = WallSize;
        _created.Add(wall);
        Physics2D.SyncTransforms();

        Assert.IsTrue(ours.IsSurrounded(), "Terrain in the path blocks the retreat");
    }

    [Test]
    public void RoutedEnemyBehind_DoesNotSurround()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Troop behind = PlaceLine("Routed", 1, EnemySoldiers, EnemyFrontage, BehindEnemyX, Vector2.right);
        Rebuild(ours, enemy, behind);
        Assert.IsTrue(ours.IsSurrounded(), "Setup: the enemy behind blocks the path before it routs");

        Kill(behind, KillsToRout);
        behind.SetShockForTests(RoutShock);
        Assert.IsTrue(behind.IsRouted, "Setup: the enemy behind routs");
        Rebuild(ours, enemy, behind);
        Assert.IsTrue(AnyLivingSoldierIn(behind, RetreatPath(ours)), "Setup: its soldiers still stand behind our troop");

        Assert.IsFalse(ours.IsSurrounded(), "A routed enemy does not block the retreat");
    }

    // --- Footprint (spec §3.3) ---

    // A formed-up line is unchanged: its soldiers stand evenly either side of the line, so both sides match the old
    // symmetric half-width.
    [Test]
    public void FormedLineFootprint_MatchesTheSymmetricHalfWidth()
    {
        Troop troop = PlaceLine("Formed", 0, EnemySoldiers, EnemyFrontage, 0f, Vector2.right);
        Rebuild(troop);
        TroopFootprint footprint = troop.Footprint;
        float symmetric = TroopFootprintBuilder.LineHalfWidth(troop, troop.Formation);

        Assert.AreEqual(FootprintKind.Rect, footprint.Kind);
        Assert.AreEqual(symmetric, footprint.FrontHalfWidth, Tolerance);
        Assert.AreEqual(symmetric, footprint.RearHalfWidth, Tolerance);
        Assert.AreEqual(0f, footprint.LineOffset, Tolerance);
    }

    // A line run ahead of its soldiers: the footprint covers the soldiers behind the line and the line's own ranks ahead
    // of it, not the soldiers' mirror image; the line points stay on the line.
    [Test]
    public void LineAheadOfItsSoldiers_FootprintCoversTheSoldiers_NotTheirMirrorImage()
    {
        PlacePlaytestCase(out Troop ours, out Troop enemy);
        Rebuild(ours, enemy);
        TroopFootprint footprint = enemy.Footprint;
        TroopFormationData formation = enemy.Formation;
        Vector2 lineCenter = (formation.Start + formation.End) * 0.5f;

        Assert.Less(Vector2.Distance(lineCenter, footprint.LineCenter), Tolerance, "Line centre on the formation line");
        Assert.Less(Vector2.Distance(formation.Start, footprint.LinePoint(TroopConstants.RightEnd))
            + Vector2.Distance(formation.End, footprint.LinePoint(TroopConstants.LeftEnd)), 2f * Tolerance,
            "Line points on the formation line");

        float furthestBehind = 0f;
        foreach (var soldier in enemy.Soldiers)
        {
            Vector2 position = enemy.WorldPosition(soldier);
            Assert.IsTrue(footprint.Contains(position), "Covers every soldier");
            float across = Vector2.Dot(position - lineCenter, footprint.Facing);
            furthestBehind = Mathf.Max(furthestBehind, -across);
            Assert.IsFalse(footprint.Contains(position - 2f * across * footprint.Facing), "Not the soldier's mirror image");
        }
        Assert.AreEqual(furthestBehind + TroopConstants.SoldierRadius, footprint.RearHalfWidth, Tolerance);

        float length = Vector2.Distance(formation.Start, formation.End);
        float rankFloor = Mathf.Max(0f, LineGeometry.RankHalfWidth(length, enemy.LivingSoldierCount)
            - 2f * LineGeometry.RankThickness);
        Assert.AreEqual(rankFloor + TroopConstants.SoldierRadius, footprint.FrontHalfWidth, Tolerance,
            "Ahead of the line: only the line's ranks");
    }

    [Test]
    public void RectAcross_WithEqualWidths_IsTheCentredRect()
    {
        Vector2 start = new(0f, -2f);
        Vector2 end = new(0f, 2f);
        TroopFootprint centred = TroopFootprint.Rect(start, end, Vector2.right, 0.75f);
        TroopFootprint across = TroopFootprint.RectAcross(start, end, Vector2.right, 0.75f, 0.75f);

        Assert.Less(Vector2.Distance(centred.Center, across.Center), Tolerance);
        Assert.Less(Vector2.Distance(centred.Facing, across.Facing), Tolerance);
        Assert.AreEqual(centred.HalfWidth, across.HalfWidth, Tolerance);
        Assert.AreEqual(centred.HalfLength, across.HalfLength, Tolerance);
        Assert.AreEqual(0f, across.LineOffset, Tolerance);
    }

    [Test]
    public void RectAcross_ReachesEachSideSeparately_AndKeepsTheLinePointsOnTheLine()
    {
        Vector2 start = new(0f, -2f);
        Vector2 end = new(0f, 2f);
        TroopFootprint footprint = TroopFootprint.RectAcross(start, end, Vector2.right, 0.5f, 3f);

        Assert.AreEqual(1.75f, footprint.HalfWidth, Tolerance);
        Assert.AreEqual(0.5f, footprint.FrontHalfWidth, Tolerance);
        Assert.AreEqual(3f, footprint.RearHalfWidth, Tolerance);
        Assert.AreEqual(0.5f, footprint.FaceCenter(TroopFace.Front).x, Tolerance, "Front face 0.5 ahead of the line");
        Assert.AreEqual(-3f, footprint.FaceCenter(TroopFace.Rear).x, Tolerance, "Rear face 3 behind it");
        Assert.Less(Vector2.Distance(start, footprint.LinePoint(TroopConstants.RightEnd)), Tolerance);
        Assert.Less(Vector2.Distance(end, footprint.LinePoint(TroopConstants.LeftEnd)), Tolerance);
        Assert.IsTrue(footprint.Contains(new Vector2(-2.9f, 0f)));
        Assert.IsFalse(footprint.Contains(new Vector2(2.9f, 0f)), "Does not mirror the rear side ahead of the line");
    }
}
