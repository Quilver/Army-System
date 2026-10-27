# Plan 10: Cleanup, Known Issues & Verification

**Goal:** Remove deprecated code and values, fix the known issues from `Architecture.md` §11, make the Troop system compile
from a clean checkout, add the test framework, update the documentation, and run the verification gates.

**Spec sections:** §0, §7.4 (removed values).
**Depends on:** Phases 01–09. Some tasks can start earlier, one phase at a time.
**Blocking questions:** Q-49 (scope). Other items are not blocked.


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

### 10a. Deprecated code (spec §7.4)

- [ ] Remove `LungeDuration`, `MeleeBeatInterval`, `MaxPendingAttacks`, `PerMeleeTime` (if not renamed), `MaxDiscipline`.
- [ ] Remove `BruiseDurationSeconds` (if present), `R_BreakScale`, `ChainMaxBendAngleDeg` (replaced by inner angle),
  `PikeSupportRangeMultiplier`, `BowRange`, `BowAccuracy`, `BowDamage`, `BowFireRate`.
- [ ] Remove `FaultPressure` and `TroopMorale.ClashCooldown`.
- [ ] Remove `FlankMultiplier` (in `ClashResolver`).
- [ ] Remove `ChargeCoordinator`'s neighbour model (done in phase 04; confirm).
- [ ] Grep for each removed name. The result must be empty except in `specifications.md` §0 and §7.4.

### 10b. Known issues (`Architecture.md` §11)

- [ ] #1 Remove the duplicate comment in `Combat/ClashResolver.cs` (done in phase 04 if the file is rewritten; confirm).
- [ ] #3 Move `TroopDebugSnapshot` out of the untracked `Diagnostics/TroopDebugOverlay.cs` into a tracked file, or remove
  the calls. Confirm a clean checkout compiles.
- [ ] #4 `MeleeAttackClock` dictionary: removed in phase 04. Confirm.
- [ ] #7 Random sources: confirm no `UnityEngine.Random` remains in `Troop/` (except `TroopSoldierShowcase` if kept).
- [ ] #8 Replace `1.8` literals with `TroopTuning` or `LineGeometry` (phase 06).
- [ ] #9 Single source for default line width and radius.
- [ ] #10 `Discipline` moved to troop level (phase 01). Confirm.
- [ ] #11 `TroopMorale.DefaultChargeDistance` removed with the old morale logic. Confirm.
- [ ] #12 Decide whether to route input through `PlayerInputMap` or keep direct reads. Record the decision in
  `Architecture.md`.
- [ ] #13 Remove `Troop._legacyStats` if `IUnit` allows it. Otherwise document why it stays.
- [ ] #14 `README.md`: replace with a short pointer to `Docs/Battle/` and `Architecture.md`. Remove stale descriptions.

### 10c. Assembly and build

- [ ] Decide whether `Troop/Rules/` becomes its own assembly definition (for example `Troop.Rules.asmdef`, no Unity
  references). Benefit: EditMode tests can reference it without the full Assembly-CSharp. Record the decision.
- [ ] If an asmdef is added, check that `Editor/` and `Rules/` references are correct and that no Runtime code depends
  on Editor code.
- [ ] Confirm the Troop folder compiles with no warnings in a clean Unity import (delete `Library/` in a copy, not in the
  working tree).

### 10d. Test framework (requires approval)

- [ ] Add `com.unity.test-framework` to `Packages/manifest.json` and let Unity update `packages-lock.json`. Per `AGENTS.md`,
  the manifest is the package source of truth. Get approval before changing it.
- [ ] Create `Assets/Battle Scripts/Troop/Tests/EditMode/` and `Tests/PlayMode/` with assembly definitions that reference
  the Troop assembly (or `Troop.Rules`).
- [ ] Add the EditMode tests listed in phases 01–09.
- [ ] Add PlayMode scenarios in `Level 1 Troop Test` or a dedicated test scene (not in Build Settings).

### 10e. Scenes & assets

- [ ] Remove `TroopCombatWorld` value overrides from `Level 1 Troop Test` (`sDistance 5`, `aDistance 2` as reported by
  inspection). Set them to the spec values (30 and 5.2), or to new values that the spec records.
- [ ] Confirm the two test scenes run with no Console errors.
- [ ] Decide whether `Flock Boid Showcase.unity` is kept (untracked) and whether it should be tracked.
- [ ] Decide whether the Troop scenes go into Build Settings (Q-49). Default: no.

### 10f. Documentation

- [ ] Update `Architecture.md` for every changed class (§2–§9) and close the known issues in §11.
- [ ] Update `specifications.md` only if a behaviour changed. Record each change in §0.
- [ ] Close the questions this work answered in `Questions.md`. Move each to "Resolved in v0.9" or add a new section.

## Verification Gates

Run in this order. Stop at the first failing gate.

| Gate | Check | Pass |
|---|---|---|
| V1: Compile | Unity compiles with no errors. Console clean on opening both test scenes. | No errors. |
| V2: EditMode | All EditMode tests pass in the Test Runner. | 100% pass. |
| V3: PlayMode | All PlayMode scenarios pass. | 100% pass. |
| V4: Statistical | Charge rate, P(Hit), fire interval, mitigation share within tolerance (see phases 02, 04, 09). | All within tolerance. |
| V5: Manual | Checklist in phase 08 and the spec §6 controls. | All boxes ticked. |
| V6: Scenes | `Level 1 Troop Test` runs 2 minutes with 7 troops and no exceptions. | No exceptions. |
| V7: Clean checkout | Copy the repo, delete `Library/`, open in Unity. | Compiles, tests pass. |

## Files

- Changed: `Troop/README.md`, `Docs/Battle/*.md`, `Packages/manifest.json` (approval), `Assets/Resources/Levels/Test/*.unity`,
  any file listed in 10a and 10b.
- New: `Troop/Tests/**`, `Troop.Rules.asmdef` (if approved), `Diagnostics/TroopDebugSnapshot.cs` (tracked).

## Acceptance Criteria

- [ ] Gates V1–V7 pass.
- [ ] `git status` shows only the intended files. No `Library/`, `Temp/` or generated `.sln`/`.csproj` changes.
- [ ] `Architecture.md` matches the code (spot-check 5 classes).
- [ ] Every item in `Questions.md` is either resolved or has a recorded default.

## Risks

- Test framework addition changes the shared package list for all team members. Get approval first.
- A clean-checkout failure (untracked `TroopDebugSnapshot`) is easy to miss on one machine. Run V7 explicitly.
