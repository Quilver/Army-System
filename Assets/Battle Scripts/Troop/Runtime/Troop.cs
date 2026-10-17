using System;
using System.Collections.Generic;
using StatSystem.Refactor;
using UnityEngine;
using UnityEngine.Serialization;

// Aggregator for one troop of soldiers. It owns the identity and configuration of the troop and composes the modules
// that do the work, running them in a fixed order each frame (see Update):
//   MonoBehaviours (toggle in the Inspector): TroopRoster, TroopFormationState, TroopMorale
//   Plain classes: TroopPivotMover, TroopPathFollower, FaultSlipMotion, TroopLineOrder, TroopLineLinks,
//                  TroopDamageReceiver, SoldierSteering, SoldierMotionConstraints, TroopSoldierViews
[RequireComponent(typeof(TroopRoster), typeof(TroopFormationState), typeof(TroopMorale))]
public sealed class Troop : IUnit
{
    const float DefaultLineWidth = 22f;

    [SerializeField] IUnitStatBlock _legacyStats;
    [SerializeField] UnitState _state = UnitState.Idle;
    [SerializeField] int _teamId;
    [SerializeField] TroopProfile _profile;
    [SerializeField, HideInInspector, Min(0.5f)] float _lineWidth = DefaultLineWidth;
    [SerializeField, HideInInspector, Min(0.1f), FormerlySerializedAs("_flockRadius")] float _troopRadius = 16.5f;
    [SerializeField, FormerlySerializedAs("_boidObstacleMask")] LayerMask _soldierObstacleMask;
    [SerializeField, Min(1f)] float _meleeMetricsWindow = MeleeMetrics.DefaultWindow;

    TroopRoster _roster;
    MeleeMetrics _meleeMetrics;
    TroopFormationState _formationState;
    TroopMorale _morale;
    readonly TroopLineLinks _lineLinks = new();
    TroopLineOrder _lineOrder;
    TroopPathFollower _path;
    TroopPivotMover _mover;
    FaultSlipMotion _faultSlip;
    TroopDamageReceiver _damage;
    SoldierMotionConstraints _constraints;
    SoldierSteering _steering;
    TroopSoldierViews _views;

    Army _army;
    TroopVisualiser _visualiser;
    bool _modulesReady;
    bool _initialized;
    bool _inMelee;
    bool _selected;
    bool _hovered;

    public event Action<SoldierAgentState> SoldierDied;

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
    public float LineWidth { get => _lineWidth; internal set => _lineWidth = value; }
    public float MinimumLineWidth =>
        Mathf.Min(Radius * 1.8f, Mathf.Max(0.5f, (LivingSoldierCount - 1) * TroopConstants.LineSpacing));
    public Vector2 LineAxis => transform.up;
    public bool IsSelected => _selected;
    public bool IsHovered => _hovered;
    public float ThreatEdgeDistance { get; internal set; } = float.PositiveInfinity;

    // --- Modules ---

    public TroopRoster Roster => _roster;
    public TroopFormationState FormationState => _formationState;
    public TroopMorale MoraleState => _morale;
    internal TroopLineOrder LineOrder => _lineOrder;

    // --- Soldiers (TroopRoster) ---

    public IReadOnlyList<SoldierAgentState> Soldiers => _roster != null ? _roster.Soldiers : Array.Empty<SoldierAgentState>();
    public int SoldierCount => _roster.Count;
    public int LivingSoldierCount => _roster.LivingCount;
    public float AverageProfileValue(Func<SoldierProfile, float> valueSelector) => _roster.AverageProfileValue(valueSelector);
    public float DistanceToNearestSoldier(Vector2 worldPosition) => _roster.DistanceToNearestSoldier(worldPosition);
    public bool HasLivingSoldierInRect(Rect worldRect) => _roster.HasLivingSoldierInRect(worldRect);
    public Vector2 WorldPosition(SoldierAgentState soldier) => _roster.WorldPosition(soldier);
    public Vector2 WorldVelocity(SoldierAgentState soldier) => _roster.WorldVelocity(soldier);
    public void ResolveContact(SoldierAgentState soldier, Vector2 worldPositionDelta, Vector2 worldVelocityDelta) =>
        _roster.ResolveContact(soldier, worldPositionDelta, worldVelocityDelta);
    public void ConstrainSoldiersToRadius() => _constraints.ConstrainAllToRadius();

    // --- Morale (TroopMorale) ---

    public float Morale => _morale.Value;
    public float Shock => _morale.Shock;
    public float FaultPressure => _morale.FaultPressure;
    public bool IsFaultSlipping => _faultSlip != null && _faultSlip.IsActive;
    public bool IsPulseLocked => IsFaultSlipping || _morale.IsPulseLocked;
    public bool RecordClashLoss(float pressureMargin) => _morale.RecordClashLoss(pressureMargin);
    internal Vector2 FaultSlipAxis => _faultSlip.Axis;

    // --- Formation (TroopFormationState) and line orders ---

    public TroopFormationData Formation
    {
        get => _formationState != null ? _formationState.Formation : null;
        internal set => _formationState.Formation = value;
    }
    public bool HasManualLineOrder => _lineOrder != null && _lineOrder.IsActive;
    public bool HasActiveLine => Formation != null || HasManualLineOrder;
    public bool HasThreatFacingFormation => Formation != null && Formation.IsThreatFacing;
    public Vector2 SegmentEndpoint(int end) => _formationState.SegmentEndpoint(end);
    public Troop EngagedEnemy => _formationState != null ? _formationState.EngagedEnemy : null;
    internal void UpdateThreatFormation(Vector2? targetHeading, float targetDistance, float deltaTime,
        Troop engagedEnemy = null, Vector2 engagedEnemyFacing = default) =>
        _formationState.UpdateThreat(targetHeading, targetDistance, deltaTime, engagedEnemy, engagedEnemyFacing);

    // --- Line links ---

    public IReadOnlyList<TroopEndpointLink> LineLinks => _lineLinks.All;
    public bool HasFreeLineEnd => _lineLinks.HasFreeEnd;
    public TroopEndpointLink GetLineLink(int end) => _lineLinks.Get(end);
    public void SetLineLink(int end, Troop other, int otherEnd) => _lineLinks.Set(end, other, otherEnd);
    public void ClearLineLink(int end) => _lineLinks.Clear(end);
    public void SetLineLinkTension(int end, Vector2 tension) => _lineLinks.SetTension(end, tension);

    // --- Movement (TroopPivotMover) ---

    public bool IsMoving => _mover.IsMoving;
    public Vector2 Destination => _mover.Destination;
    public bool HasManualMoveOrder => HasManualLineOrder || _mover.HasPlainDestination;
    public Vector2 PivotVelocity => _mover.PivotVelocity;
    public Vector2 LinePushVelocity => _mover.LinePushVelocity;
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

    // The order below matters: morale and retreat first, then the pivot moves, then soldiers react to the new pivot.
    void Update()
    {
        EnsureInitialized();
        if (!_initialized) return;

        float deltaTime = Time.deltaTime;
        if (_morale.enabled) _morale.Tick(deltaTime);
        if (_faultSlip.TickRecovery(deltaTime)) State = UnitState.Idle;
        TryBeginFaultSlip();
        _faultSlip.Tick(deltaTime);
        TryBeginFaultSlip();
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

    public void Configure(int teamId, IEnumerable<TroopProfileCount> composition, float lineWidth = DefaultLineWidth)
    {
        EnsureModules();
        _teamId = teamId;
        _profile = null;
        _lineWidth = Mathf.Clamp(lineWidth, 0.5f, Radius * 1.8f);
        _roster.SetComposition(composition);
        ResetSoldiers();
    }

    public void Configure(int teamId, TroopProfile profile)
    {
        EnsureModules();
        _teamId = teamId;
        _profile = profile;
        if (_profile != null)
            _lineWidth = Mathf.Clamp(_profile.LineWidth, 0.5f, Radius * 1.8f);
        ResetSoldiers();
    }

    // --- Orders ---

    public void MoveTo(Vector2 worldPosition)
    {
        if (_state == UnitState.Fleeing) return;
        _lineOrder.Cancel();
        _mover.MoveTo(worldPosition);
        State = UnitState.Moving;
    }

    public void Pursue(Troop target)
    {
        if (_state == UnitState.Fleeing || target == null || target == this || target.TeamId == TeamId) return;
        _lineOrder.Cancel();
        _mover.Pursue(target);
        State = UnitState.Moving;
    }

    public void OrderLine(Vector2 center, Vector2 tangent, float width, Vector2 enemyHint)
    {
        if (_state == UnitState.Fleeing) return;
        _lineOrder.Order(center, tangent, width, enemyHint);
    }

    // --- Combat ---

    public float ApplyDamage(SoldierAgentState target, float damage) =>
        ApplyDamage(target, damage, transform.position, TroopDamageReceiver.DefaultChargeDistance);

    public float ApplyDamage(SoldierAgentState target, float damage, Vector2 attackerPivotPosition, float chargeDistance = TroopDamageReceiver.DefaultChargeDistance) =>
        _damage.Apply(target, damage, attackerPivotPosition, chargeDistance);

    // Rolling melee statistics for this troop (attacks it makes, damage it deals and takes).
    public MeleeMetrics MeleeMetrics => _meleeMetrics ??= new MeleeMetrics(_meleeMetricsWindow);
    public float MeleeMetricsWindow => _meleeMetricsWindow;
    public void ResetMeleeMetrics() => MeleeMetrics.Reset();

    // Attacks currently banked by the troop's soldiers (ready to spend in a clash, and held for the next beat).
    public int ReadyMeleeAttacks
    {
        get
        {
            int total = 0;
            foreach (var soldier in Soldiers)
                if (soldier.IsAlive) total += soldier.Melee.ReadyAttacks;
            return total;
        }
    }

    public int HeldMeleeAttacks
    {
        get
        {
            int total = 0;
            foreach (var soldier in Soldiers)
                if (soldier.IsAlive) total += soldier.Melee.HeldAttacks;
            return total;
        }
    }

    public void SetMeleeState(bool inMelee)
    {
        if (_inMelee == inMelee) return;
        _inMelee = inMelee;
        Melee(inMelee);
    }

    internal void RaiseSoldierDied(SoldierAgentState soldier) => SoldierDied?.Invoke(soldier);

    internal void PulseMeleeClash(SoldierAgentState soldier, float duration) => _views.PulseClash(soldier, duration);

    // Breaks the troop if morale allows it. Safe to call whenever shock may have changed.
    internal void TryBeginFaultSlip()
    {
        if (!_morale.enabled || IsFaultSlipping || !_morale.ShouldBreak) return;
        _faultSlip.Begin(_morale.LastAttackerPivotPosition, _morale.LastChargeDistance);
        _morale.OnBroke();
        _mover.CancelOrders();
        _lineOrder.Cancel();
        State = UnitState.Fleeing;
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
        _lineOrder = new TroopLineOrder(this);
        _damage = new TroopDamageReceiver(this);
        _views = new TroopSoldierViews(this);
        _modulesReady = true;
    }

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

        if (_profile != null)
            _lineWidth = Mathf.Clamp(_profile.LineWidth, 0.5f, Radius * 1.8f);

        _roster.Spawn(composition);
        _lineWidth = Mathf.Clamp(_lineWidth, MinimumLineWidth, Radius * 1.8f);

        _army = GetComponentInParent<Army>();
        _views.Create();
        CreateVisualiser();
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
