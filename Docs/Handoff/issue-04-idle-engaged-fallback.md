# Issue 04: Engaged Idle Troops Fall Back Unprompted

**Status:** Suspected cause found, not confirmed in play. **Logging:** Barrier, Pivot, LinePressure (see
[movement-testing.md](movement-testing.md)). **Fix:** [barrier-intrusion-tracking.md](barrier-intrusion-tracking.md).

## Summary

When the player orders a troop to engage an enemy troop that has no orders, the enemy's pivot is pushed back. It is
not a retreat: the enemy's `State` stays Idle and no fault slip starts.

## Symptom

The idle defender's pivot drifts away from the attacker while the attacker holds at the gap between the lines.

## Repro

1. Place an enemy troop with no orders.
2. Select a player troop and right-click the enemy. This issues `Pursue` (`Input/TroopOrderPlanner.cs:31`).
3. Watch the defender's pivot, `State` and `LinePushVelocity` once the lines are close.

## Suspected cause

- **The pivot moves through line push.** `TroopPivotMover.Tick` adds `LinePushVelocity * dt` to the pivot every
  frame, including when the troop is Idle (`Runtime/TroopPivotMover.cs:91`). Line push is integrated from forces in
  `TroopSegmentPressureRules.IntegrateLinePush` (`Pressure/TroopSegmentPressureRules.cs:130-140`).
- **The barrier splits evenly when no one is advancing.** `ApplyLineBarrier` shares the restoring force by each
  troop's advance along the axis. When the total advance is near 0, each troop gets half
  (`Pressure/TroopSegmentPressureRules.cs:117-128`, line 122).
- **The attacker's advance cancels itself.** `PivotVelocity` is the troop's own movement plus its line push
  (`Runtime/TroopPivotMover.cs:29`). `Pursue` keeps the attacker driving at the defender's pivot, while the barrier's
  resistive term (`ResistiveGain` = 5) builds an opposing line push. As these cancel, the attacker's net
  `PivotVelocity` along the axis approaches 0, so the even split kicks in and the defender gets half the restore force.

## Ruled out

- **Contact force:** the defender's effort comes from its own drive along the axis, which is 0 when idle
  (`Pressure/TroopSegmentPressureRules.cs:97-109`).
- **Fault slip (retreat):** breaking depends on shock exceeding morale (`Runtime/TroopMorale.cs:32`) and sets
  `State = Fleeing` (`Runtime/Troop.cs:292`). Lost clashes only add fault pressure.
- **Soldier-level knock-back:** clash knock-back and bounces in `ClashResolver` and `SoldierContactResolver` move
  soldiers, not the pivot.

## Logging needed

For the engaged pair, every frame:
- Barrier: penetration, both advances, both shares, restore, both applied forces.
- Pivot: both troops' `_pivotVelocity`, `LinePushVelocity` and displacement, and the defender's state and orders.
- LinePressure: both efforts and forces, to confirm the defender's contact force is 0.

## Confirmation criteria

Confirmed if the defender's pivot displacement happens on frames where:
- penetration is above 0;
- the defender's advance is 0;
- the total advance is below the 0.0001 threshold, so the shares are 0.5 each;
- and the defender's `LinePushVelocity` points away from the attacker.

If the defender is pushed on frames where the attacker's advance is clearly above 0, the cause is something else.
Check `AlliedLinkTension` and `FriendlyOverlapResolver` if the defender is in a chain.

## Fix direction

See [barrier-intrusion-tracking.md](barrier-intrusion-tracking.md): store how far each troop has moved into the gap,
share the restoring force by that only, and never push a troop that has not intruded.

## Verification

- Repro steps: the defender's pivot displacement and `LinePushVelocity` stay at 0 while it is Idle.
- Re-run the Engagement row and the Idle and Engaged columns of the results matrix.
