# Troop System

Runtime code for battle troops: each `Troop` is a block of soldiers that moves, forms battle lines, fights in melee and
breaks under pressure. Soldiers are plain C# state objects simulated by the troop and drawn as sprites by `View/`.

## Architecture

- `Troop` (MonoBehaviour) is an aggregator. It owns identity (team, profile, army colour) and composes the modules
  that do the work.
- `TroopCombatWorld` (one per battle, `DefaultExecutionOrder(-100)`) runs the battle-wide systems in a fixed order:
  formation sensing, line linking, line pressure, soldier contact, clashes and charges. It holds no rules itself.
- Data (`Model/`) is separate from behaviour. Profiles are ScriptableObjects; runtime soldiers are serializable
  `SoldierAgentState` objects.

## Folders

| Folder | Contents |
|---|---|
| `Runtime/` | `Troop` aggregator, its modules (roster, formation, morale, movement, damage) and `TroopConstants`. |
| `Model/` | `TroopProfile`, `SoldierProfile`, `SoldierAgentState` and its sub-states, `TroopFormationData`, `TroopEndpointLink`. |
| `Combat/` | `TroopCombatWorld`, soldier contact and clashes, melee attack timing, charges, gizmos and registry. |
| `Formation/` | Threat sensing, battle-line construction, linking and hinging of allied lines. |
| `Pressure/` | Line-vs-line forces, barrier, friendly overlap and allied link tension. |
| `Soldiers/` | Per-soldier steering, target planning, spatial grid and terrain constraints. |
| `Input/` | Player selection, orders and order overlay. |
| `View/` | Sprite views, facing, clash pulse and sprite libraries. |
| `Editor/` | Custom inspectors showing live state in Play mode. |
| `Diagnostics/` | `TroopDiagnostics`: opt-in CSV logging, overlay and gizmos. Off by default. `TroopBenchmarkHarness` and `TroopBenchmarkCases`: visual benchmark layouts with pause, step, slow motion and an inspection panel. The harness auto-starts in `Level 1 Troop Test`. |

## Per-frame update order

**`TroopCombatWorld.Update`** (runs first):
1. `TroopRegistry.Refresh()` (throttled to 0.5 s) and `RemoveDestroyed()`.
2. `ThreatFormationRules.Update`: engagements and threat headings.
3. `AlliedLineConnector.Connect`: links, aims and snaps lines.
4. `TroopSegmentPressureRules.Update`: line pressure, barrier, friendly overlap and allied tension.
5. `SoldierContactIndex.Rebuild`.
6. `CrossTroopSeparation.Apply`.
7. `ChargeCoordinator.Update`: starts, keeps or cancels charges.

**`Troop.Update`** (once per troop):
1. `TroopMorale.Tick`: fault pressure decays. Shock does not; it accumulates damage until the troop breaks.
2. `FaultSlipMotion.TickRecovery`: returns the troop to Idle when a retreat finishes.
3. `TryBeginFaultSlip()`: breaks the troop if shock exceeds morale.
4. `FaultSlipMotion.Tick()`: retreat motion.
5. `TryBeginFaultSlip()` again, in case the step changed shock.
6. `TroopPivotMover.Tick()`: marching, pursuit or line-push drift.
7. `SoldierSteering.Simulate()`: target planning, flocking, terrain and radius constraints.
8. `TroopSoldierViews.Refresh()`: sprite positions, facing and pulse.

**`TroopCombatWorld.LateUpdate`** (after all troop updates):
1. `TroopRegistry.Refresh()` and `SoldierContactIndex.Rebuild` on post-move positions.
2. `SoldierContactResolver.Resolve`: bounces overlapping soldiers, marks engaged soldiers, starts clashes and resolves
   them through `ClashResolver`, then constrains soldiers to their troop radius.
3. `MeleeAttackClock.Tick`: accrues and releases melee attacks (see below).
4. `SoldierContactResolver.ApplyMeleeStates`: sets `Troop.InMelee`.

## Melee combat

Terms used in the metrics:
- **Attack**: a soldier spends a banked melee attack to *start a clash* with an enemy in contact. The attacking troop
  is credited with it. The attacking soldier also lunges at its opponent at full speed (not doubled) for up to 1.5 s.
- **Hit**: a soldier *wins* a clash. The winning troop is credited with it, whether or not it started the clash.

A clash between an opposing pair of soldiers in contact is started only by an attack:
- Soldiers in contact with an enemy are knocked apart (bounce) whether or not an attack is available. Only the
  attack starts the clash.
- If a pair is in contact and neither soldier has a banked attack, no clash starts. If both have one, the initiator is
  picked at random. The initiator spends its attack.
- There is no per-pair cooldown. A pair in contact can clash again as soon as one of its soldiers has a new attack.
- Both soldiers are knocked back, and each pressure score (forward speed, charge, aggression, melee skill and nearby
  support, plus noise) is compared. The higher score wins and the loser records a clash loss in morale. A clash the
  loser's troop is still recovering from is not won.
- The winner records a hit and deals damage, scaled by melee skill and by flank angle (4x, 2x or 1x).
- A troop's hit rate is hits / attacks. It can exceed 100% because a troop can win clashes that another soldier started.

### Attack rate: Aggression

`SoldierProfile.Aggression` (1-20, default 5) is the **expected number of melee attacks a soldier makes per
`TroopConstants.PerMeleeTime` seconds (30 s) of engagement**.

- While its troop is in melee, each soldier's attack progress grows at `Aggression / PerMeleeTime` per second. Each
  whole unit of progress becomes one attack.
- Attacks are not banked without limit. At most `MaxPendingAttacks` (4) can be pending at once, so attacks that are
  never used in a clash are lost. In practice, sustained attack rates are below Aggression when a soldier has few
  clashes.

### Synchronisation: Discipline

`SoldierProfile.Discipline` (0-20, default 5) controls how synchronised a soldier's attacks are with its troop.

- Each troop has a strike beat every `TroopConstants.MeleeBeatInterval` (1 s). The beat's phase is random per troop.
- When an attack is completed, it is held until the next beat with probability `Discipline / MaxDiscipline`, and is
  otherwise ready at once.
- Held attacks are released together on the beat.
- Discipline 0 means each attack is independent. Discipline 20 means every attack waits for the beat, so the troop
  strikes in unison.
- Discipline only changes **when** attacks land, not how many. The expected count per 30 s is still Aggression.

### Relevant code

| Class | Role |
|---|---|
| `Model/SoldierMeleeState` | Per-soldier attack progress, held and ready attacks, and the engaged flag. |
| `Combat/MeleeAttackClock` | Per-troop beat timer. Ticks every soldier's `SoldierMeleeState` once per frame. |
| `Combat/SoldierContactResolver` | Marks soldiers engaged when an opposing pair is within A_Distance (the clash distance). |
| `Combat/ClashResolver` | Compares pressure and spends a ready attack for the winner. |
| `Runtime/TroopConstants` | `PerMeleeTime`, `MeleeBeatInterval`, `MaxDiscipline`. |

## Tuning

| Setting | Location | Meaning |
|---|---|---|
| `Aggression` | `SoldierProfile` (1-20, default 5) | Expected melee attacks per 30 s while engaged (5 = one every 6 s). |
| `Discipline` | `SoldierProfile` (0-20) | 0 = independent attacks, 20 = troop strikes in unison. |
| `MeleeSkill` | `SoldierProfile` | Clash pressure and damage multiplier (0.75x-1.25x). |
| `MeleeDamage` | `SoldierProfile` | Base damage of one landed attack. |
| `PerMeleeTime` | `TroopConstants` (30) | Window over which Aggression is counted. |
| `MeleeBeatInterval` | `TroopConstants` (1 s) | Spacing of a troop's strike beats. |
| `_sDistance` / `_aDistance` | `TroopCombatWorld` (30 / 5.2) | Sensing range, and action distance. A_Distance is also the clash distance. |

Aggression also feeds clash pressure and the charge decision in `ChargeCoordinator`. Changing it affects
those systems as well as the attack rate.

## Module reference

### Runtime/
- **Troop**: aggregator. Public surface covers identity, state, orders (`MoveTo`, `Pursue`, `OrderLine`), damage
  (`ApplyDamage`), melee state (`SetMeleeState`), selection and hover. Raises `SoldierDied`.
- **TroopRoster** (MonoBehaviour): spawns soldiers from a composition and answers queries (living count, world
  positions, averages, nearest distance).
- **TroopFormationState** (MonoBehaviour): holds the current battle line (`TroopFormationData`) and smooths threat
  heading and engaged-enemy alignment.
- **TroopMorale** (MonoBehaviour): tracks shock (damage taken since the last break) and fault pressure. Decides when
  the troop breaks, then resets shock. Morale comes from the troop profile.
- **TroopPivotMover**: moves the pivot (marching, pursuit, line-push drift).
- **TroopPathFollower**: follows a `MovementSystem.NavPathfinder` path, rebuilt every 0.5 s or when the target moves.
- **FaultSlipMotion**: the retreat after a break.
- **TroopLineOrder**: a player-ordered line. Turns the troop to face the line and marches to the centre. While idle it
  holds the line shape in the soldier field (`HasIdleLine`).
- **TroopLineLinks**: two endpoint link slots (end 0 = start, end 1 = end) with tension.
- **TroopDamageReceiver**: applies damage, raises `SoldierDied`, kills the troop when no soldiers remain, and adds
  shock.
- **LinkedEndRetreat** (static): shortens a soldier's retreat near a linked end so the link holds.
- **TroopConstants** (static): shared tuning values, including `LineSpacing` (0.6), `ChargeSpeedScale` (2).
- **MeleeMetrics**: rolling 30 s record of the troop's melee (attacks, hits, damage done and received). Shown in the
  Troop inspector during Play mode, with a reset button.

### Model/
- **TroopProfile** (ScriptableObject, "Troops/Troop Profile"): composition (`TroopProfileCount` list), morale, radius
  and line width.
- **SoldierProfile** (ScriptableObject, "Troops/Soldier Profile"): sprite, steering, health, aggression, discipline,
  melee skill and melee damage.
- **SoldierAgentState**: a soldier's id, profile, life state, health, position, velocity, facing and drive fraction.
  Delegates to charge, fault, clash-pulse, separation and melee sub-states.
- **SoldierMeleeState**: melee attack accrual and discipline timing (see Melee combat).
- **SoldierChargeState, SoldierFaultMotionState, SoldierClashPulseState, SoldierSeparationState**: small state objects
  owned by `SoldierAgentState`.
- **TroopFormationData**: line start, end, facing and linked flags. Derives the tangent.
- **TroopEndpointLink**: other troop, other end and tension.
- **SoldierLifeState**: `Active`, `Injured`, `Dead`.

### Combat/
- **TroopCombatWorld** (MonoBehaviour): the controller. Runs each system in order.
- **TroopRegistry**: list of all troops, refreshed from the scene every 0.5 s, sorted by instance ID.
- **SoldierContactIndex**: spatial hash of living soldiers. Exposes `Contacts`, `CellFor` and `TryGetBucket`.
- **SoldierContact** (struct): troop, soldier and world position for one frame.
- **SoldierContactResolver**: bounces overlapping soldiers apart, starts clashes between opposing soldiers and records
  which troops are in melee.
- **ClashResolver**: knock-back, pressure contest, clash loss and damage to the loser.
- **MeleeAttackClock**: per-troop strike beat and per-soldier attack timing.
- **ChargeCoordinator**: decides charge start, keep or cancel. Uses Ising-like spreading between neighbours.
- **CrossTroopSeparation** (static): distance-falloff separation between soldiers of different troops.
- **TroopCombatGizmos** (static): scene-view drawing of ranges, lines, endpoints and links. Selected troops show a
  sphere per soldier: white if it has a banked attack, black if not.

### Formation/
- **ThreatFormationRules** (static): finds each troop's engagement (nearest enemy within action distance, with
  hysteresis) and its threat heading.
- **AlliedLineConnector** (static): entry point for line building. Groups connected allies and runs the steps below.
- **LineSegmentBuilder**, **LineLinkAssigner**, **LineEndAimer** (static): build segments, link
  ends, aim and snap linked ends.
- **LineSegment**: working segment, rebuilt each frame.
- **TroopPivots** (static): distance helper.

### Pressure/
- **TroopSegmentPressureRules**: enemy line-pressure contacts, a resistive barrier at a clear gap between lines (it
  stops a troop advancing into an enemy line and does not push a holding troop back), and
  integration of line push velocity. Lines have width: each line's half-width is the furthest soldier either side of
  its centre line plus that soldier's radius. The gap (`LineGap` = 0.3) is measured between these footprints, not
  between centre lines.
- **FriendlyOverlapResolver** (static): pushes allied pivots apart when they overlap.
- **AlliedLinkTension** (static): spring with slack and damping across allied links.
- **SegmentGeometry** (static): closest-point maths between segments.

### Soldiers/
- **SoldierSteering**: per-soldier steering, damping, velocity clamp, then terrain and radius constraints. There are no
  slots. Each soldier either seeks an enemy (charge or lunge) or follows the troop's **vector field**:
  - *Attraction*: the desired velocity points at the nearest point of the field's core, with a speed proportional to
    the distance outside the core's half-width. Soldiers slow as they close, and velocity is matched so they do not
    overshoot.
  - *Density spreading*: soldiers within `SpreadRadius` (1.2) push each other apart along the field, with a push that
    fades to zero at that radius. A crowd spreads out evenly along the shape, and sparse soldiers are left alone.
  - *Separation*: the soft force described below keeps soldiers apart and the hard pass enforces the minimum.
  - A retreating soldier queries the field shifted back by its fault offset, so the shape does not need to move.
  - Retreating and charging soldiers are exempt from the radius pull. A charging soldier moves at double speed
    (`TroopConstants.ChargeSpeedScale`).
  - Soldiers of the same troop keep their centres at least `SeparationRadiusScale` (3) × `SoldierRadius` (0.3) = 0.9
    units apart. A soft force pushes neighbours out when there is room. A hard pass (`ResolveMinimumSeparation`, 3
    iterations) enforces the minimum when soldiers press together.
  - **Pressure**: each soldier's `DriveFraction` is its desired speed over max speed (how hard the field is pulling it).
    A driving soldier is up to 4x heavier than a settled one (`PressureMassPerDrive` = 3), so it gives way less. It
    shoves settled neighbours into place and passes its approach velocity into them. This lets a battle line form
    without soldiers locking each other.
  - **Charging** soldiers are excluded from separation, spreading and pressure in both directions, so they slip through
    their own flock.
  - There is no attraction between soldiers: cohesion was removed, and alignment only matches velocity. Soldiers from
    different troops still bounce at contact (`CrossTroopSeparation`, `SoldierContactResolver`), and that distance is
    unchanged.
- **SoldierFieldPlanner**: builds the troop's `FieldShape` each frame (a core segment plus half-width, in troop-local
  space) and decides which soldiers seek instead:
  - *Battle line*: the threat-facing or linked formation. The core runs between the line's endpoints, pulled in by a
    clearance at linked ends so soldiers stay off the joint.
  - *Column*: while moving, a lane trailing the pivot. Lanes are added as the troop grows, and the column stays within
    the troop radius.
  - *Ordered line*: while idle after a line order, the troop's local Y axis at `LineWidth`.
  - *Blob*: a disc of 0.85 × radius around the pivot, a zero-length core.
  - Charging and lunging soldiers seek their enemy instead (unless the troop is retreating).
- **SoldierSpatialGrid**: per-troop spatial hash for neighbour lookup.
- **SoldierMotionConstraints**: clamps soldiers to the troop radius and checks terrain with a `CircleCast`.

### Input/
- **TroopPlayerOrders** (MonoBehaviour): reads mouse input and delegates to the systems below.
- **TroopSelection**: hover, click and box selection of the player's troops.
- **TroopOrderPlanner**: turns right-click or drag into pursue, move or battle line.
- **TroopOrderOverlay** (static): help panel, selection box and order preview.
- **TroopScreenSpace** (static): screen, GUI and world conversions.

### View/
- **TroopVisualiser** (MonoBehaviour): radius and pivot sprites.
- **TroopSoldierViews**: one sprite object per soldier, synced each frame.
- **SoldierFacingAnimator** (static): turns soldiers toward movement or the line facing when engaged.
- **ClashPulseView**: brightens a soldier's sprite during a clash.
- **SoldierSpriteLibrary** (static): procedural sprites and the model material.
- **TroopSoldierShowcase** (MonoBehaviour): test-scene profiles and orders.
- **TroopProfileSpriteDefaults** (MonoBehaviour): default sprites for `SoldierProfile` fields.

### Editor/
- **TroopEditor**: live Shock and Fault Pressure for a `Troop` in Play mode.
- **TroopModuleEditor**, **TroopRosterEditor**, **TroopFormationStateEditor**, **TroopMoraleEditor**: live state for
  the MonoBehaviour modules.

## Notes

- `TroopRegistry` and `TroopSelection` use `FindObjectsByType` on a 0.5 s throttle, so new troops can be missed for up
  to half a second.
- `ChargeCoordinator` and `ClashResolver` use `UnityEngine.Random`, so battles are not reproducible.
- `MeleeAttackClock` keeps a beat timer per troop in a dictionary. Entries for destroyed troops are not removed.
- Changes to `SoldierProfile` values are only verified in the Unity Editor. This code was not compiled or run here.
