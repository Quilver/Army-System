# Enemy AI (Basic tier) — setup guide

Implements the Basic tier of the *Enemy AI Specification & Plan* (Missions 1 and 2). Everything lives in
`Assets/Scripts/Battle/AI/` under the namespace `LeveragedWars.Battle.AI`. Spec changes and assumptions are logged in
`Docs/Battle/SpecificationUpdate.md` (SU-67 onwards).

| Folder | Contents |
| --- | --- |
| Core | `IAITroop`, `IAIChainTroop`, `IAIFootprintTroop`, `IAIObjective`, `IAIBattlefield`, `IAICommander`, orders, 2D geometry helpers, `AIApproach` (going round friends) |
| Runtime | `AIDirector` (runs the AI), `AIBlackboard` (shared facts), `AITroopAgent` (troop layer and reflexes) |
| Detachments | `Detachment` |
| Formations | Hold, Guard, Skirmish, Support fire, Advance, Flank, Capture, Fall back, Chain Line |
| Triggers | Conditions, actions, `TriggerDefinition` |
| Mission | `AIMission`, the Basic-tier commander you configure per mission |
| Editor | Setup tools: Mission 1 (§3), Mission 2 (§4), the AI vs AI test level (§6) |
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
| Attack, Charge (melee troop) | `Pursue` (the soldiers charge by themselves on contact). If the straight way to the target crosses a friendly troop, the troop first moves round its friends (`MoveTo`, see §5 Go Round Friends) |
| Attack (ranged troop) | walk to 85% of range if beyond 105% of it, then stand facing the target; `RangedFireController` picks what to shoot |
| Line (a place in a chain) | `OrderLine`, as Hold. The neighbours are joined with `ChainLinker.LinkFromOrder` (`IAIChainTroop.LinkRightTo`), the multi-troop line order's pending link |

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
yellow = falling back or returning, black = broken; a troop's Line order (its place in a chain) is a blue line to the
place, and Chain Line draws its line and places in blue.

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

`M02 Battle` is saved with it set up. To rebuild it, open Level 2 and run **Leveraged Wars > AI > Set Up Mission 2
(Level 2)**, then save the scene. The tool
(`Editor/Mission2Setup.cs`) builds the designer's plan (SU-186): the enemy army, both objectives, the marker and the AI.
Run it again to reset the AI settings: troops, the marker and the secondary objective that already exist stay where you
put them; enemy troops whose names are not in the plan are removed (it asks first). The battle is won by capturing the
primary objective (`CapturedPoint` on it); the player loses if their army routs.

The map is "Twin Villages": the walled village in the north-west (its only gap faces north, away from the player), the
wood east of the middle, the river with its bridge to the south-east and the second village beyond it. The player deploys
south of the middle (zone top at y = -5). Every enemy troop faces the player (south) except the village guard (north).

| Troop (GameObject) | Character | Detachment | Default position |
| --- | --- | --- | --- |
| First Line Spearmen 1, 2 | Spearman | FirstLine | (-11.7, 6), (-4.3, 6) |
| First Line Cavalry | Cav | FirstCavalry | (-18, 6) |
| Archers 1, 2, 3 | Archer | Archers | (-11.6, 17), (-6, 17), (-0.4, 17) |
| Second Line Spearmen 1, 2 | Spearman | SecondLine | (-9.7, 22), (-2.3, 22) |
| Second Line Cavalry | Cav | SecondCavalry | (9, 23) |
| Bandit | Skirmisher | Bandit | (9, 9) |
| Village Guard | Huskarl | VillageGuard | (-29, 25), in the wall gap |

Objectives (`CapturePointAIObjective` ids):

- **Primary** — the capture point inside the walls (-31.6, 14.8). Capturing it wins the battle.
- **Secondary** — a capture point in the south-east village (44, -30.5), radius 3.5 like the primary. Holding it at the end
  earns the *Secondary objective* achievement (70 gold, on the scene's Achievement Controller). `DestroyedOnCapture`
  destroys it when the enemy takes it: it stays the enemy's and can never be taken again (SU-184).

Marker (under `EnemyAI/Markers`): **WallGap** (-29, 32), just outside the gap in the walls, red arrow facing out (north).

Detachments:

| Id | Starting formation | Settings |
| --- | --- | --- |
| FirstLine | Advance | Target: Nearest Opponent |
| FirstCavalry | Hold | Anchor: Detachment → FirstLine, Side Offset +10 (the line's right: west, the plan's left) · Aggro 5 · Leash 8 |
| Archers | Skirmish | Fallback Point: Detachment → SecondLine, Forward Offset 5 (in front of it) · Minimum Range 7 · Max Distance From Fallback 15 · Line Gap 2 |
| SecondLine | Hold | Anchor: Start Position · Aggro 8 · Leash 13 |
| SecondCavalry | Hold | Anchor: Start Position · Aggro 5 · Leash 8 |
| Bandit | Capture | Objective: Secondary · Aggro 6 |
| VillageGuard | Guard | Guarded: Objective → Primary · Guard Position: Start Position · Aggro 6 · Leash 16 |

Triggers:

| Id | Condition | Actions |
| --- | --- | --- |
| FirstCavalryFlank | Detachment Status: FirstLine, Engaged | Set Formation: FirstCavalry → Flank (Target: Opponent Engaged With Detachment, FirstLine) |
| SecondLineGo | Logic / Any Of: Opponent / Near Detachment (SecondLine, 13); Detachment Status (FirstLine, Broken) | Set Formation: SecondLine → Advance · Set Formation: SecondCavalry → Flank (Target: Nearest Opponent To Detachment, SecondLine) |
| Bypass | Logic / All Of: Opponent / Near Point (WallGap, 13); Detachment Status (SecondLine, Waiting) | Set Formation: SecondLine → Hold (Anchor: Transform → WallGap · Aggro 8 · Leash 12) · Enable Trigger: SecondLineGo, unticked |

How it plays: the first line marches on the player, its cavalry keeping pace on the west wing and going round once the
line engages. The archers walk out from in front of the second line to shoot at the player while the first line fights,
and fall back to the second line if the player comes for them (their fallback point moves with the second line, to the
wall gap too). The second line holds until the player comes within 13 of it or the first line breaks, then attacks with
its cavalry. The bandit sets off at once, round the wood and over the bridge, to take and destroy the village: the player
has to send fast troops straight away to save the gold. The village guard holds the wall gap; a player who gets round to
the gap without meeting the second line finds it there too.

Gate check (`Mission2SetupTests.PlayedBattle`, run explicitly): with the player standing in the deployment zone, the
first line marches on it and engages, its cavalry goes round, the archers shoot while the first line fights, the bandit
destroys the village and the guard keeps to the gap. `Mission2SetupTests.SetUpBuildsThePlan` checks the tool's result
and `TheSavedSceneIsSetUp` the saved scene. Measured: the first line engages at 2 s, the archers fire from about y = 3,
and the bandit reaches the village at 17 s and destroys it at 21 s (SU-186).

## 5. Tuning

- **Lure line position** (Mission 1) matters most. Too close to the enemy and the cavalry arrives late; too far and the
  player sees it coming.
- **Aggro and leash ranges** decide how easily detachments are baited. Test by deliberately pulling single troops away.
- **Skirmish minimum range** must be well below the archer's range × preferred range fraction, or it will fall back as
  soon as it arrives (range 20 × 0.85 = 17, so 6 leaves room).
- **Several skirmishers** (SU-185): with more than one troop, the fallback point is the centre of a line facing the
  fallback facing, and each troop falls back to its own place in it, **Line Gap** (default 2) apart, so they do not crowd
  onto one spot. The leash and the choice of target are still measured from the point itself. A single troop stands on the
  point, as before.
- **Skirmish fall back never goes through the threat.** A skirmisher locked in melee does not fall back: it fights back
  and morale decides. If the straight way to the fallback point passes within the clearance of the nearest enemy (half
  of each troop's frontage + **Route Clearance Margin**, default 2), it first moves **Escape Distance** (default 8) away
  from the enemy, kept inside **Max Distance From Fallback** and on the side from which the way on is clear, then heads
  for the fallback point once the way is clear. Walking out to firing range never passes through an enemy either (it
  stands and shoots instead). Nothing changes when the way is clear. The escape spot is drawn as a yellow line in the
  Gizmos.
- **Detachment broken threshold**: a detachment is broken when the share still fighting is *below* the threshold. At
  0.5, a two-troop detachment with one troop routed is not yet broken.
- **Go Round Friends (SU-179).** A melee troop told to attack or charge never pushes through its own side's troops to
  reach its target. If the straight way crosses a friend's outline (`IAIFootprintTroop`, which `TroopAIAdapter` offers)
  grown by **Friend Clearance** (default 1.5), the troop heads for the corner on the shortest way round its friends, then
  attacks once the way is clear. Friends standing closer together than twice the clearance, such as a chain, are gone
  round as one, by the nearer end. A troop already closing on its target turns aside only when its way passes within
  half the clearance. In melee, fighting back comes first; ranged troops are not affected. The overlay shows the reflex
  as "go round" on a green Move line. Untick **Go Round Friends** to let melee troops attack straight (as before).
- Tick rates and reflex settings (brace radius, hold arrival radius, Go Round Friends, Friend Clearance) are on
  `AIDirector`.

## 6. Chain Line and the AI vs AI test level

### Chain Line (SU-176)

Keeps a detachment in one chain of linked battle lines (spec §3.4): the troops stand end to end, left to right in the
order they started in, each pair of neighbours joined by the same pending order link as a multi-troop line order, so the
troop system activates, holds and breaks the links by its own rules. Its troops must offer `IAIChainTroop` (every
`TroopAIAdapter` does).

- **Advancing.** The chain moves in bounds of **Bound Distance** towards its **Target** (the centre of the opposing army,
  the nearest opponent, or a point). The next bound waits until every troop is within **Place Tolerance** of its place
  and every link is in force, or **Wait Timeout** has passed. It turns towards the target when more than **Reface
  Threshold** off, at most **Max Turn Per Bound** a bound, and stops **Stop Distance** short of it. Untick **Advance** to
  hold the chain where it forms.
- **Fighting.** A troop attacks an opponent in front of it within **Attack Range** (pivot to pivot, no further to the side
  than half its width + **Attack Side Reach**); its Assault opens on contact. In melee with that opponent it stands its
  ground under a line order (the Line order, which gets no troop-layer reflex), so it keeps its links and can link again;
  an opponent at its flank or rear it turns on. The others move up level with the fight and hold their places. After
  **Regroup Seconds** with nobody in reach, the chain re-forms where it stands and goes on.

The troop system only keeps a link between troops in line. Within S_Distance of an enemy that holds while the chain
marches; further off the links lapse on each bound and are made again when the chain halts.

### The AI vs AI test level (SU-177)

`Assets/Resources/Levels/Test/AI vs AI.unity` is Mission 1's map with two AI armies and nobody else's orders: the player
only watches (the controls' team is -1, so no troop can be selected or ordered; hover, cards, the combat log and the
speed bar work). Open it and press Play. **Leveraged Wars > AI > Create AI vs AI Level** (`Editor/AIBattleSetup.cs`)
builds it from Mission 1's battle scene, and run again rebuilds its troops and AI (the rest of the scene is kept).

Each army (south: the battle's player army, AI team Player; north: the enemy army, AI team Enemy) has 4 Spearman, 2
Huskarl and 2 Skirmisher troops and its own `South AI` / `North AI` director under the `AI vs AI` object (one
`BattleAIBattlefield`). The spear lines start 32 apart (y = -16 and +16, x = 3), each army facing the other.
Detachment and trigger ids carry the side (`South Spears`).

| Detachment | Troops | Starting formation | Settings |
| --- | --- | --- | --- |
| Spears | 4 spearmen | Chain Line | defaults (Target: Opponent Centre) |
| Skirmishers | 2 skirmishers | Hold | Anchor: Detachment → Spears · Line Gap = the chain's width + 6 (one beyond each end) · Aggro 0 · Leash 20 |
| Huskarls | 2 huskarls | Hold | Anchor: Detachment → Spears, Forward Offset −7 · Aggro 9 · Leash 14 · Line Gap 3.5 |

| Trigger | Condition | Actions |
| --- | --- | --- |
| SkirmishersWrap | Detachment Status: Spears, Engaged | Set Formation: Skirmishers → Flank (Target: Opponent Engaged With Detachment, Spears · Flank Distance 6 · Charge Distance 10 · Approach Timeout 15) |
| HuskarlsCommit | Logic / Any Of: Detachment Status (Spears, Broken); Time Elapsed (15 s since SkirmishersWrap) | Set Formation: Huskarls → Advance (Nearest Opponent, Keep Line off) |

The battle ends when every troop of one army is out; the banner is the south army's. Gate check
(`LevelAIvsAITests.PlayedBattle`, run explicitly): both chains stand fully linked before the fight, read as one line (no
joint wider than two files, SU-178), meet with their links in force, the huskarls never stand inside their own spearmen
(SU-179), and a real fight follows. `LevelAIvsAITests.DiagnosticsWithFixes` and `DiagnosticsWithoutFixes` (explicit) log
150 s of every spearman's line and every huskarl's order, with and without SU-178 and SU-179, for comparing playtests.
