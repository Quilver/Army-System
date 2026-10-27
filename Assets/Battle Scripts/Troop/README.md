# Troop System

Battle units for Leveraged Wars. A **Troop** is the unit the player commands; it is made of **Soldiers**, autonomous
fighters simulated as plain state and steered by the troop's vector field. The rules follow
`Docs/Battle/specifications.md` (v0.9). The class-by-class description is in `Docs/Battle/Architecture.md`; decisions
and open items are in `Docs/Battle/Decisions.md` and `Docs/Battle/Questions.md`.

Troops are used by two test scenes only (`Assets/Resources/Levels/Test/Level 1 Troop Test.unity` and
`Flock Boid Showcase.unity`). Real battles still run on the legacy `Unit` system (Q-49).

## How a battle frame runs

`TroopCombatWorld` (one per battle) runs the battle-wide systems; each `Troop` runs its own modules.

1. **Engagement** (`TroopEngagementSystem`): builds every troop's footprint and measures every opposing pair with the
   footprint gap. Enemies within `S_Distance` (30) turn the troop into a battle line; within `A_Distance` (5.2) the
   troops are engaged, facing is locked, and the face each opponent attacks (Front, flanks, Rear) is recorded.
2. **Chains** (`ChainLinker`): joins allied line points into chains, solves the shared joints, breaks over-stretched,
   over-bent or obstructed links, and resolves each troop's formation state (Idle, Column, Line, Chain, Rout).
3. **Pressure**: line-vs-line barrier and push, friendly pivot separation, link tension (never moves an engaged troop).
4. **Charges** (`ChargeCoordinator`): engaged soldiers charge at `Aggression / 30 s`; Discipline-nearest friends follow.
5. **Ranged** (`RangedFireController`, `TroopProjectiles`): stationary, unengaged troops fire volleys.
6. **Troop update** (`Troop.Update`): morale decay and healing, break test, retreat, rout, pivot movement, soldier steering.
7. **Contacts** (`SoldierContactResolver`, `DuelResolver`): bounces, clashes, duels, damage through armour, Shock.

## Folders

| Folder | Contents |
|---|---|
| `Rules/` | Pure maths with no Unity lifecycle: morale, duel, support, damage, mitigation, charge rate, footprints and gaps, faces, surround path, line geometry, chain geometry and topology, ranged rules, formation priority, composite troops, seeded random. Covered by EditMode tests. |
| `Model/` | Profiles (`TroopProfile`, `SoldierProfile`, `WeaponProfile`, `ArmourProfile`), `TroopTuning` (every §7.2 value), runtime state (`SoldierAgentState`, `SoldierHealth`, `SoldierChargeState`, ...). |
| `Runtime/` | `Troop` and its modules: `TroopRoster`, `TroopMorale`, `TroopFormationState`, `TroopLineOrder`, `TroopLineLinks`, `TroopPivotMover`, `FaultSlipMotion` (retreat), `TroopRout`, `AssaultState`, `TroopDamageReceiver`, `MeleeMetrics`. |
| `Combat/` | `TroopCombatWorld`, engagement, charges, contacts, duels, support, ranged fire, projectiles, gizmos. |
| `Formation/` | `ChainLinker`, `TroopFootprintBuilder`. |
| `Pressure/` | Line pressure, friendly overlap, link tension. |
| `Soldiers/` | Steering, field shapes, motion constraints, spatial grid. |
| `Input/` | Selection, order classification and planning, chain order rules, the order overlay. |
| `View/` | Soldier sprites, projectile sprites, troop visualiser, showcase driver. |
| `Diagnostics/` | `TroopDebugOverlay` (F3 overlay, F4 gizmos: footprints, faces, gaps, joints, charges) and `TroopDebugSnapshot`. |
| `Editor/` | Inspectors (morale, state, combat metrics), the Phase 01 sample-data generator, the CLI smoke test, and `Tests/`. |

## Controls (Level 1 Troop Test)

- **LMB** click or drag (8 px): select player troops. **Shift** toggles.
- **RMB** on an enemy: pursue (Assault on contact). **RMB** on ground: move, keeping offsets; troops arrive Idle.
- **RMB drag** (8 px): battle line. Several troops split the line in order along the drag and are linked into a chain.
  The front faces the nearest enemy, or the right-hand side of the drag.
- Moving one chained troop: inside the envelope (3.0 x L_nat stretch, 65 degree hinge) it stays linked; outside it the
  link breaks on release. The ghost tether shows which (green = stays, dashed red = breaks).
- **Alt** + order: unlink a chained troop, or keep an unlinked reserve from snapping onto a line.

## Tuning

Every value from spec §7.2 is a field on `Assets/Resources/Boid Profiles/Troop Tuning.asset` (`TroopTuning`), or a
constant in `TroopConstants` when it never changes. `TroopCombatWorld` has no distance fields of its own; the scene
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
