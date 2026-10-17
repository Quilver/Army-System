using System;
using System.Collections.Generic;
using UnityEngine;

// Benchmark layouts for visual testing. Each case is a small script: it spawns troops at positions relative to the
// case origin, issues orders, and schedules timed actions. Timed actions run on the scaled game clock, so they pause
// with the harness. Cases are laid out side by side, so they do not interact.
//
// Team 0 is the player (selectable and orderable with the mouse). Team 1 is the enemy.

public enum TroopStyle
{
    Standard,   // mixed composition, default aggression, full morale
    Aggressive, // soldiers with high aggression (more and earlier charges)
    Fragile,    // low troop morale, so a modest wound causes a break
}

public sealed class BenchmarkCase
{
    public int Index;
    public string Name;
    public string Expectation;
    public Vector2 Origin;
    public Troop Primary;
    public GameObject Root;
    public float Elapsed;
    public readonly List<Troop> Troops = new();
    public readonly List<ScheduledAction> Schedule = new();
    int _nextAction;

    public void ResetTimeline()
    {
        Elapsed = 0f;
        _nextAction = 0;
    }

    // Runs due actions. Called with the scaled delta, so paused or slowed cases stay in step with the simulation.
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        Elapsed += deltaTime;
        while (_nextAction < Schedule.Count && Schedule[_nextAction].Time <= Elapsed)
            Schedule[_nextAction++].Run();
    }
}

public readonly struct ScheduledAction
{
    public readonly float Time;
    public readonly Action Run;

    public ScheduledAction(float time, Action run)
    {
        Time = time;
        Run = run;
    }
}

// Owns the runtime profiles used by the cases and destroys them when the harness is torn down.
public sealed class BenchmarkProfiles
{
    readonly Dictionary<TroopStyle, TroopProfile> _troops = new();
    readonly List<UnityEngine.Object> _owned = new();

    public TroopProfile TroopFor(TroopStyle style)
    {
        if (_troops.TryGetValue(style, out TroopProfile existing)) return existing;

        bool aggressive = style == TroopStyle.Aggressive;
        float morale = style == TroopStyle.Fragile ? 15f : 100f;
        var composition = new List<TroopProfileCount>
        {
            new() { Profile = Soldier(SoldierSpriteLibrary.Square, aggressive), Count = 12 },
            new() { Profile = Soldier(SoldierSpriteLibrary.Triangle, aggressive), Count = 6 },
            new() { Profile = Soldier(SoldierSpriteLibrary.Circle, aggressive), Count = 8 },
        };
        TroopProfile profile = TroopProfile.CreateRuntimeProfile(morale, composition, 16.5f, 22f);
        _owned.Add(profile);
        _troops[style] = profile;
        return profile;
    }

    SoldierProfile Soldier(Sprite sprite, bool aggressive)
    {
        SoldierProfile profile = aggressive
            ? SoldierProfile.CreateRuntimeProfile(sprite, aggression: 18f, meleeSkill: 50f, discipline: 5f, maxSpeed: 2.5f)
            : SoldierProfile.CreateRuntimeProfile(sprite);
        _owned.Add(profile);
        return profile;
    }

    public void Dispose()
    {
        foreach (var asset in _owned)
            if (asset != null) UnityEngine.Object.Destroy(asset);
        _owned.Clear();
        _troops.Clear();
    }
}

// Helper passed to each case definition. Everything it creates is parented under the case root.
public sealed class BenchmarkBuilder
{
    const float LineWidth = 22f;
    const float WallThickness = 1.5f;
    const float SquareSpriteSize = 0.75f; // the library square covers 24 of 32 pixels at 32 PPU

    readonly BenchmarkProfiles _profiles;
    readonly BenchmarkCase _case;
    readonly int _obstacleLayer;

    public BenchmarkBuilder(BenchmarkProfiles profiles, BenchmarkCase benchmarkCase, int obstacleLayer)
    {
        _profiles = profiles;
        _case = benchmarkCase;
        _obstacleLayer = obstacleLayer;
    }

    public Vector2 World(Vector2 local) => _case.Origin + local;

    public Troop Spawn(string troopName, int team, Vector2 local, TroopStyle style)
    {
        var root = new GameObject(troopName);
        root.transform.SetParent(_case.Root.transform, false);
        root.transform.position = World(local);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, _profiles.TroopFor(style));
        _case.Troops.Add(troop);
        return troop;
    }

    // Orders the troop into a battle line facing a local direction (the enemy side of the line).
    public void Line(Troop troop, Vector2 facingLocal)
    {
        Vector2 facing = facingLocal.normalized;
        Vector2 tangent = new(-facing.y, facing.x);
        Vector2 centre = troop.transform.position;
        troop.OrderLine(centre, tangent, LineWidth, centre + facing * 10f);
    }

    public void Move(Troop troop, Vector2 local) => troop.MoveTo(World(local));

    public void Pursue(Troop troop, Troop target) => troop.Pursue(target);

    public void At(float time, Action action) => _case.Schedule.Add(new ScheduledAction(time, action));

    // A rectangular obstacle on the terrain layer the soldiers' circle casts test against.
    public void Wall(Vector2 local, Vector2 size)
    {
        var wall = new GameObject("Wall");
        wall.transform.SetParent(_case.Root.transform, false);
        wall.transform.position = World(local);
        wall.layer = _obstacleLayer >= 0 ? _obstacleLayer : 0;
        wall.AddComponent<BoxCollider2D>().size = size;

        var renderer = wall.AddComponent<SpriteRenderer>();
        renderer.sprite = SoldierSpriteLibrary.Square;
        renderer.color = new Color(0.45f, 0.45f, 0.5f, 0.9f);
        renderer.sortingOrder = -5;
        wall.transform.localScale = new Vector3(size.x / SquareSpriteSize, size.y / SquareSpriteSize, 1f);
    }

    // The troop the panel inspects by default for this case.
    public void SetPrimary(Troop primary) => _case.Primary = primary;

    public float WallDepth => WallThickness;
}

public sealed class CaseDefinition
{
    public readonly string Name;
    public readonly string Expectation;
    public readonly Action<BenchmarkBuilder> Build;

    public CaseDefinition(string name, string expectation, Action<BenchmarkBuilder> build)
    {
        Name = name;
        Expectation = expectation;
        Build = build;
    }
}

public static class TroopBenchmarkCases
{
    const int Friendly = 0;
    const int Enemy = 1;

    public static readonly IReadOnlyList<CaseDefinition> All = new List<CaseDefinition>
    {
        // Core features
        new("1 Idle", "Pivot still. Soldiers settle into a blob with no drift.", b =>
        {
            b.SetPrimary(b.Spawn("Idle", Friendly, Vector2.zero, TroopStyle.Standard));
        }),

        new("2 Marching", "Column forms, pivot follows the path, troop arrives and returns to Idle.", b =>
        {
            Troop t = b.Spawn("March", Friendly, Vector2.zero, TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () => b.Move(t, new Vector2(0f, -40f)));
        }),

        new("3 Battle line", "Threat-facing line forms toward the enemy inside sensing range (30).", b =>
        {
            Troop t = b.Spawn("Line", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Spawn("Enemy", Enemy, new Vector2(45f, 0f), TroopStyle.Standard);
            b.SetPrimary(t);
        }),

        new("4 Chain", "Ends link and joints snap. Link tension stays within slack.", b =>
        {
            Troop left = b.Spawn("Chain L", Friendly, new Vector2(-30f, 0f), TroopStyle.Standard);
            Troop middle = b.Spawn("Chain M", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Spawn("Chain R", Friendly, new Vector2(30f, 0f), TroopStyle.Standard);
            b.Spawn("Enemy", Enemy, new Vector2(0f, 48f), TroopStyle.Standard);
            b.SetPrimary(middle);
        }),

        new("5 Engagement", "Engagement forms within A_Distance (5.2) and holds until 1.5 x A_Distance.", b =>
        {
            Troop t = b.Spawn("Pursuer", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 40f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(t, enemy));
        }),

        new("6 Charge", "Charges spread through the troop, chargers reach contact, clashes follow.", b =>
        {
            Troop t = b.Spawn("Aggressor", Friendly, Vector2.zero, TroopStyle.Aggressive);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 40f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(t, enemy));
        }),

        new("7 Retreat", "Fault slip back, hold, return. State goes Fleeing then Idle.", b =>
        {
            Troop t = b.Spawn("Fragile", Friendly, Vector2.zero, TroopStyle.Fragile);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 36f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(3f, () =>
            {
                // Four soldiers take enough damage to exceed the fragile troop's morale.
                for (int i = 0; i < 4 && i < t.Soldiers.Count; i++)
                    t.ApplyDamage(t.Soldiers[i], 20f, enemy.transform.position);
            });
        }),

        new("8 Charge support (baseline)", "Baseline only: charging soldiers with troop-mates nearby. No support mechanic yet (issue 07).", b =>
        {
            Troop left = b.Spawn("Aggressor L", Friendly, new Vector2(-20f, 0f), TroopStyle.Aggressive);
            Troop right = b.Spawn("Aggressor R", Friendly, new Vector2(20f, 0f), TroopStyle.Aggressive);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 44f), TroopStyle.Standard);
            b.SetPrimary(left);
            b.At(0f, () =>
            {
                b.Pursue(left, enemy);
                b.Pursue(right, enemy);
            });
        }),

        // Edge cases
        new("9 Edge: engaged while idle", "Enemy engages a troop with no orders. The idle troop should hold its ground (issue 04).", b =>
        {
            Troop t = b.Spawn("Idle target", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 40f), TroopStyle.Aggressive);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(enemy, t));
        }),

        new("10 Edge: engaged while marching", "Enemy engages a troop mid-march. The march should resolve into contact, not stall.", b =>
        {
            Troop t = b.Spawn("Marcher", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Spawn("Enemy", Enemy, new Vector2(40f, 0f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () => b.Move(t, new Vector2(80f, 0f)));
        }),

        new("11 Edge: engage one chain member", "One member of a chain is engaged. Only that member moves by link tension, not by the barrier.", b =>
        {
            b.Spawn("Chain L", Friendly, new Vector2(-30f, 0f), TroopStyle.Standard);
            Troop middle = b.Spawn("Chain M", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Spawn("Chain R", Friendly, new Vector2(30f, 0f), TroopStyle.Standard);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 46f), TroopStyle.Aggressive);
            b.SetPrimary(middle);
            b.At(0f, () => b.Pursue(enemy, middle));
        }),

        new("12 Edge: enter and exit chain", "Troop joins a chain at 2 s and leaves it at 14 s. Expect link created, then cleared.", b =>
        {
            b.Spawn("Chain L", Friendly, new Vector2(-30f, 0f), TroopStyle.Standard);
            b.Spawn("Chain M", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop joiner = b.Spawn("Joiner", Friendly, new Vector2(0f, -60f), TroopStyle.Standard);
            b.SetPrimary(joiner);
            b.At(2f, () => b.Move(joiner, new Vector2(30f, 0f)));
            b.At(14f, () => b.Move(joiner, new Vector2(0f, -60f)));
        }),

        new("13 Edge: flanked from the side", "Enemy attacks the line from its side. Watch the clash damage multiplier.", b =>
        {
            Troop t = b.Spawn("Line", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Line(t, Vector2.up);
            Troop enemy = b.Spawn("Flanker", Enemy, new Vector2(60f, 0f), TroopStyle.Aggressive);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(enemy, t));
        }),

        new("14 Edge: flanked from the rear", "Enemy attacks the line from behind. Watch the clash damage multiplier.", b =>
        {
            Troop t = b.Spawn("Line", Friendly, Vector2.zero, TroopStyle.Standard);
            b.Line(t, Vector2.up);
            Troop enemy = b.Spawn("Rear attacker", Enemy, new Vector2(0f, -60f), TroopStyle.Aggressive);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(enemy, t));
        }),

        new("15 Edge: re-ordered while engaged", "Engaged troop is ordered away at 8 s. Expect a clean disengage.", b =>
        {
            Troop t = b.Spawn("Engaged", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, 40f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(t, enemy));
            b.At(8f, () => b.Move(t, new Vector2(0f, -50f)));
        }),

        new("16 Edge: skirmish at engagement edge", "Enemy oscillates across A_Distance. Engagement should hold, not flicker.", b =>
        {
            Troop t = b.Spawn("Skirmisher", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop enemy = b.Spawn("Oscillator", Enemy, new Vector2(0f, 30f), TroopStyle.Standard);
            b.SetPrimary(t);
            // Pivot distances chosen to straddle the edge of A_Distance for these line lengths. Tune if needed.
            for (int k = 0; k < 30; k++)
            {
                float y = k % 2 == 0 ? 33f : 26f;
                b.At(k * 2f, () => b.Move(enemy, new Vector2(0f, y)));
            }
        }),

        new("17 Edge: terrain", "Enemy attacks across a wall. Soldiers should stop at the obstacle, not pass through it.", b =>
        {
            Troop t = b.Spawn("Behind wall", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop enemy = b.Spawn("Enemy", Enemy, new Vector2(0f, -42f), TroopStyle.Standard);
            b.Wall(new Vector2(0f, -14f), new Vector2(40f, b.WallDepth));
            b.SetPrimary(t);
            b.At(0f, () => b.Pursue(enemy, t));
        }),

        new("18 Edge: choke point", "Two lines meet in a narrow passage between two walls.", b =>
        {
            Troop north = b.Spawn("North", Friendly, new Vector2(0f, -45f), TroopStyle.Standard);
            Troop south = b.Spawn("South", Enemy, new Vector2(0f, 45f), TroopStyle.Standard);
            b.Wall(new Vector2(-9f, 0f), new Vector2(b.WallDepth, 30f));
            b.Wall(new Vector2(9f, 0f), new Vector2(b.WallDepth, 30f));
            b.SetPrimary(north);
            b.At(0f, () =>
            {
                b.Move(north, new Vector2(0f, 45f));
                b.Move(south, new Vector2(0f, -45f));
            });
        }),

        new("19 Edge: surrounded", "One troop attacked from three sides. Expect lines toward each threat.", b =>
        {
            Troop t = b.Spawn("Surrounded", Friendly, Vector2.zero, TroopStyle.Standard);
            Troop front = b.Spawn("Front", Enemy, new Vector2(0f, 40f), TroopStyle.Standard);
            Troop left = b.Spawn("Left", Enemy, new Vector2(-44f, 0f), TroopStyle.Standard);
            Troop right = b.Spawn("Right", Enemy, new Vector2(44f, 0f), TroopStyle.Standard);
            b.SetPrimary(t);
            b.At(0f, () =>
            {
                b.Pursue(front, t);
                b.Pursue(left, t);
                b.Pursue(right, t);
            });
        }),
    };
}
