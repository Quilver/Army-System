using System.Collections.Generic;
using UnityEngine;

// Turns a soldier's facing at a limited turn rate. Display only: no rule reads SoldierAgentState.Facing; the troop's
// faces decide flank and rear attacks.
//   Charging        a charger faces its charge target, a follower its leader's charge target (SU-158).
//   Engaged troop   (Troop.IsEngaged, SU-158) a soldier with a living enemy soldier within direct reach faces it: the
//                   nearest such soldier of a troop this troop is engaged with. Direct reach is what the engagement heat
//                   band counts as engaged (EngagementFrontage.Reach with A_Distance and FacingReachSlack). It keeps that
//                   target while it lives and stays within FacingTargetKeepScale x reach, and re-picks at most every
//                   FacingTargetRepickSeconds (SoldierFacingTarget), so it does not flicker between two equally near
//                   enemies. Every other soldier faces the troop's front, with no swing toward where it walks. These
//                   turns run at EngagedFacingTurnRateDeg.
//   Otherwise       (SU-154) in a line, toward the line's front, a little toward where it walks; otherwise toward where
//                   it walks, and when standing, toward the troop's facing.
// The engaged lookup needs the combat world's contact index; without a combat world (tests, editor) an engaged troop
// falls back to the unengaged behaviour.
public static class SoldierFacingAnimator
{
    const float FacingSpeedThreshold = 0.2f;
    const float TurnSpeed = 360f;
    const float EngagedFacingDeviation = 30f;

    static readonly List<int> Nearby = new();

    // Direct reach for the engaged facing, soldier centre to centre (the engagement heat band's reach).
    public static float EngagedReach(TroopTuning tuning) =>
        EngagementFrontage.Reach(tuning.ADistance, tuning.FacingReachSlack);

    // Updates soldier.Facing and returns the sprite's Z rotation in degrees. target holds the soldier's facing target
    // between frames; pass null to re-pick every call.
    public static float Update(Troop troop, SoldierAgentState soldier, SoldierFacingTarget target, float deltaTime)
    {
        TroopTuning tuning = TroopTuning.Active;
        if (target != null) target.Tick(deltaTime);

        Vector2 desiredFacing;
        float turnSpeed = TurnSpeed;
        if (TryChargeFacing(troop, soldier, out Vector2 toChargeTarget))
        {
            desiredFacing = toChargeTarget;
            turnSpeed = tuning.EngagedFacingTurnRateDeg;
            target?.Clear();
        }
        else if (TryEngagedFacing(troop, soldier, target, tuning, out Vector2 engagedFacing))
        {
            desiredFacing = engagedFacing;
            turnSpeed = tuning.EngagedFacingTurnRateDeg;
        }
        else
        {
            target?.Clear();
            desiredFacing = UnengagedFacing(troop, soldier);
        }

        Vector2 currentFacing = soldier.Facing.sqrMagnitude > 0.0001f ? soldier.Facing.normalized : (Vector2)troop.transform.right;
        if (deltaTime > 0f && desiredFacing.sqrMagnitude > 0.0001f)
        {
            float turn = Mathf.Clamp(
                Vector2.SignedAngle(currentFacing, desiredFacing),
                -turnSpeed * deltaTime,
                turnSpeed * deltaTime);
            currentFacing = Quaternion.AngleAxis(turn, Vector3.forward) * currentFacing;
            soldier.Facing = currentFacing.normalized;
        }

        return Mathf.Atan2(soldier.Facing.y, soldier.Facing.x) * Mathf.Rad2Deg - 90f;
    }

    // SU-154 behaviour, unchanged for a troop that is not engaged.
    static Vector2 UnengagedFacing(Troop troop, SoldierAgentState soldier)
    {
        Vector2 velocity = troop.WorldVelocity(soldier);
        bool moving = velocity.magnitude > FacingSpeedThreshold;
        Vector2 desiredFacing = soldier.Facing;
        if (troop.HasActiveLine && !troop.IsRouted)
        {
            desiredFacing = troop.Formation.Facing.normalized;
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
        else if (!troop.IsRouted)
        {
            desiredFacing = troop.Facing;
        }
        return desiredFacing;
    }

    // A charger toward its charge target; a follower toward its leader's.
    static bool TryChargeFacing(Troop troop, SoldierAgentState soldier, out Vector2 facing)
    {
        facing = default;
        SoldierAgentState chargeTarget = null;
        Troop chargeTroop = null;
        if (soldier.IsCharging)
        {
            chargeTarget = soldier.ChargeTarget;
            chargeTroop = soldier.ChargeTargetTroop;
        }
        else if (soldier.IsFollowing && soldier.ChargeLeader.IsCharging)
        {
            chargeTarget = soldier.ChargeLeader.ChargeTarget;
            chargeTroop = soldier.ChargeLeader.ChargeTargetTroop;
        }
        if (chargeTarget == null || !chargeTarget.IsAlive || chargeTroop == null) return false;
        facing = chargeTroop.WorldPosition(chargeTarget) - troop.WorldPosition(soldier);
        return facing.sqrMagnitude > 1e-6f;
    }

    // An engaged troop's soldier: toward its target enemy soldier, or the troop's front without one. False when the
    // troop is not engaged or there is no combat world.
    static bool TryEngagedFacing(Troop troop, SoldierAgentState soldier, SoldierFacingTarget target, TroopTuning tuning,
        out Vector2 facing)
    {
        facing = default;
        if (!troop.IsEngaged || troop.IsRouted) return false;
        TroopCombatWorld world = TroopCombatWorld.Current;
        if (world == null) return false;

        Vector2 position = troop.WorldPosition(soldier);
        float reach = EngagedReach(tuning);
        target ??= new SoldierFacingTarget();   // only when the caller keeps no target: no stability, re-picks now

        if (!IsKeepable(troop, target, position, reach * tuning.FacingTargetKeepScale))
        {
            target.Clear();
            if (target.CanRepick)
            {
                PickNearest(troop, world.ContactIndex, position, reach, target);
                target.StartRepickDelay(tuning.FacingTargetRepickSeconds);
            }
        }

        if (target.Soldier != null)
        {
            facing = target.Troop.WorldPosition(target.Soldier) - position;
            if (facing.sqrMagnitude > 1e-6f) return true;
        }
        facing = troop.HasActiveLine ? troop.Formation.Facing.normalized : troop.Facing;
        return true;
    }

    static bool IsKeepable(Troop troop, SoldierFacingTarget target, Vector2 position, float keepReach)
    {
        if (target.Soldier == null || target.Troop == null || !target.Soldier.IsAlive) return false;
        if (!IsEngagedWith(troop, target.Troop)) return false;
        return (target.Troop.WorldPosition(target.Soldier) - position).sqrMagnitude <= keepReach * keepReach;
    }

    static bool IsEngagedWith(Troop troop, Troop other)
    {
        var engagements = troop.Engagements;
        for (int i = 0; i < engagements.Count; i++)
            if (engagements[i].Engaged && engagements[i].Opponent == other) return true;
        return false;
    }

    // The nearest living soldier within reach of a troop this troop is engaged with. Candidates come from the contact
    // index (positions as of its last rebuild, gathered with a margin); distances use their current positions.
    static void PickNearest(Troop troop, SoldierContactIndex index, Vector2 position, float reach, SoldierFacingTarget target)
    {
        index.GatherWithin(position, reach + TroopConstants.NeighborRadius * 0.5f, Nearby);
        float best = reach * reach;
        var contacts = index.Contacts;
        foreach (int i in Nearby)
        {
            SoldierContact contact = contacts[i];
            if (contact.Troop == null || contact.Troop == troop || !contact.Soldier.IsAlive) continue;
            if (!IsEngagedWith(troop, contact.Troop)) continue;
            float distance = (contact.Troop.WorldPosition(contact.Soldier) - position).sqrMagnitude;
            if (distance > best) continue;
            best = distance;
            target.Set(contact.Soldier, contact.Troop);
        }
        Nearby.Clear();
    }
}
