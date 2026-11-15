using System.Collections.Generic;
using UnityEngine;

// The soldier wave (SU-166). When the troop's march sets off, stops or turns, the soldiers nearest the direction of
// travel respond first and each rank behind a moment later, so the troop stretches as it starts and bunches as it stops.
// The pivot itself moves exactly as before; only the soldiers trail it.
//
// Soldiers are kept in troop-local space and carried rigidly with the pivot. Each soldier taking part (alive, not charging,
// following, feinting, rushing back, retreating on a schedule or stunned; the troop not routed) is instead carried at its
// own velocity, WaveCarried, and the difference from the pivot's march is applied each frame as a counter-shift of its
// local position. WaveLag is how far that leaves it behind the place the pivot would have carried it to.
//   Delay      The march velocity is recorded in a short history. A soldier's delay is its depth behind the troop's
//              leading edge along the wave axis over SoldierWaveSpeed, at most SoldierWaveMaxLag over the pivot speed. Its
//              carried velocity follows the march velocity of that long ago: the front rank at once, each rank behind
//              later. The wave axis is the march direction, bisected with the average march over the delay window, so a
//              stop ripples from the old front and a turn from the side that led before and the side that leads after.
//   Catch-up   The lag the delay is still building up (the march travelled in the delay window, less the delayed
//              velocity over it) is the soldier's to keep; the rest is walked off with a critically damped pull
//              (CatchUpGain, tracking at TrackingRate = 4 x CatchUpGain). Its carried velocity changes no faster than its
//              acceleration and moves no faster than its top speed, each times SoldierWaveCatchUpSpeedScale, so it walks,
//              speeds up to catch up and settles without overshoot.
//   Limits     Along the wave axis a soldier lags at most SoldierWaveMaxLag and runs on past its place at most
//              SoldierWaveBunchFraction of its depth (so on a stop the front rank stops at once and each rank behind runs
//              on a little further); it brakes into either bound at its catch-up acceleration. A lag already outside a
//              bound (after the axis turned) is only allowed to shrink, never snapped.
// The field (SoldierSteering) steers a soldier from where it would stand without its lag (WaveLagLocal), so the field does
// not undo the wave. The clamp radius widens by the furthest lag (SoldierMotionConstraints), the footprint is measured
// from the soldiers as ever, and WorldVelocity adds the soldier's WaveVelocity. Everything is integrated in time, so it
// does not depend on the frame rate. Off (SoldierWave), every soldier's wave state is zero and nothing changes.
public sealed class SoldierWave
{
    // Pull (1/s) on the lag left once the wave has passed. Critically damped with TrackingRate: the lag settles to 2% in
    // about 1.5 s, without overshoot.
    const float CatchUpGain = 2f;
    // Rate (1/s) at which a soldier's carried velocity follows its target (critically damped: 4 x CatchUpGain).
    const float TrackingRate = 4f * CatchUpGain;
    // Speed (units/s) below which a velocity gives no direction for the wave axis.
    const float DirectionSpeed = 0.05f;
    // The delay cap uses at least this pivot speed (units/s).
    const float MinimumPivotSpeed = 0.1f;
    // History kept beyond the longest delay (s).
    const float HistoryMargin = 0.1f;

    struct Sample
    {
        public float Time;       // end of the frame this sample covers
        public Vector2 Velocity; // march velocity over that frame
        public Vector2 Travel;   // march travel since the history began, at Time
    }

    readonly Troop _troop;
    readonly List<Sample> _history = new();
    float _clock;
    Vector2 _lastVelocity;
    Vector2 _axis = Vector2.right;
    float _reach;
    bool _hasState;

    public SoldierWave(Troop troop)
    {
        _troop = troop;
    }

    // The furthest any soldier lags its place this frame (world units).
    public float Reach => _reach;
    // The direction the wave runs along (world unit vector): the soldiers furthest along it respond first.
    public Vector2 Axis => _axis;

    // A soldier the wave carries.
    public static bool TakesPart(SoldierAgentState soldier) =>
        soldier.IsAlive && soldier.Character != null && !soldier.IsCommitted && !soldier.IsReturning
        && !soldier.IsRetreatTracking && !soldier.IsStunned;

    // Forgets the history and every soldier's lag (deployment, respawn, rout, wave switched off).
    public void Reset()
    {
        foreach (var soldier in _troop.Roster.SoldierList) soldier.ClearWave();
        _history.Clear();
        _clock = 0f;
        _lastVelocity = Vector2.zero;
        _reach = 0f;
        _hasState = false;
    }

    // One frame, after the pivot has moved. marchVelocity is the pivot's own march this frame (world units/s), without
    // line pressure, retreat slip or rout flight.
    public void Tick(float deltaTime, Vector2 marchVelocity)
    {
        TroopTuning tuning = TroopTuning.Active;
        if (!tuning.SoldierWave || _troop.IsRouted)
        {
            if (_hasState) Reset();
            return;
        }
        if (deltaTime <= 0f) return;
        _hasState = true;

        float maxLag = tuning.SoldierWaveMaxLag;
        float maxDelay = maxLag / Mathf.Max(MinimumPivotSpeed, _troop.PivotSpeed);
        Record(deltaTime, marchVelocity);
        Prune(_clock - maxDelay - HistoryMargin);
        UpdateAxis(marchVelocity, maxDelay);

        Transform transform = _troop.transform;
        Vector2 localAxis = transform.InverseTransformDirection(_axis);
        List<SoldierAgentState> soldiers = _troop.Roster.SoldierList;
        float lead = float.NegativeInfinity;
        foreach (var soldier in soldiers)
            if (TakesPart(soldier)) lead = Mathf.Max(lead, Vector2.Dot(PlaceOf(soldier, transform), localAxis));

        _reach = 0f;
        float waveSpeed = Mathf.Max(0.01f, tuning.SoldierWaveSpeed);
        foreach (var soldier in soldiers)
        {
            if (!TakesPart(soldier))
            {
                if (soldier.WaveActive) soldier.ClearWave();
                continue;
            }
            if (!soldier.WaveActive)
            {
                soldier.WaveActive = true;
                soldier.WaveLag = Vector2.zero;
                soldier.WaveCarried = _lastVelocity;
            }
            float depth = Mathf.Max(0f, lead - Vector2.Dot(PlaceOf(soldier, transform), localAxis));
            float delay = Mathf.Min(depth / waveSpeed, maxDelay);
            float bunch = Mathf.Min(maxLag, depth * tuning.SoldierWaveBunchFraction);
            StepSoldier(soldier, transform, delay, bunch, deltaTime, marchVelocity, tuning, maxLag);
        }
        _lastVelocity = marchVelocity;
    }

    // Where the soldier would stand in troop-local space without its lag.
    static Vector2 PlaceOf(SoldierAgentState soldier, Transform transform) =>
        soldier.Position + (Vector2)transform.InverseTransformVector(soldier.WaveLag);

    void StepSoldier(SoldierAgentState soldier, Transform transform, float delay, float bunch, float deltaTime, Vector2 march,
        TroopTuning tuning, float maxLag)
    {
        float then = _clock - delay;
        Vector2 delayed = VelocityAt(then);
        // The lag the delay is still building: what the march travelled since then, less the delayed velocity over it.
        Vector2 pending = TravelAt(_clock) - TravelAt(then) - delayed * delay;
        Vector2 lag = soldier.WaveLag;
        Vector2 target = delayed + (lag - pending) * CatchUpGain;

        float scale = tuning.SoldierWaveCatchUpSpeedScale;
        float acceleration = soldier.Character.Acceleration * scale;
        float speedLimit = Mathf.Max(soldier.Character.MaxSpeed, march.magnitude) * scale;
        Vector2 carried = soldier.WaveCarried;
        Vector2 change = (target - carried) * (1f - Mathf.Exp(-TrackingRate * deltaTime));
        carried += Vector2.ClampMagnitude(change, acceleration * deltaTime);
        carried = Vector2.ClampMagnitude(carried, speedLimit);

        // Brake into the bounds along the axis: the lag grows (or the soldier runs on) no faster than it can stop in.
        float along = Vector2.Dot(lag, _axis);
        float growth = Vector2.Dot(march - carried, _axis);
        if (growth > 0f)
        {
            float allowed = Mathf.Sqrt(2f * acceleration * Mathf.Max(0f, maxLag - along));
            if (growth > allowed) carried += _axis * (growth - allowed);
        }
        else if (growth < 0f)
        {
            float allowed = Mathf.Sqrt(2f * acceleration * Mathf.Max(0f, along + bunch));
            if (-growth > allowed) carried -= _axis * (-growth - allowed);
        }

        Vector2 next = ClampLag(lag, lag + (march - carried) * deltaTime, maxLag, bunch);
        Vector2 delta = next - lag;
        Vector2 localDelta = transform.InverseTransformVector(delta);
        soldier.Position -= localDelta;
        soldier.WaveLag = next;
        soldier.WaveLagLocal = transform.InverseTransformVector(next);
        soldier.WaveCarried = march - delta / deltaTime;
        soldier.WaveVelocity = -delta / deltaTime;
        _reach = Mathf.Max(_reach, next.magnitude);
    }

    // Keeps the lag within [-bunch, maxLag] along the axis and maxLag overall; a lag already outside may only shrink.
    Vector2 ClampLag(Vector2 previous, Vector2 next, float maxLag, float bunch)
    {
        float previousAlong = Vector2.Dot(previous, _axis);
        float along = Vector2.Dot(next, _axis);
        float clamped = Mathf.Clamp(along, Mathf.Min(-bunch, previousAlong), Mathf.Max(maxLag, previousAlong));
        next += _axis * (clamped - along);
        return Vector2.ClampMagnitude(next, Mathf.Max(maxLag, previous.magnitude));
    }

    void UpdateAxis(Vector2 march, float window)
    {
        Vector2 mean = window > 0f ? (TravelAt(_clock) - TravelAt(_clock - window)) / window : march;
        Vector2 sum = Direction(march) + Direction(mean);
        if (sum.sqrMagnitude > 0.01f) _axis = sum.normalized;
        else if (march.sqrMagnitude > DirectionSpeed * DirectionSpeed) _axis = march.normalized;
    }

    static Vector2 Direction(Vector2 velocity) =>
        velocity.sqrMagnitude > DirectionSpeed * DirectionSpeed ? velocity.normalized : Vector2.zero;

    void Record(float deltaTime, Vector2 velocity)
    {
        if (_history.Count == 0)
            _history.Add(new Sample { Time = _clock, Velocity = _lastVelocity, Travel = Vector2.zero });
        Vector2 travel = _history[_history.Count - 1].Travel + velocity * deltaTime;
        _clock += deltaTime;
        _history.Add(new Sample { Time = _clock, Velocity = velocity, Travel = travel });
    }

    // Drops the samples that end before cutoff, keeping the one that covers it.
    void Prune(float cutoff)
    {
        int keepFrom = Covering(cutoff);
        if (keepFrom > 0) _history.RemoveRange(0, keepFrom);
    }

    // Index of the sample covering time: the first that ends at or after it (the last one past the end).
    int Covering(float time)
    {
        int low = 0;
        int high = _history.Count - 1;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (_history[mid].Time >= time) high = mid;
            else low = mid + 1;
        }
        return low;
    }

    Vector2 VelocityAt(float time) => _history[Covering(time)].Velocity;

    Vector2 TravelAt(float time)
    {
        Sample sample = _history[Covering(time)];
        return sample.Travel - sample.Velocity * (sample.Time - time);
    }
}
