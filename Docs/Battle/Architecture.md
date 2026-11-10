# Troop System: Architecture

This document describes how the code under `Assets/Scripts/Battle/Troop/` is structured (updated 2026-11-08, SU-135).
For the rules, see `specifications.md` (v0.10). For the change log and assumptions, see `SpecificationUpdate.md` and
`AssumptionsReview.md`; for open questions, `Questions.md`. For the folder overview, controls and how to run the tests,
see `Troop/README.md`.

---

## 1. Scope & Dependencies

- **Location:** `Assets/Scripts/Battle/Troop/`. It compiles into **Assembly-CSharp** (no `.asmdef`, decision D-41). The
  `Editor/` subfolder (inspectors, data generator, CLI smoke test, tests) compiles into Assembly-CSharp-Editor.
- **Dependencies on the rest of the project** (all in `Assets/Scripts/Battle/`):
  - `Army` (`Controller/Army.cs`) for the army colour and `Troop.Army`.
  - `MovementSystem.NavPathfinder` (`Pathfinding/`) for marching paths.
- **What uses it:** `Battle`, `Army`, `CapturePoint` and the battle-end conditions (`Battle End/`), and through
  `BattleEvents` (`Combat/BattleEvents.cs`: soldier kills, troop defeats, morale breaks with their cause, SU-114) the Tween
  system: achievements, the battle report and battle flow, and roster deployment (`Assets/Scripts/Tween/`). Troop code never references Tween or Campaign code (SU-57, SU-58).
- **Character classes:** `CharacterClass`, `StatCaps` and `CharacterStat` sit in `Model/` because `CharacterProfile`
  holds a class. `CampaignCharacter` and `StatGrowths` are campaign data in `Assets/Scripts/Tween/Characters/`.

## 2. Design Rules

1. **Pure rules.** Maths that does not need the engine lives in `Rules/` as static classes and small structs (Unity's
   `Vector2`/`Mathf` value types are allowed). Every rule there has EditMode tests.
2. **Named tuning.** Every value in spec §8 (Tunable Variables) is a field on one of the Battle Tuning section assets in
   `Assets/Resources/Battle Tuning/` (Soldier, Formation, Morale, Melee, Chain, Ranged, Input; code defaults when an
   asset is missing), read through `TroopTuning.Active`, or a constant in `TroopConstants`. Stat ranges are in
   `CombatStatLimits`.
3. **Seeded randomness.** All draws use `ITroopRandom`. `TroopCombatWorld` holds the battle seed; each troop derives
   its own stream from its hierarchy path, so update order does not change outcomes.
4. **One engagement metric.** S_Distance, A_Distance, line pressure, the surround test and ranged targeting all use
   footprints and the footprint gap.

## 3. Folder Map

| Folder | Contents |
|---|---|
| `Rules/` | `MoraleRules`, `DuelRules`, `CombatPower`, `SupportRules`, `DamageRules`, `MitigationResolver`, `ChargeRules`, `ClashRules`, `TroopFootprint` (+ `TroopFace`, `FootprintKind`), `FootprintGap`, `FaceClassifier`, `SurroundTest`, `LineGeometry` (line, radius and column sizes), `TroopFrontageRule`, `TroopOutline`, `TroopOutlinePolygon`, `FormationModeResolver`, `ChainGeometry`, `ChainLinkRules` (+ `ChainLinkCandidate`, `ChainBreakReason`), `ChainTopology`, `RangedRules`, `EngagementHeatStrip`, `CompositeTroopResolver`, `ITroopRandom`, `SeededTroopRandom`. |
| `Model/` | `CharacterProfile` (+ `CreateWithGains`, `Source`), `CharacterEquipment`, `SoldierStats`, `CharacterClass`, `StatCaps`, `CharacterStat`, `WeaponProfile` (abstract) with `MeleeWeaponProfile` and `RangedWeaponProfile` (D-52), `WeaponSpecialRuleEntry`, `ArmourProfile`, `WeaponSkill`, `WeaponTypes`, `CombatStatLimits`, `TroopTuning` and the seven `Battle Tuning/` sections, `FormationMode`, `SoldierAgentState`, `SoldierHealth`, `SoldierChargeState` (+ `ChargeRole`), `SoldierFaultMotionState`, `SoldierClashPulseState`, `SoldierSeparationState`, `MitigationResult` (+ `AppliedDamage`), `TroopFormationData`, `TroopEndpointLink`. |
| `Runtime/` | `Troop`, `TroopState`, `TroopOrderKind`, `TroopRoster`, `TroopFormationState`, `TroopMorale`, `TroopLineOrder`, `TroopLineLinks`, `TroopPivotMover`, `TroopPathFollower`, `BattlefieldBounds`, `FaultSlipMotion`, `TroopRout`, `RoutFleePlanner`, `AssaultState`, `EngagementRecord`, `TroopDamageReceiver`, `MeleeMetrics`, `TroopConstants`, `TroopObjects`, `TroopAssemblyInfo`. |
| `Combat/` | `TroopCombatWorld`, `TroopRegistry`, `TroopEngagementSystem`, `EngagementFrontage`, `SoldierContactIndex`, `SoldierContact`, `SoldierContactResolver`, `DuelResolver`, `SupportCalculator`, `ChargeCoordinator`, `CrossTroopSeparation`, `RangedFireController`, `TroopProjectiles`, `CombatLog`, `BattleEvents`, `TroopCombatGizmos`. |
| `Formation/` | `ChainLinker`, `TroopFootprintBuilder`, `TroopPivots`. |
| `Pressure/` | `TroopSegmentPressureRules`, `FriendlyOverlapResolver`, `AlliedLinkTension`, `SegmentGeometry`. |
| `Soldiers/` | `SoldierSteering`, `SoldierFieldPlanner` (+ `FieldShape`), `SoldierSpatialGrid`, `SoldierMotionConstraints`. |
| `Input/` | `TroopPlayerOrders`, `TroopSelection`, `TroopOrderPlanner`, `TroopOrderOverlay`, `TroopScreenSpace`, `OrderClassifier` (+ `OrderKind`), `ChainOrderRules`. |
| `View/` | `TroopSoldierViews`, `SoldierView`, `ProjectileView`, `TroopVisualiser`, `SoldierFacingAnimator`, `ClashPulseView`, `SoldierSpriteLibrary`, `TroopSoldierShowcase`, `TroopCardHud`, `BattleSpeedHud`, `CombatLogHud`, `SoldierHealthBar`, `TroopRangeMask`, `TroopOrderPathView`, `TroopEngagementView`. |
| `Diagnostics/` | `TroopDebugOverlay`, `TroopDebugSnapshot` (now in its own file, D-40). |
| `Editor/` | `TroopEditor`, `TroopModuleEditors` (roster, formation, morale), `SoldierEditors`, `BattleTuningAssets`, `Phase01DataGenerator`, `WeaponProfileMigration`, `TroopCliSmoke`, `Tests/`. |

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
4. `TroopSegmentPressureRules.Update`: line pressure (footprint gap), `FriendlyOverlapResolver` (pivots, then
   outlines, SU-94), `AlliedLinkTension`, line-push integration.
5. `SoldierContactIndex.Rebuild`, `CrossTroopSeparation.Apply`.
6. `ChargeCoordinator.Update`: tick and cancel charges and followers, start new charges.
7. `RangedFireController.Tick` for each troop, `TroopProjectiles.Tick`, `ProjectileView.Draw`.

**LateUpdate** (`StepAfterTroops`): rebuild the contact index on post-move positions, then
`SoldierContactResolver.Resolve` (bounces, `ClashRules`, `DuelResolver`), then each troop's radius clamp.

`SimulateFrame(dt)` runs Update, every `Troop.Step(dt)` and LateUpdate in one call. Tests use it to simulate battles
in Edit mode at a fixed frame time.

### 5.2 `Troop.Update` → `Step(dt)`

1. `TroopMorale.Tick` (Shock decay), bruise healing, Assault timer, link-endpoint relaxation, disorder timer.
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
- `TroopDamageReceiver.Apply` adds every applied point of Bruise and Bleed (overflow included, SU-128; nothing past
  0 HP) as Shock, ×3 when flanked, then calls `Troop.EvaluateBreak`.
- `Troop.EvaluateBreak`: when Shock > EM, `MoraleRules.Evaluate` picks the case. **A Rout** (EM < 50 % of initial):
  `EnterRout`. **B Retreat** (only in melee, SU-104): Shock 0, penalty, `FaultSlipMotion` for `RetreatDistance` away
  from the last attacker (rear first and fastest: wave delay 1.0 s, move 0.8 s at the rear to 1.8 s at the front,
  SU-133), shared retreat along the chain; its stunned soldiers die (SU-107). **B out of melee: Disorder**
  (SU-104): Shock 0, penalty, orders cancelled and refused, no movement for `DisorderSeconds` (10 s).
  **C Surrounded break** (`IsSurrounded`): Shock 0, penalty, no motion. A retreating or routed troop does not break
  again (Q-34).
- `Troop.IsSurrounded`: a living enemy soldier (not charging, following or rushing back; routed troops excluded)
  stands in `SurroundTest.RetreatPath` (box behind the rear face, RetreatDistance long), or `Physics2D.OverlapBox` hits
  the obstacle mask (SU-122). Friendly troops never block.
- Rout (`TroopRout`): final. Breaks every link, cancels orders, raises `Troop.Routed`, `Troop.Defeated` and `BattleEvents.TroopDefeated` once,
  flees at Move speed along a NavMesh path to a goal on the battlefield edge chosen by `RoutFleePlanner` (SU-121;
  straight away from the nearest enemy without a NavMesh), scatters (`FormationMode.Rout`). It leaves the field
  (`Troop.LeaveField`, `HasLeftField`) when its pivot reaches the battlefield edge (`TroopRout.IsAtFieldEdge`, SU-138) or
  after 60 s, without raising the event again. Orders are ignored.

### 6.2 Melee (spec §4.2–§4.6)

- `ChargeCoordinator`: for every soldier of an unbroken troop within `EngagementGap` (+ `ChargeGapTolerance`) of an
  enemy troop (SU-95), `p = 1 − exp(−EffectiveAggression / ChargeWindowSeconds × dt)` (45 s, Melee Tuning, SU-112). On a hit, the target is a random pick among the
  3 nearest soldiers of those enemy troops within `TroopTuning.ChargeRange` (A_Distance between soldier edges, so
  A_Distance + 2 soldier radii centre to centre; contact-index query). The
  troop's Discipline nearest soldiers each follow with p = 0.5 (`ChargeRole.Follower`). Cancel on target death,
  troop break, or the 3 s timeout; followers return when their charger's charge ends. No cooldown, no contagion.
  Only the front rank starts charges (SU-106, `CollectFrontRank`: one rank's worth of the soldiers furthest toward each
  chargeable enemy troop). Stunned soldiers neither charge nor follow (SU-107).
- Stunned soldiers (SU-107, `SoldierLifeState.Stunned`): 0 HP with Bruise >= Bleed stuns instead of killing; alive,
  cannot charge or support, heal from 0 HP and recover at `StunRecoverHp` (5); killed when the troop retreats or routs
  from melee (`Troop.KillStunnedSoldiers`).
- Combat log (SU-110): `DuelResolver` and `TroopProjectiles` record every clash and landed shot in `CombatLog`
  (Combat Power parts, chance and roll, raw damage and the armour roll); `CombatLogHud` shows it.
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
- `TroopPivotMover.UpdatePursuit`: a Pursue ends when the target is wiped out or has left the field, not when it routs
  (SU-139); the pursuer's retreat, disorder or rout cancels it through `Troop`.
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
  L_nat = Frontage × LineSpacing (SU-103). `ChainLinker` publishes the final line (`Formation`). The troop's size in units
  (`Troop.Radius`: blob, soldier clamp, pivot spacing) is derived: L_nat / 1.8, or more when its soldiers need room,
  times `TroopSizeScale` (1.5, SU-131). Frontage comes from the soldier count (`TroopFrontageRule`, SU-117).
- `FormationModeResolver`: Rout > Chain > Line > Column > Idle. `SoldierFieldPlanner.Shape` switches on the mode
  (line capsule with joint clearance, a column longer than wide with at most 3 lanes (SU-120), blob of 0.85 × radius,
  wide rout scatter).
- Line spacing 0.9 (`TroopConstants.LineSpacing`), ranks from `LineGeometry`.
- Pivot speed = Move × 0.6 (`TroopPivotMover.PivotSpeed`), Move = slowest soldier Speed.
- `TroopLineOrder` holds the line after arrival until another order (it used to drop it on arrival).
- `FriendlyOverlapResolver`: pivots at least 0.25 × (r_A + r_B) apart, engaged troops are not pushed; and (SU-94)
  outlines at least `FriendlyGap` apart for friends not linked in a chain, with right of way committed > marching >
  standing, the troop giving way moving at most `FriendlyGiveWaySpeedScale` × its march speed.
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
  - **ChainSuppressed:** cleared only after 3 s in a row outside every attach radius; finishing an order does not clear
    it (SU-130).
- `AlliedLinkTension`: spring on the gap between natural line points, applied only to unengaged troops (both shares
  to the free side when one is engaged); velocity matching only when both are free.

### 6.6 Input (spec §6)

- `TroopPlayerOrders` reads the Input System directly (decision kept, D-42). Thresholds are screen pixels from
  `TroopTuning` (box select 8 px, order drag 8 px, click pick 24 px).
- `TroopSelection`: hover, click (soldier within the pick radius, then outline, then pivot), box (pivot or any
  soldier), Shift toggles; `SelectTroop` (a left press on a troop: that troop, or its whole chain on a double click,
  `ChainMembers`) (SU-129).
- `TroopPlayerOrders`: a left press on one of the player's troops selects it at once; dragging from it and releasing
  moves the selection (`TroopOrderPlanner.FinishMove`, or `FinishPlacementMove` while deploying) (SU-129).
- `OrderClassifier`: right button: Pursue / Move / Line (a drag with one troop is a Move), and the line facing (nearest
  enemy side, else the drag's right-hand normal).
- `TroopOrderPlanner`: plans placements (shared by the ghost preview), applies chain rules (`ChainOrderRules`
  envelope; outside or Alt → unlink + ChainSuppressed; Alt on reserves → ChainSuppressed), issues orders, and creates
  pending links between adjacent troops of a multi-troop line.
- `TroopOrderOverlay`: selection box, ghost footprints and move markers, joints, ghost tether (no help panel, SU-18;
  no facing arrow, SU-33).

### 6.7 Ranged (spec §4.7)

- `RangedFireController` (per troop): fires while stationary, unengaged, not retreating or routed. Reach is per shooter
  (`RangedRules.CanReach`: weapon range and ±45° of the troop's facing, measured from the shooter, D-54). Target troop:
  the enemy troop not engaged in melee with the soldier nearest to a shooter that can reach it (Q-39). Each shot goes to
  the reachable enemy soldier with the fewest of the troop's projectiles aimed at it, then the nearest. Soldiers whose
  weapon has Volley (SU-105) fire together every `30 / FireRate × (1 ± 0.1)`; the others (the default) each shoot on
  their own clock at the same interval, first shot after a random part of it. Fire & Engage volleys (SU-90) fire on
  entering an engagement and on a Case B retreat from it. Direct-fire weapons (`ArcedShot` off) need line of sight
  (`CanSee`: no collider on `SightBlockingLayers` and no third troop's soldier within a soldier radius of the line,
  `RangedRules.LineBlockedBySoldiers`, SU-136); arced weapons ignore obstructions.
- `TroopProjectiles`: pooled projectiles at 20 units/s; the hit is rolled on landing with
  `(Accuracy + skill) / (Accuracy + skill + 10)`; damage is the weapon damage through armour, no flank Shock.

## 7. Data Model

| Asset / type | Notes |
|---|---|
| `Troop` (characters) | `TroopProfile` removed (SU-118): a troop holds its list of `CharacterProfile`s (leader first) and resolves `BaseMorale`, `Discipline` and soldier count with `CompositeTroopResolver`. `Frontage` (soldiers wide in line, SU-103) is set when its soldiers spawn at the start of the battle, from its total soldier count (`TroopFrontageRule`, SU-117), unless `Configure` is given one. `BattleReport.DeployedTroops` is a list of `DeployedTroop` character lists. |
| `CharacterProfile` | Name, class (`CharacterClass`: stat caps), portrait, soldier sprite, Numbers, Morale, Discipline, `SoldierStats` (integer stats, `MaxHealth`, up to 2 weapon skills) and `CharacterEquipment` (one required melee weapon, an optional ranged weapon, armour). Derived: `MeleeWeaponSkill`, `MeleeWeaponDamage`, `MeleeWeaponChargeBonus`, `HasSpearSupport`, `HasCouchedLance`, `MaxSpeed`. `CampaignCharacter` (Tween) adds growths. `CreateWithGains` makes the runtime copy a roster character fights as (level-up gains added, `Source` = the asset, SU-132). |
| `MeleeWeaponProfile`, `RangedWeaponProfile` | Separate ScriptableObjects on an abstract `WeaponProfile` (D-52): Damage and a list of special rules; melee adds class and ChargeBonus, ranged adds class, Range, Accuracy, FireRate and Volley. No Provisional flag (SU-134). |
| `ArmourProfile` | Deflect : Bruise : Bleed ratio and DefenceBonus (SU-89). |
| `TroopTuning` | Loads the seven Battle Tuning sections (Soldier, Formation, Morale, Melee, Chain, Ranged, Input) and exposes every value of spec §8 under its old property name. |
| `SoldierAgentState` | Life, `SoldierHealth`, motion, `SoldierChargeState` (role, target or leader, timeout, start time), clash cooldown, fault motion, pulse, separation. |

## 8. Tests

`Editor/Tests/` (EditMode, Assembly-CSharp-Editor; `InternalsVisibleTo` in `TroopAssemblyInfo`). The whole EditMode
run (Troop and Tween tests) is about 350 tests; the main files:

| File | Covers |
|---|---|
| `MoraleRulesTests` | Franz example, penalties, decay, break cases. |
| `MeleeRulesTests` | Duel and Domination rounding, labels, support, fatigue, Combat Power, damage and Lance, charge rate statistics, clash rules and rate limit. |
| `HealthAndMitigationTests` | Mitigation split and overflow, Deflect share, HP pool, bruise healing, fatigue thresholds, composite resolver. |
| `GeometryTests` | Footprint gap, line points, faces and boundaries, surround path, formation priority, line geometry. |
| `ChainTests` | Hinge angle, envelope, stretch/hinge breaks, joint, attach rules, tie-break, topology, shared retreat falloff, order envelope. |
| `InputAndRangedTests` | Order classification, box select, line facing, P(Hit), fire interval. |
| `ScenarioTests` | Rout event and despawn, retreat, surrounded break, flank Shock, facing lock, 30 s charge rate, mid-chain rout, acyclic and same-team links, pinned anchor. |
| `ChainAndRangedScenarioTests` | No link stealing, obstruction, shared retreat 10 → 7 → 4.9, archer volleys and single shots, hit rate, no fire while engaged, Fire & Engage, composite ranks. |
| `ChainEngagementScenarioTests` | Engagement hold, committed troops, auto-link facing, charge gate and reach, friendly outlines, placement, ChainSuppressed (SU-130), click and double-click selection (SU-129). |
| `GeometryTests`, `ColumnGeometryTests`, `TroopFrontageRuleTests`, `TroopOutlineTests`, `SurroundAndFootprintTests` | Troop size (SU-131), columns, frontage, outlines, footprints and the surround test. |
| `PursuitAndTerrainTests`, `RoutFleeTests` | Pathing, map edge, pursuit facing, rout flight. |
| `MeleeDurationTests` | Spearmen melee length (explicit lever sweeps run by name). |
| `BattleViewTests`, `SoldierViewTests`, `TroopCardHudTests`, `WeaponSpecialRuleTests` | Views, cards, weapon rules. |
| `Tween/Editor/Tests` | `AchievementTests` (levels, growth, gains in battle, SU-132), `CampaignCharacterTests`. |

`Editor/TroopCliSmoke.cs` plays a scene in real Play mode from the command line and fails on any logged error.

## 9. Tuning & Constants

| Place | Values |
|---|---|
| `TroopConstants` | `NeighborRadius 2.5`, `SoldierRadius 0.3`, `SeparationRadius 0.9`, `LineSpacing 0.9`, `SpreadRadius 1.2`, `PressureMassPerDrive 3`, `ContactDistance 0.85`, `MaxChainLinks 2`, wing indices. |
| `TroopTuning` | Every tunable variable of spec §8, from the seven Battle Tuning assets (code defaults when a field has not been saved yet). |
| Private constants that are not spec values | `FaultSlipMotion` settle time and pivot slip time, `SoldierSteering` retreat headroom (speed 1.5×, acceleration 6×, SU-133) and gains, `TroopFormationState.HeadingSmoothingTime`, `TroopSegmentPressureRules` barrier gains, knock-back 0.3 units. |

## 10. Scenes

- Missions (SU-119): `Assets/Missions/M01 - Level 1/Battle/M01 Battle.unity` and `M02 - Level 2/Battle/M02 Battle.unity`
  are the real battles, listed in `Resources/MissionCatalog.asset`. Enemy troops are placed in the scene; the player's
  troops are spawned by the `DeploymentZone` from `BattleReport.DeployedTroops`: the level's defaults on its own
  DataManager, or the roster deployed in the pre-battle tent (SU-84, SU-86), each roster character with its level-up
  gains (SU-132). Each has `TroopCombatWorld`, `TroopPlayerOrders`, the enemy AI and the battle sounds.
- `Level 1 Troop Test.unity`: Level 1 with seven placed troops (no roster needed), `TroopDebugOverlay` and the CLI smoke
  test scene. Not in Build Settings.
- All scenes are text-serialized (Force Text).

## 11. Known Issues and Limits

| # | Issue |
|---|---|
| 1 | The scenario tests step the battle in Edit mode. `EnterPlayMode` inside EditMode tests does not run in batch mode, and asmdef PlayMode tests cannot reference Assembly-CSharp. The CLI `smoke` command covers real Play mode. |
| 2 | Pathfinding is not exercised by the tests (no NavMesh in the test setup). |
| 3 | Physical obstruction tests rely on the `Terrain` layer existing (it does in this project). |
| 4 | The player controls (spec §6: selection, left-drag move, double click, line drags, chain envelope, ghost tether, Alt) are only checked by hand in the Editor beyond the selection and classification tests. |
| 5 | The enemy AI is the Basic tier (SU-67, `Assets/Scripts/Battle/AI/README.md`); it reaches troops only through `AI/Adapters/`. The Advanced tier (Mission 3) is still to come. `FallBackFormation` can route through the enemy, as Skirmish did before SU-124. |

## 12. Where to Change Things

| To change | Start here |
|---|---|
| A tuning value | `Resources/Battle Tuning/*.asset` / `Model/Battle Tuning/*Tuning.cs` (read through `Model/TroopTuning.cs`) |
| Hover order path, engagement view | `View/TroopOrderPathView.cs`, `View/TroopEngagementView.cs`, `Combat/EngagementFrontage.cs`, `Rules/EngagementHeatStrip.cs` |
| Morale, breaks, rout | `Rules/MoraleRules.cs`, `Runtime/TroopMorale.cs`, `Runtime/Troop.cs` (`EvaluateBreak`, `EnterRout`) |
| Charges and followers | `Combat/ChargeCoordinator.cs`, `Model/SoldierChargeState.cs` |
| Clash, duel, support, damage | `Rules/ClashRules.cs`, `Combat/DuelResolver.cs`, `Combat/SupportCalculator.cs`, `Rules/DuelRules.cs` |
| Footprints, faces, engagement | `Formation/TroopFootprintBuilder.cs`, `Rules/FootprintGap.cs`, `Rules/FaceClassifier.cs`, `Combat/TroopEngagementSystem.cs` |
| Line shape and formation state | `Runtime/TroopFormationState.cs`, `Soldiers/SoldierFieldPlanner.cs`, `Rules/FormationModeResolver.cs` |
| Chains | `Formation/ChainLinker.cs`, `Rules/ChainGeometry.cs`, `Rules/ChainLinkRules.cs`, `Pressure/AlliedLinkTension.cs` |
| Orders and selection | `Input/TroopOrderPlanner.cs`, `Input/TroopSelection.cs`, `Input/TroopPlayerOrders.cs` |
| Ranged | `Combat/RangedFireController.cs`, `Combat/TroopProjectiles.cs`, `Rules/RangedRules.cs` |
| Combat log | `Combat/CombatLog.cs` (records and text), `View/CombatLogHud.cs` (panel) |
