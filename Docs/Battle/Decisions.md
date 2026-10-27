# Decision Log and Next Steps

Running record of decisions made during implementation. Each entry says where it came from. Open items are in
`Questions.md`. Phase status is in `Plan/`.

---

## Phase 01: Data Model, Stats, Equipment, Composite Resolution

**Status (2026-10-26):** Code complete and compiles in Unity 6000.1.6f1 (batch mode, in a copy). The sample data
generator has run cleanly and its output was checked against the designer's sheets. The sample assets in the repository
were written by the open Editor **before** the integer change and the name fix, so they must be regenerated from the
Editor (Next Steps 1). Old Flock assets are not migrated.

### Verification performed

| Check | Method | Result |
|---|---|---|
| Stub compile of Phase 01 files | `dotnet build` against a UnityEngine/UnityEditor stub | 0 errors, 0 warnings |
| Behavioural checks | `RunCheck` harness (35 checks) | All 35 pass |
| Unity compile | Unity 6000.1.6f1 `-batchmode`, in a copy of the project | Exit 0. `Assembly-CSharp.dll` built. No `error CS` lines |
| Sample data generation | `-executeMethod Phase01DataGenerator.GenerateSampleData`, in the copy | Exit 0. 11 characters, 19 weapons, 7 armour. 0 sheet values out of range |
| Saved asset check | `-executeMethod Phase01Verify.Run` (a read-only verifier, copy only) | Exit 0. 0 failures. Every stat, HP and skill matches the sheets. All 11 troop lines match (Morale, Discipline, Move, Numbers) |
| Name bug | Verifier showed blank asset names | Fixed in the generator (D-29). Re-verified: all names present |

- The harnesses (`%TEMP%\opencode\phase01-check` and `%TEMP%\opencode\unity-verify`) are **not** in the repository.
- The verifier lives only in the copy. It is not part of the repo.
- **Why a copy:** the open Editor (PID 6880, launched by Unity Hub) holds the project lock. Batch mode on the project
  itself fails with `HandleProjectAlreadyOpenInAnotherInstance`. The copy has `Assets`, `Packages` and `ProjectSettings`,
  but not `Library`, so Unity reimported everything. The Editor was not closed or touched.
- **Not verified:** scene behaviour, and the runtime changes running in Play mode. Those need the Editor.

### Decisions

| ID | Decision | Source | Status |
|---|---|---|---|
| D-01 | The designer's 11 character sheets (Level 1) are the Phase 01 source of truth. | User. | Confirmed |
| D-02 | Each character is one `SoldierProfile` (soldier type) plus one `TroopProfile` whose composition is that soldier type × Numbers. | Spec §1.2, §1.4; matches the sheets. | Confirmed |
| D-03 | Weapon classes add **Mace** (Spiked Club) and **Crossbow** (Mercenary). Crossbow is classed as ranged. | User sheets. Spec §1.6 allows more classes. | Needs spec sign-off (Q-52) |
| D-04 | A soldier carries a list of up to 2 weapons. `MeleeWeapon` is the first melee weapon listed and `RangedWeapon` is the first ranged. | Spec §1.6 "up to 2 weapons". | Working assumption (Q-3, Q-51) |
| D-05 | One `Damage` field on `WeaponProfile`. For melee it is the damage bonus, and for ranged it is the base damage. | Spec §1.6. | Confirmed |
| D-06 | Weapon values: the spec samples are used for **Short Sword**, **Lance** and **Longbow** (not provisional). Other melee weapons are 0 placeholders. Other ranged weapons use the v0.8 bow baseline (damage 5, range 10, accuracy 4, fire rate 5). All placeholders are provisional. "Shortsword" on Gareth's sheet is treated as Short Sword. | Spec §1.6, §4.5. | Provisional (Q-55) |
| D-07 | Armour: 7 assets named as on the sheets. Values come from the nearest spec sample: Light 30/50/20 +4, Heavy 60/30/10 +7, and Chainmail + Shield 60/30/10 +7 (Q-6 note). All provisional. | Spec §1.6 samples. | Provisional (Q-54) |
| D-08 | Defence is 5 (default) for every soldier. The sheets give no Defence. | Spec §1.3; D-22. | Working assumption (Q-2) |
| D-09 | Speed: `MaxSpeed = Speed × 0.6` units/s. Sheet speeds 5–9 give 3.0–5.4 units/s. | Spec §1.3; Q-1. | Working assumption (Q-1) |
| D-10 | Morale: `TroopProfile.BaseMorale` (integer 1–20) is taken from the sheet. The old absolute morale field (10–100) is **not** carried over, so old assets reset to 5. `TroopMorale.Value` is now `BaseMorale × living soldiers`. Morale penalties come in Phase 03. | Spec §5.1; D-22. | Confirmed |
| D-11 | Discipline moves from `SoldierProfile` to `TroopProfile`. `MeleeAttackClock` reads `troop.Discipline` until Phase 04 removes the clock. | Spec §1.4. | Confirmed |
| D-12 | ~~HP: the sheet's HP is `SoldierProfile.MaxHealth`. Sheet values are 14–24 against the spec's average of 18.~~ **Superseded by D-23.** | Spec §1.3. | Superseded |
| D-13 | Legacy transitional fields are kept so the current clash code still compiles: `MeleeSkill` (5 × best melee skill, on the old 0–100 scale) and `MeleeDamage` (Strength + primary melee damage). Both are removed in Phase 04. | Plan 01 vs Plan 04 sequencing. | Transitional |
| D-14 | Sample troops use radius 5 and line width 9 (= 1.8 × radius). The sheets give no radius. Radius 5 matches the existing Swordsmen profile. | Q-12. | Working assumption (Q-12) |
| D-15 | `TroopTuning` holds non-stat values only: speed conversion, action window, max weapons and max skills per soldier. Each later phase adds its own values. Stat limits are in `CombatStatLimits` (D-26). | Plan 01's "all of §7.2", narrowed to avoid duplicate sources of truth. | Decision (updated by D-26) |
| D-16 | Armour validation logs a warning on a split that is not 100. It does not block the save, because Unity cannot reject a save. `ArmourProfile.IsValid` reports the result. | Unity limitation. | Decision |
| D-17 | Composite troops: Move = min Speed, BaseMorale = max, Discipline = max, soldiers = sum (spec §1.2). Radius and line width = max of the parts. | Spec §1.2. | Radius and line width: working assumption (Q-53) |
| D-18 | Old `SoldierProfile._maxSpeed` and soldier-level `_discipline` are removed. Old assets lose those values (Speed defaults to 5). | Plan 01 task list. | Confirmed (migration pending) |
| D-19 | The sample data generator is an Editor menu item (`Troops > Phase 01 > Generate Sample Data`). It updates assets in place with `CopySerialized`, so GUIDs and scene references are kept. It keeps any sprite already assigned to a soldier profile. It creates Troop Tuning only if missing. | Plan 01: "edit through Unity". | Decision |
| D-20 | Sheet "charge bonus" is read as the soldier **Charge** stat (1–20). It is not the weapon's `ChargeBonus`. | Spec §1.5 `Charge`. | Working assumption (Q-56) |
| D-21 | The current `ClashResolver` damage formula is not changed in Phase 01. Its inputs come from the legacy fields in D-13. | Plan 04 owns the clash rewrite. | Decision |
| D-22 | **All combat stats are integers from 1 to 20, default 5.** This covers Speed, Defence, Strength, Support, Charge, Aggression, BaseMorale, Discipline and every weapon skill level. | User, 2026-10-26. | Confirmed |
| D-23 | **HP is an integer from 10 to 40, default 15.** Sheet HP values (14–24) are all in range. | User, 2026-10-26. | Confirmed |
| D-24 | **Discipline is 1–20, default 5** (was 0–20). The minimum of 1 means every troop has at least one possible charge follower. This resolves Q-10 and raises Q-58. | User (1–20 range); Q-10 resolution. | Confirmed (minimum-1 consequence: Q-58) |
| D-25 | Weapon `Damage`, `ChargeBonus`, `Accuracy` and `FireRate` are integers with a minimum of 0 and no maximum. `Range` stays a float (world units). The spec samples are whole numbers. The 1–20 cap applies to soldier stats only. | Spec §1.6 samples. | Working assumption (Q-57) |
| D-26 | `CombatStatLimits` (new, pure C#) is the single source for stat limits: `Min 1`, `Max 20`, `Default 5`, `HpMin 10`, `HpMax 40`, `HpDefault 15`, and clamp helpers. `TroopTuning` lost `BaseStatValue`, `StatMaximum` and `AverageMaxHealth`. Inspector `[Range]` attributes must repeat the literals, because attributes cannot read constants that are not compile-time. | Maintainability. | Decision |
| D-27 | A **listed** weapon skill is never 0. `OnValidate` sets a listed 0 to the default (5) and clamps anything else into 1–20. A class that is not listed is unskilled (0). | D-22; Q-2 (unskilled still open). | Decision; unskilled value is Q-2 |
| D-28 | Stats passed to the profile factories (`CreateCharacterProfile`, `CreateRuntimeProfile`) are clamped into range. The generator also reports sheet values outside range as errors (currently 0). | D-22, D-23. | Decision |
| D-29 | **Generator name bug fixed.** `EditorUtility.CopySerialized` and `AssetDatabase.CreateAsset` left every generated asset with an empty name, so weapon and armour names were blank and `WeaponProfile.DisplayName` fell back to nothing. The generator now sets each asset's name from its file name. | Found by the verifier in the copy. | Fixed; re-verified |

### Notes on the changed runtime files

- `Troop.Move` = lowest soldier `Speed` over all soldiers in the roster (dead ones included, so Move does not change when
  soldiers die). It is an `int`. `Troop.Discipline` = `Profile.Discipline`, or the default 5 when there is no profile.
- `TroopRoster.MinProfileValue` is new and takes an `int` selector. It mirrors the existing `MaxProfileValue`.
- `TroopMorale.Value` = `BaseMorale × living soldiers`, where BaseMorale is an `int`.
- `TroopPivotMover` still uses the old pivot-speed formula (`max MaxSpeed × 0.65`). This changes in Phase 06, per plan.
- `TroopConstants.LineSpacing` is still 0.6. It changes to 0.9 in Phase 06, per plan.
- Runtime HP is still a `float` inside `SoldierAgentState`. The integer rule covers the **stat** (`MaxHealth`). Phase 02
  moves HP to whole points.

---

## Phases 02–10: Health, Morale, Melee, Footprints, Formation, Chains, Input, Ranged, Cleanup

**Status (2026-10-26):** Code complete for every phase. Verified with the Unity CLI runner (`Tools/UnityCli`) in
Unity 6000.1.6f1 batch mode, in a mirrored copy of the project (the open Editor keeps its lock):

| Check | Method | Result |
|---|---|---|
| Compile | `unity-cli compile` | Exit 0. No `error CS` and no Troop warnings. |
| EditMode tests | `unity-cli editmode` (Test Framework, 120 tests) | 120 passed. |
| Charge rate (Plan 04, G2) | `TroopScenarioTests.ChargeRate_Over30Seconds_IsAboutAggression` (stepped battle, 30 s) | 8.95–11.1 charges per soldier per 30 s for Aggression 10 (within the 15 % test tolerance; the pure-rule test over 200 soldiers is within 10 %). |
| Hit rate (Plan 09) | `ChainAndRangedScenarioTests.ArcherTroop_...` | 10 volleys in 30 s, hit rate 0.48 (expected 0.50). |
| Play mode | `unity-cli smoke` (Level 1 Troop Test, 120 s) | See Next Steps 1. |

The original bugs: `Troop.cs` still called the removed `TroopMorale` API (`Value`, `FaultPressure`, `IsPulseLocked`,
`RecordClashLoss`, `ShouldBreak`, `OnBroke`, `LastChargeDistance`) and `TroopDamageReceiver` passed the Shock arguments
in the wrong order (9 compile errors). Both are fixed by the Phase 03 rewrite.

### Decisions

| ID | Decision | Source | Status |
|---|---|---|---|
| D-30 | `FaultPressure`, the morale clash cooldown, `BreakCooldownSeconds` and `ChargeLockAfterBreakSeconds` are **removed**. A troop that is retreating cannot break again until the retreat ends; a routed troop never breaks again. | User plan (Phase 3 "per decisions"); Q-34. | Confirmed (spec §5.7 still lists the two cooldowns as provisional: update it) |
| D-31 | Retreat distance is `RetreatDistanceScale × TroopRadius` (1.0) away from the pivot of the last troop that hit it, or straight back from its facing when nothing hit it. Duration is the existing `FaultSlipMotion` accordion (about 3.1 s). | Q-34. | Working assumption |
| D-32 | The rout raises `Troop.Routed` and the legacy `IUnit.OnUnitDestroyed` once, through a new `IUnit.RaiseUnitDestroyed()` (the only change outside `Troop/`). Despawn after 60 s raises nothing. | Q-36. | Decision |
| D-33 | A fleeing winner (retreating or routed, `UnitState.Fleeing`) deals no damage. | Q-37. | Working assumption |
| D-34 | Charges: each frame the soldier rolls first, then searches for a target (3 nearest enemy soldiers within 7.8 units, contact-index query). Any soldier of an engaged troop may charge (Q-25). A roll with no target in range does nothing. | Spec §4.3; performance (Plan 04 risk). | Decision |
| D-35 | Followers run at the charge speed, keep within `0.6 × SupportRadius` of their charger, share its timeout and return when its charge ends. | Spec §4.4. | Decision |
| D-36 | **Q-11 option (a):** a line longer than the troop is wide widens the soldier clamp to reach its line points. | Q-11 (P1). | Working assumption: confirm |
| D-37 | `S_Distance` exit hysteresis is a tuning value, default **0** (the spec has none). The user's Phase 6 list mentions hysteresis; set `SDistanceExitHysteresis` if flicker appears. | Spec §3.1; Q-16. | Decision |
| D-38 | A line order now **holds the line after arrival** until the next order (the old code dropped it on arrival). | Spec §3.3, §6.2. | Fix |
| D-39 | **Q-19:** a multi-troop line order creates the links explicitly as **pending** links. They reserve the line points but have no geometry until both troops have arrived, then they activate. They are dropped if either troop gets a new order. | Q-19 conflict with Plan 08. | Decision |
| D-40 | `TroopDebugSnapshot` moved to `Diagnostics/TroopDebugSnapshot.cs`. It still needs to be added to git (the folder is untracked). | Architecture §11 #3. | Decision; git add pending |
| D-41 | No `Troop.asmdef`. Troop code depends on `IUnit`, `Army`, `NavPathfinder` in Assembly-CSharp. Tests live in `Troop/Editor/Tests` (Assembly-CSharp-Editor) with `InternalsVisibleTo`. | Plan 10c. | Decision |
| D-42 | Input keeps reading the Input System directly (not `PlayerInputMap`). | Architecture §11 #12. | Decision |
| D-43 | Ranged fire is per troop: every `30 / FireRate` s (±10 %) the troop fires a **volley**, one shot from each ranged soldier, spread over the target's nearest soldiers. Projectile speed 20 units/s (Q-42). | Spec §4.7 "the troop waits one interval"; Plan 09 acceptance (10 shots per 30 s). | Working assumption |
| D-44 | Joint weights: an engaged troop's line point is pinned (weight 1); otherwise the joint is the midpoint. | Spec §3.4 "weighted average", §3.4.1 pinned anchor. | Decision |
| D-45 | Attachment also requires a clear line of sight (the obstruction test that breaks links), so a link cannot re-form through a wall. | Spec §3.4.1, user plan. | Decision |
| D-46 | Domination is computed in double precision with `P_A%` rounded to 1e-6, so boundary rolls (1.5, 2.5) round half up exactly. | Spec §4.5. | Fix |
| D-47 | `com.unity.test-framework` added to `Packages/manifest.json` at **1.5.1** (the built-in version the lock file already resolved; 1.4.5 was only a dependency floor). | Plan 10d; approved by the user. | Done |
| D-48 | `S_Distance` and `A_Distance` moved from `TroopCombatWorld` to `TroopTuning`. The test scene's old overrides (5 and 2) are no longer read. | Plan 10e. | Done |
| D-49 | Friendly pivot separation never pushes an engaged troop; its friend gives way. | Spec §3.4.1 (no tension moves an engaged troop). | Decision |
| D-50 | A troop whose line is bent by a joint faces the bent line's normal on the side of its natural facing. | Spec §3.4. | Decision |
| D-51 | Unity CLI: `Tools/UnityCli/unity-cli.ps1` (compile, editmode, playmode, smoke, all, watch) and `unity-cli.sh` (client for shells that cannot start Windows processes). Results in `.unity-cli/` (gitignored). | User request. | Done |

### Phase status

| Phase | Status |
|---|---|
| 02 Health | Done earlier in this session's starting point; tests added (`HealthAndMitigationTests`). |
| 03 Morale | Done. Tests: Franz, decay, A/B/C cases, rout event and despawn, retreat, surrounded break. |
| 04 Melee | Done. Tests: duel, Domination, support, Combat Power, Lance, charge rate, clash rate limit. |
| 05 Footprints | Done. Tests: gap, faces and boundaries, surround path, flank ×3 Shock, facing lock. |
| 06 Formation | Done. Tests: priority, L_nat 29.7 / 33 per rank, spacing 0.9. |
| 07 Chains | Done. Tests: geometry, attach, tie-break, loops, teams, stealing, obstruction, mid-chain rout, pinned anchor, shared retreat, relaxation. |
| 08 Input | Done. Tests: classification, facing, envelope. Manual checklist pending (Next Steps 2). |
| 09 Ranged | Done. Tests: P(Hit), interval, volleys, no fire while engaged. |
| 10 Cleanup | Done: deprecated code removed (grep is clean), README and Architecture rewritten, test framework added. |

---

## Next Steps

1. **Play mode check.** Restart the CLI runner once (close its window, run `Tools\UnityCli\start-runner.cmd`), then
   run `unity-cli smoke`, or play `Level 1 Troop Test` in the Editor for two minutes and check the Console (gate V6).
2. **Manual checklist (Plan 08)** in `Level 1 Troop Test`: Shift toggle, box select, pursue, move with offsets,
   one-troop and three-troop line drags (joints), chained troop inside/outside the envelope, ghost tether colours, Alt.
3. **Confirm the working assumptions** D-31 (retreat direction and timing), D-36 (Q-11 option a), D-39 (pending links),
   D-43 (ranged volleys). Update `specifications.md` §5.7 and §0 for D-30 (cooldowns removed).
4. **Regenerate the sample assets** from the Editor (Phase 01 step 1 still applies). `SoldierProfile` lost the legacy
   `MeleeSkill`/`MeleeDamage` fields and `TroopProfile` lost `LineWidth`; Unity drops them on the next save.
5. **Git:** add the new files (`Troop/Rules/*`, `Troop/Editor/Tests/*`, `Troop/Diagnostics/TroopDebugSnapshot.cs`,
   `Tools/UnityCli/*`) and remove the deleted ones. Do not commit until you ask.
6. Earlier Phase 01 steps 2, 3, 5–8 (Troop Tuning asset check, sprites, Discipline minimum, Flock profile migration,
   scene profiles, Mace/Crossbow sign-off) are unchanged.
