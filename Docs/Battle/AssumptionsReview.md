# Assumptions review

Every Assumption in `SpecificationUpdate.md` (SU-01 to SU-127), grouped by system so they can be confirmed in one sitting. Generated 2026-11-07 and updated with SU-120 to SU-127: 132 assumptions across 127 SU entries.

## How to use this

For each line, change the box and, if needed, add a note after `→`:

- `[x]` keep it as written: it becomes part of the spec.
- `[~]` keep it but change it: write the change after `→`.
- `[-]` reject it: write what should happen instead after `→` (or "remove").
- `[ ]` not decided yet: it stays an assumption.

When a group is done, ask Claude to fold it into `specifications.md`. Once every group is folded in, the spec becomes v1.0 and `SpecificationUpdate.md` is archived and restarted. Items flagged *later amended* may already have been overtaken by a newer entry; check the newer SU first.

## Contents

- [Troops, characters, stats & equipment](#troops-characters-stats--equipment) (16)
- [Soldier simulation & physics](#soldier-simulation--physics) (7)
- [Formations, movement & geometry](#formations-movement--geometry) (10)
- [Combat (melee, charges, ranged)](#combat-melee-charges-ranged) (13)
- [Morale, retreat & rout](#morale-retreat--rout) (11)
- [Controls, orders & battle UI](#controls-orders--battle-ui) (9)
- [Campaign, tent, missions & deployment](#campaign-tent-missions--deployment) (40)
- [Tooling, visuals & other](#tooling-visuals--other) (26)

## Troops, characters, stats & equipment


**SU-62** XP and levels for roster characters

- [ ] `RosterCharacter.AddXp`: 100 XP per level (`XpPerLevel`), carried over. The post-battle card fills its XP bar and plays the level-up sound when it wraps. Open item: stat growth on level-up (growth chances and class caps, spec §1.1) is **not applied yet**, so the post-battle card no longer lists stat gains. *Later amended by SU-115 — check it still applies.* →

**SU-89** Armour Deflect / Bruise / Bleed is a ratio; weapons gain Piercing X and Bludgeoning X

- [ ] Piercing and Bludgeoning are **fields on every weapon** (`WeaponProfile.Piercing`, `.Bludgeoning`, 0 = none), so both **melee and ranged** weapons can have them, and one weapon may have both. They are separate from the single melee `SpecialRule` (Boar Spear, Lance), so a weapon can carry a special rule as well. *Later amended by SU-90 — check it still applies.* →
- [ ] a melee hit uses the winner's **melee weapon**; a projectile uses the **ranged weapon that fired it**. *Later amended by SU-90 — check it still applies.* →
- [ ] a soldier with **no armour at all** still takes every point as Bleed with no roll (decision 7), so Bludgeoning does not turn that damage into Bruise. A character wearing the *Unarmoured* asset does roll, and the abilities apply. *Later amended by SU-90 — check it still applies.* →
- [ ] no existing weapon has Piercing or Bludgeoning yet (all 0); set them per weapon in the Inspector. *Later amended by SU-90 — check it still applies.* →

**SU-90** Special rules are a list; Piercing / Bludgeoning become special rules; Fire & Engage; renames

- [ ] "retreat" is the morale **Case B retreat** (§5.5). A rout, a chain sympathetic retreat and an ordered move away from a fight do not trigger the retreat volley. →
- [ ] a rule on the wrong kind of weapon (Fire & Engage on melee, Spear Support or Couched Lance on ranged) has no effect and logs a warning; repeated Piercing or Bludgeoning entries add up. →
- [ ] a soldier with **no armour** still has **no TotalDefence cap**: every point rolls against 0 : 3 : 7, so all of it counts as Shock as before (decision 7). The change is that about 30% now lands as Bruise (heals) instead of Bleed. Seeded scenario tests that use unarmoured characters may need their expectations re-checked. →
- [ ] these volleys ignore the usual limits (stationary, not engaged, not retreating, target not engaged) and the front arc: a shooter needs only its weapon range to the target soldier. Soldiers that are charging also fire. Hit chance and damage are the normal ranged rules. The volleys count toward `Volleys` (and `FireAndEngageVolleys`), raise the normal volley event, and do not change the normal fire cooldown. Outside these two moments a Fire & Engage weapon fires normally. →

**SU-103** Troop radius becomes troop frontage

- [ ] a composite troop takes the largest part frontage (Q-53 as before). *Later amended by SU-118 — check it still applies.* →
- [ ] saved assets keep their number (old `_troopRadius` reads as `_frontage`): Spearmen, Archer, Skirmisher, Jagen, Robin and the Composite troop are 5 wide; Julian, Laurice, Cav and Huskarl are **3 wide**. Check those four. Before, every radius-5 troop stood about 10 wide (L_nat 9), so lines are now about half as wide. *Later amended by SU-118 — check it still applies.* →
- [ ] the troop still has a size in world units for the blob, the soldier clamp, pivot spacing, deployment spacing and pathing, now derived: `L_nat / NaturalLineRadiusScale` (the old radius-to-line ratio, so frontage 10 is the old radius 5), but never smaller than the radius whose blob (`BlobRadiusScale` x radius) holds the troop's soldiers packed at line spacing. Spearmen: 2.5 units (was 5). It is no longer a profile value (`Troop.Radius`, `TroopProfile.Radius`, `LineGeometry.TroopRadius`). *Later amended by SU-118 — check it still applies.* →

**SU-118** Troop Profile removed: troops hold their characters and set their own frontage

- [ ] `Troop.Configure` takes an optional frontage override, used only by the tests and the soldier showcase scene, which need a fixed width; 0 (the default) uses `TroopFrontageRule`. →
- [ ] scenes, the Troop prefab and the level DataManagers were migrated by replacing each troop asset reference with that asset's characters (same order). →
- [ ] the deployment zone spawns every troop first and lays them out afterwards, so the row spacing uses each troop's own frontage and size. →

**SU-125** The generic spearman is called "Spearmen"

- [ ] the generic spearman character is named "Spearmen" like the other generics (it was "Borgo", so both Mission 1 guard troops showed as Borgo); it keeps Borgo's portrait for now, and the Phase 01 generator's Borgo sheet stays a separate character. →

## Soldier simulation & physics


**SU-46** Friends never move an engaged or attacking troop

- [ ] an attacking troop counts as committed before it is engaged. A free troop still linked to an attacker is pulled along until the link snaps at 3.0 x L_nat (the pinned anchor rule). The order planner already breaks an attacker's links to troops outside the selection, so this matters mostly for orders given in code (the AI). →

**SU-94** Friendly troops do not overlap

- [ ] it uses the outline, not the footprint: the footprint grows with straggling soldiers, which would keep pushing a troop whose soldiers lag behind its pivot. →
- [ ] the troop giving way moves at no more than `FriendlyGiveWaySpeedScale` (0.5) x its own march speed, so its soldiers keep up and a marching troop is slowed while it passes a friend but never blocked for good. So a troop can still pass through a friend that is engaged; it just takes longer. →
- [ ] troops linked in a chain, or with a pending link from a line order, are exempt (their lines meet at the joints). Routed troops are exempt. Nothing moves while paused or deploying; troops placed on top of each other in deployment move apart once the battle starts. →
- [ ] who gives way. An engaged or attacking troop (SU-46) has right of way over a marching or retreating troop, which has right of way over a standing one; the troop with right of way is not moved, equals share the move. →

**SU-102** Tent hub placeholder sprites replaced by ink illustrations

- [ ] the layout (`TentLayout.json`) is re-fitted to the new background and to each sprite's aspect ratio, keeping the left-to-right zones and draw order of §2. Changed positions: the weapon rack, merchant, debts desk and chest stand left of the tent pole; the Captain beside the pole; the Tactician behind the war table; the war table moved 36 px left and 16 px down so it clears the doorway; the Dispatch-bearer sits on his stool just inside the entrance, below the doorway hotspot, holding the dispatch toward the table (the glow follows the dispatch). →
- [ ] the new background already shows the open tent door, so the Tent Flaps sprite is now a cut-out of that doorway from the background (feathered edges) rather than a separate drawing; it still brightens and scales on hover and only the doorway takes clicks. →

## Formations, movement & geometry


**SU-09** Pressure contact starts before EngagementGap

- [ ] The pressure contact range is `max(0.5 × A_Distance, EngagementGap)`, then × 1.25 to start it, × 1.6 to release it. With the defaults this is unchanged (2.6 / 3.25 / 5.2). The `max` only matters if `EngagementGap` is tuned above 2.6, so the barrier always has a contact to act on. →

**SU-24** S_Distance exit margin from footprint shape

- [ ] Bug: a troop near the S_Distance edge flickered between Idle / Column and Line. Forming a line turns its footprint from a circle into a thin rectangle, which widens the footprint gap past S_Distance; the troop drops the line, the circle comes back, and the gap closes again. Fix: a troop that already faces a threat keeps it until the gap exceeds S_Distance plus an exit margin of at least `BlobRadiu… →

**SU-42** Troop visualiser draws a steadied formation outline; click and hover use it

- [ ] - Stabilising: the outline's length, width and rounding ease toward the footprint (0.15 s); its centre and facing follow it closely (0.04 s) so a marching troop's outline does not trail its soldiers. A Line / Chain outline widens at most 1.5 units per second. While the troop is engaged, assaulting or in melee, a Line / Chain outline is held to the width it had when the fight began; it can still narrow with casualties. Column and Blob outlines are only eased. - Picking order: a living soldier within `ClickPickRadiusPixels` beats the outline of a troop standing over it (nearest soldier wins); then the outline the point is deepest inside; then a pivot within the pick radius. The same test picks the hovered troop (any team) and the enemy under an order's end point. - Hovering no longer triggers anywhere inside the troop's radius circle, only on a soldier or inside the outline. *Later amended by SU-44 — check it still applies.* →

**SU-47** Auto-links need facings within 45 degrees

- [ ] - An automatic link breaks when the facings drift more than `AutoLinkBreakFacingDeg` = 60 degrees apart, so there are 15 degrees of hysteresis. New break reason: `ChainBreakReason.Facing`. - The facings compared are each troop's own facing (toward its threat, or the ordered facing), not the facing of the bent chain line. - Each link records whether a line order made it (`TroopEndpointLink.FromOrder`). →

**SU-76** Mission 1 AI set up by a tool

- [ ] marker placement for the Level 1 map: `ArcherFallback` (-2.5, 18.5) just behind the gap between the guards; `LureLine` across the field at y = -2, firing northwards; `CavalryRoute1` (-35.5, -14) in the gap south of the river's end; `CavalryRoute2` (-33, -21) south-west of the player's deployment line. →
- [ ] the archer's minimum range is **7** (README: 6) because Level 1's archer has a Short Bow (range 10, so it stands at 8.5); 6 would let a chaser reach it. The cavalry's aggro and leash are **8 / 12** (README: 4 / 7) so it fights back if a player troop finds it. →

**SU-120** Marching columns are longer than wide and the outline hugs them

- [ ] lanes = clamp(ceil(frontage / 2), 1, 3), one fewer while there would be no more rows than lanes (a 4-soldier troop marches in single file); rows = ceil(living / lanes); core length = max(LineSpacing, (rows - 1) x LineSpacing); half-width = (lanes - 1) x LineSpacing / 2 + RankThickness; the core starts 0.05 x TroopRadius behind the pivot. Replaces "the column stays within the troop radius". →
- [ ] while the troop marches, the soldier radius clamp reaches the column's tail; after the march it shrinks back at 1.5 units/s so the tail walks in. →
- [ ] column soldiers are spread row by row along the core (column fill), front row just behind the pivot, last row at the tail. →

**SU-123** A line footprint covers its soldiers on each side of the line

- [ ] a Line / Chain footprint reaches, ahead of and behind the line separately, the furthest soldier on that side + SoldierRadius, and at least the rank field (RankHalfWidth - 2 x RankThickness); no longer one half-width mirrored to both sides. Line points stay on the formation line; a formed-up line is unchanged. →

## Combat (melee, charges, ranged)


**SU-10** Engaged lines no longer touch: melee comes from charges

- [ ] At a 2.0 gap the front soldiers of two engaged lines are about 2.6 units apart, beyond `ContactDistance` (0.85). Soldiers only meet when they charge (charge range 7.8 soldier-to-soldier, so the gap is always in reach). Passive contact clashes between standing lines no longer happen. Tune `EngagementGap` down if lines should grind together. →

**SU-28** Weapon damage shown

- [ ] Melee weapons show Strength + weapon damage (the raw damage before Domination). Ranged weapons show the weapon's damage only, because ranged damage does not add Strength (Q-5). →

**SU-36** Squash and stretch when a soldier is hit

- [ ] it plays for 0.4 s along the push direction (from the duel winner toward the loser): squash over the first 20%, spring to full stretch by 50%, settle by 100%. Only the soldier that takes damage plays it (not the winner, and not a clash that deals 0 damage). Ranged hits do not play it, since they have no knock-back. *Later amended by SU-109 — check it still applies.* →

**SU-38** Engagement view: engaged frontage as lines or a heat map

- [ ] a soldier is engaged when a living soldier of the opposing engaged troop is within EngagementGap + 2 x SoldierRadius + 0.45 (3.05 units at the defaults): the front rank counts, the rank behind it (LineSpacing 0.9 back) does not. Each engaged soldier is linked to its nearest such enemy; a pair that picks each other is drawn as one line. Lines are 1.5 px wide. The heat map bins the link midpoints into 0.9-unit cells along the contact (one soldier per side per cell is fully engaged), smooths each cell with its neighbours (1/4, 1/2, 1/4), and spans the gap from front rank to front rank, fading to 35% opacity at the two edges. Opacity runs 0.15-0.75. Routed troops are not shown. Drawn on the Terrain sorting layer (order 13): above the range mask, below the soldiers. *Later amended by SU-40 — check it still applies.* →

**SU-43** Charges are completed through a retreat, then the soldier rushes back

- [ ] - The accordion retreat (FaultSlipMotion) only moves the soldiers in formation. Chargers, followers and soldiers already returning are left out of its wave. Before, they were pinned to the spot they stood on, which stranded them. - Return: when a charge ends (contact, timeout, target lost), the soldier rushes back if its troop is retreating or no longer engaged, or it is outside 0.92 x the troop radius. Otherwise it rejoins the line as before, so ordinary melee is unchanged. Followers do the same when their charger's charge ends. - A returning soldier moves at charge speed (`ChargeSpeedScale`, 2x speed and acceleration) toward its place in the formation. It may be outside the troop radius without being clamped (the old clamp could snap it to the radius edge), and it does not start or follow a new charge. It counts as home within `LineSpacing` (0.9) of its formation band and inside 0.92 x the troop radius. The return gives up after 6 s. - Returning soldiers, like chargers, are left out of the footprint, so a soldier running back does not widen the troop's footprint (and so its engagement gap). →

**SU-45** Engagement hold

- [ ] - `EngagementHoldMargin` = 1.0 units (new, Formation Tuning). With the project's A_Distance of 2.0 the hold edge is 3.0. - The hold is per enemy: each engagement record remembers whether that pair was engaged the frame before. Both troops of a pair release in the same frame. - A held enemy keeps its engagement record even when S_Distance is smaller than A_Distance + the margin. - Assault still starts only when a troop enters engagement, so a fight that stays inside the hold no longer retriggers it. →

**SU-95** Soldiers charge only within the engagement gap; charge reach is A_Distance

- [ ] a charger picks its target only from enemy troops that pass the gate. →
- [ ] the charge distance is measured between the two soldiers' edges, like the footprint gap: centre to centre it is A_Distance + 2 soldier radii (2.6 units with the project's A_Distance of 2). Lines held EngagementGap apart have their front ranks EngagementGap + 2 radii apart, so with the project's A_Distance = EngagementGap = 2 a centre-to-centre reach of 2 would almost never find a target. →
- [ ] the gate allows `ChargeGapTolerance` (0.25 units, Melee Tuning) beyond `EngagementGap`, so lines held at the gap by the barrier do not flick in and out of charging. →

**SU-105** Ranged weapons fire in volleys only when Volley is set

- [ ] a soldier without Volley shoots once every `ActionWindowSeconds / FireRate` (+/- 10% jitter), its first shot after a random part of that interval, so the troop's shots spread out. The firing conditions, target troop and hit roll are unchanged. Each single shot goes to the reachable enemy soldier with the fewest of the troop's projectiles already flying at it (nearest on a tie). In a troop with both kinds, the volley soldiers fire together and the others on their own. →

**SU-106** Only the front rank charges

- [ ] the front rank is one rank's worth of soldiers (the line's soldiers per rank; the troop's frontage when it is not in a line), taken as the living soldiers standing furthest toward each enemy troop the troop may charge (from its pivot toward that troop's pivot), so a flank attacker is met by the soldiers on that side. Soldiers out charging or running back keep their place in that count, so rear soldiers do not step up while they are away. Followers (§4.4) are unchanged: any of the Discipline nearest may follow. →

**SU-107** Stunned soldiers

- [ ] - "At least half" is Bruise >= Bleed (Bruise >= MaxHP / 2 at 0 HP). Overflow counts as Bleed. - A stunned soldier is alive: it counts toward the troop's soldiers, EffectiveMorale and capture points. Its bruises heal at the usual 1 HP/s from 0 HP, so a fully bruised soldier is down for 5 s. - It neither charges nor follows, and cannot support (as a fatigued soldier). It can still be charged, and it still takes part in a clash it is caught in, though it deals no damage if it wins (SU-111). On 0 HP it has no HP left to take, so a hit then does nothing; once healed above 0 a hit that brings it back to 0 stuns it again or, with Bleed over half, kills it. - It is not snapped back inside the troop radius and walks back to its troop with the field; it does not rush back after a charge. - "Retreats in melee": a Case B retreat (only possible in melee, SU-104). Assumption: a **rout while in melee** also leaves its stunned behind. A disorder or a chain's shared retreat does not. The dead are credited to the enemy troop it was engaged with (`BattleEvents.SoldierKilled`). - Drawn greyed like a corpse, without its glow, until it recovers. *Later amended by SU-111 — check it still applies.* →

**SU-112** Charges per 45 s, set in Melee Tuning

- [ ] only charges use it. Ranged fire rate (shots per window) and Shock decay keep `ActionWindowSeconds` (30 s, Soldier Tuning). →

## Morale, retreat & rout


**SU-13** Retreat motion has no overshoot and return

- [ ] Before: the pivot jumped back and every soldier headed for the new line at once; the per-soldier delay only staggered an extra fall-back past the new line (to about 2× the distance) followed by a return. That hid the wave and caused the overshoot in issue-03. Now: each soldier holds the ground it stood on until its start time, then eases straight to its place in the new line. No hold, no return. R… →

**SU-26** Morale and Shock bars share one scale

- [ ] Both bars use the initial Effective Morale as full width, so the troop breaks when the red bar passes the end of the blue one. A white tick on the Shock bar marks the current Effective Morale (the break point). →

**SU-104** Retreats only from melee; out of melee a break disorders the troop

- [ ] "engaged in melee" is `Troop.InMelee` (engaged with an enemy troop, within A_Distance plus the hold margin). →
- [ ] breaking again while disordered restarts the 10 s (another penalty); if the troop is in melee by then it retreats, which ends the disorder. Case A (rout) and Case C (surrounded break) are unchanged. →

**SU-121** Routed troops flee around terrain

- [ ] with a NavMesh a routed troop runs to a flee goal on the battlefield edge within ±90° of "away from the nearest non-routed enemy" (whole circle if nothing there is reachable), scored by direction, enemy distance, detour and not passing enemies, and follows the NavMesh path to it; re-planned every 1 s and on arrival. →
- [ ] a routed pivot moving less than 0.5 units in 1 s is stuck: it picks a new goal and avoids that heading (±30°) for 5 s. →
- [ ] without a NavMesh the rout keeps the straight-line flight. →
- [ ] routed troops still do not leave the map (SU-74): at the edge they run along it until RoutDespawnSeconds. Despawning on reaching the edge was left out; say if you want it. →

**SU-122** The surround test counts enemy soldiers, not footprints

- [ ] an enemy troop blocks the retreat path only when one of its living soldiers (not charging, following or rushing back) overlaps the box behind the rear face; its footprint alone does not. Terrain still blocks; friendly troops never do. One soldier is enough. →
- [ ] routed enemy troops never block the path. →
- [ ] in play mode the Scene view draws a selected troop's retreat box: red when Surrounded, green when open. →

## Controls, orders & battle UI


**SU-18** Help panel and status panel removed

- [ ] the objectives text is not shown in the troop scenes →

**SU-20** Selected card bottom left, hover card bottom right

- [ ] the hover card sits in the bottom-right corner with its side panels extending to the left; it shows troops of any team, tagged Ally or Enemy; it is hidden while the cursor is over the selected troop (already shown); it stays on its troop while the cursor is over the card itself, so it does not flicker when a troop is under it. With several troops selected the selected card shows the first one and is tagged `+N`. *Later amended by SU-23, SU-29 — check it still applies.* →

**SU-32** Injured soldiers glow at a third

- [ ] "injured" means current HP below MaxHP (any Bruise or Bleed). A soldier whose bruises fully heal glows normally again. Alternative if preferred: only fatigued soldiers (HP <= 50%). *Later amended by SU-34 — check it still applies.* →

**SU-35** Corpses

- [ ] "the background" means behind living soldiers on the Unit sorting layer (sorting order -1; living soldiers are 1). The corpse is hidden at the end of the 10 s (at 0.6 opacity, no final fade). →

**SU-37** Hovered troop shows its order path

- [ ] shown for any team (like the hover troop card and range mask). Line orders count while the troop marches to the line, and the ordered line's footprint is drawn at the end. Move/Line paths are off-white with a dot or line ghost at the end; Attack paths are red and end at the pursued troop's pivot, with its footprint outlined. Chevrons march along the path toward the waypoint, and an arrowhead marks the end. A troop holding a line, idle, retreating or routed shows nothing. Drawn over the battle (IMGUI), under the troop cards. Colours are on `TroopPlayerOrders`. *Later amended by SU-97 — check it still applies.* →

**SU-91** Stat, class, weapon skill and armour icons on the troop card and in the tent

- [ ] in the tent the icons sit on small dark badges, since they are drawn for a dark background and the ledger is parchment. The tent icon rows are built in code over the existing text fields, so the Tent Hub scene needs no rebuild. →
- [ ] stat, weapon class and armour icons are held by one asset, `Resources/UI/Game Icons` (`GameIcons`). Any icon left empty falls back to its name as text. Mace and Crossbow have no icon yet and show their names. →

**SU-110** Combat log panel

- [ ] top-left panel, half the screen high, newest entry at the bottom; it follows the newest entry until scrolled up. Clashes and Shots toggles filter it. Each entry has a bar in the attacker's army colour. The battle keeps the newest 600 entries; times are battle time (m:ss, nothing while paused). Clicks on the panel are not orders and the mouse wheel over it scrolls the log instead of zooming the camera. *Later amended by SU-113 — check it still applies.* →

**SU-127** The Shock label stays readable at low morale

- [ ] the Shock label is drawn after the white break tick, at the bar's left end, or at its right end when the tick would fall on it. →

## Campaign, tent, missions & deployment


**SU-40** Engagement heat map replaces the lines

- [ ] - The band runs where the two lines overlap along the front, from the first troop's line to the second's. - Across the gap the heat is 4v(1 - v) (v = 0 on one line, 1 on the other): 0 on each line, 1 midway. - Along the front it fades to 0 over 2 world units at each end of the overlap (smoothstep; `End Fade`). - It is still scaled by how many soldiers fight at each point (the SU-38 soldier links, per 0.9-unit cell, smoothed), so a stretch where nobody fights shows no heat. `Density Weight` (default 1) sets how much; 0 uses the lines only. - Colour runs from light to dark red with the heat, and opacity from 0 to 0.75 (no minimum opacity any more). - Lines more than 45 degrees apart (a flank or rear attack), or that do not overlap, have no shared gap; there the band falls back to the engaged soldiers: from one side's engaged soldiers to the other's, along their extent. *Later amended by SU-41, SU-96 — check it still applies.* →

**SU-51** Tent sample data for playing Battle Preparation directly

- [ ] - Mock court: Mannus as tactician, Sir Jeagan as captain, Robin Shering as dispatch-bearer. - The sample manager stays alive (DontDestroyOnLoad) for the rest of that play session, so moving on to other scenes keeps the sample data. →

**SU-56** The tent fills the roster from Characters/Campaign

- [ ] this is for the mock phase. `_fillRosterFromCampaignFolder` on the controller turns it off once the campaign recruits characters itself. →

**SU-58** Battle end, report and scene flow on troops

- [ ] the battle report counts **kills** as enemy soldiers that died and **deaths** as player soldiers that died, whoever dealt the blow. Time taken is from the battle scene's start to the end of the battle. →

**SU-60** Deploying the tent's roster

- [ ] 20 units between troop centres; troop radius 16.5 (the `TroopProfile` default); the troop visualiser prefab is added to each troop. Pairing into composite troops still waits on PB-Q4. →

**SU-61** Character achievements on the new character system

- [ ] **Kill tracker** counts enemy soldiers whose killing blow came from the character's troop (melee or ranged). **Character survived** means the troop was not defeated. **Capture points** counts points held by the troop's army. The old PC data assets' achievement lists are not carried over; assign achievements on the campaign characters. *Later amended by SU-114 — check it still applies.* →

**SU-70** Capture points report contest and radius

- [ ] `CapturePoint` gains `IsContested` (both armies had living soldiers inside at the last count) and `Radius` (the 2D collider's larger half-extent, or the fallback radius without a collider). Behaviour is unchanged. Objective ids: a `CapturePointAIObjective` on the point sets the id (e.g. `Primary`); without one the id is the GameObject's name. →

**SU-78** Rout mission: the battle is won at over half the enemy troops

- [ ] the **player's loss** in Level 1 is unchanged (every player troop out). Set its Rout Share to 0.5 too if the rule should apply to both sides. →
- [ ] the share counts **troops**, not soldiers, and a troop that routed and left the field still counts towards the total. →

**SU-81** Optional deployment phase that pauses the battle

- [ ] a battle with Deployment on but **no deployment zone for the player army** starts fighting at once (the Level 1 Troop Test scene, the CLI smoke test). →
- [ ] battle time (`ElapsedTime`, the report's time taken) counts only fighting time. →

**SU-82** Placing troops during deployment

- [ ] a spot outside the zone is **moved inside it**, keeping the whole troop (its blob radius) in the zone where the zone is big enough. Troops placed on top of one another are not separated. *Later amended by SU-101 — check it still applies.* →
- [ ] placed soldiers keep their blob around the pivot while paused and form the line once the battle runs. (Replaced by SU-101: a placed line forms at once; other placements stand Idle.) *Later amended by SU-101 — check it still applies.* *Marked as replaced in the log.* →

**SU-84** Deployed roster is a list of troops; level default troops

- [ ] a character's troop is the troop asset in `Resources/Troop and Character Profiles/Characters/Campaign` made of that character alone (Franz's, Jagen's, Julian's and Robin's Troop). Characters without one (Brom, Gareth, Lyra, Richter) get a runtime troop of radius 5. *Later amended by SU-86 — check it still applies.* →

**SU-86** Level default troops live in BattleReport.DeployedTroops

- [ ] a report with no troops deploys none. →

**SU-88** Pause and speed controls, with orders allowed while paused

- [ ] **Pause keeps the speed**; P (or gamepad Start) toggles pause and resumes at that speed. ½, Play and 2× set their speed and resume. →
- [ ] 2× speed doubles the simulation step per frame rather than running two steps; no extra stability handling was added. Watch for jitter in large melees at 2× on low frame rates. →
- [ ] every battle starts at normal speed, and leaving the battle scene resets the speed to normal so the campaign scenes are unaffected. →
- [ ] the bar is **hidden while deploying** (deployment already holds the pause) and once the battle is over. A speed button pressed while a cutscene holds the pause sets the speed but does not end the cutscene's pause. →

**SU-98** The victory screen shows the deployed troops

- [ ] a level played on its own shows the troops the zone deployed (`BattleReport.DeployedTroops`): the roster character if that troop's character is in the roster, otherwise the character's portrait, name and stats without level or XP. →

**SU-101** Spawned and right-click-placed troops are Idle; a placed line forms at once

- [ ] a troop placed within S_Distance of an enemy still forms a threat-facing line from the next frame, as in battle, while its soldiers stay in the blob until time runs. Deployment zones are normally further away than that. →
- [ ] the soldiers are laid out as the line fill spreads them (files evenly spaced along the line, ranks 0.9 apart, first rank in front). Placing a lined-up troop with a right-click scatters its soldiers back into a blob. →

**SU-114** Achievements: break events, character mini quests, rewards

- [ ] **credit** for an enemy's break or defeat goes to the troop that dealt its last damaging hit, or to a troop in melee with it at that moment (so several troops can earn it from one enemy). *Later amended by SU-115 — check it still applies.* →
- [ ] **disordered by ranged attacks** means the hit that broke the enemy into disorder (SU-104) was a projectile from our troop; a rout or retreat caused by projectiles counts only with "Count Other Breaks". *Later amended by SU-115 — check it still applies.* →
- [ ] **initial assault** is our troop's first melee with that enemy troop, entered while our troop has Assault (it attacked with an order, spec §4.2); a retreat or rout within 15 s of first contact, before our troop leaves melee, counts. "Require Assault" off counts any first contact, including being charged. *Later amended by SU-115 — check it still applies.* →
- [ ] **lasting 1 minute in melee** is 60 s of battle time (battle speed applies, pauses do not count) in melee without our troop retreating or routing; gaps out of melee shorter than 2 s do not end the engagement. *Later amended by SU-115 — check it still applies.* →
- [ ] **outnumbered** means the enemy troop's living soldiers exceeded ours (Ratio 1, adjustable) at any moment while the two were in melee; only the two troops count, not allies on either side. *Later amended by SU-115 — check it still applies.* →
- [ ] class mini quests (all adjustable in the table): every character: Survivor, Earned Your Spurs, Routed Foe; Archer: Volley Disorder, Ten Kills; Skirmisher: Volley Disorder, Against the Odds; Cavalry: Shock Assault, Ten Kills; Huscarl: Held the Line, Against the Odds, Twenty Kills; Spearmen: Held the Line, Shock Assault. *Later amended by SU-115 — check it still applies.* →

**SU-115** Post-battle book and levelling

- [ ] a spread per character (ledger page left, record right), since one 548 x 556 page cannot hold the ledger page and the battle record. The book opens on the report; pages turn with the buttons, Q / E, Page Up / Down or the shoulders; Space / Enter / click skips a spread's animation; a spread animates once and shows its end state if reopened. *Later amended by SU-116 — check it still applies.* →
- [ ] every reward is **granted when the book opens** (`PostBattleResults`), and the animations replay what was recorded; Continue behaves as the old Next button. *Later amended by SU-116 — check it still applies.* →
- [ ] kills and casualties per character count enemy soldiers killed by, and soldiers lost from, the character's troop (`BattleReport.CharacterRecords`); routed troops count routs of each side (`BattleEvents.TroopBroke`). *Later amended by SU-116 — check it still applies.* →

**SU-117** Mandatory characters, frontage from soldier count, composite troops in the tent

- [ ] a mandatory character missing from the roster is added to it at level 1 (logged). *Later amended by SU-118, SU-119 — check it still applies.* →
- [ ] deployed characters always get a troop made at runtime, so the per-character troop assets (e.g. "Julian's Troop") and their saved frontage are no longer used for deployment. A level played on its own still uses its DataManager's default troop assets unchanged. The soldier count is the characters' `Numbers` (level-up Numbers gains are not applied to spawning yet). *Later amended by SU-118, SU-119 — check it still applies.* →

**SU-119** Mission sheets, mission catalog and mission folders

- [ ] migrated missions are titled "Level 1" and "Level 2"; their scenes are renamed "M01 Battle" and "M02 Battle", and dialogue assets found in the level folders go to `PreBattle/`. →
- [ ] the mission's achievements are added to those on the scene's Achievement Controller; achievements on scene objects (a capture point) stay on the controller. Win and loss conditions stay the scene's Battle End components for now. →
- [ ] the tent's map shows "Mission N: Title" when the mission has a title. →
- [ ] without a catalog everything falls back to the old flow (tent `PreparationData` list by level, scene index `CurrentLevel + 2`, `CurrentLevel++` after a battle). →

**SU-126** Deployment: Start Battle off the zone, one-line hint, camera on the zone

- [ ] the Start Battle button is 180 x 48 (was 280 x 72) on the 960 x 540 reference, at the bottom centre 16 units up, between the troop cards in the bottom corners; its label shrinks to fit (set at runtime by DeploymentPanel). →
- [ ] the deployment hint no longer wraps; it shrinks down to 14 pt instead. →
- [ ] when deployment starts the camera centres on the player's deployment zone, 0.15 x its orthographic size below the zone's centre, kept inside the camera bounds. →

## Tooling, visuals & other


**SU-02** Line fill details

- [ ] Pull strength is `LineFillGain = 0.8` × the field attraction strength (constant in `SoldierSteering`). Chargers, followers and retreating soldiers keep their place in the sort (the line does not reshuffle while they are away) but are not pulled. Columns, blobs and routs are unchanged. Linked ends keep the joint clearance: the fill spans the core after clearance. →

**SU-05** Unit card fallbacks

- [ ] Name: the troop's legacy stat block name, otherwise its GameObject name. Portrait: the legacy stat block portrait, otherwise the sprite of the most numerous soldier type, in army colour. *Later amended by SU-19 — check it still applies.* →

**SU-21** Troop card details

- [ ] "Models" is the troop's living soldier count. Living / starting counts per character are on the side panels. Morale turns red below 50% of its initial value, a side panel's count turns red below half its starting soldiers, and the Formation cell turns red when the troop routs. A character with no portrait shows its soldier sprite in the army colour. Sizes are set for a 1080 px tall screen and scal… →

**SU-22** Legacy unit card hidden

- [ ] The scene's uGUI unit card (`BattleUI.UnitCard`, and the Canvas's `SelectedUnit` object) sat where the selected card is now drawn. It is hidden at runtime. It can be deleted from the Canvas prefab in the Editor. →

**SU-29** Hover card lingers

- [ ] The hover card stays for 1 s after the cursor leaves the troop, and for as long as the cursor is over the card, so the cursor can reach it and open an enemy character's panel (enemy troops cannot be selected). →

**SU-44** The outline is drawn from the formation, not the soldiers

- [ ] - The outline is kept relative to the pivot, so it moves exactly with a marching troop. Changes of size, facing and shape (a line forming into a column, say) ease in over 0.2 s. - The outline can now differ from where soldiers actually stand (a soldier shoved out of the line, or a charger, is outside it). Picking still finds those soldiers by their own position (SU-42). - The melee width hold and width growth limit of SU-42 are removed; they are not needed. →

**SU-57** Troops stand alone; the legacy Unit/Model code is deleted

- [ ] a troop is **defeated** once, at rout or when its last soldier dies before it routs. This replaces the old `IUnit.OnUnitDestroyed` and keeps its timing (Q-36, D-32). →

**SU-59** Capture points count soldiers

- [ ] the point belongs to the player at `holder ≥ threshold`, to the enemy at `holder ≤ -threshold`, neutral between (the old enemy test compared against `+threshold`). `capturedBy`, `PointCaptured` and `CapturedBy` fire once when the controller changes (`CapturedBy` used to fire every frame above 0.8). →

**SU-64** Battle sounds from troop events

- [ ] `ModelSoundEffects` (the Model SFX prefab, one per battle) plays the strike sound on a damaging melee hit, the shot sound on each volley, a death grunt per soldier killed, and an optional projectile-hit sound. Assumptions: each sound type is limited to one every 0.08 s; sounds are not positioned. →

**SU-69** The AI on the troop system

- [ ] a **retreating** troop (fault slip) is treated like a routing one: the AI leaves it alone and re-issues its order once it recovers. A routed troop never recovers (spec §5.4). →
- [ ] a line order holds the troop's facing, so the AI's facing is kept while holding. To confirm in play: the troop's own threat facing may turn it instead. →
- [ ] a troop is **in melee** when it is engaged or has soldiers in melee; its melee opponent is `Troop.EngagedEnemy`. →
- [ ] a troop is on the AI team when it is under `Battle.enemy`; troops under neither army use team id 0 = player. →
- [ ] there is **no ammunition** yet; a ranged troop can always shoot. →
- [ ] troops have **one speed**, so the AI's run flag is ignored and an enemy marching at 60% of its Move speed towards a holding troop counts as a charge for the brace reflex. →

**SU-71** AI behaviour details

- [ ] **Distances are in world units** (troop radius 3–8, bow range 10–20). The AI plan's metre values are scaled down by about 3.5; all are Inspector values to tune in play. Troops are only re-ordered when the order changes, with tolerances of 0.75 units and 12°. **Engaged** means the formation reports Engaged, any troop is in melee, or a ranged troop is shooting. **Broken** means the share of troops s… →

**SU-73** Troops path around terrain on the NavMesh

- [ ] a Move or line-order march whose path ends short of the destination (destination off the NavMesh, or no route) completes where the path ends, instead of staying in Column for ever. Pursuit keeps replanning. →
- [ ] with no NavMesh under the troop the path falls back to a straight line and logs one warning per troop; terrain then stops the pivot (SU-74). →

**SU-74** Troop pivots stay off terrain and on the map

- [ ] **routed troops do not leave the map**: they run along the edge until they despawn after `RoutDespawnSeconds`, and can be caught there. Previously soldiers were carried across terrain with the pivot. →
- [ ] a pivot already inside a collider is let out (2D physics queries that start in a collider ignore it). →

**SU-75** A pursuer squares up to the face it attacks

- [ ] the attacked face is classified as for flanking (`FaceClassifier`), so a pursuer that hits a line's flank aligns with that flank. →

**SU-80** Flank skips an unreachable waypoint

- [ ] A Flank route waypoint is skipped, with a console warning, when the detachment gets no closer to it for **4 s** (Flank: Waypoint Stuck Seconds), e.g. a marker placed off the walkable ground. Previously the cavalry waited there for ever. →

**SU-96** The heat map shows only the frontage within A_Distance

- [ ] measured across the band between the two footprints' edges (line gap minus both footprint half-widths), the same metric as the engagement gap. Facing lines that are nowhere within A_Distance (still engaged by the hold margin) show nothing. →

**SU-124** Skirmishers never fall back through the threat

- [ ] a fall-back route is blocked when the straight line to the fallback point passes within half of each troop's frontage + Route Clearance Margin (default 2) of the nearest enemy and comes closer to it than the troop stands now. →
- [ ] a blocked route sends the troop Escape Distance (default 8) away from the enemy first, inside the leash, from headings 0 / ±45 / ±90° (preferring one from which the way on is clear); a cornered troop holds facing the enemy; it resumes the fall-back once the route clears by 1 more unit. →
- [ ] a troop walking out to firing range whose path would pass through the nearest enemy holds and shoots instead. →
