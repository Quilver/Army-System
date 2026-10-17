# Issue 06: Lazy Chargers

**Status:** Not confirmed; may not be an issue. **Logging:** Charge, Clash (see [movement-testing.md](movement-testing.md)).

## Summary

Charging soldiers sometimes give up part-way to their target. This may be intended. The goal is to find out whether
charges ending early is the cause, and how many clashes are missed because of it.

## Symptom

A soldier starts toward an enemy soldier at charge speed, then stops and returns to the formation before reaching
contact.

## Repro

1. Engage with an aggressive troop (high `Aggression`).
2. Select the troop and watch individual chargers (charge gizmo lines from the diagnostics help).

## Suspected cause

- **Short fixed duration.** A charge lasts `Random.Range(0.75f, 1.15f)` seconds, with a cooldown of 1.2–3 s
  (`Combat/ChargeCoordinator.cs:37`).
- **Cancel conditions.** `TickActiveCharges` cancels a charge (with a 1.1 s cooldown) when any of these is true
  (`Combat/ChargeCoordinator.cs:49-58`):
  - the target is invalid (dead, removed, or same team);
  - the troop is fault slipping;
  - the charge time has run out;
  - the target is more than 1.7 × A_Distance away.
- **Distance vs. duration.** Targets are picked up to A_Distance (5.2) away (`Combat/ChargeCoordinator.cs:106-129`).
  A charger moves at `ChargeSpeedScale` (2) × `MaxSpeed`, but is limited by acceleration and by steering damping
  (`Soldiers/SoldierSteering.cs:129-133`). Depending on the soldier profile, 0.75 s may not be long enough to cover
  the distance.
- **Moving targets.** The charger aims at the target's position plus a short velocity lead
  (`Soldiers/SoldierFieldPlanner.cs:67-68`). Clash knock-back on the target can move it away mid-charge.
- **The charge is not ended by a clash.** Nothing ends a charge when the charger clashes, so it may continue or time
  out after contact. Check this is intended.

## Logging needed

Per charge:
- start: source soldier, target, start distance, duration, source `MaxSpeed` and `Acceleration`;
- end: reason (split the combined condition at `ChargeCoordinator.cs:57`), distance left, time elapsed;
- whether the charger clashed during the charge, or within a short window (for example 0.5 s) after it ended.

## Confirmation criteria

Report:
- the share of charges by end reason;
- the share of time-outs that ended before contact (distance left above 0.85);
- the clash rate for charges that reached contact, and for those that timed out before contact;
- an estimate of missed clashes: time-outs before contact × (clash rate for charges that reached contact − clash rate
  for time-outs).

It is an issue if time-outs before contact are a large share of charges and the estimate of missed clashes is
significant compared with total clashes.

## Fix direction

If confirmed:
- End charges on contact, on a clash, or when the target becomes invalid, rather than on a fixed timer. Keep a longer
  safety time-out.
- Or scale the duration by the start distance divided by the expected charge speed, plus a margin.
- Keep the 1.7 × A_Distance range cancel so chargers do not chase indefinitely.

## Verification

- Time-outs before contact drop, and the clash rate per charge rises.
- Charge spreading (the Ising-style chance) and cooldowns behave as before.
- Chargers rejoin the formation after the charge.
- Re-run the Charge row of the results matrix.
