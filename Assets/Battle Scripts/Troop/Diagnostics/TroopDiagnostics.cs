using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[System.Flags]
public enum DiagnosticCategory
{
    None = 0,
    Pivot = 1 << 0,
    LinePressure = 1 << 1,
    Barrier = 1 << 2,
    Chain = 1 << 3,
    Engagement = 1 << 4,
    Clash = 1 << 5,
    Charge = 1 << 6,
    Retreat = 1 << 7,
    Soldier = 1 << 8,
}

// Opt-in diagnostics for the troop movement and combat systems. Off by default.
// Systems only call the static Record*/Wants/SampleDue methods. Each returns immediately when diagnostics are disabled,
// the category is off, or the troop is filtered out. Systems never read anything back from this class.
// Output: one CSV per category in Application.persistentDataPath/Diagnostics. Every row starts with Time.time and
// Time.frameCount, then an event name (sample, or an event such as start/end/outcome).
[DisallowMultipleComponent]
public sealed class TroopDiagnostics : MonoBehaviour
{
    public enum TroopFilter { All, Selected, Explicit }

    const DiagnosticCategory AllCategories = (DiagnosticCategory)0x1FF;
    const int CategoryCount = 9;
    const float FlushInterval = 2f;
    const float MarkerLifetime = 1f;
    const float RegistryRefreshInterval = 0.5f;
    const int MaxBufferChars = 1 << 16;
    const float RetreatDirectionEpsilon = 0.00001f;

    static readonly string[] CategoryNames =
    {
        "pivot", "line_pressure", "barrier", "chain", "engagement", "clash", "charge", "retreat", "soldier"
    };

    static readonly string[] Headers =
    {
        "time,frame,event,troop,troopId,state,posX,posY,pivotVelX,pivotVelY,linePushX,linePushY,hasDestination,pursuitTarget,dispX,dispY,hasOrder",
        "time,frame,event,troopA,troopIdA,troopB,troopIdB,centreDistance,halfWidthA,halfWidthB,footprintDistance,compression,driveA,driveB,effortA,effortB,forceA,forceB",
        "time,frame,event,troopA,troopIdA,troopB,troopIdB,axisX,axisY,penetration,advanceA,advanceB,shareA,shareB,restore,pushA,pushB",
        "time,frame,event,troopA,troopIdA,endA,troopB,troopIdB,endB,reason,tensionX,tensionY,tensionForce,facingAX,facingAY,facingBX,facingBY,orderA,orderB",
        "time,frame,event,troop,troopId,enemy,enemyId,segmentDistance,limitKind",
        "time,frame,event,troopA,troopIdA,soldierA,troopB,troopIdB,soldierB,soldierDistance,belowContact,initiator,pressureA,pressureB,winner,pressureMargin,damage,counted,chargingA,chargingB,lungingA,lungingB,pulseLockedA,pulseLockedB",
        "time,frame,event,sourceTroop,sourceTroopId,sourceSoldier,targetTroop,targetTroopId,targetSoldier,distance,duration,reason,distanceLeft,clashDuring",
        "time,frame,event,troop,troopId,soldier,axisX,axisY,distance,pivotDistance,delay,moveDuration,retreatScale,offset,localX,localY,reversals,settleTime",
        "time,frame,event,troop,troopId,soldier,posX,posY,velX,velY,drive,committed,charging,lunging,seekX,seekY",
    };

    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    static readonly char[] CsvSpecialChars = { ',', '"', '\n', '\r' };
    static readonly StringBuilder Row = new(256);
    static int _fields;

    [SerializeField] bool _enabled;
    [SerializeField] DiagnosticCategory _categories = AllCategories;
    [SerializeField] TroopFilter _troopFilter = TroopFilter.All;
    [SerializeField] List<Troop> _explicitTroops = new();
    [SerializeField, Min(0f)] float _sampleInterval;
    [SerializeField] bool _useSeed;
    [SerializeField] int _seed;
    [SerializeField] bool _showOverlay = true;
    [SerializeField] bool _drawGizmos = true;

    static TroopDiagnostics _instance;

    readonly Channel[] _channels = new Channel[CategoryCount];
    readonly Dictionary<Troop, Troop> _engagedEnemies = new();
    readonly Dictionary<Troop, float> _barrierShares = new();
    readonly Dictionary<SoldierAgentState, ChargeRecord> _charges = new();
    readonly Dictionary<SoldierAgentState, RetreatTrack> _retreats = new();
    readonly List<Marker> _markers = new();
    readonly List<Troop> _troopCache = new();
    readonly List<Troop> _deadTroops = new();
    string _sceneName = "";
    string _sessionStamp = "";
    string _directory;
    float _flushTimer;
    float _registryTimer;

    sealed class Channel
    {
        public StreamWriter Writer;
        public readonly StringBuilder Buffer = new(8192);
        public float NextSample;
        public int CheckedFrame = -1;
        public bool Due;
        public bool Failed;
        public int Columns;
    }

    sealed class ChargeRecord
    {
        public Troop SourceTroop;
        public SoldierAgentState Source;
        public Troop TargetTroop;
        public SoldierAgentState Target;
        public bool ClashDuring;
    }

    sealed class RetreatTrack
    {
        public int LastDirection;
        public int Reversals;
    }

    struct Marker
    {
        public Vector3 A;
        public Vector3 B;
        public bool IsPoint;
        public Color Color;
        public float Time;
    }

    // --- Lifecycle ---

    void Awake()
    {
        for (int i = 0; i < CategoryCount; i++)
        {
            _channels[i] = new Channel { Columns = CountColumns(Headers[i]) };
        }
        _sceneName = SceneManager.GetActiveScene().name;
        _sessionStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", Invariant);
        if (_useSeed) Random.InitState(_seed);
    }

    void OnEnable()
    {
        if (_instance != null && _instance != this)
            Debug.LogWarning("TroopDiagnostics: more than one instance in the scene; the newest one is used.");
        _instance = this;
    }

    void OnDisable()
    {
        CloseAll();
        if (_instance == this) _instance = null;
    }

    void OnApplicationQuit()
    {
        CloseAll();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            _showOverlay = !_showOverlay;

        _flushTimer += Time.unscaledDeltaTime;
        if (_flushTimer >= FlushInterval)
        {
            _flushTimer = 0f;
            FlushAll();
        }

        _registryTimer += Time.unscaledDeltaTime;
        if (_registryTimer >= RegistryRefreshInterval)
        {
            _registryTimer = 0f;
            RefreshRegistry();
        }

        for (int i = _markers.Count - 1; i >= 0; i--)
            if (Time.time - _markers[i].Time > MarkerLifetime) _markers.RemoveAt(i);
    }

    void RefreshRegistry()
    {
        _troopCache.Clear();
        _troopCache.AddRange(FindObjectsByType<Troop>(FindObjectsSortMode.None));

        _deadTroops.Clear();
        foreach (var pair in _engagedEnemies)
            if (pair.Key == null || pair.Value == null) _deadTroops.Add(pair.Key);
        foreach (var troop in _deadTroops) _engagedEnemies.Remove(troop);

        _deadTroops.Clear();
        foreach (var pair in _barrierShares)
            if (pair.Key == null) _deadTroops.Add(pair.Key);
        foreach (var troop in _deadTroops) _barrierShares.Remove(troop);
    }

    // --- Gating (called from hooks) ---

    public static bool IsEnabled => _instance != null && _instance._enabled;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled) CloseAll();
    }

    // True when the category is on and the troop passes the troop filter.
    public static bool Wants(DiagnosticCategory category, Troop troop)
    {
        TroopDiagnostics d = _instance;
        if (d == null || !d._enabled || troop == null || (d._categories & category) == 0) return false;
        return d.Passes(troop);
    }

    public static bool WantsPair(DiagnosticCategory category, Troop a, Troop b) =>
        Wants(category, a) || Wants(category, b);

    // Sampling is decided once per category per frame, so every troop in that frame is sampled together.
    public static bool SampleDue(DiagnosticCategory category)
    {
        TroopDiagnostics d = _instance;
        if (d == null || !d._enabled) return false;
        Channel channel = d._channels[IndexOf(category)];
        if (channel.CheckedFrame == Time.frameCount) return channel.Due;

        channel.CheckedFrame = Time.frameCount;
        channel.Due = d._sampleInterval <= 0f || Time.time >= channel.NextSample;
        if (channel.Due && d._sampleInterval > 0f) channel.NextSample = Time.time + d._sampleInterval;
        return channel.Due;
    }

    static bool CategoryOn(DiagnosticCategory category)
    {
        TroopDiagnostics d = _instance;
        return d != null && d._enabled && (d._categories & category) != 0;
    }

    bool Passes(Troop troop)
    {
        switch (_troopFilter)
        {
            case TroopFilter.Selected: return troop.IsSelected;
            case TroopFilter.Explicit: return _explicitTroops != null && _explicitTroops.Contains(troop);
            default: return true;
        }
    }

    // --- Pivot ---

    public static void RecordPivot(Troop troop, bool hasDestination, Troop pursuitTarget, Vector2 pivotVelocity,
        Vector2 linePushVelocity, Vector2 displacement)
    {
        if (!Wants(DiagnosticCategory.Pivot, troop) || !SampleDue(DiagnosticCategory.Pivot)) return;
        Begin("sample");
        Text(troop.name); Int(troop.GetInstanceID()); Text(troop.State.ToString());
        Num(troop.transform.position.x); Num(troop.transform.position.y);
        Num(pivotVelocity.x); Num(pivotVelocity.y);
        Num(linePushVelocity.x); Num(linePushVelocity.y);
        Flag(hasDestination); Text(NameOf(pursuitTarget));
        Num(displacement.x); Num(displacement.y);
        Flag(troop.HasManualMoveOrder);
        Commit(DiagnosticCategory.Pivot);
    }

    // --- Line pressure and barrier ---

    public static void RecordLinePressure(Troop first, Troop second, float centreDistance, float halfWidthFirst,
        float halfWidthSecond, float footprintDistance, float compression, float driveFirst, float driveSecond,
        float effortFirst, float effortSecond, float forceFirst, float forceSecond)
    {
        if (!WantsPair(DiagnosticCategory.LinePressure, first, second) || !SampleDue(DiagnosticCategory.LinePressure)) return;
        Begin("sample");
        Text(NameOf(first)); Int(IdOf(first)); Text(NameOf(second)); Int(IdOf(second));
        Num(centreDistance); Num(halfWidthFirst); Num(halfWidthSecond); Num(footprintDistance); Num(compression);
        Num(driveFirst); Num(driveSecond); Num(effortFirst); Num(effortSecond); Num(forceFirst); Num(forceSecond);
        Commit(DiagnosticCategory.LinePressure);
    }

    // Also records the latest barrier share per troop for the overlay, whenever barrier diagnostics are on.
    public static void RecordBarrier(Troop first, Troop second, Vector2 axis, float penetration, float firstAdvance,
        float secondAdvance, float firstShare, float secondShare, float restore, float firstPush, float secondPush)
    {
        TroopDiagnostics d = _instance;
        if (!CategoryOn(DiagnosticCategory.Barrier)) return;
        if (first != null) d._barrierShares[first] = firstShare;
        if (second != null) d._barrierShares[second] = secondShare;

        if (!WantsPair(DiagnosticCategory.Barrier, first, second)) return;
        if (first != null && second != null)
            d._markers.Add(new Marker
            {
                A = first.transform.position,
                B = second.transform.position,
                Color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(penetration / 0.3f)),
                Time = Time.time,
            });
        if (!SampleDue(DiagnosticCategory.Barrier)) return;
        Begin("sample");
        Text(NameOf(first)); Int(IdOf(first)); Text(NameOf(second)); Int(IdOf(second));
        Num(axis.x); Num(axis.y); Num(penetration); Num(firstAdvance); Num(secondAdvance);
        Num(firstShare); Num(secondShare); Num(restore); Num(firstPush); Num(secondPush);
        Commit(DiagnosticCategory.Barrier);
    }

    // --- Chain ---

    public static void RecordLinkCreated(Troop a, int endA, Troop b, int endB, float distance)
    {
        if (!WantsPair(DiagnosticCategory.Chain, a, b)) return;
        Begin("link_created");
        Text(NameOf(a)); Int(IdOf(a)); Int(endA); Text(NameOf(b)); Int(IdOf(b)); Int(endB);
        Text("available ends, distance " + distance.ToString("F2", Invariant));
        Commit(DiagnosticCategory.Chain);
    }

    public static void RecordLinkCleared(Troop troop, int end, Troop other, int otherEnd, string reason)
    {
        if (!WantsPair(DiagnosticCategory.Chain, troop, other)) return;
        Begin("link_cleared");
        Text(NameOf(troop)); Int(IdOf(troop)); Int(end); Text(NameOf(other)); Int(IdOf(other)); Int(otherEnd);
        Text(reason);
        Commit(DiagnosticCategory.Chain);
    }

    // Sampled from AlliedLinkTension: the spring tension on one link end and both troops' facing and orders.
    public static void RecordTension(Troop troop, int end, Troop other, int otherEnd, Vector2 tension, float springForce)
    {
        if (!WantsPair(DiagnosticCategory.Chain, troop, other) || !SampleDue(DiagnosticCategory.Chain)) return;
        Begin("tension");
        Text(NameOf(troop)); Int(IdOf(troop)); Int(end); Text(NameOf(other)); Int(IdOf(other)); Int(otherEnd);
        Text("");
        Num(tension.x); Num(tension.y); Num(springForce);
        Vector2 facingA = FacingOf(troop);
        Vector2 facingB = FacingOf(other);
        Num(facingA.x); Num(facingA.y); Num(facingB.x); Num(facingB.y);
        Flag(troop.HasManualMoveOrder); Flag(other != null && other.HasManualMoveOrder);
        Commit(DiagnosticCategory.Chain);
    }

    // --- Engagement ---

    // Called for every troop after the engagement search. Tracks changes for all troops (so transitions are never
    // missed), and logs events and samples only for the troops that pass the filter.
    public static void RecordEngagement(Troop troop, Troop enemy)
    {
        TroopDiagnostics d = _instance;
        if (!CategoryOn(DiagnosticCategory.Engagement) || troop == null) return;

        d._engagedEnemies.TryGetValue(troop, out Troop previous);
        bool changed = previous != enemy;
        if (enemy != null) d._engagedEnemies[troop] = enemy;
        else d._engagedEnemies.Remove(troop);

        if (!Wants(DiagnosticCategory.Engagement, troop)) return;
        if (!changed && !SampleDue(DiagnosticCategory.Engagement)) return;

        float distance = float.NaN;
        if (enemy != null && troop.Formation != null && enemy.Formation != null)
            SegmentGeometry.ClosestSegmentPoints(troop.Formation, enemy.Formation, out _, out _, out distance);
        string limit = enemy != null && enemy == troop.EngagedEnemy ? "hysteresis" : "normal";
        string eventName = !changed ? "sample" : enemy == null ? "disengaged" : previous == null ? "engaged" : "changed";

        Begin(eventName);
        Text(NameOf(troop)); Int(IdOf(troop)); Text(NameOf(enemy)); Int(IdOf(enemy));
        Num(distance); Text(enemy != null ? limit : "");
        Commit(DiagnosticCategory.Engagement);
    }

    // --- Clash ---

    public static void RecordClash(SoldierContact a, SoldierContact b, Vector2 positionA, Vector2 positionB,
        bool aInitiated, float pressureA, float pressureB)
    {
        if (!CategoryOn(DiagnosticCategory.Clash)) return;

        // A clash is always counted against any charge that is in progress, regardless of filter.
        if (_instance._charges.TryGetValue(a.Soldier, out ChargeRecord chargeA)) chargeA.ClashDuring = true;
        if (_instance._charges.TryGetValue(b.Soldier, out ChargeRecord chargeB)) chargeB.ClashDuring = true;

        if (!WantsPair(DiagnosticCategory.Clash, a.Troop, b.Troop)) return;
        float distance = Vector2.Distance(positionA, positionB);
        Begin("start");
        Text(NameOf(a.Troop)); Int(IdOf(a.Troop)); Int(a.Soldier.Id);
        Text(NameOf(b.Troop)); Int(IdOf(b.Troop)); Int(b.Soldier.Id);
        Num(distance);
        Flag(distance < SoldierContactResolver.SoldierContactDistance);
        Text(aInitiated ? "A" : "B");
        Num(pressureA); Num(pressureB);
        Text(pressureA >= pressureB ? "A" : "B");
        Num(Mathf.Abs(pressureA - pressureB));
        To(18);
        Flag(a.Soldier.IsCharging); Flag(b.Soldier.IsCharging);
        Flag(a.Soldier.Melee.IsLunging); Flag(b.Soldier.Melee.IsLunging);
        Flag(a.Troop.IsPulseLocked); Flag(b.Troop.IsPulseLocked);
        Commit(DiagnosticCategory.Clash);
    }

    // Logged after ClashResolver.ResolveWinner. troopA is the winner and troopB the loser in this row.
    public static void RecordClashOutcome(SoldierContact winner, SoldierContact loser, float pressureMargin,
        float damage, bool counted)
    {
        if (!WantsPair(DiagnosticCategory.Clash, winner.Troop, loser.Troop)) return;
        Begin("outcome");
        Text(NameOf(winner.Troop)); Int(IdOf(winner.Troop)); Int(winner.Soldier.Id);
        Text(NameOf(loser.Troop)); Int(IdOf(loser.Troop)); Int(loser.Soldier.Id);
        To(15);
        Num(pressureMargin); Num(damage); Flag(counted);
        Commit(DiagnosticCategory.Clash);
    }

    // --- Charge ---

    public static void RecordChargeStart(Troop sourceTroop, SoldierAgentState source, Troop targetTroop,
        SoldierAgentState target, float distance, float duration)
    {
        if (!CategoryOn(DiagnosticCategory.Charge)) return;
        _instance._charges[source] = new ChargeRecord
        {
            SourceTroop = sourceTroop,
            Source = source,
            TargetTroop = targetTroop,
            Target = target,
        };

        if (!WantsPair(DiagnosticCategory.Charge, sourceTroop, targetTroop)) return;
        Begin("start");
        Text(NameOf(sourceTroop)); Int(IdOf(sourceTroop)); Int(source.Id);
        Text(NameOf(targetTroop)); Int(IdOf(targetTroop)); Int(target.Id);
        Num(distance); Num(duration);
        Commit(DiagnosticCategory.Charge);
    }

    // distanceLeft is NaN when it does not apply (for example an invalid target).
    public static void RecordChargeEnd(Troop troop, SoldierAgentState source, string reason, float distanceLeft)
    {
        if (!CategoryOn(DiagnosticCategory.Charge)) return;
        if (!_instance._charges.TryGetValue(source, out ChargeRecord record)) return;
        _instance._charges.Remove(source);

        if (!WantsPair(DiagnosticCategory.Charge, record.SourceTroop, record.TargetTroop)) return;
        Begin("end");
        Text(NameOf(record.SourceTroop)); Int(IdOf(record.SourceTroop)); Int(record.Source.Id);
        Text(NameOf(record.TargetTroop)); Int(IdOf(record.TargetTroop)); Int(record.Target != null ? record.Target.Id : 0);
        To(11);
        Text(reason); Num(distanceLeft); Flag(record.ClashDuring);
        Commit(DiagnosticCategory.Charge);
    }

    // --- Retreat ---

    public static void RecordRetreatBegin(Troop troop, Vector2 axis, float distance, float pivotDistance,
        IReadOnlyList<SoldierAgentState> soldiers)
    {
        TroopDiagnostics d = _instance;
        if (!CategoryOn(DiagnosticCategory.Retreat)) return;
        for (int i = 0; i < soldiers.Count; i++) d._retreats[soldiers[i]] = new RetreatTrack();
        if (!Wants(DiagnosticCategory.Retreat, troop)) return;

        Begin("begin");
        Text(NameOf(troop)); Int(IdOf(troop));
        To(6);
        Num(axis.x); Num(axis.y); Num(distance); Num(pivotDistance);
        Commit(DiagnosticCategory.Retreat);

        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierAgentState soldier = soldiers[i];
            Begin("soldier_begin");
            Text(NameOf(troop)); Int(IdOf(troop)); Int(soldier.Id);
            To(10);
            Num(soldier.FaultDelayRemaining); Num(soldier.FaultMoveDuration); Num(soldier.FaultRetreatScale);
            Commit(DiagnosticCategory.Retreat);
        }
    }

    // Runs every frame to count direction reversals. Writes a sample row only when one is due.
    public static void RecordRetreatStep(Troop troop, SoldierAgentState soldier, float previousOffset, float offset)
    {
        TroopDiagnostics d = _instance;
        if (!CategoryOn(DiagnosticCategory.Retreat)) return;

        if (!d._retreats.TryGetValue(soldier, out RetreatTrack track))
            d._retreats[soldier] = track = new RetreatTrack();
        float step = offset - previousOffset;
        int direction = step > RetreatDirectionEpsilon ? 1 : step < -RetreatDirectionEpsilon ? -1 : 0;
        if (direction != 0)
        {
            if (track.LastDirection != 0 && direction != track.LastDirection) track.Reversals++;
            track.LastDirection = direction;
        }

        if (!Wants(DiagnosticCategory.Retreat, troop) || !SampleDue(DiagnosticCategory.Retreat)) return;
        Begin("sample");
        Text(NameOf(troop)); Int(IdOf(troop)); Int(soldier.Id);
        To(13);
        Num(offset); Num(soldier.Position.x); Num(soldier.Position.y); Int(track.Reversals);
        Commit(DiagnosticCategory.Retreat);
    }

    public static void RecordRetreatEnd(Troop troop, float settleTime)
    {
        if (!Wants(DiagnosticCategory.Retreat, troop)) return;
        Begin("end");
        Text(NameOf(troop)); Int(IdOf(troop));
        To(17);
        Num(settleTime);
        Commit(DiagnosticCategory.Retreat);
    }

    // --- Soldier (selected troops only) ---

    public static void RecordSoldiers(Troop troop, List<SoldierAgentState> soldiers, List<bool> seeks, List<Vector2> seekPoints)
    {
        if (troop == null || !troop.IsSelected || !Wants(DiagnosticCategory.Soldier, troop)) return;
        if (!SampleDue(DiagnosticCategory.Soldier)) return;

        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierAgentState soldier = soldiers[i];
            if (!soldier.IsAlive) continue;
            Vector2 world = troop.WorldPosition(soldier);
            Begin("sample");
            Text(NameOf(troop)); Int(IdOf(troop)); Int(soldier.Id);
            Num(world.x); Num(world.y);
            Num(soldier.Velocity.x); Num(soldier.Velocity.y);
            Num(soldier.DriveFraction);
            Flag(soldier.IsCommitted); Flag(soldier.IsCharging); Flag(soldier.Melee.IsLunging);
            To(14);
            if (seeks[i])
            {
                Num(seekPoints[i].x); Num(seekPoints[i].y);
            }
            Commit(DiagnosticCategory.Soldier);
        }
    }

    // --- Overlay and gizmos ---

    void OnGUI()
    {
        if (!_enabled || !_showOverlay) return;

        var text = new StringBuilder();
        int shown = 0;
        foreach (var troop in _troopCache)
        {
            if (troop == null || !troop.IsSelected) continue;
            int charging = 0;
            foreach (var soldier in troop.Soldiers)
                if (soldier.IsAlive && soldier.IsCharging) charging++;
            _barrierShares.TryGetValue(troop, out float share);

            text.Append(troop.name).Append(" #").Append(troop.GetInstanceID()).Append("  ").Append(troop.State).Append('\n');
            text.Append("  pivot v (").Append(Fmt(troop.PivotVelocity.x)).Append(", ").Append(Fmt(troop.PivotVelocity.y))
                .Append(")  push v (").Append(Fmt(troop.LinePushVelocity.x)).Append(", ").Append(Fmt(troop.LinePushVelocity.y)).Append(")\n");
            text.Append("  engaged ").Append(NameOf(troop.EngagedEnemy))
                .Append("  barrier share ").Append(_barrierShares.ContainsKey(troop) ? Fmt(share) : "-").Append('\n');
            text.Append("  links ").Append(troop.LineLinks.Count).Append("  charging ").Append(charging)
                .Append("  shock ").Append(Fmt(troop.Shock)).Append("  fault ").Append(Fmt(troop.FaultPressure)).Append('\n');
            shown++;
        }

        if (shown == 0) text.Append("TroopDiagnostics: select a troop to see live values.");
        GUI.Box(new Rect(10f, 10f, 420f, 20f + 60f * Mathf.Max(1, shown)), text.ToString());
    }

    void OnDrawGizmos()
    {
        if (!_enabled || !_drawGizmos) return;

        foreach (var troop in _troopCache)
        {
            if (troop == null || !Passes(troop)) continue;
            Vector3 pivot = troop.transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivot, pivot + (Vector3)troop.PivotVelocity * 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pivot, pivot + (Vector3)troop.LinePushVelocity * 0.5f);

            for (int end = 0; end < 2; end++)
            {
                TroopEndpointLink link = troop.GetLineLink(end);
                if (link == null) continue;
                Vector3 joint = troop.SegmentEndpoint(end);
                Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(link.Tension.magnitude / 1.5f));
                Gizmos.DrawWireSphere(joint, 0.15f);
            }
        }

        Gizmos.color = Color.magenta;
        foreach (var pair in _engagedEnemies)
        {
            if (pair.Key == null || pair.Value == null) continue;
            Gizmos.DrawLine(pair.Key.transform.position, pair.Value.transform.position);
        }

        Gizmos.color = Color.red;
        foreach (var pair in _charges)
        {
            ChargeRecord record = pair.Value;
            if (record.SourceTroop == null || record.TargetTroop == null) continue;
            Gizmos.DrawLine(record.SourceTroop.WorldPosition(record.Source), record.TargetTroop.WorldPosition(record.Target));
        }

        foreach (var marker in _markers)
        {
            float age = (Time.time - marker.Time) / MarkerLifetime;
            Color color = marker.Color;
            color.a = 1f - Mathf.Clamp01(age);
            Gizmos.color = color;
            if (marker.IsPoint) Gizmos.DrawWireSphere(marker.A, 0.25f + 0.5f * age);
            else Gizmos.DrawLine(marker.A, marker.B);
        }
    }

    // --- Output ---

    void OnValidate()
    {
        if (_sampleInterval < 0f) _sampleInterval = 0f;
    }

    void FlushAll()
    {
        for (int i = 0; i < CategoryCount; i++) Flush(i);
    }

    void CloseAll()
    {
        FlushAll();
        for (int i = 0; i < CategoryCount; i++)
        {
            Channel channel = _channels[i];
            if (channel == null || channel.Writer == null) continue;
            channel.Writer.Dispose();
            channel.Writer = null;
        }
    }

    void Flush(int index)
    {
        Channel channel = _channels[index];
        if (channel == null || channel.Writer == null || channel.Buffer.Length == 0) return;
        channel.Writer.Write(channel.Buffer.ToString());
        channel.Buffer.Clear();
        channel.Writer.Flush();
    }

    bool Open(int index)
    {
        Channel channel = _channels[index];
        if (channel.Failed) return false;
        try
        {
            if (_directory == null)
            {
                _directory = Path.Combine(Application.persistentDataPath, "Diagnostics");
                Directory.CreateDirectory(_directory);
            }
            string path = Path.Combine(_directory, _sceneName + "_" + _sessionStamp + "_" + CategoryNames[index] + ".csv");
            bool exists = File.Exists(path);
            channel.Writer = new StreamWriter(path, true, new UTF8Encoding(false));
            if (!exists) channel.Writer.WriteLine(Headers[index]);
            return true;
        }
        catch (System.Exception e)
        {
            channel.Failed = true;
            Debug.LogError("TroopDiagnostics: could not open " + CategoryNames[index] + " log: " + e.Message);
            return false;
        }
    }

    // --- Row building (only runs when a row is actually written) ---

    static void Begin(string eventName)
    {
        Row.Clear();
        Row.Append(Time.time.ToString("F3", Invariant)).Append(',')
           .Append(Time.frameCount.ToString(Invariant)).Append(',')
           .Append(eventName);
        _fields = 3;
    }

    static void Text(string value)
    {
        Row.Append(',');
        _fields++;
        if (string.IsNullOrEmpty(value)) return;
        if (value.IndexOfAny(CsvSpecialChars) >= 0)
            Row.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        else
            Row.Append(value);
    }

    static void Num(float value)
    {
        Row.Append(',');
        _fields++;
        if (float.IsFinite(value)) Row.Append(value.ToString("G6", Invariant));
    }

    static void Int(int value)
    {
        Row.Append(',').Append(value.ToString(Invariant));
        _fields++;
    }

    static void Flag(bool value)
    {
        Row.Append(',').Append(value ? '1' : '0');
        _fields++;
    }

    // Pads with empty fields until the row has the given number of fields.
    static void To(int fieldCount)
    {
        while (_fields < fieldCount) Text(null);
    }

    static void Commit(DiagnosticCategory category)
    {
        TroopDiagnostics d = _instance;
        if (d == null) return;
        int index = IndexOf(category);
        Channel channel = d._channels[index];
        if (channel.Writer == null && !d.Open(index)) return;

        To(channel.Columns);
        channel.Buffer.Append(Row).Append('\n');
        if (channel.Buffer.Length >= MaxBufferChars) d.Flush(index);
    }

    static int CountColumns(string header)
    {
        int count = 1;
        foreach (char c in header) if (c == ',') count++;
        return count;
    }

    static int IndexOf(DiagnosticCategory category)
    {
        int index = 0;
        int value = (int)category;
        while ((value >>= 1) != 0) index++;
        return index;
    }

    static string NameOf(Troop troop) => troop != null ? troop.name : "";
    static int IdOf(Troop troop) => troop != null ? troop.GetInstanceID() : 0;
    static Vector2 FacingOf(Troop troop) =>
        troop == null ? Vector2.zero
        : troop.Formation != null ? troop.Formation.Facing
        : (Vector2)troop.transform.right;
    static string Fmt(float value) => value.ToString("F2", Invariant);
}
