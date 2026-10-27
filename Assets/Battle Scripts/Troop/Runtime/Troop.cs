using System;
using System.Collections.Generic;
using StatSystem.Refactor;
using UnityEngine;
using UnityEngine.Serialization;

// Aggregator for one troop of soldiers. It owns the identity and configuration of the troop and composes the modules
// that do the work, running them in a fixed order each frame (see Update):
//   MonoBehaviours (toggle in the Inspector): TroopRoster, TroopFormationState, TroopMorale
//   Plain classes: TroopPivotMover, TroopPathFollower, FaultSlipMotion, TroopRout, TroopLineOrder, TroopLineLinks,
//                  TroopDamageReceiver, AssaultState, RangedFireController, SoldierSteering, SoldierMotionConstraints,
//                  TroopSoldierViews
// Battle-wide systems (engagement, chains, pressure, charges, clashes, ranged fire) run in TroopCombatWorld.
[RequireComponent(typeof(TroopRoster), typeof(TroopFormationState), typeof(TroopMorale))]
public sealed class Troop : IUnit
{
    [SerializeField] IUnitStatBlock _legacyStats;
    [SerializeField] UnitState _state = UnitState.Idle;
    [SerializeField] int _teamId;
    [SerializeField] TroopProfile _profile;
    [SerializeField, HideInInspector, Min(0.1f), FormerlySerializedAs("_flockRadius")] float _troopRadius = 16.5f;
    [SerializeField, FormerlySerializedAs("_boidObstacleMask")] LayerMask _soldierObstacleMask;
    [SerializeField, Min(1f)] float _meleeMetricsWindow = MeleeMetrics.DefaultWindow;

    TroopRoster _roster;
    MeleeMetrics _meleeMetrics;
    TroopFormationState _formationState;
    TroopMorale _morale;
    readonly TroopLineLinks _lineLinks = new();
    readonly List<EngagementRecord> _engagements = new();
    readonly AssaultState _assault = new();
    TroopLineOrder _lineOrder;
    TroopPathFollower _path;
    TroopPivotMover _mover;
    FaultSlipMotion _faultSlip;
    TroopRout _rout;
    TroopDamageReceiver _damage;
    RangedFireController _rangedFire;
    SoldierMotionConstraints _constraints;
    SoldierSteering _steering;
    TroopSoldierViews _views;

    Army _army;
    TroopVisualiser _visualiser;
    bool _modulesReady;
    bool _initialized;
    bool _inMelee;
    bool _engaged;
    bool _selected;
    bool _hovered;
    bool _chainSuppressed;
    float _chainSuppressOutsideTime;
    FormationMode _formationMode;
    TroopFootprint _footprint;

    public event Action<SoldierAgentState> SoldierDied;

    // Raised once when the troop routs (spec §5.4). The legacy IUnit destroyed event is raised at the same time.
    public event Action<Troop> Routed;

    // --- Randomness ---

    ITroopRandom _random;
    ITroopRandom _unseededRandom;

    // This troop's seeded stream for the current battle. Before a battle exists (no TroopCombatWorld) it falls back to an
    // unseeded stream, which is not cached so a later battle still gets its seeded stream.
    public ITroopRandom TroopRandom
    {
        get
        {
            if (_random != null) return _random;
            var world = TroopCombatWorld.Current;
            if (world == null) return _unseededRandom ??= new SeededTroopRandom(Environment.TickCount);
            return _random = world.RandomFor(StreamId);
        }
    }

    // Stable id for this troop's random stream: its position in the hierarchy (sibling indices and names). Reordering
    // the hierarchy changes the stream.
    int StreamId
    {
        get
        {
            string path = string.Empty;
            for (Transform t = transform; t != null; t = t.parent)
                path = "/" + t.GetSiblingIndex() + ":" + t.name + path;
            return SeededTroopRandom.HashText(path);
        }
    }

    // --- Identity and configuration ---

    public override IUnitStatBlock Stats
    {
        get => _legacyStats;
        set => _legacyStats = value;
    }

    public override UnitState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            ChangeState(value);
        }
    }

    public override bool InMelee => _inMelee;
    public int TeamId => _teamId;
    public TroopProfile Profile => _profile;
    public float Radius => Mathf.Max(0.1f, _profile != null ? _profile.TroopRadius : _troopRadius);
    public Color ArmyColor => _army != null ? _army.ArmyColour : Color.white;
    static TroopTuning Tuning => TroopTuning.Active;

    // Natural line length L_nat (spec §3.3). Independent of the living soldier count.
    public float NaturalLineLength => Tuning.NaturalLineLength(Radius);
    // Shortest and longest line a troop may hold (spec §3.4 stretch envelope, §6.2).
    public float MinimumLineLength => NaturalLineLength * Tuning.ChainMinStretchScale;
    public float MaximumLineLength => NaturalLineLength * Tuning.ChainMaxStretchScale;
    // Length of the line the troop uses now: the ordered length while a line order holds, otherwise L_nat.
    public float LineLength => _lineOrder != null && _lineOrder.IsActive ? _lineOrder.Length : NaturalLineLength;

    // Troop stats (spec §1.4). Move is the lowest soldier Speed; Discipline is the troop-level value.
    public int Move
    {
        get
        {
            int move = _roster != null ? _roster.MinProfileValue(profile => profile.Speed) : 0;
            return move > 0 ? move : CombatStatLimits.Default;
        }
    }
    public int Discipline => Profile != null ? Profile.Discipline : CombatStatLimits.Default;

    public bool IsSelected => _selected;
    public bool IsHovered => _hovered;

    // --- Modules ---

    public TroopRoster Roster => _roster;
    public TroopFormationState FormationState => _formationState;
    public TroopMorale MoraleState => _morale;
    internal TroopLineOrder LineOrder => _lineOrder;
    internal TroopLineLinks LineLinkState => _lineLinks;
    public RangedFireController RangedFire => _rangedFire;

    // --- Soldiers (TroopRoster) ---

    public IReadOnlyList<SoldierAgentState> Soldiers => _roster != null ? _roster.Soldiers : Array.Empty<SoldierAgentState>();
    public int SoldierCount => _roster != null ? _roster.Count : 0;
    public int LivingSoldierCount => _roster != null ? _roster.LivingCount : 0;
    public float AverageProfileValue(Func<SoldierProfile, float> valueSelector) => _roster.AverageProfileValue(valueSelector);
    public float DistanceToNearestSoldier(Vector2 worldPosition) => _roster.DistanceToNearestSoldier(worldPosition);
    public bool HasLivingSoldierInRect(Rect worldRect) => _roster.HasLivingSoldierInRect(worldRect);
    public Vector2 WorldPosition(SoldierAgentState soldier) => _roster.WorldPosition(soldier);
    public Vector2 WorldVelocity(SoldierAgentState soldier) => _roster.WorldVelocity(soldier);
    public void ResolveContact(SoldierAgentState soldier, Vector2 worldPositionDelta, Vector2 worldVelocityDelta) =>
        _roster.ResolveContact(soldier, worldPositionDelta, worldVelocityDelta);
    public void ConstrainSoldiersToRadius() => _constraints.ConstrainAllToRadius();

    // --- Morale (TroopMorale, spec §5) ---

    public float EffectiveMorale => _morale.EffectiveMorale;
    public float InitialEffectiveMorale => _morale.InitialEffectiveMorale;
    public float Shock => _morale.Shock;
    public bool IsFaultSlipping => _faultSlip != null && _faultSlip.IsActive;
    // Retreating after a break (not a sympathetic chain retreat).
    public bool IsRetreating => IsFaultSlipping && !_faultSlip.IsSympathetic;
    public bool IsRouted => _rout != null && _rout.IsRouted;
    public Vector2 RoutFleeDirection => _rout != null ? _rout.FleeDirection : Vector2.zero;
    internal Vector2 FaultSlipAxis => _faultSlip.Axis;

    // --- Formation (TroopFormationState) and line orders ---

    public FormationMode FormationMode => _formationMode;

    public TroopFormationData Formation
    {
        get => _formationState != null ? _formationState.Formation : null;
        internal set => _formationState.Formation = value;
    }
    public TroopFormationData NaturalFormation => _formationState != null ? _formationState.NaturalFormation : null;
    public bool HasManualLineOrder => _lineOrder != null && _lineOrder.IsActive;
    public bool HasActiveLine => Formation != null;
    public bool HasThreatFacingFormation => Formation != null && Formation.IsThreatFacing;
    public bool HasThreatWithinSDistance => _formationState != null && _formationState.HasThreat;
    public Vector2 SegmentEndpoint(int end) => _formationState.SegmentEndpoint(end);
    internal void UpdateNaturalFormation(Vector2? threatHeading, float threatDistance, float deltaTime) =>
        _formationState.UpdateNatural(threatHeading, threatDistance, _engaged, deltaTime);

    // Facing used for faces and ranged arcs: the line's facing, or the march heading, or the transform's right.
    public Vector2 Facing
    {
        get
        {
            if (Formation != null && Formation.Facing.sqrMagnitude > 1e-6f) return Formation.Facing.normalized;
            if (IsRouted) return RoutFleeDirection;
            Vector2 march = _mover != null ? _mover.MarchVelocity : Vector2.zero;
            if (march.sqrMagnitude > 0.0025f) return march.normalized;
            if (_formationState != null && _formationState.HasHeading) return _formationState.Heading;
            return transform.right;
        }
    }

    // --- Footprint and engagement (spec §3.3, §4.1, §4.8) ---

    public TroopFootprint Footprint => _footprint;
    public IReadOnlyList<EngagementRecord> Engagements => _engagements;
    public bool IsEngaged => _engaged;
    public bool IsAssaulting => _assault.IsActive;
    public float AssaultTimeRemaining => _assault.TimeRemaining;

    // The nearest engaged enemy troop, or null.
    public Troop EngagedEnemy
    {
        get
        {
            Troop nearest = null;
            float best = float.PositiveInfinity;
            foreach (var record in _engagements)
            {
                if (!record.Engaged || record.Opponent == null || record.Gap >= best) continue;
                best = record.Gap;
                nearest = record.Opponent;
            }
            return nearest;
        }
    }

    // Which face of this troop the given enemy troop attacks. Front when there is no record.
    public TroopFace FaceAttackedBy(Troop opponent)
    {
        foreach (var record in _engagements)
            if (record.Opponent == opponent) return record.Face;
        return TroopFace.Front;
    }

    public bool IsFlankedBy(Troop opponent) => FaceAttackedBy(opponent).IsFlanked();

    internal void SetFootprint(in TroopFootprint footprint) => _footprint = footprint;

    // Replaces this frame's engagement records. Entering engagement with an active order starts Assault (spec §4.2).
    internal void SetEngagements(List<EngagementRecord> records)
    {
        _engagements.Clear();
        _engagements.AddRange(records);
        bool engaged = false;
        foreach (var record in records)
            if (record.Engaged) engaged = true;
        if (engaged && !_engaged) _assault.OnEngaged(HasActiveOrder, Tuning.AssaultDurationSeconds);
        _engaged = engaged;
    }

    internal void SetFormationMode(FormationMode mode) => _formationMode = mode;

    // --- Line links (spec §3.4) ---

    public IReadOnlyList<TroopEndpointLink> LineLinks => _lineLinks.All;
    public bool HasFreeLineEnd => _lineLinks.HasFreeEnd;
    public bool IsChained => _lineLinks.HasActiveLink;
    public TroopEndpointLink GetLineLink(int end) => _lineLinks.Get(end);
    public bool IsLinkActive(int end) => _lineLinks.IsActive(end);

    // A troop detached by a single-unit order (or ordered with Alt) does not auto-link until the condition in
    // ChainLinker clears it (Q-22, Q-23).
    public bool ChainSuppressed => _chainSuppressed;
    internal float ChainSuppressOutsideTime { get => _chainSuppressOutsideTime; set => _chainSuppressOutsideTime = value; }

    public void SetChainSuppressed(bool suppressed)
    {
        _chainSuppressed = suppressed;
        _chainSuppressOutsideTime = 0f;
    }

    // --- Movement (TroopPivotMover) ---

    public bool IsMoving => _mover != null && _mover.IsMoving;
    public bool IsPursuing => _mover != null && _mover.IsPursuing;
    public Troop PursuitTarget => _mover?.PursuitTarget;
    // An active Move, Pursue or Line order (spec §4.2 Assault trigger).
    public bool HasActiveOrder => IsMoving || HasManualLineOrder;
    public Vector2 Destination => _mover.Destination;
    public bool HasManualMoveOrder => HasManualLineOrder || _mover.HasPlainDestination;
    public Vector2 PivotVelocity => _mover != null ? _mover.PivotVelocity : Vector2.zero;
    public Vector2 LinePushVelocity => _mover.LinePushVelocity;
    public float PivotSpeed => _mover.PivotSpeed;
    public void SetLinePushVelocity(Vector2 velocity) => _mover.SetLinePushVelocity(velocity);

    // Moves the whole troop (soldiers are positioned relative to the pivot, so they travel with it).
    public void ShiftPivot(Vector2 worldDelta) => transform.position += (Vector3)worldDelta;

    void Awake()
    {
        EnsureModules();
    }

    void Start()
    {
        EnsureInitialized();
        _path.EnsurePathfinder();
    }

    // The order below matters (spec §5, Architecture §5.2): morale decay and healing first, then retreat recovery, the
    // break test, the retreat motion, the break test again, the rout, then the pivot moves and soldiers react to it.
    void Update() => Step(Time.deltaTime);

    // One frame of the troop with an explicit time step. Tests call it directly to simulate without Play mode.
    internal void Step(float deltaTime)
    {
        EnsureInitialized();
        if (!_initialized) return;

        if (_morale.enabled) _morale.Tick(deltaTime);
        if (_roster.enabled) _roster.TickHealing(deltaTime, Tuning.BruiseHealPerSecond);
        _assault.Tick(deltaTime);
        _lineLinks.TickRelax(deltaTime, Tuning.ChainEndpointRelaxSpeed);
        if (_faultSlip.TickRecovery(deltaTime) && !IsRouted) State = UnitState.Idle;
        EvaluateBreak();
        _faultSlip.Tick(deltaTime);
        EvaluateBreak();
        TickRout(deltaTime);
        _mover.Tick(deltaTime);
        if (_roster.enabled) _steering.Simulate(deltaTime);
        _views.Refresh(deltaTime);
    }

    void OnEnable()
    {
        if (_initialized) _views.Refresh(0f);
    }

    void OnDisable()
    {
        if (_views != null) _views.SetVisible(false);
    }

    void OnDestroy()
    {
        if (_views != null) _views.DestroyAll();
    }

    public void Configure(int teamId, IEnumerable<TroopProfileCount> composition)
    {
        EnsureModules();
        _teamId = teamId;
        _profile = null;
        _roster.SetComposition(composition);
        ResetSoldiers();
    }

    public void Configure(int teamId, TroopProfile profile)
    {
        EnsureModules();
        _teamId = teamId;
        _profile = profile;
        ResetSoldiers();
    }

    // --- Orders (spec §6). A routed troop ignores every order (spec §5.4). ---

    public void MoveTo(Vector2 worldPosition)
    {
        if (IsRouted || IsRetreating) return;
        _lineOrder.Cancel();
        _mover.MoveTo(worldPosition);
        State = UnitState.Moving;
    }

    public void Pursue(Troop target)
    {
        if (IsRouted || IsRetreating || target == null || target == this || target.TeamId == TeamId) return;
        _lineOrder.Cancel();
        _mover.Pursue(target);
        State = UnitState.Moving;
    }

    // Marches to center, turns to facing and holds a line of the given length (clamped to the stretch envelope).
    public void OrderLine(Vector2 center, Vector2 facing, float length)
    {
        if (IsRouted || IsRetreating) return;
        _lineOrder.Order(center, facing, Mathf.Clamp(length, MinimumLineLength, MaximumLineLength));
        _mover.MoveTo(center);
        State = UnitState.Moving;
    }

    // The current Move or Pursue order finished (arrival, or the pursued troop is gone). A line order keeps holding.
    internal void OnOrderComplete()
    {
        if (!IsRouted && !IsFaultSlipping) State = UnitState.Idle;
        if (_chainSuppressed) SetChainSuppressed(false);
    }

    // --- Combat ---

    // Melee or ranged raw damage in whole points. It is rolled against the target's armour, then applied. Returns the
    // applied HP. A flanked hit adds x3 Shock (spec §4.8).
    public int ApplyDamage(SoldierAgentState target, int rawDamage, Vector2 attackerPivotPosition, bool flanked)
    {
        if (target == null || rawDamage <= 0 || !target.IsAlive) return 0;
        return _damage.Apply(target, Mitigate(target, rawDamage), attackerPivotPosition, flanked).Total;
    }

    // Armour rolls use this troop's stream, since the troop being hit is the one that owns the target soldier.
    MitigationResult Mitigate(SoldierAgentState target, int rawDamage)
    {
        ArmourProfile armour = target.Profile != null ? target.Profile.Armour : null;
        if (armour == null) return MitigationResolver.Unarmoured(rawDamage);
        return MitigationResolver.Resolve(rawDamage, target.TotalDefence, armour.Deflect, armour.Bruise, TroopRandom);
    }

    // Charges per troop and Assault bonuses (spec §4.2, §4.3).
    public int EffectiveAggression(SoldierProfile profile) =>
        profile == null ? 0 : (int)ChargeRules.EffectiveAggression(profile.Aggression, IsAssaulting, Tuning.AssaultAggressionBonus);

    // Rolling melee statistics for this troop (charges, clashes, duels won, damage dealt and taken).
    public MeleeMetrics MeleeMetrics => _meleeMetrics ??= new MeleeMetrics(_meleeMetricsWindow);
    public float MeleeMetricsWindow => _meleeMetricsWindow;
    public void ResetMeleeMetrics() => MeleeMetrics.Reset();

    public void SetMeleeState(bool inMelee)
    {
        if (_inMelee == inMelee) return;
        _inMelee = inMelee;
        Melee(inMelee);
    }

    internal void RaiseSoldierDied(SoldierAgentState soldier) => SoldierDied?.Invoke(soldier);

    internal void PulseMeleeClash(SoldierAgentState soldier, float duration) => _views.PulseClash(soldier, duration);

    // Every soldier is dead. A routed troop already raised the destroyed event, so it is only removed.
    internal void OnAllSoldiersDead()
    {
        ChainLinker.BreakAllLinks(this, ChainBreakReason.Invalid);
        if (IsRouted) TroopObjects.Destroy(gameObject);
        else if (Application.isPlaying) KillUnit();
        else
        {
            RaiseUnitDestroyed();
            TroopObjects.Destroy(gameObject);
        }
    }

    // --- Breaks (spec §5.3) ---

    // Breaks the troop when Shock > EffectiveMorale. Safe to call whenever Shock or the soldier count may have changed.
    // A routed troop, or one already retreating, cannot break again until the retreat ends (Q-34).
    internal void EvaluateBreak()
    {
        if (!_initialized || !_morale.enabled || IsRouted || IsRetreating) return;
        if (_morale.Shock <= _morale.EffectiveMorale) return;

        BreakCase breakCase = _morale.Preview(IsSurrounded());
        switch (breakCase)
        {
            case BreakCase.Rout:
                EnterRout();
                break;
            case BreakCase.Retreat:
                BeginRetreat();
                break;
            case BreakCase.SurroundedBreak:
                _morale.RecordSurroundedBreak();
                CancelCharges();
                break;
        }
    }

    // Case B: accordion retreat away from the attacker, Shock resets, EffectiveMorale drops by the retreat penalty.
    void BeginRetreat()
    {
        _morale.RecordRetreat();
        CancelCharges();
        _mover.CancelOrders();
        _lineOrder.Cancel();
        Vector2 away = RetreatDirection();
        float distance = Tuning.RetreatDistance;
        _faultSlip.Begin(away, distance);
        State = UnitState.Fleeing;
        ChainLinker.OnTroopRetreat(this, away, distance);
    }

    // Shared retreat of a chain (spec §3.4): pulled back with a neighbour, at no morale cost. Engaged troops never move.
    internal bool BeginSympatheticRetreat(Vector2 direction, float distance)
    {
        if (IsRouted || IsFaultSlipping || IsEngaged || distance <= 0f) return false;
        _faultSlip.Begin(direction, distance, sympathetic: true);
        return true;
    }

    Vector2 RetreatDirection()
    {
        if (_morale.HasAttacker)
        {
            Vector2 away = (Vector2)transform.position - _morale.LastAttackerPivotPosition;
            if (away.sqrMagnitude > 1e-4f) return away.normalized;
        }
        return -Facing;
    }

    // Case A (spec §5.4): final. Unlinks, scatters, flees away from the nearest enemy, raises the destroyed event once.
    void EnterRout()
    {
        CancelCharges();
        _mover.CancelOrders();
        _lineOrder.Cancel();
        _assault.Clear();
        ChainLinker.BreakAllLinks(this, ChainBreakReason.Rout);
        _rout.Begin(AwayFromNearestEnemy());
        State = UnitState.Fleeing;
        Routed?.Invoke(this);
        RaiseUnitDestroyed();
    }

    void TickRout(float deltaTime)
    {
        if (!IsRouted) return;
        _rout.UpdateFleeDirection(AwayFromNearestEnemy());
        if (_rout.Tick(deltaTime, Tuning.RoutDespawnSeconds)) TroopObjects.Destroy(gameObject);
    }

    Vector2 AwayFromNearestEnemy()
    {
        Troop nearest = null;
        float best = float.PositiveInfinity;
        var world = TroopCombatWorld.Current;
        IReadOnlyList<Troop> troops = world != null ? world.Troops : Array.Empty<Troop>();
        foreach (var other in troops)
        {
            if (other == null || other == this || other.TeamId == TeamId || other.IsRouted) continue;
            float distance = ((Vector2)other.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = other;
        }
        if (nearest == null) return _rout != null && _rout.IsRouted ? _rout.FleeDirection : -Facing;
        Vector2 away = (Vector2)transform.position - (Vector2)nearest.transform.position;
        return away.sqrMagnitude > 1e-6f ? away.normalized : -Facing;
    }

    // Surrounded (spec §4.9, Q-33): the box behind the rear face is blocked by terrain or an enemy footprint.
    public bool IsSurrounded()
    {
        TroopFootprint path = SurroundTest.RetreatPath(_footprint, Tuning.RetreatDistance);
        var world = TroopCombatWorld.Current;
        if (world != null)
        {
            foreach (var other in world.Troops)
            {
                if (other == null || other == this || other.TeamId == TeamId) continue;
                TroopFootprint enemy = other.Footprint;
                if (SurroundTest.BlockedBy(path, enemy)) return true;
            }
        }
        if (_soldierObstacleMask.value == 0) return false;
        return Physics2D.OverlapBox(path.Center, SurroundTest.BoxSize(path), SurroundTest.BoxAngleDegrees(path),
            _soldierObstacleMask) != null;
    }

    void CancelCharges()
    {
        foreach (var soldier in _roster.SoldierList)
            soldier.CancelCharge();
    }

    // Test hook: break evaluation with a given Shock.
    internal void SetShockForTests(float shock)
    {
        _morale.SetShockForTests(shock);
        EvaluateBreak();
    }

    // --- Selection ---

    public void SetSelected(bool selected)
    {
        if (_selected == selected) return;
        _selected = selected;
        if (_visualiser != null) _visualiser.SetSelected(_selected);
    }

    public void SetHovered(bool hovered)
    {
        if (_hovered == hovered) return;
        _hovered = hovered;
        if (_visualiser != null) _visualiser.SetHovered(_hovered);
    }

    // --- Setup ---

    // Creates the modules the first time it is called. Existing scene objects saved before the MonoBehaviour
    // modules existed do not get them from RequireComponent, so missing ones are added here.
    void EnsureModules()
    {
        if (_modulesReady) return;
        _roster = GetOrAdd<TroopRoster>();
        _formationState = GetOrAdd<TroopFormationState>();
        _morale = GetOrAdd<TroopMorale>();
        _roster.Bind(this);
        _formationState.Bind(this);
        _morale.Bind(this);

        if (_soldierObstacleMask.value == 0)
            _soldierObstacleMask = SoldierMotionConstraints.DefaultObstacleMask();
        _constraints = new SoldierMotionConstraints(this, _soldierObstacleMask);
        _steering = new SoldierSteering(this, _constraints);
        _path = new TroopPathFollower(this);
        _mover = new TroopPivotMover(this, _path);
        _faultSlip = new FaultSlipMotion(this);
        _rout = new TroopRout(this);
        _lineOrder = new TroopLineOrder(this);
        _damage = new TroopDamageReceiver(this);
        _rangedFire = new RangedFireController(this);
        _views = new TroopSoldierViews(this);
        _footprint = TroopFootprint.Circle(transform.position, Radius, transform.right);
        _modulesReady = true;
    }

    internal LayerMask ObstacleMask => _soldierObstacleMask;

    T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    void ResetSoldiers()
    {
        _initialized = false;
        _roster.Clear();
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        EnsureModules();
        IReadOnlyList<TroopProfileCount> composition = _profile != null ? _profile.Composition : _roster.Composition;
        if (_initialized || !_roster.enabled || composition == null || composition.Count == 0) return;

        _roster.Spawn(composition);
        _morale.CaptureInitialSoldiers(_roster.LivingCount);
        _rangedFire.Reset();

        _army = GetComponentInParent<Army>();
        _views.Create();
        CreateVisualiser();
        _footprint = TroopFootprint.Circle(transform.position, Radius * Tuning.BlobRadiusScale, transform.right);
        _initialized = true;
    }

    void CreateVisualiser()
    {
        if (_visualiser == null) _visualiser = GetComponentInChildren<TroopVisualiser>(true);
        if (_visualiser == null)
        {
            var visualiserObject = new GameObject("Troop Visualiser");
            visualiserObject.transform.SetParent(transform, false);
            _visualiser = visualiserObject.AddComponent<TroopVisualiser>();
        }
        _visualiser.Initialize(this);
    }
}
