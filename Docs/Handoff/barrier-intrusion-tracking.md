# Handoff: Barrier Intrusion Tracking

**Fixes:** [issue-04-idle-engaged-fallback.md](issue-04-idle-engaged-fallback.md). **Status:** Not started.

## Problem

`TroopSegmentPressureRules.ApplyLineBarrier` (`Assets/Battle Scripts/Troop/Pressure/TroopSegmentPressureRules.cs:117-128`)
shares the restoring force by each troop's instantaneous advance (`PivotVelocity` along the separating axis). When
neither troop is advancing, it shares the force 50/50.

Engage orders use `Pursue`, so the attacker keeps driving at the defender's pivot. Near the gap, the barrier's
resistive term builds an opposing line push that cancels the attacker's own velocity, so its net `PivotVelocity`
falls to about 0. The idle defender then gets half the restoring force, and `TroopPivotMover.Tick` moves its pivot
through `LinePushVelocity` even though it is Idle (`Runtime/TroopPivotMover.cs:91`).

## Required behaviour

- The barrier stores, per enemy pair, how much of the current penetration each troop caused by moving into the gap.
- The restoring force is shared by stored intrusion only. A troop that has not moved into the gap is never pushed.
- If there is penetration but neither troop has stored intrusion (for example the line footprint widened because
  soldiers spread, or a line rotated), apply no barrier force. Soldier-level contact (`CrossTroopSeparation`,
  `SoldierContactResolver`) handles that overlap.
- The resistive term (cancelling an advance) still applies only to a troop that is advancing.

## Design

### State

Extend the `Contact` struct (already persistent per enemy pair in `_contacts`) with:
- `float FirstIntrusion`, `float SecondIntrusion`: how far each troop has moved into the gap along the axis.
- `Vector2 FirstLastPosition`, `Vector2 SecondLastPosition`: each pivot's position at the previous update.

Initialise both positions to the current pivot positions, and both intrusions to 0, when the contact is created, so
the first frame does not count earlier movement.

### Update, per existing contact, each frame

1. Compute the separating axis from first to second (`GetSeparatingAxis`), as the barrier already does.
2. Measure each troop's pivot displacement since the last update, projected toward the other troop:
   - `firstStep = Dot(firstPosition - FirstLastPosition, axis)`
   - `secondStep = Dot(secondPosition - SecondLastPosition, -axis)`
3. If `distance < LineGap` (inside the gap):
   - `FirstIntrusion = Max(0, FirstIntrusion + firstStep)`
   - `SecondIntrusion = Max(0, SecondIntrusion + secondStep)`
   - A troop that is pushed back or backs off has a negative step, which reduces only its own intrusion.
   - Clamp the total to the current penetration (`LineGap - distance`). If the sum is larger, scale both down in
     proportion.
4. If `distance >= LineGap` (outside the gap), reset both intrusions to 0.
5. Store the current pivot positions in the contact.

Because `Contact` is a struct stored in a dictionary, write it back after changing it (`_contacts[key] = contact`).

Measuring actual pivot displacement, not `PivotVelocity`, avoids the circular case where the barrier's own push
cancels the velocity it reads. Displacement caused by the barrier pushing a troop back is negative along that
troop's axis, so it only reduces that troop's intrusion.

### Barrier force

Pass both intrusions to `ApplyLineBarrier`:

- `restore = penetration * BarrierStiffness`
- If `FirstIntrusion + SecondIntrusion > epsilon`:
  `firstShare = FirstIntrusion / (FirstIntrusion + SecondIntrusion)`, `secondShare = 1 - firstShare`.
  Otherwise both shares are 0 (no restoring force).
- `forces[first] -= axis * (restore * firstShare + firstAdvance * ResistiveGain)`
- `forces[second] += axis * (restore * secondShare + secondAdvance * ResistiveGain)`

A troop with zero intrusion and zero advance receives no barrier force.

### Notes

- `FriendlyOverlapResolver.Resolve` runs after `ApplyLinePressure` in the same update and can shift pivots. That
  movement is picked up as displacement on the next update and counts as intrusion. That is acceptable: the troop
  really moved.
- Fault slip moves the pivot away from the attacker, so it reduces intrusion. No special case is needed.
- Allied link tension can move a still troop into the gap. That counts as intrusion for that troop. Check this in
  testing with chained defenders (see Verification).
- Contacts are created at `interactionRange` and released at `releaseRange`, well outside `LineGap`, so the stored
  positions are always set before the gap is entered.

## Files

- `Assets/Battle Scripts/Troop/Pressure/TroopSegmentPressureRules.cs`: `Contact`, `ApplyLinePressure`,
  `ApplyLineBarrier`.
- `Assets/Battle Scripts/Troop/README.md`: update the Pressure section's description of the barrier.

## Verification

Use the Barrier and Pivot logging from [movement-testing.md](movement-testing.md).

- Compile in Unity `6000.1.6f1` and check the Console for errors.
- **Idle defender:** pursue an Idle enemy. The defender's pivot displacement and `LinePushVelocity` stay at 0, and
  the attacker holds at the gap.
- **Both advancing:** order two troops into each other. Both are pushed back in proportion to how far each entered.
- **Attacker stops inside the gap:** the attacker keeps its intrusion and is pushed out. The defender is untouched.
- **Footprint widening:** lines that overlap through soldiers spreading, with no pivot movement, get no barrier force.
- **Chained defender:** engage one member of a chain. Its pivot only moves by link tension, not by the barrier.
- **Retreat and chains:** fault slip and link tension behave as before.
- Re-run the Engagement row and the Idle, Engaged and Chained columns of the results matrix.
