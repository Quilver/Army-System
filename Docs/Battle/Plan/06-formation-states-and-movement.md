# Plan 06: Formation States, Movement & Line Geometry

**Goal:** Make formation state an explicit priority decision (Idle, Column, Line, Chain, Rout), remove the S_Distance
hysteresis, drive pivot speed from `Move`, use the new line length `L_nat` and spacing `0.9`, and scope pivot separation so
it does not block chain endpoints.

**Spec sections:** §1.4, §2.4, §3.1, §3.2, §3.3, §7.2 (formation).
**Depends on:** Phase 05 (footprints), phase 01 (Move, speed conversion).
**Blocks:** Phase 07 (chains need the new geometry).
**Blocking questions:** Q-11 (troop radius and stretched lines), Q-12 (`L_nat`), Q-15 (pivot separation and linked pairs).


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

### 6a. Formation state

- [ ] Add `FormationMode` enum (`Idle`, `Column`, `Line`, `Chain`, `Rout`) to `Model/`.
- [ ] Add `Troop.FormationMode` (derived each frame, not serialized). Compute it in one place: `FormationModeResolver`
  in `Rules/` (pure). Priority: Rout > Chain > Line > Column > Idle.
- [ ] Replace the implicit priority in `SoldierFieldPlanner.Shape` (`HasBattleLine`, then column, then manual line, then
  idle line, then blob) with a switch on `FormationMode`.
- [ ] Keep `UnitState` (`Idle`, `Moving`, `Fleeing`) for the legacy interface. It is not the formation mode.
- [ ] **Chain outranks Line and Column** (intended, spec §3.1). A chained, marching troop stays in Line shape.

### 6b. Triggers

- [ ] **S_Distance (Q-16).** Enemy within `S_Distance = 30` (footprint gap) → Line. Remove `DisengageDistanceScale` for
  threat sensing. Remove the heading freeze rule (Q-16 and phase 05 facing lock).
- [ ] **Column.** Active move or pursue order and not overridden by Line, Chain or Rout.
- [ ] **Idle.** Default. A line order with `HasIdleLine` is Line.
- [ ] **Remove `ThreatFormationRules` edge-distance sensing** and use footprint gap (phase 05).

### 6c. Movement & speed

- [ ] `TroopPivotMover`: pivot speed = `Move × SpeedUnitsPerStatPoint`. Remove `min(6.24, maxSpeed × 0.65)`.
- [ ] `SoldierProfile.MaxSpeedUnits = Speed × 0.6`. Soldiers steer at their own speed, but never faster than the pivot
  matches (Move is the minimum, so this holds).
- [ ] Keep `PathRebuildIntervalSeconds = 0.5` and the `TargetMovedSquared` threshold for pursuit.
- [ ] Remove the arrival tolerance special cases only if they are not needed. Check after the speed change.

### 6d. Line geometry

- [ ] `TroopConstants.LineSpacing = 0.9` (spec §7.2). Remove the 0.6 value.
- [ ] `L_nat = NaturalLineRadiusScale × TroopRadius`, with `NaturalLineRadiusScale = 1.8` (Q-12).
  - Compute in one place: `LineGeometry.NaturalLength(troopRadius)`.
  - Remove `Troop.MinimumLineWidth`'s dependence on living soldier count. Keep a minimum for drawing.
- [ ] **Soldiers per rank:** `floor(L_nat / LineSpacing)`. Ranks: `ceil(n / perRank)`. Half-width from rank count (existing
  `RankHalfWidth` formula, with the new spacing).
- [ ] **Remove `LineWidth` from `TroopProfile` and `Troop`,** and from `TroopOrderPlanner`. A drag sets the line length
  directly. Clamp it to the stretch range `[0.7, 3.0] × L_nat` (spec §6.2). The soldier-fit limit depends on Q-11.
- [ ] **Stretched lines (Q-11).** Implement the chosen option. Options: (a) soldiers outside the radius for linked lines,
  (b) radius grows with the line, (c) lower max stretch. Do not start until Q-11 is answered.
- [ ] **Joint clearance.** Keep `min(0.45, 0.15 × length)` at linked ends, as a `TroopTuning` value.

### 6e. Pivot separation (Q-15)

- [ ] `FriendlyOverlapResolver`:
  - Apply only to **pivots** of friendly troops (same army).
  - Minimum spacing `PivotSeparationScale × (r_A + r_B)`, with scale 0.25 (existing behaviour; make it a tuning value).
  - Exclude line endpoints from any check. Endpoint attachment (phase 07) must not be blocked.
  - Exclude linked pairs as well, if Q-15 answers yes.
- [ ] Verify blobbing is still prevented: troops from the same army do not stack on one pivot.

### 6f. Idle blob

- [ ] Keep `BlobRadiusScale = 0.85 × TroopRadius` for the disc. Move the value into `TroopTuning`.

## Files

- New: `Model/FormationMode.cs`, `Rules/FormationModeResolver.cs`, `Rules/LineGeometry.cs`.
- Changed: `Formation/ThreatFormationRules.cs`, `Formation/LineSegmentBuilder.cs`, `Formation/AlliedLineConnector.cs`
  (only the grouping by pivot distance is changed in phase 07), `Soldiers/SoldierFieldPlanner.cs`,
  `Runtime/TroopPivotMover.cs`, `Runtime/TroopFormationState.cs`, `Runtime/Troop.cs` (`MinimumLineWidth`, `LineWidth`),
  `Pressure/FriendlyOverlapResolver.cs`, `Runtime/TroopConstants.cs`, `Input/TroopOrderPlanner.cs` (width),
  `Model/TroopProfile.cs` (remove LineWidth).
- Assets: TroopProfile (remove LineWidth), scenes that set `TroopCombatWorld` values.

## Acceptance Criteria

- [ ] EditMode tests:
  - `FormationModeResolverTests`: each priority pair (Rout over Chain, Chain over Line, Line over Column, Column over Idle).
  - `LineGeometryTests`: `L_nat` for radius 16.5 = 29.7. Soldiers per rank = `floor(29.7 / 0.9)` = 33.
- [ ] PlayMode: a 12-soldier troop marching at Move 5 moves at 3.0 units/s (within 5%).
- [ ] PlayMode: an enemy entering 30 units (footprint gap) switches the troop to Line within 1 frame. No flicker when it
  sits at the boundary (hysteresis removed, so check the boundary test passes without oscillation).
- [ ] PlayMode: 50 friendly troops ordered to one point do not stack (no two pivots closer than the minimum).
- [ ] No `MinimumLineWidth` living-count dependence remains for `L_nat`.
- [ ] `LineSpacing` is 0.9 everywhere (grep for `0.6f` in `Troop/`).

## Risks

- Changing line length changes every chain and pressure interaction. Re-test the Level 1 scene after this phase.
- Removing S_Distance hysteresis can make a troop flip between Column and Line on the boundary. Check the test scene.
- Q-11 may force a change to the soldier clamp in `SoldierMotionConstraints`. Keep that change behind a flag until the
  answer is in.
