# Plan 02: Health, Mitigation & Damage Pipeline

**Goal:** Replace the single HP value with a combined pool split into Bleed and Bruise, add Deflect/Bruise/Bleed mitigation
against armour, Fatigue and death, and introduce the seeded random source used by all later phases.

**Spec sections:** §4.6, §1.5, §7.1 (Fatigue, Bruise).
**Depends on:** Phase 01.
**Blocks:** Phases 03, 04, 09.

## Tasks

- [ ] **`ITroopRandom`** interface in `Rules/` with `float Value()` and `float Range(float min, float max)`. Implement
  `SeededTroopRandom` (System.Random with a seed). Add a `TroopRandom` holder on `TroopCombatWorld` that is seeded from a
  serialized field (default: random seed per battle). Replace `UnityEngine.Random` in:
  - `Combat/ChargeCoordinator.cs`
  - `Combat/ClashResolver.cs`
  - `Combat/SoldierContactResolver.cs`
  - `Combat/MeleeAttackClock.cs` (until phase 04 removes it)
- [ ] **`SoldierHealth`** (plain, `[Serializable]`) in `Model/`:
  - Fields: `float bleed`, `float bruise` (points, not timers), `int maxHp`.
  - `CurrentHP = maxHp − bleed − bruise`.
  - `IsFatigued` when `CurrentHP ≤ maxHp × FatigueFraction`.
  - `IsDead` when `CurrentHP ≤ 0`.
  - `float ApplyBleed(float points)` and `float ApplyBruise(float points)`. Both return the points actually applied,
    clamped to the HP that remains. Overflow is not counted.
- [ ] **`MitigationResolver`** in `Rules/` (pure):
  - Input: `damage` (int points after domination, see phase 04), `TotalDefence`, `ArmourProfile`, `ITroopRandom`.
  - Per point up to `TotalDefence`: roll once. Deflect (0 HP, 0 Shock), Bruise, or Bleed.
  - Above `TotalDefence`: remaining points become Bleed.
  - Output struct: `deflected`, `bruised`, `bleeded`, `overflowBleed` (not counted for Shock).
- [ ] **Whole points.** Damage is a whole number of points before mitigation. Domination is rounded to the nearest whole
  number (spec §4.5), so the sum is an integer.
- [ ] **`TotalDefence`** = soldier `Defence` + armour `DefenceBonus`. Put this in `SoldierProfile` or a helper, not in the
  roster.
- [ ] **Bruise healing.** A per-troop tick (in `Troop.Update`, before the pivot moves) reduces each living soldier's bruise
  by `BruiseHealPerSecond × dt` (1 HP/s). This is deterministic and does not use the random source.
- [ ] **Replace `SoldierAgentState.ApplyDamage`** with the new pipeline. Keep `LifeState` for the views:
  - `Active`, `Fatigued` (renamed from `Injured`), `Dead`.
  - Update `SoldierProfile.cs` enum and every reader (`TroopDamageReceiver`, `TroopSoldierViews`, `TroopEditor`).
- [ ] **`TroopDamageReceiver.Apply`**:
  - Takes a mitigation result, not a raw damage number.
  - Returns the Shock contribution (`bruised + bleeded`, excluding overflow).
  - Raises `SoldierDied` when a soldier dies. When the last soldier dies, call `KillUnit` as today.
  - Flank multiplier is passed in by the caller (phase 05). Use 1 until phase 05.
- [ ] **Shock hand-off.** Add shock to `TroopMorale` through `AddShock(points)`. Phase 03 rewrites the morale rules.
- [ ] **`Fatigued` gating.** Expose `SoldierAgentState.CanSupport` (alive and not fatigued). Phase 04 uses it.

## Files

- New: `Rules/ITroopRandom.cs`, `Rules/SeededTroopRandom.cs`, `Rules/MitigationResolver.cs`, `Model/SoldierHealth.cs`.
- Changed: `Model/SoldierAgentState.cs`, `Model/SoldierProfile.cs` (enum rename), `Runtime/TroopDamageReceiver.cs`,
  `Runtime/Troop.cs` (bruise tick, `ApplyDamage` overloads), `Combat/ChargeCoordinator.cs`, `Combat/ClashResolver.cs`,
  `Combat/SoldierContactResolver.cs`, `View/TroopSoldierViews.cs`, `Editor/TroopEditor.cs`.

## Acceptance Criteria

- [ ] EditMode tests (`MitigationResolverTests`):
  - Damage 5 vs `TotalDefence` 8: all 5 points roll; no overflow.
  - Damage 12 vs `TotalDefence` 8: 8 points roll; 4 points become Bleed (overflow).
  - Heavy armour (60/30/10) with a fixed seed: the Deflect share over 10,000 rolls is within 1% of 60%.
- [ ] EditMode tests (`SoldierHealthTests`):
  - Bruise heals 1 HP per second. After 3 s, bruise drops by 3 points.
  - Bleed is permanent.
  - Fatigue threshold is `MaxHP × 0.5`. For MaxHP 15 (the default) that is 7.5, so a soldier at 7 HP is fatigued and at 8 HP
    is not. Test this for MaxHP 10 and 40 as well (thresholds 5 and 20).
  - Overflow damage on a dying soldier is not returned as applied.
- [ ] Shock returned from `Apply` equals applied Bruise + Bleed, with overflow excluded.
- [ ] No remaining `UnityEngine.Random` calls in `Troop/` (grep is empty, except `TroopSoldierShowcase` if kept).

## Risks

- Bruise healing per second is a per-soldier tick. Check the cost with 200 soldiers.
- A seeded random source changes battle outcomes. Expect a one-off balance shift.
