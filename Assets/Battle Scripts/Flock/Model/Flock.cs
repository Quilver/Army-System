using System;
using System.Collections.Generic;
using StatSystem.Refactor;
using UnityEngine;

public sealed class Flock : IUnit
{
    const float PrototypeWorldScale = 0.052f;
    const float NeighborRadius = 26f * PrototypeWorldScale;
    const float BoidRadius = 0.16f;
    const float LineSpacing = 5.8f * PrototypeWorldScale;
    const float SteeringDamping = 2.6f;
    const float BoidFacingSpeedThreshold = 0.2f;
    const float BoidTurnSpeed = 360f;
    const float EngagedFacingDeviation = 30f;
    const float ClashColorPulseIntensityScale = 18f / 5f;
    static readonly int LightColorId = Shader.PropertyToID("_Light");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    const float FaultMaximumDelay = 1f;
    const float FaultRearMoveDuration = 0.5f;
    const float FaultFrontMoveDuration = 1f;
    const float FaultHoldDuration = 0.26f;
    const float FaultReturnDuration = 0.88f;
    const float FaultSlipDuration = FaultMaximumDelay + FaultFrontMoveDuration
        + FaultHoldDuration + FaultReturnDuration;

    [SerializeField] IUnitStatBlock _legacyStats;
    [SerializeField] UnitState _state = UnitState.Idle;
    [SerializeField] int _teamId;
    [SerializeField] FlockProfile _profile;
    [SerializeField, HideInInspector, Min(0f)] float _morale = 100f;
    [SerializeField, HideInInspector, Min(0.5f)] float _lineWidth = 7f;
    [SerializeField, HideInInspector, Min(0.1f)] float _flockRadius = 102f * PrototypeWorldScale;
    [SerializeField, HideInInspector] List<FlockProfileCount> _composition = new();
    [SerializeField] FlockEndpointLink[] _lineLinks = new FlockEndpointLink[2];
    [SerializeField] LayerMask _boidObstacleMask;

    readonly List<BoidAgentState> _boids = new();
    readonly List<BoidAgentState> _livingScratch = new();
    readonly List<Vector2> _nextPositions = new();
    readonly List<Vector2> _nextVelocities = new();
    readonly List<Vector2> _boidWorldPositions = new();
    readonly List<Vector2> _boidWorldVelocities = new();
    readonly Dictionary<Vector2Int, List<BoidAgentState>> _spatialBuckets = new();
    readonly Stack<List<BoidAgentState>> _unusedBuckets = new();
    readonly List<SpriteRenderer> _boidRenderers = new();
    readonly RaycastHit2D[] _boidObstacleHits = new RaycastHit2D[4];
    MaterialPropertyBlock _boidPropertyBlock;

    Army _army;
    FlockVisualiser _visualiser;
    MovementSystem.IPathfinder _pathfinder;
    List<Vector2> _movementPath;
    ContactFilter2D _boidObstacleFilter;
    bool _initialized;
    bool _hasDestination;
    bool _pursuitActive;
    bool _pathInvalid = true;
    bool _manualLineOrder;
    Vector2 _destination;
    Vector2 _pathTarget;
    int _pathIndex;
    float _nextPathRebuild;
    Flock _pursuitTarget;
    bool _inMelee;
    bool _selected;
    float _shock;
    float _faultPressure;
    float _faultCooldownRemaining;
    float _clashCooldownRemaining;
    float _pulseCooldownRemaining;
    float _faultRecoveryRemaining;
    float _faultSlipAge = -1f;
    float _faultSlipDistance;
    float _faultPivotDistance;
    float _faultPivotDisplacement;
    Vector2 _faultSlipAxis;
    Vector2 _pivotVelocity;
    Vector2 _linePushVelocity;
    Vector2 _lastAttackerPivotPosition;
    float _lastChargeDistance = 1.66f;
    FlockFormationData _formation;
    bool _hasFormationHeading;
    Vector2 _formationHeading;
    float _formationDistance;
    int _nextBoidId;

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
    public FlockProfile Profile => _profile;
    public float Morale => _profile != null ? _profile.Morale : _morale;
    public float Shock => _shock;
    public float FaultPressure => _faultPressure;
    public bool IsFaultSlipping => _faultSlipAge >= 0f;
    public bool IsPulseLocked => IsFaultSlipping || _pulseCooldownRemaining > 0f;
    public bool IsSelected => _selected;
    public int BoidCount => _boids.Count;
    public int LivingBoidCount
    {
        get
        {
            int count = 0;
            foreach (var boid in _boids)
                if (boid.IsAlive) count++;
            return count;
        }
    }
    public IReadOnlyList<BoidAgentState> Boids => _boids;
    public bool IsMoving => _hasDestination || _pursuitActive;
    public Vector2 Destination => _destination;
    public float LineWidth => _lineWidth;
    public float Radius => Mathf.Max(0.1f, _profile != null ? _profile.FlockRadius : _flockRadius);
    public Color ArmyColor => _army != null ? _army.ArmyColour : Color.white;
    public float MinimumLineWidth => Mathf.Min(Radius * 1.8f, Mathf.Max(0.5f, (LivingBoidCount - 1) * LineSpacing));
    public Vector2 LineAxis => transform.up;
    public Vector2 PivotVelocity => _pivotVelocity + _linePushVelocity;
    public Vector2 LinePushVelocity => _linePushVelocity;
    public FlockFormationData Formation { get => _formation; internal set => _formation = value; }
    public IReadOnlyList<FlockEndpointLink> LineLinks => _lineLinks;
    public bool HasActiveLine => _formation != null || _manualLineOrder;
    public bool HasThreatFacingFormation => _formation != null && _formation.IsThreatFacing;

    public event Action<BoidAgentState> BoidDied;

    void Awake()
    {
        _boidPropertyBlock = new MaterialPropertyBlock();
        if (_boidObstacleMask.value == 0)
        {
            _boidObstacleMask = LayerMask.GetMask(
                "Terrain",
                "Water",
                "Water/flat impassible terrain",
                "Impassable Terrain",
                "Impassable");
        }
        _boidObstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _boidObstacleMask,
            useTriggers = false
        };
    }

    void Start()
    {
        EnsureInitialized();
        EnsurePathfinder();
    }

    void Update()
    {
        EnsureInitialized();
        if (!_initialized) return;
        EnsureLineLinks();

        float deltaTime = Time.deltaTime;
        UpdateFaultPressure(deltaTime);
        UpdateFaultSlip(deltaTime);
        if (_shock > Morale && _faultCooldownRemaining <= 0f && !IsFaultSlipping)
            BeginFaultSlip(_lastAttackerPivotPosition, _lastChargeDistance);
        if (_state != UnitState.Fleeing) MovePivot(deltaTime);
        SimulateBoids(deltaTime);
        UpdateBoidVisuals(deltaTime);
    }

    public void Configure(int teamId, IEnumerable<FlockProfileCount> composition, float lineWidth = 7f)
    {
        _teamId = teamId;
        _profile = null;
        _lineWidth = Mathf.Clamp(lineWidth, 0.5f, Radius * 1.8f);
        _composition = new List<FlockProfileCount>(composition);
        ResetBoids();
    }

    public void Configure(int teamId, FlockProfile profile)
    {
        _teamId = teamId;
        _profile = profile;
        if (_profile != null)
            _lineWidth = Mathf.Clamp(_profile.LineWidth, 0.5f, Radius * 1.8f);
        ResetBoids();
    }

    void ResetBoids()
    {
        _initialized = false;
        _boids.Clear();
        EnsureLineLinks();
        EnsureInitialized();
    }

    public void MoveTo(Vector2 worldPosition)
    {
        if (_state == UnitState.Fleeing) return;
        _pursuitActive = false;
        _pursuitTarget = null;
        _pathInvalid = true;
        _manualLineOrder = false;
        _destination = worldPosition;
        _hasDestination = true;
        State = UnitState.Moving;
    }

    public void Pursue(Flock target)
    {
        if (_state == UnitState.Fleeing || target == null || target == this || target.TeamId == TeamId) return;
        _pursuitTarget = target;
        _pursuitActive = true;
        _hasDestination = true;
        _pathInvalid = true;
        _manualLineOrder = false;
        State = UnitState.Moving;
    }

    public float AverageProfileValue(Func<BoidProfile, float> valueSelector)
    {
        float total = 0f;
        int count = 0;
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive || boid.Profile == null) continue;
            total += valueSelector(boid.Profile);
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    public FlockEndpointLink GetLineLink(int end)
    {
        EnsureLineLinks();
        return _lineLinks[Mathf.Clamp(end, 0, 1)];
    }

    public bool HasFreeLineEnd => GetLineLink(0) == null || GetLineLink(1) == null;

    public void SetLineLink(int end, Flock other, int otherEnd)
    {
        EnsureLineLinks();
        _lineLinks[Mathf.Clamp(end, 0, 1)] = new FlockEndpointLink { Other = other, OtherEnd = Mathf.Clamp(otherEnd, 0, 1) };
    }

    public void ClearLineLink(int end)
    {
        EnsureLineLinks();
        _lineLinks[Mathf.Clamp(end, 0, 1)] = null;
    }

    public void SetLineLinkTension(int end, Vector2 tension)
    {
        var link = GetLineLink(end);
        if (link != null) link.Tension = tension;
    }

    void EnsureLineLinks()
    {
        if (_lineLinks == null || _lineLinks.Length != 2)
            _lineLinks = new FlockEndpointLink[2];
    }

    public Vector2 SegmentEndpoint(int end)
    {
        if (_formation == null) return transform.position;
        return end == 0 ? _formation.Start : _formation.End;
    }

    public void SetLinePushVelocity(Vector2 velocity) => _linePushVelocity = velocity;

    internal void UpdateThreatFormation(Vector2? targetHeading, float targetDistance, float deltaTime)
    {
        if (!targetHeading.HasValue)
        {
            _hasFormationHeading = false;
            _formation = _manualLineOrder ? CreateManualFormation() : null;
            return;
        }

        Vector2 target = targetHeading.Value.normalized;
        if (!_hasFormationHeading)
        {
            _formationHeading = target;
            _formationDistance = targetDistance;
            _hasFormationHeading = true;
        }
        else
        {
            float blend = 1f - Mathf.Exp(-deltaTime / 0.8f);
            Vector2 smoothed = Vector2.Lerp(_formationHeading, target, blend);
            if (smoothed.sqrMagnitude > 0.000001f) _formationHeading = smoothed.normalized;
            _formationDistance += (targetDistance - _formationDistance) * blend;
        }

        float inward = Mathf.Min(Radius * 0.42f, _formationDistance * 0.22f);
        Vector2 center = (Vector2)transform.position + _formationHeading * inward;
        Vector2 tangent = new(-_formationHeading.y, _formationHeading.x);
        float halfLength = Mathf.Min(
            _lineWidth * 0.5f,
            Mathf.Sqrt(Mathf.Max(0f, Mathf.Pow(Radius * 0.9f, 2f) - inward * inward)));
        _formation = new FlockFormationData
        {
            Start = center - tangent * halfLength,
            End = center + tangent * halfLength,
            Facing = _formationHeading,
            IsThreatFacing = true
        };
    }

    FlockFormationData CreateManualFormation()
    {
        Vector2 center = transform.position;
        Vector2 tangent = LineAxis.normalized;
        Vector2 half = tangent * (_lineWidth * 0.5f);
        return new FlockFormationData
        {
            Start = center - half,
            End = center + half,
            Facing = transform.right,
            IsThreatFacing = false
        };
    }

    public void OrderLine(Vector2 center, Vector2 tangent, float width, Vector2 enemyHint)
    {
        if (_state == UnitState.Fleeing) return;
        if (tangent.sqrMagnitude < 0.001f)
            tangent = LineAxis;
        tangent.Normalize();

        _lineWidth = Mathf.Clamp(width, MinimumLineWidth, Radius * 1.8f);
        _manualLineOrder = true;
        int aliveCount = LivingBoidCount;
        int index = 0;
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive) continue;
            float t = aliveCount <= 1 ? 0.5f : (float)index / (aliveCount - 1);
            boid.HomePosition = new Vector2(0f, Mathf.Lerp(-_lineWidth * 0.5f, _lineWidth * 0.5f, t));
            index++;
        }

        Vector2 forward = new(tangent.y, -tangent.x);
        Vector2 towardEnemy = enemyHint - center;
        if (towardEnemy.sqrMagnitude > 0.001f && Vector2.Dot(forward, towardEnemy) < 0f)
            forward = -forward;

        _boidWorldPositions.Clear();
        _boidWorldVelocities.Clear();
        foreach (var boid in _boids)
        {
            _boidWorldPositions.Add(transform.TransformPoint(boid.Position));
            _boidWorldVelocities.Add(transform.TransformVector(boid.Velocity));
        }
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].Position = transform.InverseTransformPoint(_boidWorldPositions[i]);
            _boids[i].Velocity = transform.InverseTransformVector(_boidWorldVelocities[i]);
        }
        MoveTo(center);
        _manualLineOrder = true;
    }

    public void SetSelected(bool selected)
    {
        if (_selected == selected) return;
        _selected = selected;
        float scale = BoidRadius * 2f * (_selected ? 1.3f : 1f);
        foreach (var renderer in _boidRenderers)
        {
            if (renderer == null) continue;
            renderer.transform.localScale = Vector3.one * scale;
            CircleCollider2D collider = renderer.GetComponent<CircleCollider2D>();
            if (collider != null) collider.radius = BoidRadius / scale;
        }
        if (_visualiser != null) _visualiser.SetSelected(_selected);
    }

    internal void PulseMeleeClash(BoidAgentState boid, float duration)
    {
        int index = _boids.IndexOf(boid);
        if (index < 0 || index >= _boidRenderers.Count) return;
        SpriteRenderer renderer = _boidRenderers[index];
        if (renderer == null) return;

        boid.BeginClashColorPulse(duration);
        ApplyClashColorPulse(renderer, boid.ClashColorPulseProgress);
    }

    public float DistanceToNearestBoid(Vector2 worldPosition)
    {
        float nearestDistance = float.PositiveInfinity;
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive) continue;
            Vector2 position = transform.TransformPoint(boid.Position);
            nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(worldPosition, position));
        }
        return nearestDistance;
    }

    public bool HasLivingBoidInRect(Rect worldRect)
    {
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive) continue;
            if (worldRect.Contains(transform.TransformPoint(boid.Position))) return true;
        }
        return false;
    }

    public void SetMeleeState(bool inMelee)
    {
        if (_inMelee == inMelee) return;
        _inMelee = inMelee;
        Melee(inMelee);
    }

    public bool RecordClashLoss(float pressureMargin)
    {
        if (_clashCooldownRemaining > 0f) return false;
        _clashCooldownRemaining = 0.3f;
        float threshold = 1f + AverageProfileValue(profile => profile.MeleeSkill) / 100f * 0.45f;
        float pressure = 0.28f + Mathf.Clamp01(pressureMargin) * 0.22f;
        _faultPressure = Mathf.Min(threshold * 2.5f, _faultPressure + pressure);
        return true;
    }

    public float ApplyDamage(BoidAgentState target, float damage)
    {
        return ApplyDamage(target, damage, transform.position, 1.66f);
    }

    public float ApplyDamage(BoidAgentState target, float damage, Vector2 attackerPivotPosition, float chargeDistance = 1.66f)
    {
        if (target == null || !_boids.Contains(target)) return 0f;
        float appliedDamage = target.ApplyDamage(damage);
        if (appliedDamage > 0f && target.LifeState == BoidLifeState.Dead)
            BoidDied?.Invoke(target);
        if (appliedDamage > 0f && LivingBoidCount == 0)
        {
            KillUnit();
            return appliedDamage;
        }
        if (appliedDamage > 0f)
        {
            _shock += appliedDamage;
            _lastAttackerPivotPosition = attackerPivotPosition;
            _lastChargeDistance = chargeDistance;
            if (_shock > Mathf.Max(0.1f, Morale) && _faultCooldownRemaining <= 0f && !IsFaultSlipping)
                BeginFaultSlip(attackerPivotPosition, chargeDistance);
        }
        return appliedDamage;
    }

    public Vector2 WorldPosition(BoidAgentState boid) => transform.TransformPoint(boid.Position);
    public Vector2 WorldVelocity(BoidAgentState boid) => (Vector2)transform.TransformVector(boid.Velocity) + PivotVelocity;

    public void ResolveContact(BoidAgentState boid, Vector2 worldPositionDelta, Vector2 worldVelocityDelta)
    {
        boid.Position += (Vector2)transform.InverseTransformVector(worldPositionDelta);
        boid.Velocity += (Vector2)transform.InverseTransformVector(worldVelocityDelta);
    }

    void BeginFaultSlip(Vector2 attackerPivotPosition, float chargeDistance)
    {
        Vector2 towardWinner = attackerPivotPosition - (Vector2)transform.position;
        if (towardWinner.sqrMagnitude < 0.001f) towardWinner = transform.right;
        _faultSlipAxis = towardWinner.normalized;
        Vector2 localAxis = transform.InverseTransformVector(_faultSlipAxis).normalized;

        _livingScratch.Clear();
        foreach (var boid in _boids)
            if (boid.IsAlive) _livingScratch.Add(boid);

        float rearDepth = float.PositiveInfinity;
        float frontDepth = float.NegativeInfinity;
        foreach (var boid in _livingScratch)
        {
            float depth = Vector2.Dot(boid.Position, localAxis);
            rearDepth = Mathf.Min(rearDepth, depth);
            frontDepth = Mathf.Max(frontDepth, depth);
        }

        foreach (var boid in _livingScratch)
        {
            float depth = Vector2.Dot(boid.Position, localAxis);
            float frontness = frontDepth - rearDepth > 0.0001f
                ? Mathf.InverseLerp(rearDepth, frontDepth, depth)
                : 0f;
            float delay = frontness * FaultMaximumDelay;
            float moveDuration = Mathf.Lerp(FaultRearMoveDuration, FaultFrontMoveDuration, frontness);
            boid.CancelCharge();
            boid.SetFaultMotion(delay, moveDuration);
        }

        _shock = 0f;
        _faultPressure = Mathf.Max(0f, _faultPressure - Mathf.Max(0.1f, Morale));
        _faultSlipDistance = Mathf.Min(Mathf.Max(chargeDistance * 1.8f, Radius * 0.9f), Radius * 1.5f);
        _faultPivotDistance = Radius * 0.48f;
        _faultPivotDisplacement = 0f;
        _faultSlipAge = 0f;
        _faultCooldownRemaining = 1.6f;
        _pulseCooldownRemaining = 1.2f;
        _faultRecoveryRemaining = FaultSlipDuration;
        _hasDestination = false;
        _pursuitActive = false;
        _pursuitTarget = null;
        InvalidatePath();
        _manualLineOrder = false;
        _pivotVelocity = Vector2.zero;
        State = UnitState.Fleeing;
    }

    void UpdateFaultPressure(float deltaTime)
    {
        _faultCooldownRemaining = Mathf.Max(0f, _faultCooldownRemaining - deltaTime);
        _clashCooldownRemaining = Mathf.Max(0f, _clashCooldownRemaining - deltaTime);
        _pulseCooldownRemaining = Mathf.Max(0f, _pulseCooldownRemaining - deltaTime);
        _faultPressure *= Mathf.Exp(-deltaTime / 22f);
        _shock *= Mathf.Exp(-deltaTime / 22f);
        if (_faultRecoveryRemaining > 0f)
        {
            _faultRecoveryRemaining -= deltaTime;
            if (_faultRecoveryRemaining <= 0f) State = UnitState.Idle;
        }
        if (_shock > Mathf.Max(0.1f, Morale) && _faultCooldownRemaining <= 0f && !IsFaultSlipping)
            BeginFaultSlip(_lastAttackerPivotPosition, _lastChargeDistance);
    }

    void UpdateFaultSlip(float deltaTime)
    {
        if (!IsFaultSlipping) return;
        _faultSlipAge += deltaTime;

        float pivotProgress = Mathf.Clamp01(_faultSlipAge / 0.14f);
        float easedPivotProgress = pivotProgress * pivotProgress * (3f - 2f * pivotProgress);
        float pivotDisplacement = _faultPivotDistance * easedPivotProgress;
        float pivotStep = pivotDisplacement - _faultPivotDisplacement;
        Vector2 pivotDelta = -_faultSlipAxis * pivotStep;
        transform.position += (Vector3)pivotDelta;
        Vector2 localCompensation = (Vector2)transform.InverseTransformVector(-pivotDelta);
        foreach (var boid in _boids)
            boid.Position += localCompensation;
        _faultPivotDisplacement = pivotDisplacement;

        bool recovered = true;
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive) continue;
            float previousOffset = boid.FaultOffset;
            float localTime = _faultSlipAge - boid.FaultDelayRemaining;
            if (localTime <= 0f)
            {
                boid.FaultOffset = 0f;
            }
            else if (localTime < boid.FaultMoveDuration)
            {
                float progress = localTime / boid.FaultMoveDuration;
                boid.FaultOffset = _faultSlipDistance * Mathf.SmoothStep(0f, 1f, progress);
            }
            else if (localTime < boid.FaultMoveDuration + FaultHoldDuration)
            {
                boid.FaultOffset = _faultSlipDistance;
            }
            else if (localTime < boid.FaultMoveDuration + FaultHoldDuration + FaultReturnDuration)
            {
                float progress = (localTime - boid.FaultMoveDuration - FaultHoldDuration) / FaultReturnDuration;
                boid.FaultOffset = _faultSlipDistance * (1f - Mathf.SmoothStep(0f, 1f, progress));
            }
            else
            {
                boid.FaultOffset = 0f;
            }
            float retreatStep = boid.FaultOffset - previousOffset;
            if (Mathf.Abs(retreatStep) > 0f)
                boid.Position += (Vector2)transform.InverseTransformVector(-_faultSlipAxis * retreatStep);
            if (boid.FaultOffset >= 1f) recovered = false;
        }

        if (_faultSlipAge <= FaultSlipDuration || !recovered) return;
        _faultSlipAge = -1f;
        foreach (var boid in _boids)
        {
            boid.FaultOffset = 0f;
            boid.SetFaultMotion(0f, 0.01f);
            boid.FaultDelayRemaining = 0f;
        }
    }

    void EnsureInitialized()
    {
        IReadOnlyList<FlockProfileCount> composition = _profile != null ? _profile.Composition : _composition;
        if (_initialized || composition == null || composition.Count == 0) return;

        if (_profile != null)
            _lineWidth = Mathf.Clamp(_profile.LineWidth, 0.5f, Radius * 1.8f);

        _boids.Clear();
        var profiles = new List<BoidProfile>();
        foreach (var entry in composition)
        {
            if (entry == null || entry.Profile == null) continue;
            for (int i = 0; i < Mathf.Max(0, entry.Count); i++)
                profiles.Add(entry.Profile);
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            var boid = new BoidAgentState(_nextBoidId++, profiles[i], RandomBlobOffset())
            {
                Facing = transform.right
            };
            _boids.Add(boid);
        }

        _lineWidth = Mathf.Clamp(_lineWidth, MinimumLineWidth, Radius * 1.8f);

        _army = GetComponentInParent<Army>();
        CreateVisuals();
        CreateVisualiser();
        _initialized = true;
    }

    void ResetBlobHomePositions()
    {
        foreach (var boid in _boids)
            if (boid.IsAlive) boid.HomePosition = RandomBlobOffset();
    }

    Vector2 RandomBlobOffset()
    {
        Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
        float gaussianRadius = Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(0.0001f, UnityEngine.Random.value))) * Radius * 0.22f;
        return direction * Mathf.Min(gaussianRadius, Radius * 0.85f);
    }

    void MovePivot(float deltaTime)
    {
        Vector2 position = transform.position;
        _pivotVelocity = Vector2.zero;
        if (_pursuitActive)
        {
            if (_pursuitTarget == null)
            {
                _pursuitActive = false;
                _hasDestination = false;
                InvalidatePath();
                State = UnitState.Idle;
            }
            else
            {
                _destination = GetPursuitDestination();
                _hasDestination = true;
                if ((_destination - _pathTarget).sqrMagnitude > 0.04f)
                    _pathInvalid = true;
            }
        }

        if (_hasDestination)
        {
            Vector2 offset = _destination - position;
            if (offset.sqrMagnitude <= 0.0004f && _linePushVelocity.sqrMagnitude <= 0.0025f)
            {
                position = _destination;
                if (_pursuitActive)
                {
                    _pivotVelocity = Vector2.zero;
                }
                else
                {
                    _hasDestination = false;
                    State = UnitState.Idle;
                    InvalidatePath();
                    if (_manualLineOrder)
                    {
                        _manualLineOrder = false;
                        ResetBlobHomePositions();
                        if (_formation != null && !_formation.IsThreatFacing) _formation = null;
                    }
                }
            }
            else if (deltaTime > 0f && offset.sqrMagnitude > 0.000001f)
            {
                float maxSpeed = MaxPivotSpeed();
                if (_pathInvalid || _movementPath == null || Time.time >= _nextPathRebuild)
                    RebuildPath();

                Vector2 step = Vector2.zero;
                if (_movementPath != null && _movementPath.Count > 0)
                {
                    while (_pathIndex < _movementPath.Count - 1
                        && Vector2.Distance(position, _movementPath[_pathIndex]) <= 0.2f)
                        _pathIndex++;

                    Vector2 waypointOffset = _movementPath[_pathIndex] - position;
                    if (waypointOffset.sqrMagnitude > 0.000001f)
                        step = waypointOffset.normalized * Mathf.Min(waypointOffset.magnitude, maxSpeed * deltaTime);
                }
                _pivotVelocity = step / deltaTime;
                position += step;
            }
        }

        transform.position = (Vector3)position + (Vector3)(_linePushVelocity * deltaTime);
    }

    float MaxPivotSpeed() => Mathf.Min(6.24f, MaxProfileValue(profile => profile.MaxSpeed) * 0.65f);

    Vector2 GetPursuitDestination() => _pursuitTarget.transform.position;

    void EnsurePathfinder()
    {
        if (_pathfinder != null) return;
        var pathObject = new GameObject("Flock NavPathfinder");
        pathObject.transform.SetParent(transform, false);
        var pathfinder = pathObject.AddComponent<MovementSystem.NavPathfinder>();
        pathfinder.SetNavigationSize(Vector2.one * (Radius * 2f));
        _pathfinder = pathfinder;
    }

    void RebuildPath()
    {
        EnsurePathfinder();
        _pathTarget = _destination;
        _movementPath = _pathfinder.GetPath(_destination);
        _pathIndex = _movementPath != null && _movementPath.Count > 1 ? 1 : 0;
        _pathInvalid = false;
        _nextPathRebuild = Time.time + 0.5f;
    }

    void InvalidatePath()
    {
        _movementPath = null;
        _pathIndex = 0;
        _pathInvalid = true;
    }

    void SimulateBoids(float deltaTime)
    {
        foreach (var bucket in _spatialBuckets.Values)
        {
            bucket.Clear();
            _unusedBuckets.Push(bucket);
        }
        _spatialBuckets.Clear();
        foreach (var boid in _boids)
        {
            if (!boid.IsAlive) continue;
            Vector2Int cell = CellFor(boid.Position);
            if (!_spatialBuckets.TryGetValue(cell, out var bucket))
            {
                bucket = _unusedBuckets.Count > 0 ? _unusedBuckets.Pop() : new List<BoidAgentState>();
                _spatialBuckets.Add(cell, bucket);
            }
            bucket.Add(boid);
        }

        while (_nextPositions.Count < _boids.Count)
        {
            _nextPositions.Add(Vector2.zero);
            _nextVelocities.Add(Vector2.zero);
        }

        int activeIndex = 0;
        int livingCount = LivingBoidCount;
        for (int boidIndex = 0; boidIndex < _boids.Count; boidIndex++)
        {
            var boid = _boids[boidIndex];
            _nextPositions[boidIndex] = boid.Position;
            _nextVelocities[boidIndex] = boid.Velocity;
            if (!boid.IsAlive || boid.Profile == null) continue;
            boid.TickCooldowns(deltaTime);

            Vector2 target = GetBoidTarget(boid, activeIndex++, livingCount);
            float targetStrength = boid.ChargeTarget != null ? 5.2f
                : PivotVelocity.sqrMagnitude > 0.0025f ? 3.2f
                : _formation != null ? 2.5f
                : 1.3f;

            Vector2 cohesion = Vector2.zero;
            Vector2 alignment = Vector2.zero;
            Vector2 separation = Vector2.zero;
            int neighbors = 0;
            Vector2Int cell = CellFor(boid.Position);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (!_spatialBuckets.TryGetValue(cell + new Vector2Int(x, y), out var bucket)) continue;
                foreach (var other in bucket)
                {
                    if (other == boid) continue;
                    Vector2 difference = boid.Position - other.Position;
                    float distanceSquared = difference.sqrMagnitude;
                    if (distanceSquared >= NeighborRadius * NeighborRadius) continue;

                    neighbors++;
                    cohesion += other.Position;
                    alignment += other.Velocity;
                    float distance = Mathf.Sqrt(Mathf.Max(distanceSquared, 0.001f));
                    separation += difference / distance * ((NeighborRadius - distance) / NeighborRadius * 0.94f * boid.Profile.Separation / 1.8f);
                }
            }

            Vector2 steering = (target - boid.Position) * targetStrength;
            if (neighbors > 0)
            {
                cohesion = cohesion / neighbors - boid.Position;
                alignment = alignment / neighbors - boid.Velocity;
                steering += cohesion * boid.Profile.Cohesion;
                steering += alignment * boid.Profile.Alignment;
            }
            steering += separation * boid.Profile.Separation;
            steering += boid.ExternalSeparation;
            float distanceFromPivot = boid.Position.magnitude;
            float softRadius = Radius * 0.92f;
            if (distanceFromPivot > softRadius)
                steering -= boid.Position / distanceFromPivot * ((distanceFromPivot - softRadius) * 3.3f);
            if (boid.Profile.BrownianMotion > 0f)
                steering += UnityEngine.Random.insideUnitCircle * boid.Profile.BrownianMotion * 2.5f;

            Vector2 acceleration = Vector2.ClampMagnitude(steering, boid.Profile.Acceleration);
            Vector2 velocity = boid.Velocity + acceleration * deltaTime;
            velocity *= Mathf.Exp(-SteeringDamping * deltaTime);
            velocity = Vector2.ClampMagnitude(velocity, boid.Profile.MaxSpeed);
            Vector2 nextPosition = boid.Position + velocity * deltaTime;
            ConstrainBoidMotionToTerrain(boid, ref velocity, ref nextPosition);
            _nextVelocities[boidIndex] = velocity;
            _nextPositions[boidIndex] = nextPosition;
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            var boid = _boids[i];
            if (!boid.IsAlive) continue;
            boid.Velocity = _nextVelocities[i];
            boid.Position = _nextPositions[i];
            ConstrainBoidToRadius(boid);
        }
    }

    void ConstrainBoidMotionToTerrain(BoidAgentState boid, ref Vector2 velocity, ref Vector2 nextPosition)
    {
        if (_boidObstacleMask.value == 0) return;

        Vector2 start = transform.TransformPoint(boid.Position);
        Vector2 end = transform.TransformPoint(nextPosition);
        Vector2 displacement = end - start;
        float distance = displacement.magnitude;
        if (distance <= 0.0001f) return;

        Vector2 direction = displacement / distance;
        int hitCount = Physics2D.CircleCast(
            start,
            BoidRadius,
            direction,
            _boidObstacleFilter,
            _boidObstacleHits,
            distance);
        int closestIndex = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D candidate = _boidObstacleHits[i];
            if (candidate.collider == null || candidate.distance >= closestDistance) continue;
            if (candidate.distance <= 0.0001f
                && candidate.normal.sqrMagnitude > 0.0001f
                && Vector2.Dot(direction, candidate.normal) >= 0f)
                continue;
            closestIndex = i;
            closestDistance = candidate.distance;
        }
        if (closestIndex < 0) return;

        RaycastHit2D obstacleHit = _boidObstacleHits[closestIndex];
        const float CollisionSkin = 0.005f;
        end = start + direction * Mathf.Max(0f, obstacleHit.distance - CollisionSkin);
        Vector2 worldVelocity = transform.TransformVector(velocity);
        float intoObstacle = Vector2.Dot(worldVelocity, obstacleHit.normal);
        if (intoObstacle < 0f)
            worldVelocity -= obstacleHit.normal * intoObstacle;
        velocity = transform.InverseTransformVector(worldVelocity);
        nextPosition = transform.InverseTransformPoint(end);
    }

    Vector2 GetBoidTarget(BoidAgentState boid, int index, int livingCount)
    {
        Vector2 target;
        if (boid.ChargeTarget != null && boid.ChargeTargetFlock != null && !IsFaultSlipping)
        {
            Vector2 targetWorld = boid.ChargeTargetFlock.WorldPosition(boid.ChargeTarget)
                + boid.ChargeTargetFlock.WorldVelocity(boid.ChargeTarget) * 0.12f;
            target = transform.InverseTransformPoint(targetWorld);
        }
        else if (PivotVelocity.sqrMagnitude > 0.0025f)
        {
            Vector2 heading = PivotVelocity.normalized;
            Vector2 lateral = new(-heading.y, heading.x);
            float spacing = 9f * PrototypeWorldScale;
            int rowCapacity = Mathf.Max(2, Mathf.FloorToInt(Radius * 0.8f / spacing));
            int lanes = Mathf.Min(2, Mathf.Max(1, Mathf.CeilToInt((float)livingCount / rowCapacity)));
            int rows = Mathf.CeilToInt((float)livingCount / lanes);
            int row = index / lanes;
            int lane = index % lanes;
            float lateralSpacing = lanes > 1 ? Mathf.Min(8.5f * PrototypeWorldScale, Radius * 0.16f) : 0f;
            float laneOffset = (lane - (lanes - 1) * 0.5f) * lateralSpacing;
            float progress = rows > 1 ? (float)row / (rows - 1) : 0f;
            float trail = Radius * (0.05f + progress * 0.85f);
            Vector2 worldTarget = (Vector2)transform.position - heading * trail + lateral * laneOffset;
            Vector2 trailTarget = transform.InverseTransformPoint(worldTarget);
            target = _formation != null
                ? Vector2.Lerp(trailTarget, GetFormationBoidTarget(index, livingCount), 0.9f)
                : trailTarget;
        }
        else if (_formation != null)
        {
            target = GetFormationBoidTarget(index, livingCount);
        }
        else
        {
            target = boid.HomePosition;
        }

        if (boid.FaultOffset > 0f && IsFaultSlipping)
        {
            Vector2 worldTarget = (Vector2)transform.TransformPoint(target) - _faultSlipAxis * boid.FaultOffset;
            Vector2 offset = worldTarget - (Vector2)transform.position;
            if (offset.magnitude > Radius * 0.94f) offset = offset.normalized * Radius * 0.94f;
            target = (Vector2)transform.InverseTransformPoint((Vector2)transform.position + offset);
        }

        return target;
    }

    Vector2 GetFormationBoidTarget(int index, int livingCount)
    {
        Vector2 start = _formation.Start;
        Vector2 delta = _formation.End - start;
        float length = delta.magnitude;
        Vector2 tangent = length > 0.0001f ? delta / length : LineAxis;
        float endpointClearance = 4.5f * PrototypeWorldScale;
        float startGap = GetLineLink(0) != null ? Mathf.Min(endpointClearance, length * 0.15f) : 0f;
        float endGap = GetLineLink(1) != null ? Mathf.Min(endpointClearance, length * 0.15f) : 0f;
        float along = startGap + (index + 0.5f) / Mathf.Max(1, livingCount) * Mathf.Max(0f, length - startGap - endGap);
        return transform.InverseTransformPoint(start + tangent * along);
    }

    void ConstrainBoidToRadius(BoidAgentState boid)
    {
        float distance = boid.Position.magnitude;
        if (distance <= Radius) return;
        Vector2 normal = boid.Position / distance;
        boid.Position = normal * Radius;
        float outwardSpeed = Vector2.Dot(boid.Velocity, normal);
        if (outwardSpeed > 0f) boid.Velocity -= normal * outwardSpeed;
    }

    public void ConstrainBoidsToRadius()
    {
        foreach (var boid in _boids)
            if (boid.IsAlive) ConstrainBoidToRadius(boid);
    }

    static Vector2Int CellFor(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / NeighborRadius),
            Mathf.FloorToInt(position.y / NeighborRadius));
    }

    void CreateVisuals()
    {
        foreach (var renderer in _boidRenderers)
        {
            if (renderer != null) Destroy(renderer.gameObject);
        }
        _boidRenderers.Clear();

        foreach (var boid in _boids)
        {
            var visual = new GameObject($"Boid {boid.Id}");
            int unitLayer = LayerMask.NameToLayer("Unit");
            if (unitLayer >= 0) visual.layer = unitLayer;
            var body = visual.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = visual.AddComponent<CircleCollider2D>();
            float scale = BoidRadius * 2f * (_selected ? 1.3f : 1f);
            collider.radius = BoidRadius / scale;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = boid.Profile != null && boid.Profile.Sprite != null
                ? boid.Profile.Sprite
                : BoidSpriteLibrary.Square;
            renderer.sharedMaterial = BoidSpriteLibrary.ModelMaterial;
            renderer.sortingLayerName = "Unit";
            renderer.sortingOrder = 1;
            renderer.color = ArmyColor;
            visual.transform.localScale = Vector3.one * scale;
            _boidRenderers.Add(renderer);
        }
        UpdateBoidVisuals();
    }

    void CreateVisualiser()
    {
        if (_visualiser == null) _visualiser = GetComponentInChildren<FlockVisualiser>(true);
        if (_visualiser == null)
        {
            var visualiserObject = new GameObject("Flock Visualiser");
            visualiserObject.transform.SetParent(transform, false);
            _visualiser = visualiserObject.AddComponent<FlockVisualiser>();
        }
        _visualiser.Initialize(this);
    }

    float MaxProfileValue(Func<BoidProfile, float> valueSelector)
    {
        float max = 0f;
        foreach (var boid in _boids)
            if (boid.IsAlive && boid.Profile != null)
                max = Mathf.Max(max, valueSelector(boid.Profile));
        return max;
    }

    void UpdateBoidVisuals(float deltaTime = 0f)
    {
        int count = Mathf.Min(_boids.Count, _boidRenderers.Count);
        for (int i = 0; i < count; i++)
        {
            var boid = _boids[i];
            var renderer = _boidRenderers[i];
            if (renderer == null) continue;

            renderer.enabled = boid.IsAlive;
            renderer.color = ArmyColor;
            bool pulseEnded = boid.TickClashColorPulse(deltaTime);
            if (boid.IsClashColorPulsing)
                ApplyClashColorPulse(renderer, boid.ClashColorPulseProgress);
            else if (pulseEnded)
                ApplyClashColorPulse(renderer, 1f);
            if (!boid.IsAlive) continue;
            renderer.transform.position = transform.TransformPoint(boid.Position);

            Vector2 velocity = WorldVelocity(boid);
            bool moving = velocity.magnitude > BoidFacingSpeedThreshold;
            Vector2 desiredFacing = boid.Facing;
            if (HasThreatFacingFormation)
            {
                desiredFacing = Formation.Facing.normalized;
                if (moving)
                {
                    float deviation = Vector2.SignedAngle(desiredFacing, velocity.normalized);
                    float clampedDeviation = Mathf.Clamp(deviation, -EngagedFacingDeviation, EngagedFacingDeviation);
                    desiredFacing = Quaternion.AngleAxis(clampedDeviation, Vector3.forward) * desiredFacing;
                }
            }
            else if (moving)
            {
                desiredFacing = velocity.normalized;
            }

            Vector2 currentFacing = boid.Facing.sqrMagnitude > 0.0001f ? boid.Facing.normalized : (Vector2)transform.right;
            if (deltaTime > 0f && desiredFacing.sqrMagnitude > 0.0001f)
            {
                float turn = Mathf.Clamp(
                    Vector2.SignedAngle(currentFacing, desiredFacing),
                    -BoidTurnSpeed * deltaTime,
                    BoidTurnSpeed * deltaTime);
                currentFacing = Quaternion.AngleAxis(turn, Vector3.forward) * currentFacing;
                boid.Facing = currentFacing.normalized;
            }

            float angle = Mathf.Atan2(boid.Facing.y, boid.Facing.x) * Mathf.Rad2Deg - 90f;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void ApplyClashColorPulse(SpriteRenderer renderer, float progress)
    {
        Material material = renderer.sharedMaterial;
        if (material == null) return;
        int colorProperty = material.HasProperty(LightColorId) ? LightColorId : ColorId;
        if (!material.HasProperty(colorProperty)) return;

        renderer.GetPropertyBlock(_boidPropertyBlock);
        float intensityScale = Mathf.Lerp(ClashColorPulseIntensityScale, 1f, progress);
        _boidPropertyBlock.SetColor(colorProperty, material.GetColor(colorProperty) * intensityScale);
        renderer.SetPropertyBlock(_boidPropertyBlock);
    }

    void OnDisable()
    {
        foreach (var renderer in _boidRenderers)
            if (renderer != null) renderer.enabled = false;
    }

    void OnEnable()
    {
        if (_initialized) UpdateBoidVisuals();
    }

    void OnDestroy()
    {
        foreach (var renderer in _boidRenderers)
            if (renderer != null) Destroy(renderer.gameObject);
    }

}
