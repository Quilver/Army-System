using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class FlockCombatWorld : MonoBehaviour
{
    // The reference behavior uses Canvas pixels; the battle map is scaled at 0.052 world units per pixel.
    const float PrototypeWorldScale = 0.052f;
    const float CellSize = 26f * PrototypeWorldScale;
    const float BoidContactDistance = 8.5f * PrototypeWorldScale;
    const float ClashDistance = 11f * PrototypeWorldScale;

    readonly List<Flock> _flocks = new();
    readonly List<Contact> _contacts = new();
    readonly Dictionary<Vector2Int, List<int>> _buckets = new();
    readonly Stack<List<int>> _unusedBuckets = new();
    readonly HashSet<Flock> _engagedFlocks = new();
    readonly Dictionary<AgentPairKey, float> _contactCooldowns = new();
    readonly List<ChargeRequest> _pendingCharges = new();
    readonly List<AgentPairKey> _cooldownKeys = new();
    readonly FlockSegmentPressureRules _segmentPressure = new();
    [SerializeField, Min(0f)] float _sDistance = 184f * PrototypeWorldScale;
    [SerializeField, Min(0f)] float _aDistance = 32f * PrototypeWorldScale;
    const float ClashPulseDuration = 0.2f;
    float _nextFlockRefresh;

    void Update()
    {
        RefreshFlocks();
        _flocks.RemoveAll(flock => flock == null);
        FlockFormationRules.UpdateThreatFormations(_flocks, _sDistance, Time.deltaTime);
        FlockFormationRules.ConnectAlliedLines(_flocks);
        _segmentPressure.Update(_flocks, _aDistance, Time.deltaTime);
        BuildContactBuckets();
        ApplyCrossFlockSeparation();
        UpdateBattlePulses(Time.deltaTime);
        TickContactCooldowns(Time.deltaTime);
    }

    void LateUpdate()
    {
        RefreshFlocks();
        BuildContactBuckets();
        ResolveContacts();
        UpdateMeleeStates();
    }

    void RefreshFlocks()
    {
        if (Time.unscaledTime < _nextFlockRefresh) return;
        _nextFlockRefresh = Time.unscaledTime + 0.5f;
        _flocks.Clear();
        _flocks.AddRange(FindObjectsByType<Flock>(FindObjectsSortMode.None));
        _flocks.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
    }

    void BuildContactBuckets()
    {
        foreach (var bucket in _buckets.Values)
        {
            bucket.Clear();
            _unusedBuckets.Push(bucket);
        }
        _buckets.Clear();
        _contacts.Clear();

        foreach (var flock in _flocks)
        {
            if (flock == null) continue;
            foreach (var boid in flock.Boids)
            {
                if (!boid.IsAlive) continue;
                int contactIndex = _contacts.Count;
                Vector2 position = flock.WorldPosition(boid);
                _contacts.Add(new Contact(flock, boid, position));
                Vector2Int cell = CellFor(position);
                if (!_buckets.TryGetValue(cell, out var bucket))
                {
                    bucket = _unusedBuckets.Count > 0 ? _unusedBuckets.Pop() : new List<int>();
                    _buckets.Add(cell, bucket);
                }
                bucket.Add(contactIndex);
            }
        }
    }

    void ApplyCrossFlockSeparation()
    {
        foreach (var contact in _contacts)
            contact.Boid.ClearExternalSeparation();

        for (int i = 0; i < _contacts.Count; i++)
        {
            Contact a = _contacts[i];
            Vector2Int cell = CellFor(a.Position);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (!_buckets.TryGetValue(cell + new Vector2Int(x, y), out var bucket)) continue;
                foreach (int j in bucket)
                {
                    if (j <= i) continue;
                    Contact b = _contacts[j];
                    if (a.Flock == b.Flock) continue;
                    if (a.Flock.TeamId == b.Flock.TeamId
                        && !a.Flock.HasThreatFacingFormation
                        && !b.Flock.HasThreatFacingFormation)
                        continue;
                    Vector2 difference = a.Position - b.Position;
                    float distance = difference.magnitude;
                    if (distance <= 0.052f || distance >= CellSize) continue;

                    float falloff = (CellSize - distance) / CellSize;
                    float aScale = a.Boid.Profile != null ? a.Boid.Profile.Separation / 1.8f : 1f;
                    float bScale = b.Boid.Profile != null ? b.Boid.Profile.Separation / 1.8f : 1f;
                    float teamScale = a.Flock.TeamId == b.Flock.TeamId ? 0.94f : 0.78f;
                    Vector2 direction = difference / distance;
                    a.Boid.AddExternalSeparation((Vector2)a.Flock.transform.InverseTransformVector(direction * falloff * teamScale * aScale));
                    b.Boid.AddExternalSeparation((Vector2)b.Flock.transform.InverseTransformVector(-direction * falloff * teamScale * bScale));
                }
            }
        }

    }

    void ResolveContacts()
    {
        _engagedFlocks.Clear();
        float clashDistanceSquared = ClashDistance * ClashDistance;

        for (int i = 0; i < _contacts.Count; i++)
        {
            Contact a = _contacts[i];
            if (a.Flock == null || !a.Boid.IsAlive) continue;
            Vector2Int cell = CellFor(a.Position);
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (!_buckets.TryGetValue(cell + new Vector2Int(x, y), out var bucket)) continue;
                foreach (int j in bucket)
                {
                    if (j <= i) continue;
                    Contact b = _contacts[j];
                    if (b.Flock == null || !a.Boid.IsAlive || !b.Boid.IsAlive) continue;
                    if (a.Flock != b.Flock
                        && a.Flock.TeamId == b.Flock.TeamId
                        && !a.Flock.HasThreatFacingFormation
                        && !b.Flock.HasThreatFacingFormation)
                        continue;
                    Vector2 positionA = a.Flock.WorldPosition(a.Boid);
                    Vector2 positionB = b.Flock.WorldPosition(b.Boid);
                    Vector2 difference = positionA - positionB;
                    float distanceSquared = difference.sqrMagnitude;
                    if (distanceSquared >= clashDistanceSquared) continue;

                    Vector2 normal = distanceSquared > 0.000001f
                        ? difference / Mathf.Sqrt(distanceSquared)
                        : (a.Flock.GetInstanceID() < b.Flock.GetInstanceID() ? Vector2.right : Vector2.left);
                    float distance = Mathf.Sqrt(distanceSquared);
                    if (distance < BoidContactDistance)
                        ResolveBounce(a, b, normal, distance);

                    if (a.Flock.TeamId == b.Flock.TeamId) continue;
                    _engagedFlocks.Add(a.Flock);
                    _engagedFlocks.Add(b.Flock);
                    var pairKey = new AgentPairKey(a, b);
                    if (_contactCooldowns.ContainsKey(pairKey)) continue;
                    _contactCooldowns[pairKey] = 1.15f;
                    ResolveClash(a, b, positionA, positionB);
                }
            }
        }

        foreach (var flock in _flocks)
            if (flock != null) flock.ConstrainBoidsToRadius();
    }

    static void ResolveBounce(Contact a, Contact b, Vector2 normal, float distance)
    {
        float penetration = BoidContactDistance - distance;
        a.Flock.ResolveContact(a.Boid, normal * (penetration * 0.5f), Vector2.zero);
        b.Flock.ResolveContact(b.Boid, -normal * (penetration * 0.5f), Vector2.zero);

        Vector2 relativeVelocity = a.Flock.WorldVelocity(a.Boid) - b.Flock.WorldVelocity(b.Boid);
        float closingSpeed = Vector2.Dot(relativeVelocity, normal);
        if (closingSpeed >= 0f) return;

        float impulse = -closingSpeed * 0.25f;
        a.Flock.ResolveContact(a.Boid, Vector2.zero, normal * impulse);
        b.Flock.ResolveContact(b.Boid, Vector2.zero, -normal * impulse);
    }

    void ResolveClash(Contact a, Contact b, Vector2 positionA, Vector2 positionB)
    {
        Vector2 towardB = (positionB - positionA).normalized;
        float impactDistance = Mathf.Min(6f * PrototypeWorldScale, Mathf.Min(a.Flock.Radius, b.Flock.Radius) * 0.06f);
        float maxSpeed = Mathf.Max(
            a.Boid.Profile != null ? a.Boid.Profile.MaxSpeed : 0f,
            b.Boid.Profile != null ? b.Boid.Profile.MaxSpeed : 0f);
        float impactSpeed = Mathf.Max(maxSpeed * 1.7f, 105f * PrototypeWorldScale);
        a.Flock.ResolveContact(a.Boid, -towardB * impactDistance, Vector2.zero);
        b.Flock.ResolveContact(b.Boid, towardB * impactDistance, Vector2.zero);
        a.Flock.ResolveContact(a.Boid, Vector2.zero, -towardB * impactSpeed);
        b.Flock.ResolveContact(b.Boid, Vector2.zero, towardB * impactSpeed);
        a.Flock.PulseMeleeClash(a.Boid, ClashPulseDuration);
        b.Flock.PulseMeleeClash(b.Boid, ClashPulseDuration);

        float pressureA = ClashPressure(a, towardB) + Random.Range(-0.12f, 0.12f);
        float pressureB = ClashPressure(b, -towardB) + Random.Range(-0.12f, 0.12f);
        if (pressureA >= pressureB)
            ResolveClashWinner(a, b, pressureA - pressureB);
        else
            ResolveClashWinner(b, a, pressureB - pressureA);
    }

    float ClashPressure(Contact contact, Vector2 towardOpponent)
    {
        BoidProfile profile = contact.Boid.Profile;
        if (profile == null) return 0f;
        float forwardSpeed = Mathf.Max(0f, Vector2.Dot(contact.Flock.WorldVelocity(contact.Boid), towardOpponent))
            / Mathf.Max(0.1f, profile.MaxSpeed);
        int nearbySupport = 0;
        foreach (var ally in _contacts)
        {
            if (ally.Flock.TeamId != contact.Flock.TeamId || ally.Boid == contact.Boid) continue;
            if (Vector2.Distance(ally.Position, contact.Flock.WorldPosition(contact.Boid)) < 1.768f)
                nearbySupport++;
        }
        return forwardSpeed * 0.55f
            + (contact.Boid.ChargeTarget != null ? 0.65f : 0f)
            + profile.Aggression / 100f * 0.3f
            + profile.MeleeSkill / 100f * 0.5f
            + Mathf.Min(5, nearbySupport) * 0.12f;
    }

    void ResolveClashWinner(Contact winner, Contact loser, float pressureMargin)
    {
        if (!loser.Flock.RecordClashLoss(pressureMargin)) return;
        if (winner.Flock.State == UnitState.Fleeing || winner.Boid.Profile == null || winner.Boid.Profile.MeleeDamage <= 0f)
            return;
        if (!winner.Boid.TryBeginMelee(winner.Boid.Profile.Aggression)) return;

        float damage = winner.Boid.Profile.MeleeDamage
            * Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp01(winner.Boid.Profile.MeleeSkill / 100f))
            * FlankMultiplier(
                loser.Flock,
                (Vector2)winner.Flock.transform.position - (Vector2)loser.Flock.transform.position);
        loser.Flock.ApplyDamage(loser.Boid, damage, winner.Flock.transform.position, _aDistance);
    }

    static float FlankMultiplier(Flock defender, Vector2 directionToAttacker)
    {
        Vector2 facing = defender.Formation != null ? defender.Formation.Facing : (Vector2)defender.transform.right;
        float angle = Vector2.Angle(facing, directionToAttacker);
        if (angle < 45f) return 4f;
        if (angle < 120f) return 2f;
        return 1f;
    }

    void UpdateMeleeStates()
    {
        foreach (var flock in _flocks)
            if (flock != null) flock.SetMeleeState(_engagedFlocks.Contains(flock));
    }

    void TickContactCooldowns(float deltaTime)
    {
        _cooldownKeys.Clear();
        foreach (var pair in _contactCooldowns) _cooldownKeys.Add(pair.Key);
        foreach (var key in _cooldownKeys)
        {
            float remaining = _contactCooldowns[key] - deltaTime;
            if (remaining <= 0f) _contactCooldowns.Remove(key);
            else _contactCooldowns[key] = remaining;
        }
    }

    void UpdateBattlePulses(float deltaTime)
    {
        foreach (var flock in _flocks)
        {
            if (flock == null) continue;
            foreach (var boid in flock.Boids)
            {
                boid.TickCharge(deltaTime);
                if (boid.ChargeTarget == null) continue;
                bool targetValid = boid.ChargeTargetFlock != null
                    && boid.ChargeTargetFlock.TeamId != flock.TeamId
                    && _flocks.Contains(boid.ChargeTargetFlock)
                    && boid.ChargeTarget.IsAlive
                    && boid.ChargeTargetFlock.Boids.Contains(boid.ChargeTarget);
                float distance = targetValid
                    ? Vector2.Distance(flock.WorldPosition(boid), boid.ChargeTargetFlock.WorldPosition(boid.ChargeTarget))
                    : float.PositiveInfinity;
                if (!targetValid || flock.IsFaultSlipping || boid.ChargeTimeRemaining <= 0f || distance > _aDistance * 1.7f)
                    boid.CancelCharge(1.1f);
            }
        }

        _pendingCharges.Clear();
        foreach (var source in _contacts)
        {
            BoidProfile profile = source.Boid.Profile;
            if (profile == null || profile.Aggression <= 0f || source.Flock.IsPulseLocked
                || source.Flock.State == UnitState.Fleeing || source.Boid.ChargeTarget != null
                || source.Boid.ChargeCooldownRemaining > 0f)
                continue;

            Contact? target = FindChargeTarget(source);
            if (!target.HasValue) continue;

            int neighbors = 0;
            int chargingNeighbors = 0;
            foreach (var ally in _contacts)
            {
                if (ally.Flock != source.Flock || ally.Boid == source.Boid) continue;
                if (Vector2.Distance(source.Position, ally.Position) > 54f * PrototypeWorldScale) continue;
                neighbors++;
                if (ally.Boid.ChargeTarget != null) chargingNeighbors++;
            }
            float localCharge = neighbors > 0 ? (float)chargingNeighbors / neighbors : 0f;
            float aggression = profile.Aggression / 100f;
            float field = aggression * 2.4f - 1.7f + localCharge * 2.4f;
            float isingProbability = 1f / (1f + Mathf.Exp(-field));
            float rate = aggression * (0.08f + 0.25f * isingProbability + 0.4f * localCharge);
            if (Random.value >= 1f - Mathf.Exp(-rate * deltaTime)) continue;
            _pendingCharges.Add(new ChargeRequest(source.Boid, target.Value.Boid, target.Value.Flock));
        }

        foreach (var charge in _pendingCharges)
            charge.Source.SetCharge(charge.Target, charge.TargetFlock, Random.Range(0.75f, 1.15f), Random.Range(1.2f, 3f));
    }

    Contact? FindChargeTarget(Contact source)
    {
        float nearestDistance = _aDistance;
        Contact? nearest = null;
        Vector2 facing = source.Flock.Formation != null
            ? source.Flock.Formation.Facing
            : (Vector2)source.Flock.transform.right;
        float frontDepth = Vector2.Dot(source.Position - (Vector2)source.Flock.transform.position, facing);
        if (frontDepth < -source.Flock.Radius * 0.12f) return null;

        foreach (var candidate in _contacts)
        {
            if (candidate.Flock.TeamId == source.Flock.TeamId) continue;
            Vector2 offset = candidate.Position - source.Position;
            float distance = offset.magnitude;
            if (distance >= nearestDistance || distance <= 0.0001f) continue;
            if (Vector2.Dot(offset / distance, facing) < 0.15f) continue;
            nearestDistance = distance;
            nearest = candidate;
        }
        return nearest;
    }

    void OnDrawGizmos()
    {
        IReadOnlyList<Flock> flocks = Application.isPlaying
            ? _flocks
            : FindObjectsByType<Flock>(FindObjectsSortMode.None);
        if (flocks == null) return;
        Color oldColor = Gizmos.color;
        const int segments = 32;
        foreach (var flock in flocks)
        {
            if (flock == null) continue;
            Vector2 pivot = flock.transform.position;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.65f);
            DrawGizmoCircle(pivot, flock.Radius + _sDistance, segments);

            FlockFormationData formation = flock.Formation;
            if (formation != null)
            {
                Gizmos.color = flock.ArmyColor;
                Gizmos.DrawLine(formation.Start, formation.End);
                Gizmos.DrawSphere(formation.Start, 0.08f);
                Gizmos.DrawSphere(formation.End, 0.08f);
            }

            for (int end = 0; end < 2; end++)
            {
                FlockEndpointLink link = flock.GetLineLink(end);
                if (link == null || link.Other == null || flock.GetInstanceID() >= link.Other.GetInstanceID()) continue;
                Vector2 joint = flock.SegmentEndpoint(end);
                Vector2 halfTension = link.Tension * 0.5f;
                Vector2 localEndpoint = joint - halfTension;
                Vector2 otherEndpoint = joint + halfTension;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(localEndpoint, otherEndpoint);
                Gizmos.DrawSphere(localEndpoint, 0.07f);
                Gizmos.DrawSphere(otherEndpoint, 0.07f);
                Gizmos.DrawSphere(joint, 0.09f);
            }

            if (!flock.IsSelected) continue;
            Gizmos.color = new Color(flock.ArmyColor.r, flock.ArmyColor.g, flock.ArmyColor.b, 0.9f);
            foreach (var boid in flock.Boids)
            {
                if (!boid.IsAlive) continue;
                Vector2 center = flock.WorldPosition(boid);
                DrawGizmoCircle(center, _aDistance, segments);
            }
        }
        Gizmos.color = oldColor;
    }

    static void DrawGizmoCircle(Vector2 center, float radius, int segments)
    {
        Vector3 previous = center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    static Vector2Int CellFor(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize));
    }

    readonly struct Contact
    {
        public readonly Flock Flock;
        public readonly BoidAgentState Boid;
        public readonly Vector2 Position;

        public Contact(Flock flock, BoidAgentState boid, Vector2 position)
        {
            Flock = flock;
            Boid = boid;
            Position = position;
        }
    }

    readonly struct AgentPairKey : System.IEquatable<AgentPairKey>
    {
        readonly long _first;
        readonly long _second;

        public AgentPairKey(Contact a, Contact b)
        {
            long idA = ((long)a.Flock.GetInstanceID() << 32) ^ (uint)a.Boid.Id;
            long idB = ((long)b.Flock.GetInstanceID() << 32) ^ (uint)b.Boid.Id;
            if (idA <= idB)
            {
                _first = idA;
                _second = idB;
            }
            else
            {
                _first = idB;
                _second = idA;
            }
        }

        public bool Equals(AgentPairKey other) => _first == other._first && _second == other._second;
        public override bool Equals(object obj) => obj is AgentPairKey other && Equals(other);
        public override int GetHashCode() => unchecked((_first.GetHashCode() * 397) ^ _second.GetHashCode());
    }

    readonly struct ChargeRequest
    {
        public readonly BoidAgentState Source;
        public readonly BoidAgentState Target;
        public readonly Flock TargetFlock;

        public ChargeRequest(BoidAgentState source, BoidAgentState target, Flock targetFlock)
        {
            Source = source;
            Target = target;
            TargetFlock = targetFlock;
        }
    }

}
