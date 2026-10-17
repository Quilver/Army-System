# Issue 03: Soldiers That Retreat Last Overshoot and Yo-Yo

**Status:** Not confirmed. **Logging:** Retreat, Soldier (see [movement-testing.md](movement-testing.md)).

## Summary

During a fault slip (retreat after a break), the soldiers that start their retreat last overshoot their place in
the formation and oscillate back into position.

## Symptom

After a troop breaks, the front soldiers (nearest the attacker) move back, then overshoot their final position and
bounce back and forth before settling.

## Repro

1. Engage a troop with low morale so it breaks quickly.
2. Select the breaking troop and watch its front soldiers through the retreat and the return.
3. Repeat with a troop that is linked at one end (`LinkedEndRetreat` reduces retreat near linked ends).

## Suspected cause

- **Front soldiers start late and move slowly.** Delay is up to `MaximumDelay` (1 s) and move duration up to
  `FrontMoveDuration` (1 s), scaled by how far forward the soldier is (`Runtime/FaultSlipMotion.cs:8-14, 92-112`).
- **The slip ends on a timer.** `Tick` ends the slip after `SlipDuration` (3.14 s) once no soldier has
  `FaultOffset >= 1`. It then sets every `FaultOffset` to 0 at once (`Runtime/FaultSlipMotion.cs:77-88`). A
  front soldier still part-way through its return jumps to zero offset, so the field target it is pulled toward
  moves suddenly.
- **Position is moved directly.** The retreat step changes `soldier.Position` without changing `Velocity`
  (`Runtime/FaultSlipMotion.cs:73-76`), while steering also pulls the soldier toward the field shifted by its fault
  offset (`Soldiers/SoldierSteering.cs:146`). The two can disagree, and the velocity built up by the field pull
  carries the soldier past its place.
- **The pivot also slips.** The pivot eases back over `PivotSlipTime` and soldiers are compensated
  (`Runtime/FaultSlipMotion.cs:124-136`). Any mismatch between the pivot slip and the soldier offsets shows up as
  extra movement.
- Soldiers retreating are exempt from the radius pull (`Soldiers/SoldierSteering.cs:228`), so nothing limits the
  overshoot except the field.

## Logging needed

Per soldier, from the start of the slip until about 2 s after it ends:
- delay, move duration, `FaultOffset`, `FaultRetreatScale`;
- distance to its field position (the nearest point of the field shape);
- velocity along the slip axis, and the number of direction reversals along that axis;
- the frame on which the slip ends, and each soldier's offset just before it was reset.

## Confirmation criteria

Confirmed if the soldiers with the largest delay show an overshoot above a threshold (for example 0.3 units past the
field position) and more than one reversal along the slip axis, while rear soldiers do not. Note whether the
overshoot starts at the offset reset at the end of the slip, or during the return.

## Fix direction

Pick based on the confirmed cause:
- **End of slip:** do not reset offsets while any soldier is mid-return; let each soldier finish its own return, or
  extend the slip by the largest delay.
- **Velocity mismatch:** when the retreat step moves a soldier, also set its velocity to match the step, and zero or
  damp the along-axis velocity when its offset reaches 0.
- **Field overshoot:** use critically damped velocity matching for retreating soldiers (the field already uses
  `VelocityMatchGain`; check that it applies while the fault offset changes).

## Verification

- Front soldiers settle with no more than one reversal and no visible overshoot.
- Retreat distance, timing and the Fleeing then Idle state change are unchanged.
- Linked-end retreats (`LinkedEndRetreat`) still keep the link joint in place.
- Re-run the Retreat row of the results matrix.
