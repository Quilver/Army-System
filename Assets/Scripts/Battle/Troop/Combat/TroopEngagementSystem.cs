using System.Collections.Generic;
using UnityEngine;

// Measures every pair of opposing troops once per frame with the footprint gap (spec §4.1, Q-16), so S_Distance,
// A_Distance and faces all use the same metric:
//   - engagement records (gap, attacking face, engaged) for each troop, which also start Assault;
//   - the threat heading from enemies within S_Distance, which turns the troop's line toward them. A troop pursuing an
//     enemy within S_Distance takes its heading from that target alone and squares up to the face it attacks, and keeps
//     turning to it while engaged with it (SU-75).
// Engagement starts at A_Distance and has an exit hold (SU-45, Q-16): two troops that were engaged last frame stay engaged
// until their gap passes A_Distance + EngagementHoldMargin. Engaged lines are held at EngagementGap by the barrier, which can
// equal A_Distance, so without the hold a fight would flick in and out of 'engaged' (charges, facing lock, chain pinning).
// S_Distance has exit hysteresis too (see ExitMargin): forming a line changes a troop's footprint from a circle to a thin
// rectangle, which widens the gap, so without it a troop at the S_Distance edge would flick between Line and Idle / Column
// every few frames.
public sealed class TroopEngagementSystem
{
    struct Threat
    {
        public Vector2 Direction;
        public float PivotDistance;
        public float Gap;
        public float Facing;
    }

    readonly List<EngagementRecord> _records = new();
    readonly List<Threat> _threats = new();

    public void Update(IReadOnlyList<Troop> troops, float deltaTime)
    {
        TroopTuning tuning = TroopTuning.Active;
        foreach (var troop in troops)
        {
            if (troop == null) continue;
            troop.SetFootprint(TroopFootprintBuilder.Build(troop));
            troop.UpdateOutline(deltaTime);
        }

        foreach (var troop in troops)
        {
            if (troop == null) continue;
            _records.Clear();
            _threats.Clear();
            TroopFootprint own = troop.Footprint;
            Vector2 pivot = troop.transform.position;
            Vector2 heading = troop.PivotVelocity.sqrMagnitude > 0.0025f ? troop.PivotVelocity.normalized : troop.Facing;
            bool threatened = troop.HasThreatWithinSDistance;
            float nearestPivot = float.PositiveInfinity;
            float holdRange = tuning.ADistance + tuning.EngagementHoldMargin;
            Troop pursuitTarget = troop.PursuitTarget;
            bool hasPursuitHeading = false;
            Vector2 pursuitHeading = Vector2.zero;
            float pursuitDistance = 0f;

            foreach (var other in troops)
            {
                if (other == null || other == troop || other.TeamId == troop.TeamId) continue;
                TroopFootprint theirs = other.Footprint;
                float gap = FootprintGap.Gap(own, theirs);
                // troop.Engagements still holds last frame's records here (SetEngagements runs after this loop).
                bool held = gap <= holdRange && troop.WasEngagedWith(other);
                float sensing = tuning.SDistance + (threatened ? ExitMargin(troop, other, tuning) : 0f);
                if (gap > sensing && !held) continue;

                TroopFace face = FaceClassifier.Classify(own, pivot, theirs, tuning.FrontFaceHalfAngleDeg, tuning.RearFaceAngleDeg);
                _records.Add(new EngagementRecord(other, gap, face, gap <= tuning.ADistance || held));
                if (other.IsRouted) continue;

                Vector2 offset = (Vector2)other.transform.position - pivot;
                float pivotDistance = offset.magnitude;
                Vector2 direction = pivotDistance > 1e-4f ? offset / pivotDistance : heading;
                _threats.Add(new Threat
                {
                    Direction = direction,
                    PivotDistance = pivotDistance,
                    Gap = gap,
                    Facing = Vector2.Dot(direction, heading)
                });
                nearestPivot = Mathf.Min(nearestPivot, pivotDistance);

                if (other == pursuitTarget)
                {
                    pursuitHeading = PursuitHeading(theirs, other.transform.position, own, pivot, gap, tuning.SDistance,
                        tuning.ADistance, tuning.FrontFaceHalfAngleDeg, tuning.RearFaceAngleDeg);
                    pursuitDistance = pivotDistance;
                    hasPursuitHeading = true;
                }
            }

            troop.SetEngagements(_records);
            troop.SetMeleeState(troop.IsEngaged);

            if (_threats.Count == 0 || troop.IsRouted)
            {
                troop.UpdateNaturalFormation(null, 0f, deltaTime);
                continue;
            }

            if (hasPursuitHeading)
            {
                // The facing lock still holds while the pursuer is engaged only with other enemies.
                bool engagedElsewhere = troop.IsEngaged && !troop.WasEngagedWith(pursuitTarget);
                troop.UpdateNaturalFormation(pursuitHeading, pursuitDistance, deltaTime, turnWhileEngaged: !engagedElsewhere);
                continue;
            }

            Vector2 weightedDirection = Vector2.zero;
            float totalWeight = 0f;
            float weightedDistance = 0f;
            foreach (var threat in _threats)
            {
                float weight = ThreatWeight(threat, nearestPivot, tuning.SDistance);
                totalWeight += weight;
                weightedDirection += threat.Direction * weight;
                weightedDistance += threat.PivotDistance * weight;
            }

            if (weightedDirection.sqrMagnitude < 1e-6f || totalWeight <= 0f)
                troop.UpdateNaturalFormation(_threats[0].Direction, _threats[0].PivotDistance, deltaTime);
            else
                troop.UpdateNaturalFormation(weightedDirection.normalized, weightedDistance / totalWeight, deltaTime);
        }
    }

    // The heading of a troop pursuing target (SU-75): toward the target's pivot while the gap is S_Distance, turning
    // as the gap closes to A_Distance until it squares up to the face it attacks, so its line meets a line target's
    // front, flank or rear parallel. A target without a line (blob or column) is faced pivot to pivot.
    public static Vector2 PursuitHeading(in TroopFootprint target, Vector2 targetPivot, in TroopFootprint own,
        Vector2 ownPivot, float gap, float sDistance, float aDistance, float frontHalfAngleDeg, float rearAngleDeg)
    {
        Vector2 toTarget = targetPivot - ownPivot;
        toTarget = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : own.Facing;
        if (target.Kind != FootprintKind.Rect) return toTarget;

        TroopFace face = FaceClassifier.Classify(target, targetPivot, own, frontHalfAngleDeg, rearAngleDeg);
        Vector2 squared = -FaceClassifier.OutwardNormal(target, face);
        float closeness = sDistance > aDistance ? Mathf.InverseLerp(sDistance, aDistance, gap) : 1f;
        Vector2 heading = Vector2.Lerp(toTarget, squared, closeness);
        return heading.sqrMagnitude > 1e-6f ? heading.normalized : squared;
    }

    // Extra gap before a troop that already faces a threat drops it again: at least the shape change of both footprints
    // between a blob (radius BlobRadiusScale x TroopRadius) and a thin line, so a formation change alone cannot move the
    // pair back across S_Distance. SDistanceExitHysteresis (TroopTuning) is used instead when it is larger.
    static float ExitMargin(Troop troop, Troop other, TroopTuning tuning)
    {
        float shapeChange = tuning.BlobRadiusScale * (troop.Radius + other.Radius);
        return Mathf.Max(tuning.SDistanceExitHysteresis, shapeChange);
    }

    // Nearer enemies, and enemies ahead of the march, weigh more.
    static float ThreatWeight(Threat threat, float nearestPivotDistance, float sDistance)
    {
        float proximity = sDistance <= 0f
            ? (threat.Gap <= 0f ? 1f : 0f)
            : 1f / Mathf.Pow(1f + Mathf.Max(0f, threat.Gap) / sDistance, 2f);
        float nearestBias = threat.PivotDistance <= nearestPivotDistance + 0.0001f ? 4f : 0.35f;
        return proximity * (1f + 0.5f * threat.Facing) * nearestBias;
    }
}
