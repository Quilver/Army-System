# Troop System

Battle units for Leveraged Wars. A **Troop** is the unit the player commands; it is made of **Soldiers**, autonomous
fighters simulated as plain state and steered by the troop's vector field. The rules follow
`Docs/Battle/specifications.md` (v0.9). The class-by-class description is in `Docs/Battle/Architecture.md`; decisions
and open items are in `Docs/Battle/Decisions.md` and `Docs/Battle/Questions.md`.

Troops are the only battle units: Level 1, Level 2 and `Assets/Resources/Levels/Test/Level 1 Troop Test.unity` use them.
The battle-wide events in `Combat/BattleEvents.cs` are how the rest of the game (battle end, achievements, the battle
report, sounds) follows a battle; Troop code never references the campaign.

## How a battle frame runs

`TroopCombatWorld` (one per battle) runs the battle-wide systems; each `Troop` runs its own modules.

1. **Engagement** (`TroopEngagementSystem`): builds every troop's footprint and measures every opposing pair with the
   footprint gap. Enemies within `S_Distance` (30) turn the troop into a battle line; within `A_Distance` (5.2) the
   troops are engaged, facing is locked, and the face each opponent attacks (Front, flanks, Rear) is recorded.
2. **Chains** (`ChainLinker`): joins allied line points into chains, solves the shared joints, breaks over-stretched,
   over-bent or obstructed links, and resolves each troop's formation state (Idle, Column, Line, Chain, Rout).
3. **Pressure**: line-vs-line barrier and push, friendly pivot and outline separation (SU-94), link tension (never
   moves an engaged troop).
4. **Charges** (`ChargeCoordinator`): front-rank soldiers (SU-106) of a troop within `EngagementGap` of an enemy troop
   charge at `Aggression / 45 s` (`ChargeWindowSeconds`, Melee Tuning, SU-112), reaching up to `A_Distance` (SU-95); Discipline-nearest friends follow. Stunned
   soldiers (SU-107) do neither.
5. **Ranged** (`RangedFireController`, `TroopProjectiles`): stationary, unengaged troops fire: Volley weapons in sync,
   others each on its own timing (SU-105).
6. **Troop update** (`Troop.Update`): morale decay and healing, break test, retreat (in melee) or disorder (out of melee,
   SU-104), rout, pivot movement, soldier steering.
7. **Contacts** (`SoldierContactResolver`, `DuelResolver`): bounces, clashes, duels, damage through armour, Shock.

## Folders

| Folder | Contents |
|---|---|
| `Rules/` | Pure maths with no Unity lifecycle: morale, duel, support, damage, mitigation, charge rate, footprints and gaps, faces, surround path, line geometry, chain geometry and topology, ranged rules, formation priority, composite troops, seeded random. Covered by EditMode tests. |
| `Model/` | Profiles (`CharacterProfile`, `SoldierProfile`, `WeaponProfile` (abstract) / `MeleeWeaponProfile` / `RangedWeaponProfile`, `ArmourProfile`), `TroopTuning` (every §7.2 value), character classes (`CharacterClass`, `StatCaps`, `CharacterStat`), runtime state (`SoldierAgentState`, `SoldierHealth`, `SoldierChargeState`, ...). |
| `Runtime/` | `Troop` and its modules: `TroopRoster`, `TroopMorale`, `TroopFormationState`, `TroopLineOrder`, `TroopLineLinks`, `TroopPivotMover`, `FaultSlipMotion` (retreat), `TroopRout`, `AssaultState`, `TroopDamageReceiver`, `MeleeMetrics`. |
| `Combat/` | `TroopCombatWorld`, engagement, charges, contacts, duels, support, ranged fire, projectiles, gizmos. |
| `Formation/` | `ChainLinker`, `TroopFootprintBuilder`. |
| `Pressure/` | Line pressure, friendly overlap, link tension. |
| `Soldiers/` | Steering, field shapes, motion constraints, spatial grid. |
| `Input/` | Selection and hover, order classification and planning, chain order rules, the order overlay. |
| `View/` | Soldier sprites, projectile sprites, troop visualiser, showcase driver, troop cards (`TroopCardHud`: selected troop bottom left, hovered troop bottom right), the order paths of the selected troops and the hovered troop (`TroopOrderPathView`) and the engagement view (`TroopEngagementView`). |
| `Diagnostics/` | `TroopDebugOverlay` (F4 gizmos: footprints, faces, gaps, joints, charges) and `TroopDebugSnapshot`. |
| `Editor/` | Inspectors (morale, state, combat metrics), the Phase 01 sample-data generator, the CLI smoke test, and `Tests/`. |

## Controls (battle scenes)

- **LMB** click or drag (8 px): select player troops. **Shift** toggles.
- **P** (or gamepad Start): pause and resume. No selection or orders while paused.
- **RMB** on an enemy: pursue (Assault on contact). **RMB** on ground: move, keeping offsets; troops arrive Idle.
- **RMB drag** (8 px): battle line. Several troops split the line in order along the drag and are linked into a chain.
  The front faces the nearest enemy, or the right-hand side of the drag.
- Moving one chained troop: inside the envelope (3.0 x L_nat stretch, 65 degree hinge) it stays linked; outside it the
  link breaks on release. The ghost tether shows which (green = stays, dashed red = breaks).
- **Alt** + order: unlink a chained troop, or keep an unlinked reserve from snapping onto a line.
- **Selected** troops, and the troop under the cursor (any team), with a Move, Line or Attack order: the path to the
  waypoint is drawn with arrows (red for an attack, ending at the target, whose footprint is outlined; the ordered
  line's footprint for a line order). Selected troops show it all the time (SU-97).
- **Combat log**: the arrow tab on the left edge opens and hides a log of every clash and shot, with bonuses and damage
  (SU-110). Clashes / Shots toggles filter it.
- **F5** toggles the engagement heat map: a red band across the gap between engaged troops' lines, most intense in the
  middle of the gap and fading smoothly toward each troop's line and the ends of the engagement. It covers only the
  stretch of front within `A_Distance` of the enemy (SU-96). Its settings are on
  `TroopPlayerOrders`.

## Tuning

Every value from spec §7.2 is a field on one of the Battle Tuning assets in `Assets/Resources/Battle Tuning/`, or a
constant in `TroopConstants` when it never changes:

| Asset | Sections |
|---|---|
| Soldier Tuning | Stats and actions, soldier steering |
| Formation Tuning | Distances (S_Distance, A_Distance, EngagementGap), formation and movement, friendly gap |
| Morale Tuning | Morale and retreat, faces |
| Melee Tuning | Charges and clashes, support and duel, Assault |
| Chain Tuning | Chains, link spring |
| Ranged Tuning | Ranged, composite troops |
| Input Tuning | Input thresholds (pixels) |

Code reads them through `TroopTuning.Active` (property names unchanged). A missing asset falls back to the code
defaults; **Troops > Battle Tuning > Create Missing Assets** recreates it. Characters, troops, classes and equipment are
in `Assets/Resources/Troop and Character Profiles/`. `TroopCombatWorld` has no distance fields of its own; the scene
values it used to override (S 5, A 2) are no longer read.

## Tests and the command line

Tests are in `Editor/Tests/` and compile into Assembly-CSharp-Editor (asmdef test assemblies cannot reference
Assembly-CSharp). Rule tests are plain NUnit tests. Scenario tests build a small battle in Edit mode and step it with
`TroopCombatWorld.SimulateFrame` at a fixed frame time and seed, so they are deterministic.

Run them from the Unity Test Runner (EditMode), or from the command line with the runner in `Tools/UnityCli/`:

```
powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 compile
powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 editmode [-TestFilter MoraleRulesTests]
powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 smoke      # plays Level 1 Troop Test for 120 s
powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 all
```

The runner mirrors `Assets`, `Packages` and `ProjectSettings` into `%LOCALAPPDATA%\ArmySystemUnityCli`, so it works while
the Editor has the project open. `Tools\UnityCli\start-runner.cmd` starts it in watch mode, where other tools queue runs
with `Tools/UnityCli/unity-cli.sh run <command>`. Results are written to `.unity-cli/results/` (gitignored).
