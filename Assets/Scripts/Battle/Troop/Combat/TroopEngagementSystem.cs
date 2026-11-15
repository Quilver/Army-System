using System.Collections.Generic;
using UnityEngine;

// Measures every pair of opposing troops once per frame with the footprint gap (spec §4.1, Q-16), so S_Distance,
// A_Distance and faces all use the same metric:
//   - engagement records (gap, attacking face, engaged) for each troop, which also start Assault;
//   - whether an enemy is within S_Distance, which turns the troop into a battle line. The line faces the troop's own
//     heading (where it marches or was ordered to face): troops no longer turn toward enemies by themselves (SU-154).
//     A troop pursuing an enemy within S_Distance takes its heading from that target and squares up to the face it
//     attacks, and keeps turning to it while engaged with it (SU-75). Two troops pursuing each other face pivot to pivot
//     instead (SU-163).
//   - the assault wheel (SU-163): a pursuer engaged with its target wheels square to the face it attacks, kept by
//     TroopAssaultWheel, about the end of its front rank nearest the enemy (TroopFormationState). Only the attacker turns:
//     a troop that is attacked and not pursuing that attacker keeps its facing lock.
// Engagement starts at A_Distance and has an exit hold (SU-45, Q-16): two troops that were engaged last frame stay engaged
// until their gap passes A_Distance + EngagementHoldMargin. Engaged lines are held at EngagementGap by the barrier, which can
// equal A_Distance, so without the hold a fight would flick in and out of 'engaged' (charges, facing lock, chain pinning).
// S_Distance has exit hysteresis too (see ExitMargin): forming a line changes a troop's footprint from a circle to a thin
// rectangle, which widens the gap, so without it a troop at the S_Distance edge would flick between Line and Idle / Column
// every few frames.
public sealed class TroopEngagementSystem
{
    readonly List<EngagementRecord> _records = new();

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
            TroopFootprint own = troop.Footprint;
            Vector2 pivot = troop.transform.position;
            bool threatened = troop.HasThreatWithinSDistance;
            bool hasThreat = false;
            float holdRange = tuning.ADistance + tuning.EngagementHoldMargin;
            Troop pursuitTarget = troop.PursuitTarget;
            bool hasPursuitHeading = false;
            Vector2 pursuitHeading = Vector2.zero;

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
                hasThreat = true;

                if (other == pursuitTarget)
                {
                    // SU-163: two troops pursuing each other face pivot to pivot, so neither chases the other's faces.
                    pursuitHeading = TroopAssaultWheel.IsMutual(troop, other, tuning)
                        ? PivotToPivot(other.transform.position, pivot, own.Facing)
                        : PursuitHeading(theirs, other.transform.position, own, pivot, gap, tuning.SDistance,
                            tuning.ADistance, tuning.FrontFaceHalfAngleDeg, tuning.RearFaceAngleDeg);
                    hasPursuitHeading = true;
                }
            }

            troop.SetEngagements(_records);
            troop.SetMeleeState(troop.IsEngaged);

            if (!hasThreat || troop.IsRouted)
            {
                troop.AssaultWheel.Reset();
                troop.UpdateNaturalFormation(false, null, deltaTime);
                continue;
            }

            if (hasPursuitHeading)
            {
                // SU-163: engaged with its target, the pursuer wheels square to the face it attacks.
                if (tuning.AssaultWheel && troop.WasEngagedWith(pursuitTarget))
                {
                    Vector2 assaultHeading = troop.AssaultWheel.TargetHeading(troop, pursuitTarget, tuning);
                    troop.UpdateNaturalFormation(true, null, deltaTime, turnWhileEngaged: true,
                        assaultHeading: assaultHeading);
                    continue;
                }
                troop.AssaultWheel.Reset();
                // The facing lock still holds while the pursuer is engaged only with other enemies.
                bool engagedElsewhere = troop.IsEngaged && !troop.WasEngagedWith(pursuitTarget);
                troop.UpdateNaturalFormation(true, pursuitHeading, deltaTime, turnWhileEngaged: !engagedElsewhere);
                continue;
            }

            troop.AssaultWheel.Reset();
            troop.UpdateNaturalFormation(true, null, deltaTime);
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

    static Vector2 PivotToPivot(Vector2 targetPivot, Vector2 ownPivot, Vector2 fallback)
    {
        Vector2 toTarget = targetPivot - ownPivot;
        return toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : fallback;
    }

    // Extra gap before a troop that already faces a threat drops it again: at least the shape change of both footprints
    // between a blob (radius BlobRadiusScale x TroopRadius) and a thin line, so a formation change alone cannot move the
    // pair back across S_Distance. SDistanceExitHysteresis (TroopTuning) is used instead when it is larger.
    static float ExitMargin(Troop troop, Troop other, TroopTuning tuning)
    {
        float shapeChange = tuning.BlobRadiusScale * (troop.Radius + other.Radius);
        return Mathf.Max(tuning.SDistanceExitHysteresis, shapeChange);
    }
}
