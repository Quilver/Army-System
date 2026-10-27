# Plan 04: Melee: Charges, Support, Clash & Duel

**Goal:** Replace banked attacks, the strike beat, Lunge and the pressure-score clash with the v0.9 model: stochastic charges,
passive charge followers, clash on contact, a probability duel with Domination, support and flank penalties, and Assault.

**Spec sections:** §4.1–§4.5, §4.8 (support and flank penalties), §4.2 (Assault).
**Depends on:** Phases 02 and 03.
**Blocks:** Phase 08 (pursue order), phase 09 (ranged needs melee state).
**Blocking questions:** Q-26 (clash rate), Q-7 (Boar Spear), Q-9 (follower pool), Q-24 (pursue end),
Q-27 (charge timeout), Q-28 (duel labels). Q-25, Q-29, Q-30, Q-4, Q-8 have defaults.


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

### 4a. Remove the old system

- [ ] Delete `Combat/MeleeAttackClock.cs` and its call in `TroopCombatWorld.LateUpdate`.
- [ ] Strip `Model/SoldierMeleeState.cs` to the engaged flag only. Remove `ReadyAttacks`, `HeldAttacks`, `Progress`,
  `TrySpendAttack`, `BeginLunge`, `IsLunging`.
- [ ] Remove `TroopConstants.LungeDuration`, `MeleeBeatInterval`, `MaxPendingAttacks`, `MaxDiscipline`, `PerMeleeTime`
  (replaced by `ActionWindowSeconds` in `TroopTuning`).
- [ ] Remove `Troop.ReadyMeleeAttacks`, `HeldMeleeAttacks`, and the attack gizmos (white and black spheres) in
  `TroopCombatGizmos`.
- [ ] `MeleeMetrics`: keep the window. Replace attack counting with charges started, clashes, and duel wins.
- [ ] Remove the neighbour charge model (`LocalChargeFraction`, Ising constants) in `ChargeCoordinator` (Q-30).

### 4b. Charges

- [ ] **Rewrite `Combat/ChargeCoordinator.cs`:**
  - Per soldier, per frame: `p = 1 − exp(−rate × dt)`, with `rate = EffectiveAggression / ActionWindowSeconds` and
    `EffectiveAggression = Aggression + AssaultAggressionBonus (if Assault)`.
  - Eligibility: the soldier's troop is engaged (footprint gap ≤ A_Distance) and not routed, and an enemy soldier is
    within `ChargeRangeScale × A_Distance` (7.8, Q-25). Not in `ChargeLockAfterBreak`.
  - Target: random pick among the `ChargeCandidateCount = 3` nearest enemy soldiers.
  - Use a spatial query (`SoldierContactIndex` or the troop grid), not a loop over every contact. This fixes the
    O(n²) issue in Architecture §11 item 5.
- [ ] **Charge state** (`SoldierChargeState`): add `Phase` (`Approach`, `Contact`, `Returning`), `StartTime` (for the
  "first charge" rule in Q-28), `Timeout`.
- [ ] **Cancel rules:** target dies, the charger's troop breaks, or `ChargeTimeoutSeconds` (3 s) expires (Q-27).
  Retarget or cancel on target death per Q-27.
- [ ] **No cooldown after a charge** (Q-29). Remove `ChargeCooldownRemaining` if unused.
- [ ] **Speed.** Charger uses `ChargeSpeedScale × max speed` and acceleration (existing steering hook).

### 4c. Followers (passive support)

- [ ] When a soldier starts a charge, select the troop's `Discipline` nearest **same-troop** living soldiers (Q-9).
  Each follows with `ChargeFollowProbability = 0.50`.
- [ ] Followers get a `Follower` role (`SoldierChargeState.Role`). They move to stay within `SupportRadius` of the charger.
- [ ] Followers bypass separation, density spreading and drive pressure, the same as chargers (`IsCommitted` in
  `SoldierAgentState`). Update `SoldierSteering` to check it.
- [ ] Followers never start clashes. Exclude them from the contact resolver's clash initiation.
- [ ] After the charger's clash ends, followers return to the line.

### 4d. Clash

- [ ] **Contact.** Opposing soldiers closer than `ContactDistance` (0.85) are in contact.
- [ ] **Trigger.** A clash happens when:
  - (a) a charger reaches contact, or
  - (b) two opposing soldiers are in contact and neither is charging. Rate-limited by `ClashCooldownSeconds` (1.0) per
    soldier (Q-26).
- [ ] **Knock-back** is kept (`ClashResolver` impulses). Keep the magnitudes for now.
- [ ] **Remove** the pressure score (`ClashPressure`) and the `TryPickInitiator` attack logic.
- [ ] **Remove** the facing damage multiplier (`FlankMultiplier`, Q-38).

### 4e. Pure rules

- [ ] `Rules/CombatPower.cs`: `WeaponSkill + ChargeBonus (if charging) + SupportBonus`.
  `ChargeBonus = Charge + weapon ChargeBonus + AssaultChargeBonus (if Assault)`. Both charging → both get it (Q-4).
- [ ] `Rules/SupportRules.cs`:
  - Eligible = same army, `CanSupport` (alive, not fatigued), within `SupportRadius` (3). A Boar Spear supporter counts
    within 6 (Q-7).
  - `SupportingSoldiers = min(eligible, Support)`. `SupportBonus = SupportingSoldiers × 2`.
  - Clash against a flanking enemy → 0 (phase 05 supplies the face).
- [ ] `Rules/DuelRules.cs`:
  - `P_A = CP_A / (CP_A + CP_B)`. Roll `R ∈ [0, 100]`. A wins if `R ≤ P_A × 100`.
  - A is the charger. If both charge, the earlier charge is A. If neither, the lower soldier ID is A (Q-28).
  - `Domination = round(|R − P_A × 100| / 10)` (rounded to nearest whole number).
- [ ] `Rules/DamageRules.cs`:
  - `Raw = Strength + WeaponDamageBonus + Domination`. Lance × 2 while charging (Q-8).
  - Hand the result to `MitigationResolver` (phase 02).

### 4f. Assault

- [ ] Add `AssaultState` on `Troop`: `TimeRemaining` (10 s).
- [ ] **Trigger:** a troop with an active order (`Move`, `Pursue`, `Line`) that becomes engaged. Set the timer.
- [ ] **Refresh:** a pursuing troop that re-engages after the enemy retreated refreshes the timer.
- [ ] **Effects:** `+5` Aggression (in `EffectiveAggression`) and `+5` Charge while charging (in `ChargeBonus`).
- [ ] Assault does not apply to troops without an order (even when engaged).

### 4g. Pursue

- [ ] Pursue ends per Q-24. Until answered, use: target dies or the order is replaced.

## Files

- New: `Rules/CombatPower.cs`, `Rules/SupportRules.cs`, `Rules/DuelRules.cs`, `Rules/DamageRules.cs`, `Runtime/AssaultState.cs`.
- Changed: `Combat/ChargeCoordinator.cs`, `Combat/SoldierContactResolver.cs`, `Combat/ClashResolver.cs`,
  `Model/SoldierChargeState.cs`, `Model/SoldierMeleeState.cs`, `Model/SoldierAgentState.cs`, `Soldiers/SoldierSteering.cs`,
  `Runtime/Troop.cs`, `Runtime/TroopMorale.cs` (charge lock read), `Runtime/MeleeMetrics.cs`, `Combat/TroopCombatGizmos.cs`,
  `Runtime/TroopConstants.cs`, `Combat/TroopCombatWorld.cs`.
- Deleted: `Combat/MeleeAttackClock.cs`.

## Acceptance Criteria

- [ ] EditMode tests:
  - `DuelRulesTests`: P = 60%, roll 40 → charger wins, Domination 2. Roll 70 → defender wins, Domination `|70 − 60| / 10 = 1`.
    Rounding to nearest whole number is tested at 1.5 and 2.5.
  - `SupportRulesTests`: 6 eligible, `Support = 5` → 5 counted, bonus 10. Fatigued soldiers excluded. Flank → 0.
  - `CombatPowerTests`: both charging → both have the charge bonus.
  - `DamageRulesTests`: Lance doubles total damage only while charging.
- [ ] PlayMode: 200 engaged soldiers, 30 s → charges per soldier ≈ Aggression ± 10% (measured via `MeleeMetrics`).
- [ ] PlayMode: in a 1-soldier vs 1-soldier contact with no charge, clashes ≤ 1 per second (rate limit works).
- [ ] No `ReadyAttacks`, `BeginLunge`, `MeleeBeat` or `MaxDiscipline` references remain (grep).
- [ ] Charge candidate search does not loop over all contacts (profile with 400 soldiers, no frame over 5 ms for charges).

## Risks

- Removing banked attacks changes clash frequency. Tune `ClashCooldownSeconds` against the phase 03 break tests.
- Follower bypass of separation can cause overlapping soldiers. Check the crowd after a charge in the Level 1 scene.
- Q-27 (retarget or cancel) changes the charge flow. Implement after the answer.
