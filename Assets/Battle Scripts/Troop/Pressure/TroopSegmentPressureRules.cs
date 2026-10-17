using System.Collections.Generic;
using UnityEngine;

// Enemy battle lines push against each other; the resulting force drives each troop's line push velocity.
public sealed class TroopSegmentPressureRules
{
    const float LineGap = 0.3f;             // clear gap between the footprints of two engaged lines
    const float ContactStartScale = 1.25f;  // pressure contact begins at this multiple of the contact range
    const float BarrierStiffness = 60f;     // force per unit of penetration inside the line gap
    // Matches the 5 / s decay in IntegrateLinePush, so a steady resistive force cancels an advancing troop's approach speed.
    const float ResistiveGain = 5f;
    readonly Dictionary<long, Contact> _contacts = new();

    struct Contact
    {
        public Troop First;
        public Troop Second;
        public Vector2 Axis;
    }

    public void Update(IReadOnlyList<Troop> troops, float actionDistance, float deltaTime)
    {
        var byId = new Dictionary<int, Troop>();
        var forces = new Dictionary<Troop, Vector2>();
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            byId[troop.GetInstanceID()] = troop;
            forces[troop] = Vector2.zero;
        }

        PruneStaleContacts(byId);
        var halfWidths = new Dictionary<Troop, float>();
        foreach (var troop in troops)
            if (troop != null) halfWidths[troop] = LineHalfWidth(troop);
        ApplyLinePressure(troops, actionDistance, halfWidths, forces);
        FriendlyOverlapResolver.Resolve(troops);
        AlliedLinkTension.Apply(troops, forces);
        IntegrateLinePush(troops, forces, deltaTime);
    }

    void PruneStaleContacts(Dictionary<int, Troop> byId)
    {
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
    }

    void ApplyLinePressure(IReadOnlyList<Troop> troops, float actionDistance, Dictionary<Troop, float> halfWidths, Dictionary<Troop, Vector2> forces)
    {
        // The pressure contest starts within contactRange of each other's footprints, and ramps up a little before that.
        float contactRange = actionDistance * 0.5f;
        float interactionRange = contactRange * ContactStartScale;
        float releaseRange = interactionRange * 1.6f;
        if (interactionRange <= 0f) _contacts.Clear();
        if (interactionRange <= 0f) return;

        for (int i = 0; i < troops.Count; i++)
        for (int j = i + 1; j < troops.Count; j++)
        {
            Troop first = troops[i];
            Troop second = troops[j];
            if (first == null || second == null || first.TeamId == second.TeamId) continue;
            long key = PairKey(first.GetInstanceID(), second.GetInstanceID());
            if (first.Formation == null || second.Formation == null)
            {
                _contacts.Remove(key);
                continue;
            }

            // Lines have width: the gap is measured between their footprints, not between their centre lines.
            SegmentGeometry.ClosestSegmentPoints(first.Formation, second.Formation, out Vector2 pointA, out Vector2 pointB, out float centreDistance);
            float distance = centreDistance - halfWidths[first] - halfWidths[second];
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

            if (distance < LineGap)
                ApplyLineBarrier(first, second, GetSeparatingAxis(first, second, pointA, pointB), LineGap - distance, forces);

            float compression = Mathf.Clamp01((releaseRange - distance) / Mathf.Max(0.0001f, releaseRange));
            float firstDrive = Mathf.Max(0f, Vector2.Dot(first.PivotVelocity, contact.Axis));
            float secondDrive = Mathf.Max(0f, Vector2.Dot(second.PivotVelocity, -contact.Axis));
            float firstMelee = first.AverageProfileValue(profile => profile.MeleeSkill);
            float secondMelee = second.AverageProfileValue(profile => profile.MeleeSkill);
            float firstEffort = Mathf.Clamp01(firstDrive / Mathf.Max(0.1f, first.AverageProfileValue(profile => profile.MaxSpeed)))
                * (1f - firstMelee / 100f * 0.25f);
            float secondEffort = Mathf.Clamp01(secondDrive / Mathf.Max(0.1f, second.AverageProfileValue(profile => profile.MaxSpeed)))
                * (1f - secondMelee / 100f * 0.25f);
            float contactForce = compression * (31.2f + 93.6f * compression);
            float firstForce = contactForce * firstEffort * LineResistance(second);
            float secondForce = contactForce * secondEffort * LineResistance(first);
            forces[first] -= contact.Axis * firstForce;
            forces[second] += contact.Axis * secondForce;
            TroopDiagnostics.RecordLinePressure(first, second, centreDistance, halfWidths[first], halfWidths[second], distance,
                compression, firstDrive, secondDrive, firstEffort, secondEffort, firstForce, secondForce);
        }
    }

    // Resistive barrier for a gap that is being entered. It opposes only a troop that is advancing into the other line:
    // the advance is cancelled and the penetration is pushed back out. A troop holding its ground receives no push.
    // If neither troop is advancing (overlap while both are still), the correction is shared evenly.
    // Axis points from first to second.
    static void ApplyLineBarrier(Troop first, Troop second, Vector2 axis, float penetration, Dictionary<Troop, Vector2> forces)
    {
        float firstAdvance = Mathf.Max(0f, Vector2.Dot(first.PivotVelocity, axis));
        float secondAdvance = Mathf.Max(0f, Vector2.Dot(second.PivotVelocity, -axis));
        float totalAdvance = firstAdvance + secondAdvance;
        float firstShare = totalAdvance > 0.0001f ? firstAdvance / totalAdvance : 0.5f;
        float secondShare = 1f - firstShare;

        float restore = penetration * BarrierStiffness;
        float firstPush = restore * firstShare + firstAdvance * ResistiveGain;
        float secondPush = restore * secondShare + secondAdvance * ResistiveGain;
        forces[first] -= axis * firstPush;
        forces[second] += axis * secondPush;
        TroopDiagnostics.RecordBarrier(first, second, axis, penetration, firstAdvance, secondAdvance, firstShare, secondShare,
            restore, firstPush, secondPush);
    }

    static void IntegrateLinePush(IReadOnlyList<Troop> troops, Dictionary<Troop, Vector2> forces, float deltaTime)
    {
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            Vector2 velocity = (troop.LinePushVelocity + forces[troop] * deltaTime) * Mathf.Exp(-5f * deltaTime);
            float maximumSpeed = Mathf.Max(0.6f, troop.AverageProfileValue(profile => profile.MaxSpeed) * 1.25f);
            if (velocity.magnitude > maximumSpeed) velocity = velocity.normalized * maximumSpeed;
            troop.SetLinePushVelocity(velocity);
        }
    }

    // Half the width a line's footprint covers: the furthest soldier on either side of the centre line (soldiers stand
    // in front of and behind it), plus that soldier's own radius. Only soldiers alongside the line are counted.
    static float LineHalfWidth(Troop troop)
    {
        float soldierRadius = TroopConstants.SoldierRadius;
        TroopFormationData formation = troop.Formation;
        if (formation == null) return soldierRadius;

        Vector2 tangent = formation.Tangent;
        Vector2 normal = new(-tangent.y, tangent.x);
        float length = Vector2.Distance(formation.Start, formation.End);
        float furthest = 0f;
        IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
        for (int i = 0; i < soldiers.Count; i++)
        {
            if (!soldiers[i].IsAlive) continue;
            Vector2 offset = troop.WorldPosition(soldiers[i]) - formation.Start;
            float along = Vector2.Dot(offset, tangent);
            if (along < -soldierRadius || along > length + soldierRadius) continue;
            furthest = Mathf.Max(furthest, Mathf.Abs(Vector2.Dot(offset, normal)));
        }
        return furthest + soldierRadius;
    }

    static float LineResistance(Troop troop)
    {
        float melee = troop.AverageProfileValue(profile => profile.MeleeSkill);
        float aggression = troop.AverageProfileValue(profile => profile.Aggression);
        return Mathf.Max(0.08f, (0.25f + melee / 100f * 0.9f + aggression / TroopConstants.MaxAggression * 0.15f) * (troop.IsFaultSlipping ? 0.2f : 1f));
    }

    static Vector2 GetSeparatingAxis(Troop first, Troop second, Vector2 pointA, Vector2 pointB)
    {
        Vector2 gap = pointB - pointA;
        if (gap.sqrMagnitude > 0.000001f) return gap.normalized;
        Vector2 centers = (Vector2)second.transform.position - (Vector2)first.transform.position;
        if (centers.sqrMagnitude > 0.000001f) return centers.normalized;
        return first.Formation != null && first.Formation.Facing.sqrMagnitude > 0.0001f
            ? first.Formation.Facing.normalized
            : Vector2.right;
    }

    static long PairKey(int first, int second)
    {
        uint a = (uint)Mathf.Min(first, second);
        uint b = (uint)Mathf.Max(first, second);
        return ((long)a << 32) | b;
    }
}
