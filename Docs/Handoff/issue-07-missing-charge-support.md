# Issue 07: Missing Charge Support

**Status:** Missing feature; baseline needed. **Logging:** Charge, Clash (see [movement-testing.md](movement-testing.md)).

## Summary

There is no mechanic for other soldiers to follow and support a charging soldier. A charger runs ahead alone, and its
troop-mates only join if they start their own charge.

## Current behaviour

- **Charge spreading only.** Neighbours become more likely to start their own charge when nearby troop-mates are
  charging (Ising-style chance in `Combat/ChargeCoordinator.cs:79-104`). They pick their own targets.
- **Chargers leave their troop-mates behind.** Charging soldiers move at double speed and skip separation, so they
  slip through their own troop (`Soldiers/SoldierSteering.cs:129, 184, 192`).
- **Support counts in the clash.** `ClashResolver.ClashPressure` adds 0.12 per ally within 3.3 units, up to 5
  (`Combat/ClashResolver.cs:45-59`). A lone charger gets little of this.

## Baseline to record first

Using the Charge and Clash logging, per charge:
- allies within 3.3 of the charger when it clashes;
- the charger's clash win rate, compared with soldiers that are not charging;
- how far ahead of the formation the charger is when it clashes.

## Design questions

Answer these before implementing:
- **Who follows?** The nearest N troop-mates, or those within a radius and behind the charger relative to the facing.
- **How do they move?** At charge speed or normal speed, and do they keep separation from each other?
- **Where do they go?** A point behind and beside the charger, or the charger's target?
- **How long?** Until the charge ends, or until a clash, with a maximum time.
- **Radius limit.** How far can supporters leave the troop radius?
- **Committed?** Do supporters count as committed (`SoldierAgentState.IsCommitted`), which exempts them from the
  radius pull and the density spread?
- **Clash behaviour.** Can supporters start their own clashes, or only add support pressure?

## Fix direction

- Add a support state alongside the charge and lunge state on `SoldierAgentState` (for example a
  `SoldierSupportState` in `Model/`, holding the supported charger, the follow offset and the time remaining).
- In `ChargeCoordinator`, assign supporters when a charge starts, and release them when the charge ends or is
  cancelled.
- In `SoldierFieldPlanner.TrySeek`, return the follow point for supporters (behind and beside the charger, in troop
  local space).
- Decide how supporters take part in separation, spreading and the minimum-separation pass in `SoldierSteering`.
- Update the Troop README (Combat and Soldiers sections).

## Verification

- Allies within 3.3 at a charger's clash, and the charger's win rate, rise compared with the baseline.
- Supporters do not scatter the line or leave large gaps in the formation.
- Supporters rejoin the field when the charge ends.
- Fault slip cancels support as it does charges.
- Re-run the Charge and Charge support rows of the results matrix.
