using System.Collections.Generic;
using UnityEngine;

public sealed class FlockSegmentPressureRules
{
    const float PrototypeWorldScale = 0.052f;
    const float ForceScale = PrototypeWorldScale;
    readonly Dictionary<long, Contact> _contacts = new();

    struct Contact
    {
        public Flock First;
        public Flock Second;
        public Vector2 Axis;
    }

    public void Update(IReadOnlyList<Flock> flocks, float actionDistance, float deltaTime)
    {
        var byId = new Dictionary<int, Flock>();
        var forces = new Dictionary<Flock, Vector2>();
        foreach (var flock in flocks)
        {
            if (flock == null) continue;
            byId[flock.GetInstanceID()] = flock;
            forces[flock] = Vector2.zero;
        }

        var stale = new List<long>();
        foreach (var pair in _contacts)
        {
            if (pair.Value.First == null || pair.Value.Second == null
                || !byId.ContainsKey(pair.Value.First.GetInstanceID())
                || !byId.ContainsKey(pair.Value.Second.GetInstanceID())
                || pair.Value.First.TeamId == pair.Value.Second.TeamId)
                stale.Add(pair.Key);
        }
        foreach (long key in stale) _contacts.Remove(key);

        float interactionRange = actionDistance * 0.5f;
        float releaseRange = interactionRange * 1.6f;
        if (interactionRange <= 0f) _contacts.Clear();
        if (interactionRange > 0f)
        {
            for (int i = 0; i < flocks.Count; i++)
            for (int j = i + 1; j < flocks.Count; j++)
            {
                Flock first = flocks[i];
                Flock second = flocks[j];
                if (first == null || second == null || first.TeamId == second.TeamId) continue;
                long key = PairKey(first.GetInstanceID(), second.GetInstanceID());
                if (first.Formation == null || second.Formation == null)
                {
                    _contacts.Remove(key);
                    continue;
                }

                ClosestSegmentPoints(first.Formation, second.Formation, out Vector2 pointA, out Vector2 pointB, out float distance);
                if (!_contacts.TryGetValue(key, out Contact contact))
                {
                    if (distance >= interactionRange) continue;
                    contact = new Contact { First = first, Second = second, Axis = GetSeparatingAxis(first, second, pointA, pointB) };
                    _contacts.Add(key, contact);
                }
                else if (distance >= releaseRange)
                {
                    _contacts.Remove(key);
                    continue;
                }

                float compression = Mathf.Clamp01((releaseRange - distance) / Mathf.Max(0.0001f, releaseRange));
                float firstDrive = Mathf.Max(0f, Vector2.Dot(first.PivotVelocity, contact.Axis));
                float secondDrive = Mathf.Max(0f, Vector2.Dot(second.PivotVelocity, -contact.Axis));
                float firstMelee = first.AverageProfileValue(profile => profile.MeleeSkill);
                float secondMelee = second.AverageProfileValue(profile => profile.MeleeSkill);
                float firstEffort = Mathf.Clamp01(firstDrive / Mathf.Max(0.1f, first.AverageProfileValue(profile => profile.MaxSpeed)))
                    * (1f - firstMelee / 100f * 0.25f);
                float secondEffort = Mathf.Clamp01(secondDrive / Mathf.Max(0.1f, second.AverageProfileValue(profile => profile.MaxSpeed)))
                    * (1f - secondMelee / 100f * 0.25f);
                float contactForce = compression * (600f + 1800f * compression) * ForceScale;
                float firstForce = contactForce * firstEffort * LineResistance(second);
                float secondForce = contactForce * secondEffort * LineResistance(first);
                forces[first] -= contact.Axis * firstForce;
                forces[second] += contact.Axis * secondForce;
            }
        }

        foreach (var flock in flocks)
        {
            if (flock == null) continue;
            for (int end = 0; end < 2; end++)
            {
                FlockEndpointLink link = flock.GetLineLink(end);
                if (link == null || link.Other == null || flock.GetInstanceID() >= link.Other.GetInstanceID()) continue;
                float gap = link.Tension.magnitude;
                float extension = Mathf.Max(0f, gap - 3f * PrototypeWorldScale);
                float tension = Mathf.Min(120f * PrototypeWorldScale, extension * 1.2f);
                if (gap <= 0.0001f || tension <= 0f) continue;
                Vector2 force = link.Tension / gap * tension;
                forces[flock] += force;
                if (forces.ContainsKey(link.Other)) forces[link.Other] -= force;
            }
        }

        foreach (var flock in flocks)
        {
            if (flock == null) continue;
            Vector2 velocity = (flock.LinePushVelocity + forces[flock] * deltaTime) * Mathf.Exp(-5f * deltaTime);
            float maximumSpeed = Mathf.Max(12f * PrototypeWorldScale, flock.AverageProfileValue(profile => profile.MaxSpeed) * 1.25f);
            if (velocity.magnitude > maximumSpeed) velocity = velocity.normalized * maximumSpeed;
            flock.SetLinePushVelocity(velocity);
        }
    }

    static float LineResistance(Flock flock)
    {
        float melee = flock.AverageProfileValue(profile => profile.MeleeSkill);
        float aggression = flock.AverageProfileValue(profile => profile.Aggression);
        return Mathf.Max(0.08f, (0.25f + melee / 100f * 0.9f + aggression / 100f * 0.15f) * (flock.IsFaultSlipping ? 0.2f : 1f));
    }

    static Vector2 GetSeparatingAxis(Flock first, Flock second, Vector2 pointA, Vector2 pointB)
    {
        Vector2 gap = pointB - pointA;
        if (gap.sqrMagnitude > 0.000001f) return gap.normalized;
        Vector2 centers = (Vector2)second.transform.position - (Vector2)first.transform.position;
        if (centers.sqrMagnitude > 0.000001f) return centers.normalized;
        return first.Formation != null && first.Formation.Facing.sqrMagnitude > 0.0001f
            ? first.Formation.Facing.normalized
            : Vector2.right;
    }

    static void ClosestSegmentPoints(FlockFormationData a, FlockFormationData b, out Vector2 pointA, out Vector2 pointB, out float distance)
    {
        Vector2 r = a.End - a.Start;
        Vector2 s = b.End - b.Start;
        Vector2 q = b.Start - a.Start;
        float denominator = Cross(r, s);
        if (Mathf.Abs(denominator) > 0.000001f)
        {
            float t = Cross(q, s) / denominator;
            float u = Cross(q, r) / denominator;
            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                pointA = a.Start + r * t;
                pointB = pointA;
                distance = 0f;
                return;
            }
        }

        pointA = a.Start;
        pointB = ClosestPoint(a.Start, b.Start, b.End);
        distance = Vector2.Distance(pointA, pointB);
        TryPair(a.End, ClosestPoint(a.End, b.Start, b.End), ref pointA, ref pointB, ref distance);
        TryPair(ClosestPoint(b.Start, a.Start, a.End), b.Start, ref pointA, ref pointB, ref distance);
        TryPair(ClosestPoint(b.End, a.Start, a.End), b.End, ref pointA, ref pointB, ref distance);
    }

    static Vector2 ClosestPoint(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
        return start + segment * t;
    }

    static void TryPair(Vector2 candidateA, Vector2 candidateB, ref Vector2 bestA, ref Vector2 bestB, ref float bestDistance)
    {
        float candidateDistance = Vector2.Distance(candidateA, candidateB);
        if (candidateDistance >= bestDistance) return;
        bestA = candidateA;
        bestB = candidateB;
        bestDistance = candidateDistance;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    static long PairKey(int first, int second)
    {
        uint a = (uint)Mathf.Min(first, second);
        uint b = (uint)Mathf.Max(first, second);
        return ((long)a << 32) | b;
    }
}
