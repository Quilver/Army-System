# Issue 05: Clash Without Base Contact

**Status:** Not confirmed; needs a design decision. **Logging:** Clash (see [movement-testing.md](movement-testing.md)).

## Summary

Soldiers clash while they are visibly apart. Clashes are started within the action distance (A_Distance, 5.2), while
soldiers only physically touch within 0.85.

## Design note

This is the documented behaviour: the Troop README says "A_Distance is also the clash distance". So this is a design
decision as much as a bug. Decide whether clashes should require base contact before changing the code, and update
the README to match.

## Symptom

Clash pulses, knock-back and damage happen between soldiers several units apart.

## Repro

1. Engage an enemy troop.
2. Select either troop and watch the clash pulses in the first seconds of contact, before the lines close up.

## Suspected cause

- `SoldierContactResolver.Resolve` uses `actionDistance` as the clash distance
  (`Combat/SoldierContactResolver.cs:21-24`).
- `ResolvePair` returns early only beyond that distance (line 66). Below it, it marks both soldiers engaged, adds
  both troops to melee, and starts a clash if either soldier has a banked attack (lines 76-87).
- Physical contact (bounce) uses `SoldierContactDistance` (0.85) (lines 8, 73-74).
- `ClashResolver.Resolve` then knocks both soldiers back by up to 0.6 and gives them an impact velocity
  (`Combat/ClashResolver.cs:19-27`), which can push them further apart.
- The initiator lunges at its opponent (`Combat/SoldierContactResolver.cs:86`), but the clash is resolved
  immediately, not when the lunge arrives.

## Logging needed

For every clash: the soldier pair, their distance at the moment the clash starts, whether it is below 0.85, the
initiator, and whether either soldier was charging or lunging. Build a histogram of clash distances.

## Confirmation criteria

Confirmed if a significant share of clashes (report the percentage) start with soldiers more than 0.85 apart. Report
the median and 90th percentile distance.

## Fix direction

If the decision is that clashes require contact:
- Add a separate clash distance close to contact (for example 1.0–1.2), and use it for starting clashes in
  `ResolvePair`.
- Keep A_Distance for engagement marking and attack accrual, so soldiers near the enemy still build attacks.
- Option: start the lunge at A_Distance and resolve the clash only when the lunging soldier reaches contact.
- Review the knock-back distance and impact speed in `ClashResolver` for the new range.
- Update the Troop README (Melee combat and Tuning sections).

If the decision is to keep the current range: document it, and consider shortening the visual gap (lunge speed or
duration) so clashes read as contact.

## Verification

- Clash distance histogram: clashes start at or near contact.
- Attack rate and hit rate (`MeleeMetrics`) do not collapse compared with the baseline.
- Melee still starts promptly when lines meet.
- Re-run the Engagement and Charge rows of the results matrix.
