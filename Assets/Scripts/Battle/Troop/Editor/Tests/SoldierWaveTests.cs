using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// The soldier wave (SU-166): when a troop sets off, stops or turns, the soldiers nearest the direction of travel respond
// first and each rank behind a moment later, so the troop stretches as it starts and bunches as it stops; the pivot moves
// as before. A four-rank line (32 soldiers, frontage 8) is stepped in Edit mode with a fixed frame time and seed.
[Category("Scenario")]
public sealed class SoldierWaveTests
{
    const float FrameTime = 1f / 30f;
    const int Frontage = 8;
    const int Soldiers = 32;
    const int Ranks = 4;
    // Each rank starts within this of rank spacing / wave speed after the one in front.
    const float RankDelayTolerance = 0.1f;
    // The last soldier is back in its place at most this long after the pivot arrives.
    const float TrailBound = 2.5f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        NewWorld();
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

    void NewWorld()
    {
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(4242);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    SerializedProperty Find(string property, out SerializedObject serialized)
    {
        foreach (var section in _tuning.Sections)
        {
            serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found != null) return found;
        }
        serialized = null;
        Assert.Fail($"No Battle Tuning section has the field {property}");
        return null;
    }

    void UseTuning(string property, bool value)
    {
        Find(property, out SerializedObject serialized).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        TroopTuning.OverrideForTests(_tuning);
    }

    void UseTuning(string property, float value)
    {
        Find(property, out SerializedObject serialized).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        TroopTuning.OverrideForTests(_tuning);
    }

    CharacterProfile Character(int soldiers, int aggression = 5)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, 10,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null),
            new SoldierStats(5, 5, 5, 5, 5, aggression, 40));
        _created.Add(character);
        return character;
    }

    // A troop standing in a placed line whose front rank is centred on lineCenter.
    Troop PlacedLine(string name, int team, Vector2 lineCenter, Vector2 facing, int soldiers = Soldiers, int aggression = 5)
    {
        var root = new GameObject(name);
        root.transform.position = lineCenter;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { Character(soldiers, aggression) }, Frontage);
        troop.PlaceAt(lineCenter, facing, troop.NaturalLineLength, holdLine: true);
        return troop;
    }

    // One frame of a run: where every soldier stands, the pivot, and the wave state.
    sealed class Frame
    {
        public float Time;
        public Vector2 Pivot;
        public bool Moving;
        public Vector2[] Positions;
        public float MaxLag;
        public float MaxWaveSpeed;
        public float MaxAhead;
        public float MaxBehind;
        public float MaxAcross;
    }

    sealed class Run
    {
        public readonly List<Frame> Frames = new();
        public int[] RankOf;       // rank of each soldier when the run began, front = 0
        public float FormedDepth;  // front rank to rear rank when the run began
        public Vector2 Front;
        public float FrameTime;

        public float RankMean(int frame, int rank)
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < RankOf.Length; i++)
            {
                if (RankOf[i] != rank) continue;
                sum += Vector2.Dot(Frames[frame].Positions[i], Front);
                count++;
            }
            return sum / Mathf.Max(1, count);
        }

        // Mean world velocity of a rank over the frame before `frame`.
        public Vector2 RankVelocity(int frame, int rank)
        {
            if (frame == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            int count = 0;
            for (int i = 0; i < RankOf.Length; i++)
            {
                if (RankOf[i] != rank) continue;
                sum += Frames[frame].Positions[i] - Frames[frame - 1].Positions[i];
                count++;
            }
            return sum / Mathf.Max(1, count) / FrameTime;
        }

        public float Depth(int frame) => RankMean(frame, 0) - RankMean(frame, Ranks - 1);

        public float SoldierSpeed(int frame, int soldier) =>
            frame == 0 ? 0f : Vector2.Distance(Frames[frame].Positions[soldier], Frames[frame - 1].Positions[soldier]) / FrameTime;

        public int ArrivalFrame()
        {
            for (int f = 1; f < Frames.Count; f++)
                if (Frames[f - 1].Moving && !Frames[f].Moving) return f;
            return -1;
        }
    }

    static Frame Capture(Troop troop, float time)
    {
        var frame = new Frame
        {
            Time = time,
            Pivot = troop.transform.position,
            Moving = troop.IsMoving,
            Positions = new Vector2[troop.Soldiers.Count],
        };
        for (int i = 0; i < troop.Soldiers.Count; i++)
        {
            SoldierAgentState soldier = troop.Soldiers[i];
            frame.Positions[i] = troop.WorldPosition(soldier);
            frame.MaxLag = Mathf.Max(frame.MaxLag, soldier.WaveLag.magnitude);
            frame.MaxWaveSpeed = Mathf.Max(frame.MaxWaveSpeed, soldier.WaveVelocity.magnitude);
            float along = Vector2.Dot(soldier.WaveLag, troop.SoldierWaveAxis);
            frame.MaxAhead = Mathf.Max(frame.MaxAhead, -along);
            frame.MaxBehind = Mathf.Max(frame.MaxBehind, along);
            frame.MaxAcross = Mathf.Max(frame.MaxAcross, (soldier.WaveLag - troop.SoldierWaveAxis * along).magnitude);
        }
        return frame;
    }

    static Run Begin(Troop troop, Vector2 front, float frameTime)
    {
        var run = new Run { Front = front, FrameTime = frameTime, RankOf = new int[troop.Soldiers.Count] };
        var order = new List<int>();
        for (int i = 0; i < troop.Soldiers.Count; i++) order.Add(i);
        order.Sort((a, b) => Vector2.Dot(troop.WorldPosition(troop.Soldiers[b]), front)
            .CompareTo(Vector2.Dot(troop.WorldPosition(troop.Soldiers[a]), front)));
        int perRank = Mathf.CeilToInt(troop.Soldiers.Count / (float)Ranks);
        for (int k = 0; k < order.Count; k++) run.RankOf[order[k]] = Mathf.Min(Ranks - 1, k / perRank);
        run.Frames.Add(Capture(troop, 0f));
        run.FormedDepth = run.Depth(0);
        return run;
    }

    void Step(Run run, Troop troop, float seconds, System.Action<int> perFrame = null)
    {
        int frames = Mathf.CeilToInt(seconds / run.FrameTime - 1e-4f);
        for (int i = 0; i < frames; i++)
        {
            _world.SimulateFrame(run.FrameTime);
            Frame last = run.Frames[run.Frames.Count - 1];
            run.Frames.Add(Capture(troop, last.Time + run.FrameTime));
            perFrame?.Invoke(run.Frames.Count - 1);
        }
    }

    // A placed four-rank line facing up, settled, then ordered to a line `distance` ahead. A march without an enemy near
    // is made in column (spec §3.1), so an enemy line stands 30 ahead, within S_Distance and out of reach, to keep the
    // troop in line.
    (Troop troop, Run run) MarchAhead(float distance, float seconds, float frameTime = FrameTime,
        System.Action<Troop, int> perFrame = null)
    {
        Troop troop = InLineWithEnemyAhead("Marcher");
        for (int i = 0; i < Mathf.CeilToInt(1.5f / frameTime); i++) _world.SimulateFrame(frameTime);
        Assert.AreEqual(Ranks, LineGeometry.Ranks(troop.LivingSoldierCount, troop.Formation.Length), "four ranks");
        Run run = Begin(troop, Vector2.up, frameTime);
        troop.OrderLine(Vector2.up * distance, Vector2.up, troop.NaturalLineLength);
        Step(run, troop, seconds, perFrame != null ? f => perFrame(troop, f) : null);
        return (troop, run);
    }

    Troop InLineWithEnemyAhead(string name)
    {
        PlacedLine("Watcher", 1, Vector2.up * 30f, Vector2.down);
        return PlacedLine(name, 0, Vector2.zero, Vector2.up);
    }

    float ExpectedRankDelay => TroopConstants.LineSpacing / _tuning.SoldierWaveSpeed;

    // First frame at which the rank moves forward at half the pivot's march speed, from `from` on; -1 if never.
    static int StartFrame(Run run, int rank, float speed, int from = 1)
    {
        for (int f = from; f < run.Frames.Count; f++)
            if (Vector2.Dot(run.RankVelocity(f, rank), run.Front) >= 0.5f * speed) return f;
        return -1;
    }

    static float MarchSpeed(Run run)
    {
        float best = 0f;
        for (int f = 1; f < run.Frames.Count; f++)
            best = Mathf.Max(best, Vector2.Distance(run.Frames[f].Pivot, run.Frames[f - 1].Pivot) / run.FrameTime);
        return best;
    }

    [Test]
    public void SettingOff_EachRankStepsOffAMomentAfterTheOneInFront_AndTheTroopStretchesThenReforms()
    {
        var (troop, run) = MarchAhead(12f, 7f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "the troop marched in line");
        int arrival = run.ArrivalFrame();
        Assert.Greater(arrival, 0, "the pivot arrives");
        float speed = MarchSpeed(run);
        var starts = new float[Ranks];
        for (int rank = 0; rank < Ranks; rank++)
        {
            int frame = StartFrame(run, rank, speed);
            Assert.Greater(frame, 0, $"rank {rank} starts");
            starts[rank] = run.Frames[frame].Time;
        }
        float peak = float.MinValue;
        int peakFrame = 0;
        for (int f = 0; f < run.Frames.Count; f++)
            if (run.Depth(f) > peak) { peak = run.Depth(f); peakFrame = f; }
        // On the march, just before the pivot arrives.
        float final = run.Depth(arrival - 1);
        TestContext.WriteLine($"march {speed:F2} u/s; rank starts {string.Join(", ", System.Array.ConvertAll(starts, s => s.ToString("F3")))} s; " +
            $"depth formed {run.FormedDepth:F2}, peak {peak:F2} at {run.Frames[peakFrame].Time:F2} s, before arrival ({run.Frames[arrival].Time:F2} s) {final:F2}; " +
            $"max lag {run.Frames[peakFrame].MaxLag:F2}");

        // In the first moments the front rank walks faster than the rear rank.
        int early = Mathf.RoundToInt(0.2f / FrameTime);
        Assert.Greater(Vector2.Dot(run.RankVelocity(early, 0), run.Front),
            Vector2.Dot(run.RankVelocity(early, Ranks - 1), run.Front) + 0.5f * speed, "front faster than rear at 0.2 s");
        for (int rank = 1; rank < Ranks; rank++)
            Assert.AreEqual(ExpectedRankDelay, starts[rank] - starts[rank - 1], RankDelayTolerance,
                $"rank {rank} starts a rank spacing / wave speed after rank {rank - 1}");
        Assert.Greater(peak, run.FormedDepth + 1f, "the troop stretches as it sets off");
        Assert.AreEqual(run.FormedDepth, final, 0.2f, "and is back to its formed depth on the march");
    }

    [Test]
    public void Stopping_FrontRankStopsFirst_RanksCloseUpAndSettleWithoutOscillating()
    {
        // Without the wave first: the depth the troop settles to after this march.
        UseTuning("_soldierWave", false);
        var (_, off) = MarchAhead(12f, 10f);
        float settledDepthOff = off.Depth(off.Frames.Count - 1);
        TestDestroyAndReset();
        UseTuning("_soldierWave", true);

        var (_, run) = MarchAhead(12f, 10f);
        int arrival = run.ArrivalFrame();
        Assert.Greater(arrival, 0, "the pivot arrives");
        float speed = MarchSpeed(run);
        float arrivalTime = run.Frames[arrival].Time;

        // Stop time of each rank: first frame from the arrival on at which it walks forward below a fifth of the march.
        var stops = new float[Ranks];
        for (int rank = 0; rank < Ranks; rank++)
        {
            stops[rank] = float.NaN;
            for (int f = arrival; f < run.Frames.Count; f++)
            {
                if (Vector2.Dot(run.RankVelocity(f, rank), run.Front) > 0.2f * speed) continue;
                stops[rank] = run.Frames[f].Time - arrivalTime;
                break;
            }
        }
        float depthAtArrival = run.Depth(arrival);
        float least = float.MaxValue;
        for (int f = arrival; f < run.Frames.Count; f++) least = Mathf.Min(least, run.Depth(f));

        // Settled: no soldier lags or runs on past its place, and the depth is the one the troop settles to without it.
        int settled = -1;
        for (int f = arrival; f < run.Frames.Count && settled < 0; f++)
            if (run.Frames[f].MaxLag < 0.02f && run.Frames[f].MaxWaveSpeed < 0.05f
                && Mathf.Abs(run.Depth(f) - settledDepthOff) < 0.15f) settled = f;
        float settleTime = settled > 0 ? run.Frames[settled].Time - arrivalTime : float.NaN;

        // Once settled, the lag only dies away: it never grows again (no overshoot, no oscillation).
        float laterSpeed = 0f;
        float laterWave = 0f;
        float lagGrowth = 0f;
        for (int f = Mathf.Max(settled, arrival) + 1; f < run.Frames.Count; f++)
        {
            laterWave = Mathf.Max(laterWave, run.Frames[f].MaxWaveSpeed);
            lagGrowth = Mathf.Max(lagGrowth, run.Frames[f].MaxLag - run.Frames[f - 1].MaxLag);
            for (int i = 0; i < run.RankOf.Length; i++) laterSpeed = Mathf.Max(laterSpeed, run.SoldierSpeed(f, i));
        }
        var series = new System.Text.StringBuilder();
        for (int f = arrival; f < Mathf.Min(run.Frames.Count, arrival + 40); f += 3)
            series.Append($" [{run.Frames[f].Time - arrivalTime:F1}: d {run.Depth(f):F2} r0 {run.RankMean(f, 0) - run.RankMean(arrival, 0):F2} r1 {run.RankMean(f, 1) - run.RankMean(arrival, 1):F2} r3 {run.RankMean(f, 3) - run.RankMean(arrival, 3):F2} ahead {run.Frames[f].MaxAhead:F2}]");
        TestContext.WriteLine("after arrival:" + series);
        TestContext.WriteLine($"arrival {arrivalTime:F2} s; rank stops after it {string.Join(", ", System.Array.ConvertAll(stops, s => s.ToString("F3")))} s; " +
            $"depth at arrival {depthAtArrival:F2}, least {least:F2}, settles to {run.Depth(run.Frames.Count - 1):F2} " +
            $"({settledDepthOff:F2} without the wave); settled {settleTime:F2} s after arrival; " +
            $"later max soldier speed {laterSpeed:F3}, max wave speed {laterWave:F4}, lag growth {lagGrowth:F5}");

        Assert.Less(stops[0], stops[Ranks - 1], "the front rank stops before the rear rank");
        for (int rank = 1; rank < Ranks; rank++)
            Assert.LessOrEqual(stops[rank - 1], stops[rank] + 1e-4f, $"rank {rank} stops after rank {rank - 1}");
        Assert.Less(least, depthAtArrival - 0.1f, "the ranks close up as the troop stops");
        Assert.Greater(settled, 0, "every soldier settles in its place");
        Assert.LessOrEqual(settleTime, TrailBound, "every soldier is back in its place within the bound");
        Assert.LessOrEqual(laterWave, 0.05f, "the wave motion only dies away once settled");
        Assert.LessOrEqual(lagGrowth, 1e-4f, "no lag grows again once settled");
        Assert.Less(laterSpeed, 0.15f, "no soldier keeps moving (or oscillating) once settled");
    }

    [Test]
    public void TurningOnTheMarch_LeadingSoldiersTurnBeforeTrailingOnes()
    {
        Troop troop = InLineWithEnemyAhead("Turner");
        Simulate(1.5f);
        Run run = Begin(troop, Vector2.up, FrameTime);
        troop.OrderLine(Vector2.up * 12f, Vector2.up, troop.NaturalLineLength);
        Step(run, troop, 3f);
        int turnFrame = run.Frames.Count - 1;
        // The same facing, but the line's place is off to the right: the pivot turns from up to right.
        Vector2 here = troop.LineCenterAhead(troop.transform.position, Vector2.up, troop.NaturalLineLength);
        troop.OrderLine(here + Vector2.right * 20f, Vector2.up, troop.NaturalLineLength);
        Step(run, troop, 3f);
        Assert.AreEqual(FormationMode.Line, troop.FormationMode, "the troop marched in line");

        // Leading and trailing soldiers: the quarter furthest along, and the quarter furthest back, the way the troop now
        // turns (between up and right). Turn time: first frame after the order at which the group walks more right than up.
        Vector2 bisector = (Vector2.up + Vector2.right).normalized;
        var order = new List<int>();
        for (int i = 0; i < run.RankOf.Length; i++) order.Add(i);
        Vector2[] at = run.Frames[turnFrame].Positions;
        order.Sort((a, b) => Vector2.Dot(at[b], bisector).CompareTo(Vector2.Dot(at[a], bisector)));
        int quarter = order.Count / 4;
        List<int> leading = order.GetRange(0, quarter);
        List<int> trailing = order.GetRange(order.Count - quarter, quarter);
        float leadTurn = TurnTime(run, leading, turnFrame);
        float trailTurn = TurnTime(run, trailing, turnFrame);
        var pivot = new System.Text.StringBuilder();
        for (int f = turnFrame + 1; f < Mathf.Min(run.Frames.Count, turnFrame + 40); f += 3)
        {
            Vector2 v = (run.Frames[f].Pivot - run.Frames[f - 1].Pivot) / FrameTime;
            pivot.Append($" {v.x:F1},{v.y:F1}");
        }
        TestContext.WriteLine($"leading quarter turns {leadTurn:F3} s after the order, trailing quarter {trailTurn:F3} s; pivot velocity{pivot}");
        Assert.IsFalse(float.IsNaN(leadTurn), "the leading soldiers turn");
        Assert.IsFalse(float.IsNaN(trailTurn), "the trailing soldiers turn");
        Assert.Less(leadTurn + 0.15f, trailTurn, "the leading soldiers turn before the trailing ones");
    }

    static float TurnTime(Run run, List<int> group, int from)
    {
        for (int f = from + 1; f < run.Frames.Count; f++)
        {
            Vector2 sum = Vector2.zero;
            foreach (int i in group) sum += run.Frames[f].Positions[i] - run.Frames[f - 1].Positions[i];
            Vector2 velocity = sum / group.Count / run.FrameTime;
            if (velocity.x > velocity.y && velocity.x >= 0.3f) return run.Frames[f].Time - run.Frames[from].Time;
        }
        return float.NaN;
    }

    void Simulate(float seconds)
    {
        for (int i = 0; i < Mathf.CeilToInt(seconds / FrameTime); i++) _world.SimulateFrame(FrameTime);
    }

    [Test]
    public void PivotArrival_IsUnchanged_AndTheLastSoldierTrailsItByABoundedTime()
    {
        var (_, on) = MarchAhead(12f, 10f);
        int arrivalOn = on.ArrivalFrame();
        int back = -1;
        for (int f = arrivalOn; f < on.Frames.Count && back < 0; f++) if (on.Frames[f].MaxLag < 0.05f) back = f;
        TestDestroyAndReset();
        UseTuning("_soldierWave", false);
        var (_, off) = MarchAhead(12f, 10f);
        int arrivalOff = off.ArrivalFrame();
        TestContext.WriteLine($"pivot arrives at frame {arrivalOn} with the wave, {arrivalOff} without; the last soldier " +
            $"is back in place {(back - arrivalOn) * FrameTime:F2} s after the pivot");
        Assert.Greater(arrivalOn, 0);
        Assert.AreEqual(arrivalOff, arrivalOn, "the pivot arrives on the same frame");
        Assert.AreEqual(off.Frames[arrivalOff].Pivot.x, on.Frames[arrivalOn].Pivot.x, 1e-4f);
        Assert.AreEqual(off.Frames[arrivalOff].Pivot.y, on.Frames[arrivalOn].Pivot.y, 1e-4f);
        Assert.Greater(back, 0);
        Assert.LessOrEqual((back - arrivalOn) * FrameTime, TrailBound, "the last soldier trails the pivot by a bounded time");
    }

    void TestDestroyAndReset()
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        NewWorld();
    }

    [Test]
    public void NoSoldierLeavesTheRadiusClampOrTheFootprint_ThroughAStartAndAStop()
    {
        float worstRadius = float.MinValue;
        float worstFootprint = float.MinValue;
        MarchAhead(12f, 9f, FrameTime, (troop, frame) =>
        {
            float radius = troop.SoldierClampRadius;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                worstRadius = Mathf.Max(worstRadius, soldier.Position.magnitude - radius);
                worstFootprint = Mathf.Max(worstFootprint,
                    troop.Footprint.SignedDistance(troop.WorldPosition(soldier)) + TroopConstants.SoldierRadius);
            }
        });
        TestContext.WriteLine($"worst: {worstRadius:F3} beyond the clamp radius, {worstFootprint:F3} beyond the footprint");
        Assert.LessOrEqual(worstRadius, 1e-3f, "inside the clamp radius");
        // The footprint is built at the start of the frame; the soldiers walk on for a frame after it.
        Assert.LessOrEqual(worstFootprint, 0.15f, "inside the footprint");
    }

    [Test]
    public void FrameRate_DoesNotChangeTheWave()
    {
        var (_, slow) = MarchAhead(12f, 3f, 1f / 30f);
        float slowPeak = PeakDepth(slow);
        TestDestroyAndReset();
        var (_, fast) = MarchAhead(12f, 3f, 1f / 120f);
        float fastPeak = PeakDepth(fast);
        TestContext.WriteLine($"peak depth {slowPeak:F3} at 30 fps, {fastPeak:F3} at 120 fps");
        Assert.AreEqual(slowPeak, fastPeak, 0.1f);
    }

    static float PeakDepth(Run run)
    {
        float peak = float.MinValue;
        for (int f = 0; f < run.Frames.Count; f++) peak = Mathf.Max(peak, run.Depth(f));
        return peak;
    }

    // Toggle off: the march is the same as before the wave. OffChecksum was captured with this scenario on the code
    // before SU-166 (commit bda437a4); the run with the wave off gives the same value to the last digit.
    [Test]
    public void Off_MatchesTheMarchBeforeTheWave()
    {
        UseTuning("_soldierWave", false);
        // No jitter: the only random draws in this march, so the result does not depend on the random streams, which
        // follow the scene hierarchy (Troop.StreamId) and so on the tests run before this one.
        UseTuning("_soldierBrownianMotion", 0f);
        var (troop, run) = MarchAhead(12f, 6f);
        Frame last = run.Frames[run.Frames.Count - 1];
        double checksum = 0;
        for (int i = 0; i < last.Positions.Length; i++)
            checksum += (i + 1) * (last.Positions[i].x * 3.0 + last.Positions[i].y * 7.0);
        TestContext.WriteLine($"checksum {checksum:R}; soldier 0 at {last.Positions[0].x:R}, {last.Positions[0].y:R}; " +
            $"soldier 31 at {last.Positions[31].x:R}, {last.Positions[31].y:R}");
        foreach (var soldier in troop.Soldiers)
        {
            Assert.AreEqual(Vector2.zero, soldier.WaveLag);
            Assert.AreEqual(Vector2.zero, soldier.WaveVelocity);
        }
        Assert.AreEqual(OffChecksum, checksum, 1e-6, "the march matches the one before the wave");
    }

    // Sum over the soldiers of (index + 1) x (3x + 7y) of their world positions after the march, before SU-166.
    const double OffChecksum = 37090.847328335047;

    // Chargers, followers, feinters and soldiers rushing back, and soldiers retreating on a schedule, are never carried
    // by the wave.
    [Test]
    public void ChargersAndRetreatingSoldiers_AreLeftOutOfTheWave()
    {
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.down, aggression: 0);
        Troop attacker = PlacedLine("Attacker", 0, new Vector2(0f, -8f), Vector2.up, aggression: 10);
        attacker.Pursue(defender);
        int committedFrames = 0;
        int retreatFrames = 0;
        bool retreated = false;
        // A soldier that begins its return during a frame was walked by the wave earlier in that frame; it is checked
        // from the next frame on.
        var outsideBefore = new HashSet<SoldierAgentState>();
        var outsideNow = new HashSet<SoldierAgentState>();
        for (int i = 0; i < Mathf.CeilToInt(8f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            if (!retreated && i * FrameTime > 5f && attacker.InMelee && attacker.EffectiveMorale > 0f)
            {
                attacker.SetShockForTests(attacker.EffectiveMorale + 0.5f);
                retreated = attacker.IsRetreating;
            }
            foreach (var troop in new[] { attacker, defender })
            {
                if (troop == null) continue;
                foreach (var soldier in troop.Soldiers)
                {
                    bool outside = soldier.IsCommitted || soldier.IsReturning || soldier.IsRetreatTracking;
                    if (!outside || !soldier.IsAlive) continue;
                    outsideNow.Add(soldier);
                    if (!outsideBefore.Contains(soldier)) continue;
                    if (soldier.IsCommitted || soldier.IsReturning) committedFrames++;
                    else retreatFrames++;
                    Assert.AreEqual(Vector2.zero, soldier.WaveLag, "no lag on a soldier outside the wave");
                    Assert.AreEqual(Vector2.zero, soldier.WaveVelocity, "no wave motion on a soldier outside the wave");
                }
            }
            (outsideBefore, outsideNow) = (outsideNow, outsideBefore);
            outsideNow.Clear();
        }
        TestContext.WriteLine($"retreated {retreated}; {committedFrames} charger-frames and {retreatFrames} retreat-frames checked");
        Assert.Greater(committedFrames, 0, "some soldiers charged");
        Assert.IsTrue(retreated, "the attacker retreated");
        Assert.Greater(retreatFrames, 0, "some soldiers retreated on the schedule");
    }
}
