# Enemy AI (Basic tier) — setup guide

Implements the Basic tier of the *Enemy AI Specification & Plan* (Missions 1 and 2). Everything lives in
`Assets/Scripts/Battle/AI/` under the namespace `LeveragedWars.Battle.AI`. Spec changes and assumptions are logged in
`Docs/Battle/SpecificationUpdate.md` (SU-67 onwards).

| Folder | Contents |
| --- | --- |
| Core | `IAITroop`, `IAIObjective`, `IAIBattlefield`, `IAICommander`, orders, 2D geometry helpers |
| Runtime | `AIDirector` (runs the AI), `AIBlackboard` (shared facts), `AITroopAgent` (troop layer and reflexes) |
| Detachments | `Detachment` |
| Formations | Hold, Guard, Skirmish, Support fire, Advance, Flank, Capture, Fall back |
| Triggers | Conditions, actions, `TriggerDefinition` |
| Mission | `AIMission`, the Basic-tier commander you configure per mission |
| Adapters | `TroopAIAdapter`, `CapturePointAIObjective`, `BattleAIBattlefield`: the only code that knows the troop system |
| Debug | `AIDebugOverlay` |

The type dropdowns on formations, conditions and actions use the MackySoft `SubclassSelector` already in the project.

## Conventions

- **2D.** The ground is the XY plane; Z is ignored. A marker's or troop's facing is its **red arrow** (transform right),
  the same as `Troop.Facing`.
- **Units**, not metres. For scale: troop radius 3–8, bow range 10–20, a troop marches 3–5 units/s. All defaults and
  the mission values below are starting points to tune in play.

## 1. How the AI reaches the troops

`BattleAIBattlefield` finds every `Troop` and `CapturePoint` in the scene and gives each an adapter
(`TroopAIAdapter`, `CapturePointAIObjective`) if it has none, at Awake and every 0.5 s after (for spawned troops).
You do not need to add adapters by hand, except a `CapturePointAIObjective` to set an objective's **Id** (otherwise the
id is the GameObject's name).

Orders map onto the troop's own orders:

| AI order | Troop order |
| --- | --- |
| Hold (position, facing) | `OrderLine` at the position, facing, natural line length |
| Move, Fall back | `MoveTo` (there is no run/walk: troops have one speed) |
| Attack, Charge (melee troop) | `Pursue` (the soldiers charge by themselves on contact) |
| Attack (ranged troop) | walk to 85% of range if beyond 105% of it, then stand facing the target; `RangedFireController` picks what to shoot |

A troop is on the AI team when it is under `Battle.enemy`; troops under neither army use their team id (0 = player).

## 2. Scene setup

1. Create an `EnemyAI` GameObject and add `BattleAIBattlefield`, `AIDirector`, `AIMission` and `AIDebugOverlay`.
2. `AIDirector` finds the battlefield in the scene and the `AIMission` on its own GameObject if the fields are left
   empty. Leave **Begin On Start** ticked, even with a deployment phase: deployment pauses time, so the AI acts once
   the fighting begins.
3. On `AIMission`, add detachments (drag the enemy troops into **Troops**) and triggers. The dropdown beside
   **Starting Formation**, **Condition** and each **Action** picks the type.
4. On `AIDebugOverlay`, set **Director**. Turn Gizmos on in the Game view to see detachment labels, states and order lines.

Overlay colours: cyan = waiting/holding, green = moving, red = attacking or engaged, magenta = charging,
yellow = falling back or returning, black = broken.

## 3. Mission 1 — rout (Level 1)

Open Level 1 and run **Leveraged Wars > AI > Set Up Mission 1 (Level 1)**, then save the scene. The tool
(`Editor/Mission1Setup.cs`) builds everything below; run it again to reset the AI settings (markers you have moved stay
where they are). The battle is won by routing or destroying over half the enemy army (enemy `Rout`, Rout Share 0.5).

Scene markers (under `EnemyAI/Markers`; the red arrow is the facing):

- **ArcherFallback** (-2.5, 18.5) — just behind the gap between the two guarding infantry, red arrow facing the player.
- **LureLine** (0, -2) — across the archer's harass path. The line runs along the marker's green axis; its red arrow
  points towards the enemy side, so it fires when a player troop crosses towards the enemy.
- **CavalryRoute1** (-35.5, -14), **CavalryRoute2** (-33, -21) — take the cavalry south round the river's end and
  behind the player's deployment line.

Detachments:

| Id | Troops | Starting formation | Settings |
| --- | --- | --- | --- |
| Archers | Archer | Skirmish | Fallback Point: Transform → ArcherFallback · Minimum Range 7 · Max Distance From Fallback 30 |
| Guards | Alpha, Beta | Guard | Guarded: Transform → ArcherFallback · Guard Position: Start Position · Aggro 10 · Leash 16 |
| Cavalry | Cav | Hold | Anchor: Start Position · Aggro 8 · Leash 12 |

Triggers:

| Id | Condition | Actions |
| --- | --- | --- |
| CavalryLoop | Opponent / Crosses Line → LureLine | Set Formation: Cavalry → Flank (Target: Rearmost Opponent · Route: CavalryRoute1, CavalryRoute2) |
| GuardsAdvance | Detachment Status: Archers, Broken | Set Formation: Guards → Advance (Nearest Opponent) |

The guards guard the fallback point rather than the archer itself: guarding the archer sends them after every troop it
harasses (SU-77). `AIDirector` starts with the scene: the deployment phase pauses time, so the AI acts once the
player presses Start Battle (SU-81).

Gate check (`Mission1SetupTests.PlayedBattle`, run explicitly): the archer walks out and shoots while the guards hold
their post; when a player troop chases it, it falls back to the guards, who intercept; the chaser crossing the lure
line sends the cavalry round the river onto the rearmost player troop.

## 4. Mission 2 — capture (Level 2)

Scene markers:

- **WallGap** — in the opening of the walls, red arrow facing the player.
- Objective ids must match: `Primary` (inside the walls) and `Secondary` (add `CapturePointAIObjective` to each point).

Detachments:

| Id | Troops | Starting formation | Settings |
| --- | --- | --- | --- |
| FirstLine | 2 infantry | Advance | Target: Nearest Opponent |
| FirstCavalry | 1 cavalry | Hold | Anchor: Detachment → FirstLine, Side Offset −10 (left of the line) · Aggro 5 · Leash 8 |
| SecondLine | 2 infantry | Hold | Anchor: Start Position · Aggro 8 · Leash 13 |
| Archers | 3 archers | Support Fire | Supported: SecondLine · Stand Off −5 (in front, as on the plan) |
| SecondCavalry | 1 cavalry | Hold | Anchor: Start Position · Aggro 5 · Leash 8 |
| Bandit | 1 infantry | Capture | Objective: Secondary |
| Boss | 1 infantry | Guard | Guarded: Objective → Primary · Guard Position: Start Position · Aggro 6 · Leash 8 |

Triggers:

| Id | Condition | Actions |
| --- | --- | --- |
| FirstCavalryFlank | Detachment Status: FirstLine, Engaged | Set Formation: FirstCavalry → Flank (Target: Opponent Engaged With Detachment, FirstLine) |
| SecondLineGo | Logic / Any Of: Opponent / Near Detachment (SecondLine, 13); Detachment Status (FirstLine, Broken) | Set Formation: SecondLine → Advance · Set Formation: SecondCavalry → Flank (Target: Nearest Opponent To Detachment, SecondLine) |
| Bypass | Logic / All Of: Opponent / Near Point (WallGap, 13); Detachment Status (SecondLine, Waiting) | Set Formation: SecondLine → Hold (Anchor: Transform → WallGap · Aggro 8 · Leash 12) · Enable Trigger: SecondLineGo, unticked |

The archers follow SecondLine automatically, including to the wall gap. Win and loss conditions stay in the battle
mechanics.

Gate check: both win paths work, and a player who rushes past the first line finds the second line and archers at the
wall gap.

## 5. Tuning

- **Lure line position** (Mission 1) matters most. Too close to the enemy and the cavalry arrives late; too far and the
  player sees it coming.
- **Aggro and leash ranges** decide how easily detachments are baited. Test by deliberately pulling single troops away.
- **Skirmish minimum range** must be well below the archer's range × preferred range fraction, or it will fall back as
  soon as it arrives (range 20 × 0.85 = 17, so 6 leaves room).
- **Detachment broken threshold**: a detachment is broken when the share still fighting is *below* the threshold. At
  0.5, a two-troop detachment with one troop routed is not yet broken.
- Tick rates and reflex settings (brace radius, hold arrival radius) are on `AIDirector`.
