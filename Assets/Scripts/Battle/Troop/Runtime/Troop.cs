using System;
using System.Collections.Generic;
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
public sealed class Troop : MonoBehaviour
{
    [SerializeField] TroopState _state = TroopState.Idle;
    [SerializeField] int _teamId;
    // The characters that make up this troop (spec §1.1, §1.2): one is an ordinary troop, two or more a composite troop.
    // Each spawns its own Numbers of soldiers. The first one names the troop and gives its portrait.
    [SerializeField, Tooltip("The characters in this troop. The first one names the troop and gives its portrait.")]
    List<CharacterProfile> _characters = new();
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
    // Troop frontage (SU-103, SU-117), fixed when the soldiers are spawned at the start of the battle: the one given to
    // Configure, otherwise TroopFrontageRule from the troop's total soldier count. 0 until then.
    int _frontage;
    int _frontageOverride;
    readonly List<TroopStatLine> _statLines = new();
    bool _inMelee;
    bool _engaged;
    bool _selected;
    bool _hovered;
    bool _chainSuppressed;
    float _chainSuppressOutsideTime;
    FormationMode _formationMode;
    // Deployment: the soldiers were put straight into a placed line (PlaceAt), not left in their blob.
    bool _soldiersInPlacedLine;
    // Disordered (SU-104): seconds left during which the troop cannot move.
    float _disorderRemaining;
    // The enemy troop whose hit last damaged this troop (SU-114): credited with this troop's breaks.
    Troop _lastAttacker;
    bool _lastAttackRanged;
    float _lastAttackTime = float.NegativeInfinity;
    TroopFootprint _footprint;
    readonly TroopOutline _outline = new();

    public event Action<SoldierAgentState> SoldierDied;

    // Raised once when the troop routs (spec §5.4).
    public event Action<Troop> Routed;
    // Raised once when the troop is out of the battle: at rout, or when its last soldier dies before it routs. The
    // battle-wide BattleEvents.TroopDefeated is raised at the same time.
    public event Action<Troop> Defeated;
    public event Action<TroopState> StateChanged;
    public event Action EnteredMelee, ExitedMelee;
    bool _defeated;
    public bool IsDefeated => _defeated;

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

    public TroopState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(value);
        }
    }

    public bool InMelee => _inMelee;
    public int TeamId => _teamId;
    // The characters in this troop, leader first.
    public IReadOnlyList<CharacterProfile> Characters => _characters;
    public bool HasCharacters => FirstCharacter != null;
    // The first character, or null when the troop has none.
    public CharacterProfile FirstCharacter
    {
        get
        {
            if (_characters == null) return null;
            foreach (var character in _characters)
                if (character != null) return character;
            return null;
        }
    }
    // The troop's name and portrait are its first character's.
    public string DisplayName => FirstCharacter != null ? FirstCharacter.CharacterName : name;
    public Sprite Portrait => FirstCharacter != null ? FirstCharacter.Portrait : null;
    // Troop stats resolved from the characters (spec §1.2, CompositeTroopResolver):
    //   Move = min(Speed_i), BaseMorale = max(Morale_i), Discipline = max(Discipline_i), soldiers = sum(Numbers_i).
    public ResolvedTroopStats Resolved
    {
        get
        {
            _statLines.Clear();
            if (_characters != null)
                foreach (var character in _characters)
                    if (character != null) _statLines.Add(character.StatLine);
            return CompositeTroopResolver.Resolve(_statLines);
        }
    }
    // Troop frontage (SU-103): about how many soldiers wide the troop stands in line formation. Set when the battle
    // starts (SU-117): from the troop's total soldier count by TroopFrontageRule, unless Configure was given one.
    public int Frontage => _frontage > 0 ? _frontage : DetermineFrontage();
    // The troop's size in world units (blob, soldier clamp, pivot spacing), derived from the frontage and the soldiers it
    // was spawned with (LineGeometry.TroopRadius). It is no longer a profile value.
    public float Radius => Tuning.TroopRadius(Frontage, SpawnedSoldierCount);
    int SpawnedSoldierCount => _roster != null && _roster.Count > 0 ? _roster.Count : Resolved.InitialSoldierCount;
    public Color ArmyColor => _army != null ? _army.ArmyColour : Color.white;
    // The army this troop belongs to (its parent Army), or null.
    public Army Army => _army;
    static TroopTuning Tuning => TroopTuning.Active;

    // Natural line length L_nat = Frontage x LineSpacing (spec §3.3, SU-103). Independent of the living soldier count.
    public float NaturalLineLength => Tuning.NaturalLineLength(Frontage);
    // Shortest and longest line a troop may hold (spec §3.4 stretch envelope, §6.2).
    public float MinimumLineLength => NaturalLineLength * Tuning.ChainMinStretchScale;
    public float MaximumLineLength => NaturalLineLength * Tuning.ChainMaxStretchScale;
    // Length of the line the troop uses now: the ordered length while a line order holds, otherwise L_nat.
    public float LineLength => _lineOrder != null && _lineOrder.IsActive ? _lineOrder.Length : NaturalLineLength;

    // Troop stats (spec §1.4). Move is the lowest soldier Speed; Discipline is resolved from the characters (spec §1.2).
    public int Move
    {
        get
        {
            int move = _roster != null ? _roster.MinProfileValue(profile => profile.Speed) : 0;
            return move > 0 ? move : CombatStatLimits.Default;
        }
    }
    public int Discipline => HasCharacters ? Resolved.Discipline : CombatStatLimits.Default;

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
    public float AverageProfileValue(Func<CharacterProfile, float> valueSelector) => _roster.AverageProfileValue(valueSelector);
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
    // Disordered (SU-104): broke while not engaged in melee, so it stands where it is, unable to move or take orders, for
    // DisorderSeconds. It can still fight and shoot.
    public bool IsDisordered => _disorderRemaining > 0f && !IsRouted;
    public float DisorderRemaining => IsDisordered ? _disorderRemaining : 0f;
    // While routed: the way the troop runs (along its NavMesh flee path, else away from the nearest enemy).
    public Vector2 RoutFleeDirection => _rout != null ? _rout.MoveDirection : Vector2.zero;
    // The rout's flee goal on the NavMesh (RoutFleePlanner); false without a NavMesh, when the flight is a straight line.
    internal bool TryGetRoutFleeGoal(out Vector2 goal)
    {
        goal = _rout != null ? _rout.FleeGoal : Vector2.zero;
        return _rout != null && _rout.HasFleeGoal;
    }
    // The enemy troop whose hit last damaged this troop, whether it was a projectile, and when (Time.time). Null / -inf
    // until the troop is first hurt by a known attacker (SU-114).
    public Troop LastAttacker => _lastAttacker;
    public bool LastAttackWasRanged => _lastAttackRanged;
    public float LastAttackTime => _lastAttackTime;
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
    // turnWhileEngaged lifts the engaged facing lock for this update: a pursuer keeps squaring up to the face of its
    // target that it attacks (SU-75). The engaged flag only drives that lock in TroopFormationState.
    internal void UpdateNaturalFormation(Vector2? threatHeading, float threatDistance, float deltaTime,
        bool turnWhileEngaged = false) =>
        _formationState.UpdateNatural(threatHeading, threatDistance, _engaged && !turnWhileEngaged, deltaTime);

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
    // The formation outline for display and picking (TroopOutline): drawn from the formation, not the soldiers, so it is
    // steady in melee and on the march. The rules keep using Footprint.
    public TroopFootprint Outline => _outline.IsSeeded ? _outline.At(transform.position) : _footprint;
    public IReadOnlyList<EngagementRecord> Engagements => _engagements;
    public bool IsEngaged => _engaged;
    // Engaged (with the engagement hold) or carrying an attack order. Chain tension, shared joints, shared retreat and
    // friendly pivot separation never move a committed troop: its friends give way instead (SU-46).
    public bool IsCommitted => !IsRouted && (_engaged || IsPursuing);
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

    // Whether this troop was engaged with the given enemy in its current records (last frame's while the engagement
    // system is measuring). Used for the engagement hold (SU-45).
    public bool WasEngagedWith(Troop opponent)
    {
        foreach (var record in _engagements)
            if (record.Opponent == opponent) return record.Engaged;
        return false;
    }

    internal void SetFootprint(in TroopFootprint footprint) => _footprint = footprint;
    internal void UpdateOutline(float deltaTime) => _outline.Update(this, deltaTime);

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

    // The order the troop is carrying out now (shown when it is hovered). A line order counts while the troop marches to
    // the line; once it holds the line there is nothing left to show.
    public TroopOrderKind CurrentOrder
    {
        get
        {
            if (_mover == null || IsRouted || IsFaultSlipping || !IsMoving) return TroopOrderKind.None;
            if (IsPursuing) return TroopOrderKind.Attack;
            return HasManualLineOrder ? TroopOrderKind.Line : TroopOrderKind.Move;
        }
    }

    // The ordered line (centre, facing, length) while a line order is active.
    public bool TryGetLineOrder(out Vector2 center, out Vector2 facing, out float length)
    {
        bool active = HasManualLineOrder;
        center = active ? _lineOrder.Center : Vector2.zero;
        facing = active ? _lineOrder.Facing : Vector2.zero;
        length = active ? _lineOrder.Length : 0f;
        return active;
    }

    // Fills points with the route of the current order: the pivot, the planned path waypoints still ahead, then the
    // destination (the pursued troop's pivot for an attack). A straight line when no path is planned yet. Returns the
    // order kind; points is empty when there is no order.
    public TroopOrderKind CollectOrderPath(List<Vector2> points)
    {
        points.Clear();
        TroopOrderKind kind = CurrentOrder;
        if (kind == TroopOrderKind.None) return kind;

        Vector2 position = transform.position;
        _path.CollectRemainingPath(position, points);
        if (points.Count == 0) points.Add(position);
        Vector2 destination = kind == TroopOrderKind.Attack && PursuitTarget != null
            ? (Vector2)PursuitTarget.transform.position
            : Destination;
        if ((points[points.Count - 1] - destination).sqrMagnitude > 0.01f) points.Add(destination);
        return kind;
    }

    // Moves the whole troop (soldiers are positioned relative to the pivot, so they travel with it). The shift is kept
    // off terrain and on the map (ConstrainPivotStep).
    public void ShiftPivot(Vector2 worldDelta) => transform.position += (Vector3)ConstrainPivotStep(worldDelta);

    // A pivot displacement limited to the battlefield (BattlefieldBounds, inset by the blob radius so soldiers stay on
    // the map) and clipped against terrain obstacles with a slide along them (SU-74).
    public Vector2 ConstrainPivotStep(Vector2 worldDelta) => ConstrainPivotStep(transform.position, worldDelta);

    Vector2 ConstrainPivotStep(Vector2 start, Vector2 worldDelta)
    {
        if (worldDelta.sqrMagnitude <= 1e-12f) return worldDelta;
        Vector2 delta = BattlefieldBounds.ClampStep(start, worldDelta, Radius * Tuning.BlobRadiusScale);
        return _constraints != null ? _constraints.ClipPivotStep(start, delta) : delta;
    }

    // Applies ConstrainPivotStep to everything that moved the pivot this frame: retreat slip, rout flight, the march
    // and the line push.
    void KeepPivotOnTheField(Vector2 before)
    {
        Vector3 position = transform.position;
        Vector2 moved = (Vector2)position - before;
        if (moved.sqrMagnitude <= 1e-12f) return;
        Vector2 allowed = ConstrainPivotStep(before, moved);
        if ((allowed - moved).sqrMagnitude <= 1e-12f) return;
        transform.position = new Vector3(before.x + allowed.x, before.y + allowed.y, position.z);
    }

    // A Move (or a line order's march) whose path is used up ends there, even when it stops short of the destination:
    // the destination is off the NavMesh (the path ends at the nearest reachable point), or there is no route (SU-73).
    void CompleteOrderAtPathEnd()
    {
        if (!IsMoving || IsPursuing || IsRouted || IsFaultSlipping) return;
        if (!_path.IsAtEnd(transform.position)) return;
        _mover.CancelOrders();
        OnOrderComplete();
    }

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
        if (_disorderRemaining > 0f) _disorderRemaining = Mathf.Max(0f, _disorderRemaining - deltaTime);
        _lineLinks.TickRelax(deltaTime, Tuning.ChainEndpointRelaxSpeed);
        if (_faultSlip.TickRecovery(deltaTime) && !IsRouted) State = TroopState.Idle;
        EvaluateBreak();
        Vector2 pivotBefore = transform.position;
        _faultSlip.Tick(deltaTime);
        EvaluateBreak();
        TickRout(deltaTime);
        // A disordered troop does not march, pursue or get pushed along by line pressure (SU-104).
        if (IsDisordered) _mover.SetLinePushVelocity(Vector2.zero);
        else _mover.Tick(deltaTime);
        KeepPivotOnTheField(pivotBefore);
        CompleteOrderAtPathEnd();
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

    // The troop is gone (all dead, or a rout despawned). Its soldier sprites go with it here as well as in OnDestroy, which
    // Edit mode (the scenario tests) never calls.
    void DestroyTroop()
    {
        _views?.DestroyAll();
        TroopObjects.Destroy(gameObject);
    }

    // Sets up the troop for the battle from its characters (leader first) and spawns their soldiers. Its frontage is
    // determined then from its total soldier count (TroopFrontageRule, SU-117); a frontage above 0 is used instead
    // (tests and showcase scenes that need a fixed width).
    public void Configure(int teamId, IEnumerable<CharacterProfile> characters, int frontage = 0)
    {
        EnsureModules();
        _teamId = teamId;
        _characters = characters != null ? new List<CharacterProfile>(characters) : new List<CharacterProfile>();
        _frontageOverride = Mathf.Max(0, frontage);
        ResetSoldiers();
    }

    int DetermineFrontage() =>
        _frontageOverride > 0 ? _frontageOverride : TroopFrontageRule.ForSoldierCount(Resolved.InitialSoldierCount);

    // --- Orders (spec §6). A routed troop ignores every order (spec §5.4). ---

    public void MoveTo(Vector2 worldPosition)
    {
        if (IsRouted || IsRetreating || IsDisordered) return;
        _lineOrder.Cancel();
        _mover.MoveTo(worldPosition);
        State = TroopState.Moving;
    }

    public void Pursue(Troop target)
    {
        if (IsRouted || IsRetreating || IsDisordered || target == null || target == this || target.TeamId == TeamId) return;
        _lineOrder.Cancel();
        _mover.Pursue(target);
        State = TroopState.Moving;
    }

    // Marches to center, turns to facing and holds a line of the given length (clamped to the stretch envelope).
    public void OrderLine(Vector2 center, Vector2 facing, float length)
    {
        if (IsRouted || IsRetreating || IsDisordered) return;
        _lineOrder.Order(center, facing, Mathf.Clamp(length, MinimumLineLength, MaximumLineLength));
        _mover.MoveTo(center);
        State = TroopState.Moving;
    }

    // Deployment (SU-101): puts the troop straight at center, facing the given way, without marching. Time stands still
    // while deploying, so its soldiers are put straight into the shape it is in, and the troop, its outline and its card
    // all show the same formation:
    //   holdLine   a right-drag line: it holds a line of the given length, its soldiers standing in that line's ranks.
    //   otherwise  the spawn and a right-click: it stands Idle, like a troop arriving from a Move, its soldiers in the
    //              blob around the pivot.
    // Ignored once routed or retreating.
    public void PlaceAt(Vector2 center, Vector2 facing, float length, bool holdLine)
    {
        EnsureInitialized();
        if (IsRouted || IsRetreating) return;
        if (facing.sqrMagnitude < 1e-6f) facing = transform.right;
        facing.Normalize();
        _mover.CancelOrders();
        _lineOrder.Cancel();
        transform.SetPositionAndRotation(new Vector3(center.x, center.y, transform.position.z),
            Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg));
        bool formationOn = _formationState != null && _formationState.enabled;
        if (holdLine)
        {
            float lineLength = Mathf.Clamp(length, MinimumLineLength, MaximumLineLength);
            _lineOrder.Order(center, facing, lineLength);
            // The line moves with the troop at once: until the combat world rebuilds it next frame, the formation (and
            // the outline drawn from it) would still be where the troop stood before it was placed (SU-92).
            if (formationOn) Formation = TroopFormationState.Build(center, facing, lineLength, HasThreatWithinSDistance);
            _formationMode = IsChained ? FormationMode.Chain : FormationMode.Line;
            _roster.ArrangeInLine(lineLength);
            _soldiersInPlacedLine = true;
        }
        else
        {
            if (formationOn) Formation = null;
            _formationMode = FormationMode.Idle;
            // Soldiers stood in a line from an earlier line placement: back into the blob.
            if (_soldiersInPlacedLine) _roster.ScatterInBlob();
            _soldiersInPlacedLine = false;
        }
        _footprint = TroopFootprint.Circle(transform.position, Radius * Tuning.BlobRadiusScale, transform.right);
        _outline.Reset();
        State = TroopState.Idle;
        _views.Refresh(0f);
    }

    // The current Move or Pursue order finished (arrival, or the pursued troop is gone). A line order keeps holding.
    internal void OnOrderComplete()
    {
        if (!IsRouted && !IsFaultSlipping) State = TroopState.Idle;
        // ChainSuppressed is not cleared here: it ends 3 s after the troop leaves every attach radius (SU-130).
    }

    // --- Combat ---

    // Melee or ranged raw damage in whole points. It is rolled against the target's armour, then applied. Returns the
    // applied HP. A flanked hit adds x3 Shock (spec §4.8).
    // attacker (optional) is credited with the kill in BattleEvents.SoldierKilled. weapon (optional) is the weapon that
    // dealt the damage; its Piercing and Bludgeoning change the armour roll (SU-89).
    public int ApplyDamage(SoldierAgentState target, int rawDamage, Vector2 attackerPivotPosition, bool flanked,
        Troop attacker = null, WeaponProfile weapon = null) =>
        ApplyDamageDetailed(target, rawDamage, attackerPivotPosition, flanked, attacker, weapon, out _).Total;

    // As ApplyDamage, also giving the armour roll (for the combat log). Returns what the soldier took.
    internal AppliedDamage ApplyDamageDetailed(SoldierAgentState target, int rawDamage, Vector2 attackerPivotPosition,
        bool flanked, Troop attacker, WeaponProfile weapon, out MitigationResult mitigation)
    {
        mitigation = default;
        if (target == null || rawDamage <= 0 || !target.IsAlive) return default;
        mitigation = Mitigate(target, rawDamage, weapon);
        // Recorded before the hit lands, since the hit may break the troop at once; undone when it does no damage.
        var previous = (_lastAttacker, _lastAttackRanged, _lastAttackTime);
        if (attacker != null && attacker != this)
            (_lastAttacker, _lastAttackRanged, _lastAttackTime) = (attacker, weapon is RangedWeaponProfile, Time.time);
        AppliedDamage applied = _damage.Apply(target, mitigation, attackerPivotPosition, flanked);
        if (applied.Total <= 0) (_lastAttacker, _lastAttackRanged, _lastAttackTime) = previous;
        if (!target.IsAlive) BattleEvents.RaiseSoldierKilled(this, target, attacker);
        return applied;
    }

    // Armour rolls use this troop's stream, since the troop being hit is the one that owns the target soldier.
    MitigationResult Mitigate(SoldierAgentState target, int rawDamage, WeaponProfile weapon)
    {
        ArmourProfile armour = target.Character != null ? target.Character.Armour : null;
        int piercing = weapon != null ? weapon.Piercing : 0;
        int bludgeoning = weapon != null ? weapon.Bludgeoning : 0;
        if (armour == null) return MitigationResolver.Unarmoured(rawDamage, TroopRandom, piercing, bludgeoning);
        return MitigationResolver.Resolve(rawDamage, target.TotalDefence, armour.Deflect, armour.Bruise, armour.Bleed,
            TroopRandom, piercing, bludgeoning);
    }

    // Charges per troop and Assault bonuses (spec §4.2, §4.3).
    public int EffectiveAggression(CharacterProfile character) =>
        character == null ? 0 : (int)ChargeRules.EffectiveAggression(character.Aggression, IsAssaulting, Tuning.AssaultAggressionBonus);

    // Rolling melee statistics for this troop (charges, clashes, duels won, damage dealt and taken).
    public MeleeMetrics MeleeMetrics => _meleeMetrics ??= new MeleeMetrics(_meleeMetricsWindow);
    public float MeleeMetricsWindow => _meleeMetricsWindow;
    public void ResetMeleeMetrics() => MeleeMetrics.Reset();

    public void SetMeleeState(bool inMelee)
    {
        if (_inMelee == inMelee) return;
        _inMelee = inMelee;
        if (inMelee) EnteredMelee?.Invoke();
        else ExitedMelee?.Invoke();
    }

    internal void RaiseSoldierDied(SoldierAgentState soldier) => SoldierDied?.Invoke(soldier);

    // Test clean-up (TestScene): Edit mode never calls OnDestroy, so the soldier sprites are destroyed explicitly.
    internal void DestroyViewsForTests() => _views?.DestroyAll();

    internal void PulseMeleeClash(SoldierAgentState soldier, float duration) => _views.PulseClash(soldier, duration);
    // Squash and stretch on a soldier that took damage and was pushed back (worldPushDirection points away from the hit).
    internal void PlayHitSquash(SoldierAgentState soldier, Vector2 worldPushDirection) =>
        _views?.PlayHitSquash(soldier, worldPushDirection);

    // Every soldier is dead. A routed troop was already defeated, so it is only removed.
    internal void OnAllSoldiersDead()
    {
        ChainLinker.BreakAllLinks(this, ChainBreakReason.Invalid);
        RaiseDefeated();
        DestroyTroop();
    }

    void RaiseDefeated()
    {
        if (_defeated) return;
        _defeated = true;
        Defeated?.Invoke(this);
        BattleEvents.RaiseTroopDefeated(this);
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
                // Retreats only come from melee (SU-104): out of melee the troop is disordered instead.
                if (InMelee) BeginRetreat();
                else BeginDisorder();
                break;
            case BreakCase.SurroundedBreak:
                _morale.RecordSurroundedBreak();
                RaiseBroke(TroopBreakKind.SurroundedBreak);
                CancelCharges();
                break;
        }
    }

    void RaiseBroke(TroopBreakKind kind) =>
        BattleEvents.RaiseTroopBroke(new TroopBreakInfo(this, kind, _lastAttacker, _lastAttackRanged));

    // Case B out of melee (SU-104): the troop is disordered for DisorderSeconds. It stops where it is and cannot move or
    // take orders. Shock resets and EffectiveMorale drops by the retreat penalty, as for a retreat. Charges in flight are
    // completed. Breaking again while disordered restarts the time (or retreats, if the troop is now in melee).
    void BeginDisorder()
    {
        _morale.RecordRetreat();
        RaiseBroke(TroopBreakKind.Disorder);
        _mover.CancelOrders();
        _lineOrder.Cancel();
        _disorderRemaining = Tuning.DisorderSeconds;
        if (!IsRouted) State = TroopState.Idle;
    }

    // Case B: accordion retreat away from the attacker, Shock resets, EffectiveMorale drops by the retreat penalty.
    // Only a troop engaged in melee retreats (SU-104). A retreat ends any disorder. Stunned soldiers are left behind and
    // die (SU-107).
    void BeginRetreat()
    {
        _disorderRemaining = 0f;
        _morale.RecordRetreat();
        RaiseBroke(TroopBreakKind.Retreat);
        if (KillStunnedSoldiers()) return;
        // Charges in flight are completed; the chargers then rush back to the retreating troop (SU-43).
        _mover.CancelOrders();
        _lineOrder.Cancel();
        Vector2 away = RetreatDirection();
        float distance = Tuning.RetreatDistance;
        _faultSlip.Begin(away, distance);
        State = TroopState.Fleeing;
        ChainLinker.OnTroopRetreat(this, away, distance);
    }

    // Shared retreat of a chain (spec §3.4): pulled back with a neighbour, at no morale cost. Engaged or attacking troops
    // never move (SU-46).
    internal bool BeginSympatheticRetreat(Vector2 direction, float distance)
    {
        if (IsRouted || IsFaultSlipping || IsCommitted || IsDisordered || distance <= 0f) return false;
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

    // Stunned soldiers of a troop falling back from melee are left behind and killed (SU-107), credited to the enemy it
    // was engaged with. Returns true when that killed the last of the troop's soldiers (the troop is then removed).
    bool KillStunnedSoldiers()
    {
        if (!InMelee) return false;
        Troop killer = EngagedEnemy;
        bool any = false;
        foreach (var soldier in _roster.SoldierList)
        {
            if (!soldier.KillIfStunned()) continue;
            any = true;
            RaiseSoldierDied(soldier);
            BattleEvents.RaiseSoldierKilled(this, soldier, killer);
        }
        if (!any || LivingSoldierCount > 0) return false;
        OnAllSoldiersDead();
        return true;
    }

    // Case A (spec §5.4): final. Unlinks, scatters, flees away from the nearest enemy, raises the destroyed event once.
    // A rout from melee leaves its stunned soldiers behind too (SU-107, assumption).
    void EnterRout()
    {
        KillStunnedSoldiers();
        CancelCharges(returnToTroop: false);
        _mover.CancelOrders();
        _lineOrder.Cancel();
        _assault.Clear();
        ChainLinker.BreakAllLinks(this, ChainBreakReason.Rout);
        _rout.Begin(AwayFromNearestEnemy());
        State = TroopState.Fleeing;
        Routed?.Invoke(this);
        RaiseBroke(TroopBreakKind.Rout);
        RaiseDefeated();
    }

    void TickRout(float deltaTime)
    {
        if (!IsRouted) return;
        _rout.UpdateFleeDirection(AwayFromNearestEnemy());
        _rout.SteerAlongPath(_mover.PivotSpeed, deltaTime);
        if (_rout.Tick(deltaTime, Tuning.RoutDespawnSeconds)) DestroyTroop();
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

    // Surrounded (spec §4.9, §5.3, Q-33, SU-122): the box behind the rear face (SurroundTest.RetreatPath) is blocked by
    // terrain or by an enemy troop. An enemy blocks it only with a soldier standing in the box, not with its footprint: a
    // line footprint can reach well past its soldiers (a pivot run ahead of them), which gave false surrounds. Routed
    // enemies never block (they are fleeing, and AwayFromNearestEnemy ignores them too); friendly troops never block.
    public bool IsSurrounded()
    {
        TroopFootprint path = SurroundTest.RetreatPath(_footprint, Tuning.RetreatDistance);
        var world = TroopCombatWorld.Current;
        if (world != null)
        {
            foreach (var other in world.Troops)
            {
                if (other == null || other == this || other.TeamId == TeamId || other.IsRouted) continue;
                if (HasSoldierInRetreatPath(other, path)) return true;
            }
        }
        if (_soldierObstacleMask.value == 0) return false;
        return Physics2D.OverlapBox(path.Center, SurroundTest.BoxSize(path), SurroundTest.BoxAngleDegrees(path),
            _soldierObstacleMask) != null;
    }

    // A soldier blocks the retreat path when its body (a disc of this radius) overlaps the path box.
    const float RetreatPathSoldierRadius = TroopConstants.SoldierRadius;

    // True when a living soldier of enemy that counts toward its footprint (not charging, following a charger or rushing
    // back: those are on their way somewhere else, SU-43) stands in the retreat path.
    static bool HasSoldierInRetreatPath(Troop enemy, in TroopFootprint path)
    {
        IReadOnlyList<SoldierAgentState> soldiers = enemy.Soldiers;
        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierAgentState soldier = soldiers[i];
            if (!soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning) continue;
            if (path.SignedDistance(enemy.WorldPosition(soldier)) < RetreatPathSoldierRadius) return true;
        }
        return false;
    }

    void CancelCharges(bool returnToTroop = true)
    {
        foreach (var soldier in _roster.SoldierList)
            soldier.CancelCharge(returnToTroop);
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
        _rout = new TroopRout(this, _path);
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
        _frontage = 0;
        _roster.Clear();
        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        EnsureModules();
        IReadOnlyList<CharacterProfile> characters = _characters;
        if (_initialized || !_roster.enabled || characters == null || characters.Count == 0) return;

        _frontage = DetermineFrontage();
        _roster.Spawn(characters);
        _morale.CaptureInitialSoldiers(_roster.LivingCount);
        _rangedFire.Reset();

        _army = GetComponentInParent<Army>();
        _views.Create();
        CreateVisualiser();
        _footprint = TroopFootprint.Circle(transform.position, Radius * Tuning.BlobRadiusScale, transform.right);
        _outline.Reset();
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
