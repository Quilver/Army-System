using System.Collections.Generic;
using UnityEngine;

// Enemy battle lines push against each other (spec §3.3 line-vs-line pressure); the resulting force drives each troop's
// line push velocity. The gap between two lines is the footprint gap (the same metric as engagement). Engaged lines are
// held apart at EngagementGap (TroopTuning, default 2) by the barrier.
public sealed class TroopSegmentPressureRules
{
    const float ContactStartScale = 1.25f;  // pressure contact begins at this multiple of the contact range
    const float BarrierStiffness = 60f;     // force per unit of penetration inside the line gap
    // Matches the 5 / s decay in IntegrateLinePush, so a steady resistive force cancels an advancing troop's approach speed.
    const float ResistiveGain = 5f;
    const float ShareEpsilon = 0.0001f;
    readonly Dictionary<long, Contact> _contacts = new();

    struct Contact
    {
        public Troop First;
        public Troop Second;
        public Vector2 Axis;
        public float FirstIntrusion;     // how far the first troop has moved into the gap, along the axis
        public float SecondIntrusion;    // how far the second troop has moved into the gap, along the axis
        public Vector2 FirstLastPosition;
        public Vector2 SecondLastPosition;
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
        ApplyLinePressure(troops, actionDistance, forces);
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

    void ApplyLinePressure(IReadOnlyList<Troop> troops, float actionDistance, Dictionary<Troop, Vector2> forces)
    {
        // The pressure contest starts within contactRange of each other's footprints, and ramps up a little before that.
        // It always starts before the lines reach EngagementGap, so the barrier has a contact to act on.
        float engagementGap = TroopTuning.Active.EngagementGap;
        float contactRange = Mathf.Max(actionDistance * 0.5f, engagementGap);
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
            SegmentGeometry.ClosestSegmentPoints(first.Formation, second.Formation, out Vector2 pointA, out Vector2 pointB, out _);
            float distance = FootprintGap.Gap(first.Footprint, second.Footprint);
            if (!_contacts.TryGetValue(key, out Contact contact))
            {
                if (distance >= interactionRange) continue;
                contact = new Contact
                {
                    First = first,
                    Second = second,
                    Axis = GetSeparatingAxis(first, second, pointA, pointB),
                    FirstLastPosition = first.transform.position,
                    SecondLastPosition = second.transform.position,
                };
                _contacts.Add(key, contact);
            }
            else if (distance >= releaseRange)
            {
                _contacts.Remove(key);
                continue;
            }

            Vector2 barrierAxis = GetSeparatingAxis(first, second, pointA, pointB);
            UpdateIntrusion(ref contact, first, second, barrierAxis, distance, engagementGap);
            _contacts[key] = contact;

            if (distance < engagementGap)
                ApplyLineBarrier(first, second, barrierAxis, engagementGap - distance, contact.FirstIntrusion, contact.SecondIntrusion, forces);

            float compression = Mathf.Clamp01((releaseRange - distance) / Mathf.Max(0.0001f, releaseRange));
            float firstDrive = Mathf.Max(0f, Vector2.Dot(first.PivotVelocity, contact.Axis));
            float secondDrive = Mathf.Max(0f, Vector2.Dot(second.PivotVelocity, -contact.Axis));
            float firstMelee = MeleeSkillFraction(first);
            float secondMelee = MeleeSkillFraction(second);
            float firstEffort = Mathf.Clamp01(firstDrive / Mathf.Max(0.1f, first.AverageProfileValue(profile => profile.MaxSpeed)))
                * (1f - firstMelee * 0.25f);
            float secondEffort = Mathf.Clamp01(secondDrive / Mathf.Max(0.1f, second.AverageProfileValue(profile => profile.MaxSpeed)))
                * (1f - secondMelee * 0.25f);
            float contactForce = compression * (31.2f + 93.6f * compression);
            float firstForce = contactForce * firstEffort * LineResistance(second);
            float secondForce = contactForce * secondEffort * LineResistance(first);
            forces[first] -= contact.Axis * firstForce;
            forces[second] += contact.Axis * secondForce;
        }
    }

    // Records how far each troop has moved into the gap since the last update. Uses measured pivot displacement, not
    // PivotVelocity, so the barrier's own push cannot cancel the value it reads. A troop pushed or backing off has a
    // negative step, which reduces only its own intrusion. Intrusion is clamped to the current penetration.
    static void UpdateIntrusion(ref Contact contact, Troop first, Troop second, Vector2 axis, float distance, float engagementGap)
    {
        Vector2 firstPosition = first.transform.position;
        Vector2 secondPosition = second.transform.position;

        if (distance < engagementGap)
        {
            float firstStep = Vector2.Dot(firstPosition - contact.FirstLastPosition, axis);
            float secondStep = Vector2.Dot(secondPosition - contact.SecondLastPosition, -axis);
            contact.FirstIntrusion = Mathf.Max(0f, contact.FirstIntrusion + firstStep);
            contact.SecondIntrusion = Mathf.Max(0f, contact.SecondIntrusion + secondStep);

            float penetration = engagementGap - distance;
            float total = contact.FirstIntrusion + contact.SecondIntrusion;
            if (total > penetration && total > 0f)
            {
                float scale = penetration / total;
                contact.FirstIntrusion *= scale;
                contact.SecondIntrusion *= scale;
            }
        }
        else
        {
            contact.FirstIntrusion = 0f;
            contact.SecondIntrusion = 0f;
        }

        contact.FirstLastPosition = firstPosition;
        contact.SecondLastPosition = secondPosition;
    }

    // Resistive barrier for a gap that is being entered. Two parts:
    // - Restoring force is shared only by stored intrusion (how far each troop has moved into the gap). A troop that
    //   has not intruded is never pushed. If neither has intruded (penetration from footprint widening or rotation),
    //   no restoring force is applied; soldier-level contact handles that overlap.
    // - Resistive force opposes only a troop that is advancing into the other line, cancelling its advance.
    // Axis points from first to second.
    static void ApplyLineBarrier(Troop first, Troop second, Vector2 axis, float penetration, float firstIntrusion, float secondIntrusion, Dictionary<Troop, Vector2> forces)
    {
        float firstAdvance = Mathf.Max(0f, Vector2.Dot(first.PivotVelocity, axis));
        float secondAdvance = Mathf.Max(0f, Vector2.Dot(second.PivotVelocity, -axis));
        float totalIntrusion = firstIntrusion + secondIntrusion;
        float firstShare = 0f;
        float secondShare = 0f;
        if (totalIntrusion > ShareEpsilon)
        {
            firstShare = firstIntrusion / totalIntrusion;
            secondShare = 1f - firstShare;
        }

        float restore = penetration * BarrierStiffness;
        forces[first] -= axis * (restore * firstShare + firstAdvance * ResistiveGain);
        forces[second] += axis * (restore * secondShare + secondAdvance * ResistiveGain);
        TroopDebugSnapshot.RecordBarrier(first, second, axis, penetration, firstShare, secondShare);
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

    // Average melee weapon skill as a 0-1 fraction of the stat maximum.
    static float MeleeSkillFraction(Troop troop) =>
        troop.AverageProfileValue(profile => profile.MeleeWeaponSkill) / CombatStatLimits.Max;

    static float LineResistance(Troop troop)
    {
        float melee = MeleeSkillFraction(troop);
        float aggression = troop.AverageProfileValue(profile => profile.Aggression) / CombatStatLimits.Max;
        return Mathf.Max(0.08f, (0.25f + melee * 0.9f + aggression * 0.15f) * (troop.IsFaultSlipping ? 0.2f : 1f));
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
