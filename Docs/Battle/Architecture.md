# Troop System: Architecture

This document describes how the code under `Assets/Battle Scripts/Troop/` is structured after Phases 01–10 of the plan.
For the rules, see `specifications.md` (v0.9). For decisions and working assumptions, see `Decisions.md` and
`Questions.md`. For the folder overview, controls and how to run the tests, see `Troop/README.md`.

---

## 1. Scope & Dependencies

- **Location:** `Assets/Battle Scripts/Troop/`. It compiles into **Assembly-CSharp** (no `.asmdef`, decision D-41). The
  `Editor/` subfolder (inspectors, data generator, CLI smoke test, tests) compiles into Assembly-CSharp-Editor.
- **Dependencies on the rest of the project:**
  - `IUnit` and `UnitState` (`Unit/`): `Troop` derives from `IUnit`. `IUnit.RaiseUnitDestroyed()` (added for the rout
    event) raises the destroyed events without destroying the object.
  - `Army` (`Controller/Army.cs`) for the army colour; `StatSystem.Refactor.IUnitStatBlock` for the legacy stats field.
  - `MovementSystem.NavPathfinder` (`Pathfinding/`) for marching paths.
- Nothing outside Troop uses it at runtime except through `IUnit`. Battle wiring for Troops is out of scope (Q-49).

## 2. Design Rules

1. **Pure rules.** Maths that does not need the engine lives in `Rules/` as static classes and small structs (Unity's
   `Vector2`/`Mathf` value types are allowed). Every rule there has EditMode tests.
2. **Named tuning.** Every value in spec §7.2 is a field on `TroopTuning` (`Assets/Resources/Boid Profiles/Troop
   Tuning.asset`, code defaults when missing) or a constant in `TroopConstants`. Stat ranges are in `CombatStatLimits`.
3. **Seeded randomness.** All draws use `ITroopRandom`. `TroopCombatWorld` holds the battle seed; each troop derives
   its own stream from its hierarchy path, so update order does not change outcomes.
4. **One engagement metric.** S_Distance, A_Distance, line pressure, the surround test and ranged targeting all use
   footprints and the footprint gap.

## 3. Folder Map

| Folder | Contents |
|---|---|
| `Rules/` | `MoraleRules`, `DuelRules`, `CombatPower`, `SupportRules`, `DamageRules`, `MitigationResolver`, `ChargeRules`, `ClashRules`, `TroopFootprint` (+ `TroopFace`, `FootprintKind`), `FootprintGap`, `FaceClassifier`, `SurroundTest`, `LineGeometry`, `FormationModeResolver`, `ChainGeometry`, `ChainLinkRules` (+ `ChainLinkCandidate`, `ChainBreakReason`), `ChainTopology`, `RangedRules`, `CompositeTroopResolver`, `ITroopRandom`, `SeededTroopRandom`. |
| `Model/` | `TroopProfile`, `SoldierProfile` (+ `SoldierLifeState`), `WeaponProfile`, `ArmourProfile`, `WeaponSkill`, `WeaponTypes`, `CombatStatLimits`, `TroopTuning`, `FormationMode`, `SoldierAgentState`, `SoldierHealth`, `SoldierChargeState` (+ `ChargeRole`), `SoldierFaultMotionState`, `SoldierClashPulseState`, `SoldierSeparationState`, `MitigationResult` (+ `AppliedDamage`), `TroopFormationData`, `TroopEndpointLink`. |
| `Runtime/` | `Troop`, `TroopRoster`, `TroopFormationState`, `TroopMorale`, `TroopLineOrder`, `TroopLineLinks`, `TroopPivotMover`, `TroopPathFollower`, `FaultSlipMotion`, `TroopRout`, `AssaultState`, `EngagementRecord`, `TroopDamageReceiver`, `MeleeMetrics`, `TroopConstants`, `TroopObjects`, `TroopAssemblyInfo`. |
| `Combat/` | `TroopCombatWorld`, `TroopRegistry`, `TroopEngagementSystem`, `SoldierContactIndex`, `SoldierContact`, `SoldierContactResolver`, `DuelResolver`, `SupportCalculator`, `ChargeCoordinator`, `CrossTroopSeparation`, `RangedFireController`, `TroopProjectiles`, `TroopCombatGizmos`. |
| `Formation/` | `ChainLinker`, `TroopFootprintBuilder`, `TroopPivots`. |
| `Pressure/` | `TroopSegmentPressureRules`, `FriendlyOverlapResolver`, `AlliedLinkTension`, `SegmentGeometry`. |
| `Soldiers/` | `SoldierSteering`, `SoldierFieldPlanner` (+ `FieldShape`), `SoldierSpatialGrid`, `SoldierMotionConstraints`. |
| `Input/` | `TroopPlayerOrders`, `TroopSelection`, `TroopOrderPlanner`, `TroopOrderOverlay`, `TroopScreenSpace`, `OrderClassifier` (+ `OrderKind`), `ChainOrderRules`. |
| `View/` | `TroopSoldierViews`, `ProjectileView`, `TroopVisualiser`, `SoldierFacingAnimator`, `ClashPulseView`, `SoldierSpriteLibrary`, `TroopSoldierShowcase`, `TroopProfileSpriteDefaults`. |
| `Diagnostics/` | `TroopDebugOverlay`, `TroopDebugSnapshot` (now in its own file, D-40). |
| `Editor/` | `TroopEditor`, `TroopModuleEditors` (roster, formation, morale), `Phase01DataGenerator`, `TroopCliSmoke`, `Tests/`. |

Removed: `MeleeAttackClock`, `ClashResolver`, `SoldierMeleeState`, `LinkedEndRetreat`, `ThreatFormationRules`,
`AlliedLineConnector`, `LineLinkAssigner`, `LineEndAimer`, `LineSegmentBuilder`, `LineSegment`.

## 4. Composition

`Troop` is an aggregator. `TroopRoster`, `TroopFormationState` and `TroopMorale` are MonoBehaviours (toggle in the
Inspector); the rest are plain classes created in `Troop.EnsureModules`. `Configure(team, profile)` is the setup path.
Spawning captures the initial soldier count for the rout threshold.

## 5. Per-Frame Order

### 5.1 `TroopCombatWorld` (`[DefaultExecutionOrder(-100)]`)

**Update** (`StepBeforeTroops`)
1. `TroopRegistry.Refresh` (0.5 s) and `RemoveDestroyed`.
2. `TroopEngagementSystem.Update`: footprints (`TroopFootprintBuilder`), engagement records per opposing pair (gap,
   face, engaged), Assault on entering engagement, `InMelee`, threat heading from enemies within S_Distance →
   `TroopFormationState.UpdateNatural`.
3. `ChainLinker.Update`: validate links, solve joints and break, attach, publish final lines, ChainSuppressed,
   formation modes.
4. `TroopSegmentPressureRules.Update`: line pressure (footprint gap), `FriendlyOverlapResolver`, `AlliedLinkTension`,
   line-push integration.
5. `SoldierContactIndex.Rebuild`, `CrossTroopSeparation.Apply`.
6. `ChargeCoordinator.Update`: tick and cancel charges and followers, start new charges.
7. `RangedFireController.Tick` for each troop, `TroopProjectiles.Tick`, `ProjectileView.Draw`.

**LateUpdate** (`StepAfterTroops`): rebuild the contact index on post-move positions, then
`SoldierContactResolver.Resolve` (bounces, `ClashRules`, `DuelResolver`), then each troop's radius clamp.

`SimulateFrame(dt)` runs Update, every `Troop.Step(dt)` and LateUpdate in one call. Tests use it to simulate battles
in Edit mode at a fixed frame time.

### 5.2 `Troop.Update` → `Step(dt)`

1. `TroopMorale.Tick` (Shock decay), bruise healing, Assault timer, link-endpoint relaxation.
2. `FaultSlipMotion.TickRecovery` (retreat ends → Idle).
3. `EvaluateBreak`.
4. `FaultSlipMotion.Tick` (retreat motion).
5. `EvaluateBreak` again.
6. Rout tick (flee direction, despawn timer).
7. `TroopPivotMover.Tick` (march, pursue, flee, line push).
8. `SoldierSteering.Simulate`.
9. `TroopSoldierViews.Refresh`.

## 6. Subsystems

### 6.1 Morale (spec §5)

- `TroopMorale`: Shock, `RetreatCount`, `SurroundedBreakCount`, initial soldier count. `EffectiveMorale` =
  BaseMorale × living − 5 × retreats − 20 × surrounded breaks (`MoraleRules`). Shock decays at
  `0.20 × max(0, EM) / 30` per second at all times. No fault pressure, clash cooldown, break cooldown or charge lock.
- `TroopDamageReceiver.Apply` adds the applied Bruise + Bleed (overflow excluded) as Shock, ×3 when flanked, then
  calls `Troop.EvaluateBreak`.
- `Troop.EvaluateBreak`: when Shock > EM, `MoraleRules.Evaluate` picks the case. **A Rout** (EM < 50 % of initial):
  `EnterRout`. **B Retreat**: Shock 0, penalty, `FaultSlipMotion` for `RetreatDistanceScale × Radius` away from the last
  attacker, shared retreat along the chain. **C Surrounded break** (`IsSurrounded`): Shock 0, penalty, no motion.
  A retreating or routed troop does not break again (Q-34).
- `Troop.IsSurrounded`: `SurroundTest.RetreatPath` (box behind the rear face, RetreatDistance long) overlaps an enemy
  footprint, or `Physics2D.OverlapBox` hits the obstacle mask. Friendly troops never block (Q-33).
- Rout (`TroopRout`): final. Breaks every link, cancels orders, raises `Troop.Routed` and `IUnit.OnUnitDestroyed` once,
  flees away from the nearest enemy at Move speed, scatters (`FormationMode.Rout`), despawns after 60 s without raising
  the event again. Orders are ignored.

### 6.2 Melee (spec §4.2–§4.6)

- `ChargeCoordinator`: for every soldier of an engaged, unbroken troop, `p = 1 − exp(−EffectiveAggression / 30 × dt)`.
  On a hit, the target is a random pick among the 3 nearest enemy soldiers within 7.8 units (contact-index query). The
  troop's Discipline nearest soldiers each follow with p = 0.5 (`ChargeRole.Follower`). Cancel on target death,
  troop break, or the 3 s timeout; followers return when their charger's charge ends. No cooldown, no contagion.
- Committed soldiers (chargers and followers) skip separation, density spreading and drive pressure in both directions
  and may leave the radius (`SoldierSteering`, `SoldierMotionConstraints`). They move at ChargeSpeedScale (2×).
- `SoldierContactResolver`: opposing soldiers closer than 0.85 bounce; `ClashRules` decides a clash — a charger in
  contact always clashes (the charge ends), followers never start one, other contacts need both soldiers off their 1 s
  clash cooldown.
- `DuelResolver`: knock-back, labels (A = charger, earlier charger, or lower id), Combat Power
  (`CombatPower` with `SupportCalculator`), roll, `DuelRules` (Domination = round(|R − P_A%| / 10)), damage
  (`DamageRules`: Strength + weapon damage + Domination, ×2 for a charging Lance) through `Troop.ApplyDamage` (armour
  via `MitigationResolver`). A fleeing winner deals no damage (Q-37).
- `SupportCalculator`: same-team, alive, not fatigued soldiers within 3 units (6 for a Boar Spear), capped by the
  clashing soldier's Support, ×2 Combat Power; 0 against a flanking enemy.
- `AssaultState`: a troop with an active Move, Pursue or Line order that becomes engaged gets 10 s of +5 Aggression and
  +5 Charge while charging. Re-engaging during a pursuit refreshes it.
- `MeleeMetrics`: rolling 30 s counts of charges, followers, clashes, duel wins, damage, shots and hits.

### 6.3 Footprints, faces and engagement (spec §3.3, §4.1, §4.8)

- `TroopFootprintBuilder`: Line/Chain → rectangle around the line (half-width = furthest soldier + soldier radius);
  marching → capsule; Idle/Rout → circle.
- `FootprintGap`: separating-axis gap between rounded boxes; negative when they overlap.
- `TroopEngagementSystem` stores an `EngagementRecord` per opponent within S_Distance: gap, `FaceClassifier` face
  (defender pivot → attacker's nearest footprint point; ±45° Front, beyond 135° Rear), engaged (gap ≤ 5.2).
  `Troop.FaceAttackedBy` / `IsFlankedBy` answer clashes and support.
- Facing lock: while engaged, `TroopFormationState` does not change the heading.

### 6.4 Formation state and lines (spec §3.1–§3.3)

- `TroopFormationState.UpdateNatural` builds the natural line through the pivot: the ordered facing and length while a
  line order is active (and the troop holds it, or an enemy is within S), otherwise the smoothed threat heading at
  L_nat = 1.8 × TroopRadius. `ChainLinker` publishes the final line (`Formation`).
- `FormationModeResolver`: Rout > Chain > Line > Column > Idle. `SoldierFieldPlanner.Shape` switches on the mode
  (line capsule with joint clearance, column lanes, blob of 0.85 × radius, wide rout scatter).
- Line spacing 0.9 (`TroopConstants.LineSpacing`), ranks from `LineGeometry`.
- Pivot speed = Move × 0.6 (`TroopPivotMover.PivotSpeed`), Move = slowest soldier Speed.
- `TroopLineOrder` holds the line after arrival until another order (it used to drop it on arrival).
- `FriendlyOverlapResolver`: pivots only, 0.25 × (r_A + r_B), engaged troops are not pushed.
- Stretched lines: `SoldierMotionConstraints.EffectiveRadius` widens the clamp to reach the line points (Q-11 a, D-36).

### 6.5 Chains (spec §3.4, §3.4.1)

- `TroopLineLinks`: two line points (0 = right wing, 1 = left wing), each a `TroopEndpointLink` (other troop, other
  end, tension, joint, pending), plus relaxation offsets.
- `ChainLinker` each frame:
  - **Validate:** links need a reciprocal, the same team, no rout, and both troops in Line. Pending links (created by a
    multi-troop line order) activate when both troops have arrived; they drop if either gets a new order.
  - **Joints:** J = weighted average of the two natural line points (an engaged side is pinned, weight 1). Break on
    required length > 3.0 × L_nat, inner hinge angle < 65°, or terrain between the pivots through J or at J. Then J is
    pushed out so neither line is shorter than 0.7 × L_nat.
  - **Attach:** free line points of different wings, facing the same way, within 0.25 × L_nat, clear line of sight, not
    ChainSuppressed, not closing a loop (`ChainTopology`), sorted by (distance, lower instance id). Linked points are
    never candidates (no stealing).
  - **Relax:** a broken stretched line point keeps its offset and shrinks it at 2 units/s.
  - **Shared retreat** (`OnTroopRetreat`): unengaged neighbours whose link would stretch past 1.4 × L_nat or bend past
    30° retreat 70 % of the previous hop (`FaultSlipMotion` sympathetic, no morale cost).
  - **ChainSuppressed:** cleared when the order completes or after 3 s outside every attach radius.
- `AlliedLinkTension`: spring on the gap between natural line points, applied only to unengaged troops (both shares
  to the free side when one is engaged); velocity matching only when both are free.

### 6.6 Input (spec §6)

- `TroopPlayerOrders` reads the Input System directly (decision kept, D-42). Thresholds are screen pixels from
  `TroopTuning` (box select 8 px, order drag 8 px, click pick 24 px).
- `TroopSelection`: hover, click (footprint, pivot or soldier within the pick radius), box (pivot or any soldier),
  Shift toggles.
- `OrderClassifier`: Pursue / Move / Line, and the line facing (nearest enemy side, else the drag's right-hand normal).
- `TroopOrderPlanner`: plans placements (shared by the ghost preview), applies chain rules (`ChainOrderRules`
  envelope; outside or Alt → unlink + ChainSuppressed; Alt on reserves → ChainSuppressed), issues orders, and creates
  pending links between adjacent troops of a multi-troop line.
- `TroopOrderOverlay`: help, selection box, ghost footprints, facing arrows, joints, ghost tether.

### 6.7 Ranged (spec §4.7)

- `RangedFireController` (per troop): fires while stationary, unengaged, not retreating or routed. Target: nearest
  enemy troop whose footprint is within weapon range and inside ±45° of the facing, and that is not engaged (Q-39).
  Each volley every ranged soldier shoots one of the target's nearest soldiers; the next volley follows after
  `30 / FireRate × (1 ± 0.1)`.
- `TroopProjectiles`: pooled projectiles at 20 units/s; the hit is rolled on landing with
  `(Accuracy + skill) / (Accuracy + skill + 10)`; damage is the weapon damage through armour, no flank Shock.

## 7. Data Model

| Asset / type | Notes |
|---|---|
| `TroopProfile` | `BaseMorale`, `Discipline`, composition, `TroopRadius`. `LineWidth` removed (line length comes from L_nat and orders). Derived `Move`, `SoldierCount`. `CreateComposite` uses `CompositeTroopResolver`. |
| `SoldierProfile` | Integer stats, `MaxHealth`, up to 2 weapons and skills, armour, steering. Legacy `MeleeSkill`/`MeleeDamage` removed; derived `MeleeWeaponSkill`, `MeleeWeaponDamage`, `MeleeWeaponChargeBonus`, `HasBoarSpear`, `HasLance`, `MaxSpeed`. |
| `WeaponProfile`, `ArmourProfile` | As Phase 01. |
| `TroopTuning` | All §7.2 values: stats/actions, distances, formation, morale, faces, charges, support, Assault, chains, ranged, input. |
| `SoldierAgentState` | Life, `SoldierHealth`, motion, `SoldierChargeState` (role, target or leader, timeout, start time), clash cooldown, fault motion, pulse, separation. |

## 8. Tests

`Editor/Tests/` (EditMode, Assembly-CSharp-Editor; `InternalsVisibleTo` in `TroopAssemblyInfo`): 120 tests.

| File | Covers |
|---|---|
| `MoraleRulesTests` | Franz example, penalties, decay, break cases. |
| `MeleeRulesTests` | Duel and Domination rounding, labels, support, fatigue, Combat Power, damage and Lance, charge rate statistics, clash rules and rate limit. |
| `HealthAndMitigationTests` | Mitigation split and overflow, Deflect share, HP pool, bruise healing, fatigue thresholds, composite resolver. |
| `GeometryTests` | Footprint gap, line points, faces and boundaries, surround path, formation priority, line geometry. |
| `ChainTests` | Hinge angle, envelope, stretch/hinge breaks, joint, attach rules, tie-break, topology, shared retreat falloff, order envelope. |
| `InputAndRangedTests` | Order classification, box select, line facing, P(Hit), fire interval. |
| `ScenarioTests` | Rout event and despawn, retreat, surrounded break, flank Shock, facing lock, 30 s charge rate, mid-chain rout, acyclic and same-team links, pinned anchor. |
| `ChainAndRangedScenarioTests` | No link stealing, obstruction, shared retreat 10 → 7 → 4.9, archer volleys and hit rate, no fire while engaged. |

`Editor/TroopCliSmoke.cs` plays a scene in real Play mode from the command line and fails on any logged error.

## 9. Tuning & Constants

| Place | Values |
|---|---|
| `TroopConstants` | `NeighborRadius 2.5`, `SoldierRadius 0.3`, `SeparationRadius 0.9`, `LineSpacing 0.9`, `SpreadRadius 1.2`, `PressureMassPerDrive 3`, `ContactDistance 0.85`, `LineGap 0.3`, `MaxChainLinks 2`, wing indices. |
| `TroopTuning` | Every other §7.2 value (see the asset's Inspector headers). |
| Private constants that are not spec values | `FaultSlipMotion` timings, `TroopFormationState.HeadingSmoothingTime`, `TroopSegmentPressureRules` barrier gains, steering gains, knock-back magnitudes (kept from the earlier code, provisional). |

## 10. Scenes

- `Level 1 Troop Test.unity`: 7 troops, `TroopCombatWorld`, `TroopPlayerOrders`, `TroopDebugOverlay`, plus legacy
  battle objects. The scene is binary-serialized; its old `_sDistance 5` / `_aDistance 2` overrides are no longer read
  because the distances moved to `TroopTuning`.
- `Flock Boid Showcase.unity`: `TroopSoldierShowcase` spawns three troops and orders lines.

## 11. Known Issues and Limits

| # | Issue |
|---|---|
| 1 | The scenario tests step the battle in Edit mode. `EnterPlayMode` inside EditMode tests does not run in batch mode, and asmdef PlayMode tests cannot reference Assembly-CSharp. The CLI `smoke` command covers real Play mode. |
| 2 | Pathfinding is not exercised by the tests (no NavMesh in the test setup). |
| 3 | Physical obstruction tests rely on the `Terrain` layer existing (it does in this project). |
| 4 | The manual checklist for §6 (Plan 08) still needs a person in the Editor. |
| 5 | `Troop._legacyStats` stays: `IUnit.Stats` is abstract. |

## 12. Where to Change Things

| To change | Start here |
|---|---|
| A tuning value | `Troop Tuning.asset` / `Model/TroopTuning.cs` |
| Morale, breaks, rout | `Rules/MoraleRules.cs`, `Runtime/TroopMorale.cs`, `Runtime/Troop.cs` (`EvaluateBreak`, `EnterRout`) |
| Charges and followers | `Combat/ChargeCoordinator.cs`, `Model/SoldierChargeState.cs` |
| Clash, duel, support, damage | `Rules/ClashRules.cs`, `Combat/DuelResolver.cs`, `Combat/SupportCalculator.cs`, `Rules/DuelRules.cs` |
| Footprints, faces, engagement | `Formation/TroopFootprintBuilder.cs`, `Rules/FootprintGap.cs`, `Rules/FaceClassifier.cs`, `Combat/TroopEngagementSystem.cs` |
| Line shape and formation state | `Runtime/TroopFormationState.cs`, `Soldiers/SoldierFieldPlanner.cs`, `Rules/FormationModeResolver.cs` |
| Chains | `Formation/ChainLinker.cs`, `Rules/ChainGeometry.cs`, `Rules/ChainLinkRules.cs`, `Pressure/AlliedLinkTension.cs` |
| Orders and selection | `Input/TroopOrderPlanner.cs`, `Input/TroopSelection.cs`, `Input/TroopPlayerOrders.cs` |
| Ranged | `Combat/RangedFireController.cs`, `Combat/TroopProjectiles.cs`, `Rules/RangedRules.cs` |
