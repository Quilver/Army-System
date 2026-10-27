# Handoff: Movement System Logging and Testing

## Goal

Add opt-in diagnostics to the troop movement and combat systems, then run a structured test pass over the features
and edge cases below. The diagnostics also provide the evidence each known-issue document needs.

## Scope

- Code under `Assets/Battle Scripts/Troop/`.
- Diagnostics are off by default. With them off, gameplay must be identical (no extra allocations in hot loops, no
  changed ordering, no use of `UnityEngine.Random`).
- Do not change gameplay behaviour as part of this work. Fixes belong to the issue documents.

## Logging infrastructure

### `TroopDiagnostics` component

A MonoBehaviour placed next to `TroopCombatWorld` (one per battle). Suggested location:
`Assets/Battle Scripts/Troop/Diagnostics/TroopDiagnostics.cs`.

Inspector settings:
- **Enabled** master switch.
- **Categories** (flags): Pivot, LinePressure, Barrier, Chain, Engagement, Clash, Charge, Retreat, Soldier.
- **Troop filter**: all troops, selected troops only, or an explicit list.
- **Sample interval**: every frame (0) or every N seconds. Events (link created, charge ended, clash) are always
  logged when their category is on.
- **Seed**: optional. When set, call `Random.InitState(seed)` at startup so charge and clash randomness is
  repeatable.

Outputs:
- **CSV**, one file per category: `Application.persistentDataPath/Diagnostics/<scene>_<yyyyMMdd_HHmmss>_<category>.csv`.
  Buffer rows in memory and flush periodically and on `OnDisable` / `OnApplicationQuit`. First column is
  `Time.time`, second is `Time.frameCount`.
- **Overlay** (OnGUI, toggle key): live values for selected troops: state, pivot velocity, line push velocity,
  engaged enemy, barrier share, links, charging soldier count, shock, fault pressure.
- **Gizmos**: pivot velocity and line push vectors, barrier penetration along the axis, engagement pairs, link
  joints and tension, charge lines (source to target), clash markers that fade over about 1 s.

Hooks:
- Static methods such as `TroopDiagnostics.RecordBarrier(...)` that return immediately when disabled or when the
  category or troop is filtered out.
- Systems must not read anything back from diagnostics.
- Identify troops by `name` plus `GetInstanceID()`, and soldiers by `SoldierAgentState.Id`.

### Data to record

| Category     | Hook location                                                                                                                                                | Fields                                                                                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Pivot        | `TroopPivotMover.Tick`                                                                                                                                       | troop, state, position, `_pivotVelocity`, `LinePushVelocity`, has destination, pursuit target, displacement this frame, whether the troop has any order      |
| LinePressure | `TroopSegmentPressureRules.ApplyLinePressure`                                                                                                                | pair, centre distance, both half-widths, footprint distance, compression, both drives, both efforts, both forces                                             |
| Barrier      | `TroopSegmentPressureRules.ApplyLineBarrier`                                                                                                                 | pair, axis, penetration, both advances, both shares, restore, both applied forces                                                                            |
| Chain        | `LineLinkAssigner.LinkAvailableEnds`, `LineLinkAssigner.Prune`, `LineLinkAssigner.ClearGroupLinks`, `LineEndAimer.SnapLinkedEnds`, `AlliedLinkTension.Apply` | events: link created or cleared (troops, ends, reason); samples: tension vector, tension force, each troop's facing and order                                |
| Engagement   | `ThreatFormationRules.FindEngagements`                                                                                                                       | troop, engaged enemy, segment distance, limit used (normal or hysteresis); event when the engaged enemy changes                                              |
| Clash        | `SoldierContactResolver.ResolvePair`, `ClashResolver.Resolve`, `ClashResolver.ResolveWinner`                                                                 | soldier pair, soldier distance, below `SoldierContactDistance` (0.85) or not, initiator, both pressures, winner, damage, blocked by clash cooldown           |
| Charge       | `ChargeCoordinator`                                                                                                                                          | start: source, target, distance, duration; end: reason (time out, out of range, invalid target, fault slip), distance left; clash during the charge (yes/no) |
| Retreat      | `FaultSlipMotion`                                                                                                                                            | begin: axis, distance, pivot distance; per soldier: delay, move duration, offset, distance to field position, reversals; end: time to settle                 |
| Soldier      | `SoldierSteering.Simulate`                                                                                                                                   | selected troops only: soldier, position, velocity, drive fraction, committed, charging, lunging, seek point                                                  |

`ChargeCoordinator.TickActiveCharges` cancels charges in a single condition (`ChargeCoordinator.cs:57`). Split the
condition when logging so the end reason is recorded.

### Derived metrics

Compute these in the overlay or in the CSV analysis:
- **Unordered pivot drift**: total displacement of a troop that is Idle with no destination. Expected 0.
- **Clashes outside base contact**: count and share of clashes with soldier distance above 0.85, plus a histogram.
- **Charge outcomes**: share of charges by end reason, and share of charges that ended with no clash.
- **Retreat overshoot**: maximum distance past the final field position, and direction reversals per soldier.
- **Chain events per minute**, and pivot jumps (displacement above a threshold in one frame) when a link forms.

## Test scenes

- Levels in `Assets/Resources/Levels/` for realistic layouts, terrain and choke points.
- `TroopSoldierShowcase` for controlled setups.
- Use the diagnostics seed for runs that are compared before and after a fix.

## Features to test

For each feature record: setup, orders given, expected behaviour, logged evidence (CSV file names), pass or fail.

| Feature | Setup | Expected behaviour |
|---|---|---|
| Idle | One troop, no enemies in sensing range | Pivot still. Soldiers settle into a blob with no drift. |
| Marching | Move order across open ground | Column forms, pivot follows the path, troop arrives and returns to Idle. |
| Battle line | Enemy inside sensing range (30) | Threat-facing line forms toward the enemy. |
| Chain | Two or three allied troops side by side near an enemy | Ends link, joints snap, link tension stays within slack. |
| Engagement | Pursue an enemy | Engagement forms within A_Distance (5.2) and holds until 1.5 × A_Distance. |
| Charge | Aggressive troop engaging | Charges spread through the troop, chargers reach contact and clashes follow. |
| Retreat | Damage a troop past its morale | Fault slip back, hold and return. State goes Fleeing then Idle. |
| Charge support | Charging soldier with nearby troop-mates | Not implemented. Record the baseline (see issue 07). |

## Edge cases to test

Run each edge case against the features it applies to.

| Edge case | Setup |
|---|---|
| Idle | An enemy engages a troop that has no orders. |
| Moving | Engage a troop while it is marching. |
| Chained | Engage one member of a chain. |
| Enter/exit chain | Move a troop into an existing chain, then order it away. |
| Flanked | Attack a line from its side and from its rear (flank multipliers 2× and 4×). |
| Engaged | Give a new order to a troop that is already engaged. |
| Skirmishing | Repeated brief contacts at the edge of A_Distance, to exercise engagement hysteresis. |
| Terrain | Engage near obstacles (`SoldierMotionConstraints` circle-casts). |
| Choke points | Two lines meet in a narrow passage. |
| Surrounded | One troop attacked from three or more sides. |

## Results matrix

Fill in pass, fail, n/a, or the issue number that explains a failure.

| | Idle | Moving | Chained | Enter/exit chain | Flanked | Engaged | Skirmishing | Terrain | Choke points | Surrounded |
|---|---|---|---|---|---|---|---|---|---|---|
| Idle | | | | | | | | | | |
| Marching | | | | | | | | | | |
| Battle line | | | | | | | | | | |
| Chain | | | | | | | | | | |
| Engagement | | | | | | | | | | |
| Charge | | | | | | | | | | |
| Retreat | | | | | | | | | | |
| Charge support | | | | | | | | | | |

## Known issues

Each has its own document with the logging and confirmation criteria it needs:

1. [Chains missing controls for exiting](issue-01-chain-exit-controls.md)
2. [Snapping into chains when flanking breaks the surround](issue-02-flank-snapping-breaks-surround.md)
3. [Soldiers that retreat last overshoot and yo-yo](issue-03-retreat-overshoot-yoyo.md)
4. [Engaged idle troops fall back unprompted](issue-04-idle-engaged-fallback.md)
5. [Clash without base contact](issue-05-clash-without-contact.md)
6. [Lazy chargers](issue-06-lazy-chargers.md)
7. [Missing charge support](issue-07-missing-charge-support.md)

## Deliverables

- `TroopDiagnostics` and its hooks, off by default.
- The completed results matrix, with links to the CSVs used.
- Baseline metrics for issues 03, 05, 06 and 07, recorded in their documents.

## Verification

- Unity compiles with no Console errors.
- With diagnostics off, a seeded run matches a seeded run without the diagnostics code.
- With diagnostics on, every CSV opens cleanly and the overlay shows live values for the selected troop.
