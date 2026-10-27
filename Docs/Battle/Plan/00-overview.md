# Plan 00: Overview & Gate

This folder splits the implementation of `Docs/Battle/specifications.md` (v0.9) into phases. Each phase file lists its goal,
dependencies, blocking questions, tasks, files and acceptance criteria. Open questions are in `Docs/Battle/Questions.md`.

## 1. Phase Index

| # | File | Phase | Blocking questions (P1 in bold) |
|---|---|---|---|
| 01 | `01-data-and-stats.md` | Data model, stats, equipment, composite resolution | **Q-2**, Q-1, Q-3, Q-6 |
| 02 | `02-health-and-damage.md` | Bleed / Bruise / Fatigue / Death, mitigation | Q-5 (later) |
| 03 | `03-morale-and-breaks.md` | Effective Morale, Shock decay, break cases, retreat, rout | **Q-34**, Q-33, Q-35, Q-36 |
| 04 | `04-melee-and-charges.md` | Charges, followers, clash, duel, support, domination, assault | **Q-26**, Q-7, Q-9, Q-24, Q-27, Q-28 |
| 05 | `05-footprints-faces-engagement.md` | Footprint rectangles, footprint gap, faces, flanking | **Q-31**, Q-16, Q-32 |
| 06 | `06-formation-states-and-movement.md` | Explicit state priority, S_Distance, column, line geometry, pivot separation | **Q-11**, **Q-12**, Q-15 |
| 07 | `07-chains.md` | Hinge, attach, break, shared retreat, topology | **Q-13**, **Q-20**, **Q-22** |
| 08 | `08-input-and-orders.md` | Selection, move/pursue/line orders, chain manipulation, overlay | **Q-17**, Q-47, Q-19 |
| 09 | `09-ranged-combat.md` | Bows, projectiles, hit chance, fire timing | **Q-39**, **Q-40** |
| 10 | `10-cleanup-and-verification.md` | Remove deprecated code, fix known issues, tests, scenes | Q-49 (scope) |

Bold = P1. Phases cannot start until their P1 questions are answered (see §4).

## 2. Dependency Graph

```
01 Data ──► 02 Health ──► 03 Morale ──► 04 Melee ──► 08 Input (pursue, orders)
                                  │          │
                                  │          └──► 09 Ranged (needs 02, 05)
                                  ▼
            05 Footprints ──► 06 Formation ──► 07 Chains ──► 08 Input (chain manipulation)
                                                                    │
                                                                    ▼
                                                  10 Cleanup & Verification
```

- **Strictly sequential:** 01 → 02 → 03 → 04 (the damage and morale pipeline is shared).
- **Can run in parallel after 02:** 05 (geometry) with 03/04.
- **Needs 06 before 07:** chains need the new line geometry (`L_nat`).
- **Needs 07 before the chain part of 08:** ghost tether and envelope checks.
- **10 is last.** Some cleanup can start earlier, one phase at a time.

## 3. Working Rules (apply to every phase)

1. **Named tuning values.** Every number in `specifications.md` §7.2 is a field on a `TroopTuning` ScriptableObject
   (new, under `Assets/Resources/` for runtime loading), or a `const` in `TroopConstants` if it never changes. No new magic
   numbers in code.
2. **Pure rules.** Maths that does not need Unity (mitigation, duel, support, morale, chain geometry) lives in plain C#
   classes under `Troop/Rules/` with no `MonoBehaviour` dependency. This is what makes EditMode tests possible.
3. **Seeded randomness.** Replace `UnityEngine.Random` with an injected `ITroopRandom` (one seeded instance per battle).
   Tests use a fixed seed. Introduce this in Phase 02 and use it everywhere after that.
4. **Serialized assets.** Edit `.asset` files and scenes through the Unity Editor. Keep `.meta` files. When renaming a
   serialized field, add `[FormerlySerializedAs]`.
5. **Verify in the Editor.** After each C# change, wait for compile, check the Console, then run
   `Assets/Battle Scripts/Troop` test scenes. Per `AGENTS.md`, the Test Runner is the test tool.
6. **Git.** Do not commit unless asked. Keep changes scoped to `Assets/Battle Scripts/Troop/`, `Docs/Battle/`, and the
   profile assets for the phase.
7. **Docs.** When a phase changes behaviour, update `Docs/Battle/Architecture.md` (§6 and §9). Update the spec only if the
   behaviour changes, and record the change in `specifications.md` §0.

## 4. Gates

| Gate | Condition | Unblocks |
|---|---|---|
| **G0: Spec sign-off** | Q-1, Q-2, Q-11, Q-12, Q-13, Q-17, Q-20, Q-22, Q-26, Q-31, Q-34, Q-39, Q-40, Q-49 answered or defaults accepted. | Phase 01 (scales), phase 05 (faces), phase 06 (geometry), phase 07 (chains), phase 09 (ranged). |
| **G1: Pipeline** | Phases 01–03 complete. Unit tests for mitigation, morale and the composite resolver pass. | Phase 04. |
| **G2: Combat** | Phase 04 tests pass, and a 30 s charge-rate measurement is within 10% of the expected count. | Phases 08 (pursue) and 09. |
| **G3: Geometry** | Phases 05–06 done. Line and footprint debug views match the spec. | Phase 07. |
| **G4: Chains** | Phase 07 done. Each §3.4.1 edge case reproduced. | Chain part of phase 08. |
| **G5: Release** | Phase 10 done. Both test scenes run with no Console errors. | Handoff. |

## 5. Definition of Done (per phase)

- All tasks in the phase file are ticked.
- Acceptance criteria in the phase file pass.
- No new Console errors or warnings in `Level 1 Troop Test`.
- Tuning values are fields, not literals.
- `Architecture.md` updated for changed classes.
- Unanswered questions that the phase touched are noted in `Questions.md`.

## 6. Test Strategy

| Level | Tool | What |
|---|---|---|
| EditMode unit tests | Unity Test Framework (EditMode) | Pure rules in `Troop/Rules/`: mitigation split, duel and domination, support, morale formula and decay, composite resolution, hinge geometry, hit chance. |
| PlayMode scenario tests | Unity Test Framework (PlayMode) | Scripted scenes: clash count, charge rate over 30 s, break and retreat, chain link and unlink, rout event. |
| Statistical checks | EditMode with fixed seeds, many samples | Charge rate, P(Hit), fire interval jitter. Assert within tolerance. |
| Manual checklist | `Level 1 Troop Test` | Selection, orders, ghost tether, overlay, faces, debug views. |

The test framework is not in `Packages/manifest.json`. Adding it changes the package source of truth, so do this only with
approval (Phase 10 task).

## 7. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Stretched lines cannot fit inside the troop radius (Q-11). | Chains look wrong, soldiers clip out of the footprint. | Answer Q-11 before phase 06. Prototype in the test scene first. |
| Chain changes break the existing line pressure. | Lines pass through each other. | Keep `TroopSegmentPressureRules` unchanged until phase 07. Add a regression scene. |
| Removing banked attacks changes battle balance. | Fewer or more clashes than expected. | Measure clashes per troop per minute before and after phase 04. Tune `ClashCooldownSeconds`. |
| `UnityEngine.Random` in many systems. | Battles not reproducible, tests flaky. | Phase 02 introduces `ITroopRandom`. |
| Untracked `TroopDebugOverlay.cs` is referenced by production code. | A clean checkout does not compile. | Phase 10 moves `TroopDebugSnapshot` to tracked code. |
| Balance values in assets are on the old scale (morale 10–100). | Troops break at wrong times. | Phase 01 includes a migration table signed off by the designer. |
| O(n²) charge and clash code. | Frame drops with large troops. | Phase 04 uses a spatial query for candidates. Measure with 200 soldiers. |
