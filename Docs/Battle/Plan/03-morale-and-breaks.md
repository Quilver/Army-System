# Plan 03: Morale, Shock, Retreat & Rout

**Goal:** Replace `TroopMorale`'s fault pressure and cooldown model with the v0.9 rules: Effective Morale, continuous Shock
decay, break cases A/B/C, a retreat blocked by terrain or troops, final rout, and the rout event.

**Spec sections:** §5, §5.7 (cooldowns), §3.3 (surround).
**Depends on:** Phase 02 (Shock input).
**Blocks:** Phase 04 (morale affects charges and clash), phase 07 (rout unlinks chains).
**Blocking questions:** Q-34 (retreat mechanics), Q-33 (surround test), Q-35 (rout direction), Q-36 (destroyed event).


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

- [ ] **Rewrite `Runtime/TroopMorale.cs`:**
  - Remove `FaultPressure`, `RecordClashLoss`, `ClashCooldown`, `DecayTime`, `_faultPressure`.
  - Add `RetreatCount`, `SurroundedBreakCount`, `Shock` (float).
  - `BaseMorale` from `TroopProfile.BaseMorale` (1–20). Add `InitialSoldierCount` captured at configure time.
- [ ] **`EffectiveMorale` formula** (pure, in `Rules/MoraleRules.cs`):
  `BaseMorale × CurrentSoldierCount − RetreatCount × 5 − SurroundedBreakCount × 20`.
  Expose `InitialEffectiveMorale = BaseMorale × InitialSoldierCount`.
- [ ] **Shock decay** (continuous, in `TroopMorale.Tick`):
  `Shock = max(0, Shock − 0.20 × max(0, EffectiveMorale) / 30 × dt)`. Shock is not clamped above.
- [ ] **Shock accumulation.** `AddShock(points, face)` adds `points × (flank ? 3 : 1)`. The `points` value is already the
  applied Bruise + Bleed count from phase 02.
- [ ] **Break test** `EvaluateBreak()`. Called every frame and after every shock change or soldier death:
  - If `Shock ≤ EffectiveMorale`: no break.
  - Else if `EffectiveMorale < 0.5 × InitialEffectiveMorale`: **Rout** (case A).
  - Else if `Surrounded`: **Surrounded break** (case C). No retreat. Shock = 0. `SurroundedBreakCount++`.
  - Else: **Retreat** (case B). Shock = 0. `RetreatCount++`. Start `FaultSlipMotion`.
- [ ] **Surround test** `SurroundedCheck` (Q-33). Build a rectangle `RetreatDistance` long behind the rear face. Return
  true if it intersects impassable terrain (`CircleCast` or `OverlapBox`), or an enemy footprint. Friendly footprints
  count only if Q-33 says so. Use `RetreatDistance = 1.0 × TroopRadius` (provisional, Q-34).
- [ ] **Retreat.** Reuse `FaultSlipMotion`. Replace the distance formula (`chargeDist × 1.8`) with `RetreatDistance`.
  Duration and re-break behaviour follow Q-34.
- [ ] **Cooldowns (Q-34).** Keep `BreakCooldownSeconds = 1.6` and `ChargeLockAfterBreakSeconds = 1.2` only if confirmed.
  The charge lock is read by phase 04.
- [ ] **Rout** (`Troop.EnterRout()`):
  - Set `State = Fleeing` and a new `RoutState` flag. Cancel orders and line orders.
  - Unlink from any chain now (call `LineLinks.ClearAll` and notify neighbours). Phase 07 supplies the neighbour call.
  - Flee direction (Q-35). Default: away from the nearest enemy.
  - Raise the destroyed event once (Q-36). Check what `IUnit.KillUnit` does, and whether it destroys the GameObject.
    If it does, add a separate `RaiseRouted` path that fires `OnUnitDestroyed` without destroying the object.
  - Start a 60 s despawn timer (`RoutDespawnSeconds`). Destroy the GameObject when it expires.
  - Rout is final: `MoveTo`, `Pursue` and `OrderLine` return early for a routed troop.
- [ ] **Remove the old break gates:** the `IsPulseLocked` property and `PulseLockDuration` (replaced by
  `ChargeLockAfterBreakSeconds`) and the old `BreakCooldown` check in `ShouldBreak`.
- [ ] **Shock on soldier death.** When a soldier dies, the morale count drops (automatic through `CurrentSoldierCount`).
  Run `EvaluateBreak()` after the death.
- [ ] **Update `Troop.Update` order** (spec §5 and Architecture §5.2):
  1. Morale tick (decay).
  2. Retreat recovery.
  3. `EvaluateBreak`.
  4. Retreat motion.
  5. `EvaluateBreak` again.
  6. Pivot movement.
  7. Steering.
  8. Views.
- [ ] **Update `Editor/TroopEditor.cs` and `TroopMoraleEditor`:** show EffectiveMorale, Initial, Shock, Retreat count,
  Surrounded count, and the case that would apply if the troop broke now.

## Files

- New: `Rules/MoraleRules.cs`, `Rules/SurroundTest.cs`.
- Changed: `Runtime/TroopMorale.cs`, `Runtime/Troop.cs` (`TryBeginFaultSlip`, `EnterRout`, `Update`),
  `Runtime/FaultSlipMotion.cs` (retreat distance and duration), `Runtime/TroopDamageReceiver.cs`,
  `Combat/ClashResolver.cs` (remove `RecordClashLoss`), `Editor/TroopModuleEditors.cs`.

## Acceptance Criteria

- [ ] EditMode tests (`MoraleRulesTests`):
  - Franz (`BaseMorale 5`, initial 12): initial EM = 60. After 2 deaths, EM = 50.
  - Shock decay: 50 EM gives 0.333 Shock/s. After 30 s with no hits, Shock drops by 10.
  - Break cases:
    - EM 25, initial 60 (under 50%): rout.
    - EM 40, not surrounded: retreat, EM becomes 35, shock 0.
    - EM 40, surrounded: no retreat, EM becomes 20, shock 0.
- [ ] A routed troop raises the destroyed event exactly once. It is not destroyed until 60 s pass.
- [ ] `Level 1 Troop Test`: a troop with a flank attack breaks faster than one attacked from the front.
- [ ] A troop cannot start a second retreat while the first is still running (or Q-34 behaviour, once answered).
- [ ] No `FaultPressure` references remain (grep).

## Risks

- Removing fault pressure means breaks depend only on Shock. Expect troops to break sooner or later than before. Re-check
  the break tests after phase 04 changes damage.
- Rout event wiring could double-count if `KillUnit` also raises the event. Test both paths.
