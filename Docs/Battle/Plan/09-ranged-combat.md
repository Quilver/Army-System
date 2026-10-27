# Plan 09: Ranged Combat

**Goal:** Add ranged troops to the Troop system: a fire controller for stationary, unengaged troops, projectiles, the hit
formula, fire timing with jitter, and damage through the phase 02 pipeline. The Troop system has no ranged combat today.
The only ranged code is in the legacy `Unit/Ranged Combat/` folder, which is not reused.

**Spec sections:** §4.7, §1.6 (ranged weapons), §7.2 (ranged values).
**Depends on:** Phase 01 (weapon data), phase 02 (damage), phase 05 (front arc).
**Blocking questions:** Q-39 (no shooting into melee), Q-40 (line of fire and friendly fire).
Q-5, Q-41 to Q-46 have defaults.


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

- [ ] **`Rules/RangedRules.cs`** (pure):
  - `HitChance(accuracy, skill, shotDC) = (acc + skill) / (acc + skill + shotDC)`. `shotDC` = `BaseShotDC` (10).
  - `FireInterval(fireRate, jitter, rng) = (ActionWindowSeconds / fireRate) × (1 + U(−jitter, +jitter))`.
- [ ] **Fire controller** `Combat/RangedFireController.cs`:
  - Runs per troop. Eligible when the troop has a ranged weapon, is not moving, is not engaged (Q-46 default: no
    soldier engaged), and is not routed or fleeing.
  - Target: nearest enemy troop within `WeaponRange` and in front of the line (front arc, Q-41: within ±45° of the
    troop's facing, measured from the pivot).
  - Not into melee (Q-39): do not target a troop that is engaged. Other interpretations per Q-39.
  - Line of fire and friendly fire (Q-40): decide on a rule, then implement.
  - Shot target: the nearest living enemy soldier in the chosen troop, measured from the shooter's pivot.
  - Interval from `FireInterval`. Store the next-shot time per troop.
- [ ] **Projectile** `Combat/TroopProjectile.cs` (plain C# state, rendered by a view):
  - Start position, target soldier, launch time, flight time (speed provisional).
  - Hit resolved on arrival (Q-42 default): the hit roll happens when the projectile lands.
  - On arrival: roll `HitChance`. On a hit, call `TroopDamageReceiver` with the weapon's `Damage`.
- [ ] **Projectile view** (`View/ProjectileView.cs`): a sprite or line, pooled. Pool the objects so shots do not allocate.
- [ ] **Damage path.** Use phase 02's `MitigationResolver` with the target's `TotalDefence` and armour. No flank
  multiplier (spec §4.7). Strength is not added (Q-5).
- [ ] **Melee loadout.** A ranged troop's soldiers carry a melee weapon (validated in phase 01). In melee they use it.
- [ ] **Hit skill (Q-44).** Use the soldier's skill for the weapon's class (`Bow`, `Javelin`). If the soldier has no skill
  in that class, use the unskilled default from Q-2.
- [ ] **Legacy code.** Do not wire `Unit/Ranged Combat/` into the Troop system. Leave it for the legacy battle system.

## Files

- New: `Rules/RangedRules.cs`, `Combat/RangedFireController.cs`, `Combat/TroopProjectile.cs`, `View/ProjectileView.cs`,
  `Model/ProjectileSet.cs` (pool).
- Changed: `Combat/TroopCombatWorld.cs` (update order: after charges, before morale update), `Runtime/Troop.cs`
  (ranged eligibility), `Editor/TroopModuleEditors.cs` (fire state), `Combat/TroopCombatGizmos.cs` (range and front arc).
- Assets: long bow WeaponProfile (phase 01), a troop profile with a ranged soldier (e.g., Archer) using it.

## Acceptance Criteria

- [ ] EditMode tests (`RangedRulesTests`, fixed seed):
  - `HitChance(2, 6, 10)` = 8/18 ≈ 0.444. Over 10,000 rolls, the hit rate is within 1% of 44.4%.
  - Long bow fire interval: mean 3.0 s, every interval within ±10% of 3.0 s.
- [ ] PlayMode: an archer troop stationary with an enemy 10 units away fires about 10 shots per 30 s.
- [ ] PlayMode: a moving or engaged archer troop does not fire.
- [ ] PlayMode: no shots at a troop that is in melee (Q-39 applied).
- [ ] Projectiles do not allocate per shot (check the pool in the Profiler).
- [ ] Ranged damage uses the mitigation path (armour reduces Bleed).

## Risks

- Projectiles add per-frame work. Pool them and cap the active count.
- The front-arc rule and no-shoot-into-melee rule interact. Test a troop that is flanked by a melee line.
- Q-40 (friendly fire) can make the shots look wrong in a crowd. Check with the Level 1 Archer Flock.
