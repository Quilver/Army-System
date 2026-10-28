# Specification Update Log

Running log of changes to the battle system and of assumptions made while building it, against
`Docs/Battle/specifications.md` (v0.9). Newest entries first. Each entry gives what changed, the spec sections it
touches, and whether it is a **Decision** (asked for by the designer) or an **Assumption** (filled in to make the
change work; confirm or replace). `specifications.md` itself is not edited; fold entries into it when they are agreed.

---

## 2026-10-27 (3): Stronger retreat accordion, no facing orders for a single troop

### SU-14 Retreat move time grows from rear to front (Decision, revises SU-12 and SU-13)
- Spec §2.3, §5.5 step 1; §7.2 retreat tuning.
- The rear soldiers now start first **and** move fastest; the front soldiers start last **and** move slowest, so the
  troop stretches out much further before it closes up again.
- `RetreatSoldierMoveSeconds` (1.2 s, one value for everyone) is replaced by **`RetreatRearMoveSeconds`** (default
  **0.8 s**) and **`RetreatFrontMoveSeconds`** (default **1.8 s**). Each soldier's move time is interpolated by how far
  forward it stands along the attack axis, the same measure as its start delay.
- `RetreatWaveDelaySeconds` default raised from 0.6 s to **1.0 s**. With the defaults the rearmost soldier has reached
  its new place before the frontmost one starts, so the stretch reaches the full retreat distance.
- Retreat (Fleeing) length is now `RetreatWaveDelaySeconds + max(rear, front move) + 0.5 s settle` = **3.3 s** with the
  defaults (was 2.3 s). The troop still cannot break again until it ends (D-30).
- Code: `TroopTuning` (`RetreatRearMoveSeconds`, `RetreatFrontMoveSeconds`, `RetreatMoveSecondsAt`),
  `FaultSlipMotion.AssignSoldierMotion` / `SlipDuration`.

### SU-15 Retreat acceleration headroom 3x -> 6x (Assumption, consequence of SU-14)
- Spec §5.5.
- A soldier's acceleration (8) with 3x headroom cannot cover a 5-unit retreat in 0.8 s; the rear would lag its schedule
  and the rear/front speed difference would be lost. `SoldierSteering.RetreatAccelerationHeadroom` is raised to 6x so
  the rear ranks keep to `RetreatRearMoveSeconds`. Speed headroom (1.5x the scheduled speed) is unchanged.

### SU-16 A single troop cannot be given a facing; its drag is a Move (Decision, revises spec §6.2 "Single troop")
- Spec §6.2 "Drag at or above `OrderDragMinPixels`", §3.3 triggers.
- With one troop selected, an RMB drag at or above `OrderDragMinPixels` is a plain **Move** to the release point, not a
  battle line. The troop gets no line order and no ordered facing. Its line forms, and faces the threat, automatically
  when an enemy comes within `S_Distance` (§3.3 trigger 1).
- Drags with two or more troops selected are unchanged: a chained battle line with the Q-17 facing.
- `Troop.OrderLine` itself is unchanged and still used by scripted setups and tests; only player input stops issuing it
  for a single troop.
- Code: `OrderClassifier.Classify` takes the selected count; `TroopOrderPlanner` (single-troop line branch removed);
  help text in `TroopOrderOverlay`.

### SU-17 A single-troop drag released on an enemy is a Move, not a Pursue (Assumption)
- Spec §6.2.
- Pursue stays a click (below `OrderDragMinPixels`) on an enemy troop. A drag is always a Move for a single troop,
  wherever it is released. Change `OrderClassifier.Classify` if a drag onto an enemy should pursue instead.

---

## 2026-10-27 (2): Engagement gap, retreat distance, retreat wave

### SU-07 EngagementGap replaces LineGap (Decision)
- Spec §3.3 "Line-vs-line pressure", §4.1 "Engagement", §7.2 `LineGap`.
- `LineGap` (constant 0.3) is renamed **`EngagementGap`** and moved to `TroopTuning` (Distances), default **2.0** units.
- Two opposing troops are still **engaged** when their footprint gap is within `A_Distance` (5.2). Once engaged, the
  line barrier holds them apart at a footprint gap of `EngagementGap` instead of 0.3.
- Code: `TroopTuning.EngagementGap`; `TroopSegmentPressureRules` reads it; `TroopConstants.LineGap` is removed.

### SU-08 A_Distance and EngagementGap are both footprint gaps (Decision, confirms Q-16)
- Spec §3.3, §4.1, §7.2.
- `A_Distance`, `S_Distance`, `EngagementGap` and the pressure contact ranges all use the footprint gap
  (`FootprintGap.Gap`, the gap between the two troops' footprint rectangles or circles), never pivot or centre-line
  distance. This was already the case for `A_Distance` and `S_Distance`.

### SU-09 Pressure contact starts before EngagementGap (Assumption)
- Spec §3.3 "Line-vs-line pressure".
- The pressure contact range is `max(0.5 × A_Distance, EngagementGap)`, then × 1.25 to start it, × 1.6 to release it.
  With the defaults this is unchanged (2.6 / 3.25 / 5.2). The `max` only matters if `EngagementGap` is tuned above 2.6,
  so the barrier always has a contact to act on.

### SU-10 Engaged lines no longer touch: melee comes from charges (Assumption, consequence of SU-07)
- Spec §4.3-§4.5.
- At a 2.0 gap the front soldiers of two engaged lines are about 2.6 units apart, beyond `ContactDistance` (0.85).
  Soldiers only meet when they charge (charge range 7.8 soldier-to-soldier, so the gap is always in reach). Passive
  contact clashes between standing lines no longer happen. Tune `EngagementGap` down if lines should grind together.

### SU-11 Retreat distance is a fixed TroopTuning value in units (Decision, Q-34)
- Spec §5.5 step 2, §4.9 Surrounded, §7.2 `RetreatDistance`.
- Replaces `RetreatDistance = 1.0 × TroopRadius` (`RetreatDistanceScale`) with **`TroopTuning.RetreatDistance`**,
  default **5.0** units (equal to the old value for the radius-5 troops in use), the same for every troop.
- Used for the retreat itself, the shared-retreat chain (falloff still applies per hop) and the Surrounded path length.
- Any value set on the old `RetreatDistanceScale` in `Troop Tuning.asset` is no longer read.

### SU-12 Retreat is a rear-to-front wave (Decision, spec §2.3, §5.5 step 1)
- When a troop retreats, the soldiers furthest from the attacker fall back first; the others follow with a delay that
  grows toward the front, so the troop stretches and closes up again (accordion).
- New tuning: `RetreatWaveDelaySeconds` (default **0.6 s**: delay between the rearmost and the frontmost soldier
  starting) and `RetreatSoldierMoveSeconds` (default **1.2 s**: time each soldier takes to reach its new place).
- Delay is proportional to how far forward the soldier stands along the attack axis.

### SU-13 Retreat motion has no overshoot and return (Assumption, replaces D-31 timing)
- Spec §5.5; Decisions D-31; Handoff issue-03.
- Before: the pivot jumped back and every soldier headed for the new line at once; the per-soldier delay only staggered
  an extra fall-back past the new line (to about 2× the distance) followed by a return. That hid the wave and caused
  the overshoot in issue-03.
- Now: each soldier holds the ground it stood on until its start time, then eases straight to its place in the new
  line. No hold, no return.
- Retreat (Fleeing) length is now `RetreatWaveDelaySeconds + RetreatSoldierMoveSeconds + 0.5 s settle` = **2.3 s** with
  the defaults (was 3.14 s). The troop cannot break again until it ends (D-30 unchanged).

---

## 2026-10-27 (1): Line fill and battle UI

### SU-01 Battle lines fill their length (Decision, spec §2.1, §3.3)
- In Line and Chain, soldiers are spread evenly along the line instead of bunching at its middle.
- Soldiers still have no slots: each frame the living soldiers are sorted by position along the line core and grouped
  into files of `Ranks` soldiers (`Ranks = ceil(soldiers / floor(L / LineSpacing))`, spec §3.3); each file is pulled
  toward an evenly spaced point along the core. Depth within a file comes from separation and the half-width band.

### SU-02 Line fill details (Assumption)
- Pull strength is `LineFillGain = 0.8` × the field attraction strength (constant in `SoldierSteering`).
- Chargers, followers and retreating soldiers keep their place in the sort (the line does not reshuffle while they are
  away) but are not pulled. Columns, blobs and routs are unchanged.
- Linked ends keep the joint clearance: the fill spans the core after clearance.

### SU-03 Help panel at top centre, 3× size (Decision, spec §6 UI)
- The controls help moves from bottom left to top centre, with 3× text (36 px) and up to 3× width (1440 px). It is
  capped so it never covers the right-hand status panel, and wraps.

### SU-04 Unit card shows the selected troop (Decision, spec §6 UI)
- The bottom-left unit card shows the first selected troop: name, portrait and stats (soldiers alive/total, Move,
  BaseMorale, Discipline; per soldier type: Speed, Defence (+armour), Strength, Support, Charge, Aggression, HP,
  weapons with skill, armour). Hidden while nothing is selected.

### SU-05 Unit card fallbacks (Assumption)
- Name: the troop's legacy stat block name, otherwise its GameObject name.
- Portrait: the legacy stat block portrait, otherwise the sprite of the most numerous soldier type, in army colour.

### SU-06 Status panel (Decision, spec §6 UI)
- The right-hand panel (F3) shows per selected troop: formation (with line length, links, ordered or threat-facing),
  status (Idle, Moving, Pursuing, Holding line, Engaged, Retreating, Routed), Effective Morale vs initial, Shock with
  its break level and decay rate, velocity, soldiers, and nearby or engaged enemies. F5 adds the debug values.
