# Specification Update Log

Running log of changes to the battle system and of assumptions made while building it, against
`Docs/Battle/specifications.md` (v0.9). Newest entries first. Each entry gives what changed, the spec sections it
touches, and whether it is a **Decision** (asked for by the designer) or an **Assumption** (filled in to make the
change work; confirm or replace). `specifications.md` itself is not edited; fold entries into it when they are agreed.

---

## 2026-11-01 (6): Steady outline; charges survive a retreat

### SU-43 Charges are completed through a retreat, then the soldier rushes back (Decision + Assumption, amends spec §4.3, §5.5)
- Decision: when a troop retreats (break Case B, or a chain's sympathetic retreat), soldiers in the middle of a charge
  are no longer stranded. They **complete their charge** and then **rush back** to their troop.
- Spec §4.3 "the charge is cancelled if ... the charger's troop breaks" now reads: cancelled if the charger's troop
  **routs**. A retreat does not cancel charges or charge support; a rout still does (and no return follows a rout).
- Assumptions:
  - The accordion retreat (FaultSlipMotion) only moves the soldiers in formation. Chargers, followers and soldiers
    already returning are left out of its wave. Before, they were pinned to the spot they stood on, which stranded them.
  - Return: when a charge ends (contact, timeout, target lost), the soldier rushes back if its troop is retreating or no
    longer engaged, or it is outside 0.92 x the troop radius. Otherwise it rejoins the line as before, so ordinary melee
    is unchanged. Followers do the same when their charger's charge ends.
  - A returning soldier moves at charge speed (`ChargeSpeedScale`, 2x speed and acceleration) toward its place in the
    formation. It may be outside the troop radius without being clamped (the old clamp could snap it to the radius edge),
    and it does not start or follow a new charge. It counts as home within `LineSpacing` (0.9) of its formation band
    and inside 0.92 x the troop radius. The return gives up after 6 s.
  - Returning soldiers, like chargers, are left out of the footprint, so a soldier running back does not widen the
    troop's footprint (and so its engagement gap).
- Code: `SoldierChargeState`, `SoldierAgentState`, `SoldierSteering`, `SoldierMotionConstraints`, `SoldierFieldPlanner`,
  `ChargeCoordinator`, `FaultSlipMotion`, `Troop`, `TroopFootprintBuilder`. Tests: `TroopOutlineTests`.

### SU-44 The outline is drawn from the formation, not the soldiers (Decision + Assumption, replaces the SU-42 stabilising)
- Decision: the outline must be stable. It jittered in a marching column and pulsed thicker and thinner because it was
  built from the furthest soldiers. It is now drawn from the shape the soldiers are steered into (`TroopOutline`):
  - Line / Chain: line point to line point, half-width `RankHalfWidth(length, living soldiers)` plus one soldier radius.
    It changes only when the line moves, turns or stretches, or a casualty drops a rank.
  - Column: the marching lane (from 0.05 to 0.85 x troop radius behind the pivot, as wide as its ranks), along a smoothed
    march heading (0.35 s). The column stays for 0.3 s after the troop stops, so stop-start pathing does not flick it
    to the blob.
  - Idle: the blob disc (`BlobRadiusScale` x troop radius); Rout: the scatter disc. Both plus one soldier radius.
- Assumptions:
  - The outline is kept relative to the pivot, so it moves exactly with a marching troop. Changes of size, facing and
    shape (a line forming into a column, say) ease in over 0.2 s.
  - The outline can now differ from where soldiers actually stand (a soldier shoved out of the line, or a charger, is
    outside it). Picking still finds those soldiers by their own position (SU-42).
  - The melee width hold and width growth limit of SU-42 are removed; they are not needed.
- Code: `Rules/TroopOutline.cs` (was `TroopOutlineStabiliser.cs`), `Troop.Outline`. Tests: `TroopOutlineTests`.

---

## 2026-11-01 (5): Troop visualiser shows the formation outline

### SU-42 Troop visualiser draws a steadied formation outline; click and hover use it (Decision + Assumption, spec §3.3 / §6.1 UI)
- Decision: the flock visualiser is now the **troop visualiser**: the prefab is `Troop Visualiser` (and its children
  `Troop Pivot`, `Troop Radius`); the class was already `TroopVisualiser`.
- Decision: instead of the radius disc, the visualiser draws the troop's **formation outline** (its footprint shape: the
  thick line of a Line / Chain troop, the capsule of a Column, the circle of a Blob / Rout) as a faint fill and an army
  colour stroke. Like the old disc it is invisible until the troop is hovered or selected (fill 15/255 hovered, 40/255
  selected; stroke 110/255 hovered, 230/255 selected; 2 px, 3 px selected). The radius disc is kept behind a
  `Show Radius` tick on the visualiser (off).
- Decision: the outline must not flare in melee. The **footprint is unchanged** (it still drives engagement, faces and
  surround); the outline is a separate, steadied copy (`Troop.Outline`, `TroopOutlineStabiliser`).
- Decision: a troop is selected (and hovered) by pointing at one of its living soldiers, or inside its outline.
- Assumptions:
  - Stabilising: the outline's length, width and rounding ease toward the footprint (0.15 s); its centre and facing
    follow it closely (0.04 s) so a marching troop's outline does not trail its soldiers. A Line / Chain outline widens
    at most 1.5 units per second. While the troop is engaged, assaulting or in melee, a Line / Chain outline is held to
    the width it had when the fight began; it can still narrow with casualties. Column and Blob outlines are only eased.
  - Picking order: a living soldier within `ClickPickRadiusPixels` beats the outline of a troop standing over it (nearest
    soldier wins); then the outline the point is deepest inside; then a pivot within the pick radius. The same test picks
    the hovered troop (any team) and the enemy under an order's end point.
  - Hovering no longer triggers anywhere inside the troop's radius circle, only on a soldier or inside the outline.
- Code: `Rules/TroopOutlineStabiliser.cs`, `Rules/TroopOutlinePolygon.cs`, `View/TroopVisualiser.cs`, `Troop.Outline`,
  `TroopEngagementSystem`, `TroopSelection`, `TroopPlayerOrders`. Tests: `TroopOutlineTests`.

---

## 2026-11-01 (4): Heat map is a smooth gradient

### SU-41 Soldier count no longer affects the heat map (Decision, amends SU-40)
- Decision: the number of soldiers fighting does not change the heat. The heat map is a smooth gradient from the two
  troop lines alone: 4v(1 - v) across the gap, times a smoothstep fade over `End Fade` (2 units) at each end of the
  engagement. `Density Weight` is removed.
- To keep the gradient smooth, the mesh has a column every 0.25 units along the front and 16 rows across the gap.
- Flank and rear attacks still use the engaged soldiers only to place the band (now a straight band at their mean
  positions); the heat inside it follows the same gradient.

---

## 2026-11-01 (3): Engagement heat map from the troop lines

### SU-40 Engagement heat map replaces the lines (Decision + Assumption, amends SU-38)
- Decision: the engagement view is the heat map only; the red-lines mode is removed. **F5** toggles it on and off
  (on by default).
- Decision: the heat map is built from the two troops' lines (their footprint centre lines), across the gap between
  them, with less heat the nearer a point is to either troop: most intense in the middle of the line gap, least intense
  at the ends of the engagement and at each troop's line.
- Assumptions:
  - The band runs where the two lines overlap along the front, from the first troop's line to the second's.
  - Across the gap the heat is 4v(1 - v) (v = 0 on one line, 1 on the other): 0 on each line, 1 midway.
  - Along the front it fades to 0 over 2 world units at each end of the overlap (smoothstep; `End Fade`).
  - It is still scaled by how many soldiers fight at each point (the SU-38 soldier links, per 0.9-unit cell, smoothed),
    so a stretch where nobody fights shows no heat. `Density Weight` (default 1) sets how much; 0 uses the lines only.
  - Colour runs from light to dark red with the heat, and opacity from 0 to 0.75 (no minimum opacity any more).
  - Lines more than 45 degrees apart (a flank or rear attack), or that do not overlap, have no shared gap; there the
    band falls back to the engaged soldiers: from one side's engaged soldiers to the other's, along their extent.
- Code: `Rules/EngagementHeatStrip.cs`, `View/TroopEngagementView.cs`, `TroopPlayerOrders` (settings). Tests:
  `BattleViewTests`.

---

## 2026-11-01 (2): Hover order paths, engagement view, Battle Tuning split

### SU-37 Hovered troop shows its order path (Decision + Assumption, spec §6 UI)
- Decision: hovering a troop that has a Move or Attack order draws arrows along the path it is taking to the order's
  waypoint (the navmesh waypoints still ahead, from the pivot).
- Assumptions: shown for any team (like the hover troop card and range mask). Line orders count while the troop marches
  to the line, and the ordered line's footprint is drawn at the end. Move/Line paths are off-white with a dot or line
  ghost at the end; Attack paths are red and end at the pursued troop's pivot, with its footprint outlined. Chevrons
  march along the path toward the waypoint, and an arrowhead marks the end. A troop holding a line, idle, retreating or
  routed shows nothing. Drawn over the battle (IMGUI), under the troop cards. Colours are on `TroopPlayerOrders`.
- Code: `View/TroopOrderPathView.cs`, `Troop.CurrentOrder` / `CollectOrderPath` / `TryGetLineOrder`,
  `TroopPathFollower.CollectRemainingPath`, `Runtime/TroopOrderKind.cs`. Tests: `BattleViewTests`.

### SU-38 Engagement view: engaged frontage as lines or a heat map (Decision + Assumption, spec §4.1 / §6 UI)
- Decision: engaged troops show how much of their frontage is engaged, in one of two modes with a toggle:
  (A) thin red lines between the engaged soldiers; (B) a red heat map in the gap between the engaged lines, darker and
  more opaque where more soldiers are engaged.
- Decision: shown for every engaged pair on the field. **F5** cycles Lines -> Heat map -> Off; the starting mode is set
  on `TroopPlayerOrders` (default Lines).
- Assumptions: a soldier is engaged when a living soldier of the opposing engaged troop is within
  EngagementGap + 2 x SoldierRadius + 0.45 (3.05 units at the defaults): the front rank counts, the rank behind it
  (LineSpacing 0.9 back) does not. Each engaged soldier is linked to its nearest such enemy; a pair that picks each
  other is drawn as one line. Lines are 1.5 px wide. The heat map bins the link midpoints into 0.9-unit cells along the
  contact (one soldier per side per cell is fully engaged), smooths each cell with its neighbours (1/4, 1/2, 1/4), and
  spans the gap from front rank to front rank, fading to 35% opacity at the two edges. Opacity runs 0.15-0.75.
  Routed troops are not shown. Drawn on the Terrain sorting layer (order 13): above the range mask, below the soldiers.
- Code: `Combat/EngagementFrontage.cs`, `Rules/EngagementHeatStrip.cs`, `View/TroopEngagementView.cs`,
  `Resources/Shaders/TroopOverlayColor.shader`, `TroopPlayerOrders` (F5, styles). Tests: `BattleViewTests`.

### SU-39 Tuning split into Battle Tuning assets; profiles folder renamed (Decision + Assumption, spec §7.2)
- Decision: `Assets/Resources/Boid Profiles` is renamed `Troop and Character Profiles` (GUIDs kept, so scenes and
  profiles still link).
- Decision: Troop Tuning is split into ScriptableObjects by section, in the new folder `Assets/Resources/Battle Tuning`.
- Assumption (grouping, chosen by the designer from two options): Soldier (stats and actions, soldier steering),
  Formation (distances, formation and movement), Morale (morale and retreat, faces), Melee (charges and clashes,
  support and duel, Assault), Chain (chains, link spring), Ranged (ranged, composite troops), Input.
- The old `Troop Tuning.asset` is removed; its values were copied into the new assets unchanged (including the
  test-scene A_Distance 2.0 and S_Distance 5.0 noted under SU-31). Values it did not hold yet (soldier steering,
  composite ranks) take their code defaults, as before. No behaviour change.
- Code reads every value through `TroopTuning.Active` as before (same property names); `TroopTuning` is now a plain
  class that loads the seven sections. Tests use `TroopTuning.CreateDefaults()`.

### Test note (no behaviour change)
- The composite-troop scenario tests depend on the random stream each troop takes from its place in the hierarchy, and
  soldier sprites outlive their troop in Edit mode (Troop.OnDestroy does not run there). `BattleViewTests` removes its
  soldier sprites in TearDown so it does not shift the streams of later fixtures. Other fixtures still leave them.

---

## 2026-11-01: Hit squash and stretch

### SU-36 Squash and stretch when a soldier is hit (Decision + Assumption, spec §4.5 / §6 UI)
- Decision: a soldier that takes melee damage and is knocked back squashes in, then its back stretches outward (wider
  and longer, the front narrowing), then returns to normal. Done in the vertex stage of `Unlit/ModelShader`.
- Assumptions: it plays for 0.4 s along the push direction (from the duel winner toward the loser): squash over the first
  20%, spring to full stretch by 50%, settle by 100%. Only the soldier that takes damage plays it (not the winner, and not
  a clash that deals 0 damage). Ranged hits do not play it, since they have no knock-back.
- Shape amounts are material properties on ModelMat (Squash 0.4, Stretch 0.35, Taper 0.8); per-soldier `_HitDir` and
  `_Deform` are set through the property block. Diagram: `Docs/Battle/Visuals/hit-squash-stretch.svg`.
- Code: `ModelShader.shader` (SquashStretch), `TroopSoldierViews.PlayHitSquash` / `HitDeform`, `DuelResolver`.
  Tests: `SoldierViewTests` (curve timing, both soldier shaders compile).

---

## 2026-10-31 (6): Grey injured glow, corpses

### SU-34 Fatigued glow is grey (Decision, replaces SU-32's injured dimming)
- Injured soldiers that are not fatigued keep the full glow. (ModelMat's glow is white, about 1.2 HDR, so dimming it
  to a third already read as grey and could not be told apart from fatigued.)
- Fatigued soldiers (HP <= FatigueFraction x MaxHP, 50% by default) glow grey at a third of the healthy emission's
  luminosity (Rec. 709 luminance of the material colour, divided by 3). Clash flashes play on top of either.

### SU-35 Corpses (Decision + Assumption, spec §6 UI)
- Decision: a dead soldier's sprite stays where it fell for 10 s with no emission (emission mask colour black), drawn
  behind the living, with its army colour at half saturation, and its opacity fading from 1 to 0.6 over the 10 s.
- Assumptions: "the background" means behind living soldiers on the Unit sorting layer (sorting order -1; living
  soldiers are 1). The corpse is hidden at the end of the 10 s (at 0.6 opacity, no final fade).
- Decision (amended): corpses have no collider. On death the soldier's collider is disabled and its kinematic body
  leaves the physics simulation.
- The opaque model shader cannot fade, so corpses switch to `Unlit/ModelCorpseShader` (Resources/Battle prefabs/Model
  prefabs), an alpha-blended copy that draws the emission mask in black.
- Tests: `Editor/Tests/SoldierViewTests.cs` (grey injured glow; corpse timing, sorting, emission and fade).

---

## 2026-10-31 (5): Injured soldiers dimmed, no facing arrow in order previews

### SU-32 Injured soldiers glow at a third (Decision + Assumption, spec §6 UI)
- Decision: an injured soldier's emission (the glowing mark, material colour `_Color` on ModelMat, or `_Light` where a
  material has it) is dimmed to 1/3 luminosity. Clash flashes still play on top, from the dimmed level.
- Assumption: "injured" means current HP below MaxHP (any Bruise or Bleed). A soldier whose bruises fully heal glows
  normally again. Alternative if preferred: only fatigued soldiers (HP <= 50%).
- Code: `TroopSoldierViews.UpdateEmission`, `ClashPulseView.Apply(..., brightness)`.

### SU-33 No facing arrow in order previews (Decision, spec §6.2)
- The ghost preview while giving an order no longer draws a facing arrow, because troops set their own facing (toward
  the threat, or from the chain line). Ghost footprints (front edge still drawn brighter), joints, markers and tethers
  are unchanged.

### Diagrams
- `Docs/Battle/MovementEdgeCases/` 01-13 (movement) and 14-23 (engagements with chains) show the current rules.
  Open questions flagged there: line fill inside terrain (12), engagement exit hysteresis (13), whether an engaged chain
  end may refuse its flank (18), and flankers snapping into chains, issue 02 (23).

---

## 2026-10-31 (4): Soldier hover HP bar

### SU-31 HP bar over the hovered soldier (Decision, spec §4.6 / §6 UI)
- In Play mode, a small bar appears just above the soldier under the cursor (any team, living soldiers only; picked
  within 1.25 soldier radii). The whole bar is MaxHP: green = current HP, red = Bruise, black = Bleed, with a thin white
  border. It is at least 30 px wide and widens with zoom. Hidden while the cursor is over a troop card.
- Code: `View/SoldierHealthBar.cs`, driven by `TroopPlayerOrders`.

### Test note (no behaviour change)
- The scenario tests now run on a fresh default `TroopTuning` instead of the project asset, whose test-scene values
  (A_Distance 2.0, S_Distance 5.0) made 4 tests fail. Open question for the designer: the asset's A_Distance equals
  EngagementGap (2.0), so engaged lines are held right at the engagement edge and charge range is only 3.

---

## 2026-10-31 (3): Troop card redesign, soldier HP inspector

### SU-25 Troop card main block (Decision, amends SU-19 and SU-23)
- Spec §6.1.
- Title: the characters' names joined with ` + `, then the soldier count in brackets as living / starting, e.g.
  `Gareth + Lyra (14/24)`. The separate "Models" cell is removed.
- Portraits of every character in the troop side by side (the main block widens when more than two would be narrow).
- Morale is a blue bar (Effective Morale / initial) with a red Shock bar just below.
- Formation state stays, on its own row.

### SU-26 Morale and Shock bars share one scale (Assumption)
- Spec §5.3.
- Both bars use the initial Effective Morale as full width, so the troop breaks when the red bar passes the end of the
  blue one. A white tick on the Shock bar marks the current Effective Morale (the break point).

### SU-27 Character side panels on demand (Decision, amends SU-19)
- Spec §6.1.
- Side panels are hidden by default. Clicking a character's portrait shows or hides that character's panel; an open
  panel's portrait has an army-colour outline. The choice is kept per troop and character for the session.
- Stats are one column: HP, Defence (+armour bonus), **Armour** (Deflect / Bruise / Bleed, spec §1.6), Support, Charge,
  Aggression. Speed and Strength are removed.
- Each equipped weapon shows its skill in brackets and its damage, e.g. `Gladius (6)   11 dmg`.

### SU-28 Weapon damage shown (Assumption)
- Spec §4.6, Q-5.
- Melee weapons show Strength + weapon damage (the raw damage before Domination). Ranged weapons show the weapon's damage
  only, because ranged damage does not add Strength (Q-5).

### SU-29 Hover card lingers (Assumption, amends SU-20)
- The hover card stays for 1 s after the cursor leaves the troop, and for as long as the cursor is over the card, so
  the cursor can reach it and open an enemy character's panel (enemy troops cannot be selected).

### SU-30 Soldier HP in the Inspector (Decision, tooling)
- Each soldier's sprite object (`Soldier <id>`) carries a `SoldierView` component. Selecting it in Play mode shows the
  soldier's troop, character, life state, an HP bar, current / max HP, Bleed, Bruise, fatigue and charge role.
- The Troop Roster component's Inspector lists every soldier of the troop with an HP bar (foldout "Soldier HP").

---

## 2026-10-31 (2): Troop card fixes, S_Distance exit margin

### SU-23 Troop card text and layout (Decision, amends SU-19 and SU-20)
- Spec §6.1.
- The "Selected" / "Hover Ally / Enemy" tags are removed.
- The troop's name is every character's name joined with ` + ` (e.g. `Gareth + Lyra`). A long name wraps onto more
  lines and the card grows taller.
- All card text is 1.5× larger; panel widths and line heights grow to fit (main block 320, side panels 300 at 1080 p).
- Each equipped weapon is shown with the character's skill in its class next to it, e.g. `Gladius (6)`; 0 means
  unskilled. The weapon class is no longer shown.

### SU-24 S_Distance exit margin from footprint shape (Assumption, amends D-37 and Q-16)
- Spec §3.1, §3.3.
- Bug: a troop near the S_Distance edge flickered between Idle / Column and Line. Forming a line turns its footprint
  from a circle into a thin rectangle, which widens the footprint gap past S_Distance; the troop drops the line, the
  circle comes back, and the gap closes again.
- Fix: a troop that already faces a threat keeps it until the gap exceeds S_Distance plus an exit margin of at least
  `BlobRadiusScale × (r_A + r_B)` (8.5 units for two radius-5 troops). `SDistanceExitHysteresis` still applies when it
  is larger. Entry is unchanged (footprint gap ≤ S_Distance).

---

## 2026-10-31: Troop cards replace the help panel, status panel and unit card

### SU-18 Help panel and status panel removed (Decision, supersedes SU-03 and SU-06)
- Spec §6 UI.
- The top-centre controls help and the right-hand troop status panel (F3, F5) are gone. `TroopDebugOverlay` now only
  draws gizmos (F4). The controls are still listed in `Troop/README.md`.
- With no help panel, nothing at the top of the screen blocks clicks any more. The old fixed top-left no-click area
  (500 × 140 px, kept for the objectives text) is also removed (Assumption: the objectives text is not shown in the
  troop scenes). Only the troop cards block clicks.

### SU-19 Troop card layout (Decision, supersedes SU-04, SU-05 and SU-17)
- Spec §6.1.
- **Main block**, top to bottom: the first character's name; the first character's portrait;
  `Models | Effective Morale / Initial Effective Morale | Formation state`.
- **Side panels**, one per character in the troop: character name, class, living / starting soldiers of that
  character, soldier stats (Speed, HP, Defence (+armour bonus), Strength, Support, Charge, Aggression), each weapon with
  its skill, and armour.
- Drawn by `TroopCardHud` (IMGUI) from `TroopPlayerOrders`. `TroopSelectionCard` is removed.

### SU-20 Selected card bottom left, hover card bottom right (Decision + Assumption)
- Spec §6.1.
- Decision: the selected troop's card is in the bottom-left corner, and a second card shows the troop under the cursor.
- Assumptions: the hover card sits in the bottom-right corner with its side panels extending to the left; it shows
  troops of any team, tagged Ally or Enemy; it is hidden while the cursor is over the selected troop (already shown);
  it stays on its troop while the cursor is over the card itself, so it does not flicker when a troop is under it.
  With several troops selected the selected card shows the first one and is tagged `+N`.

### SU-21 Troop card details (Assumption)
- "Models" is the troop's living soldier count. Living / starting counts per character are on the side panels.
- Morale turns red below 50% of its initial value, a side panel's count turns red below half its starting soldiers,
  and the Formation cell turns red when the troop routs.
- A character with no portrait shows its soldier sprite in the army colour.
- Sizes are set for a 1080 px tall screen and scale with screen height (0.8× to 2.5×).

### SU-22 Legacy unit card hidden (Assumption)
- The scene's uGUI unit card (`BattleUI.UnitCard`, and the Canvas's `SelectedUnit` object) sat where the selected card
  is now drawn. It is hidden at runtime. It can be deleted from the Canvas prefab in the Editor.

---

## 2026-10-29: Character and Troop pre-battle data

### SU-14 SoldierProfile is removed; Character holds the soldier data (Decision)
- Spec §1.1, §1.2, §1.4, §1.5, §1.6.
- New ScriptableObject **`CharacterProfile`** (menu Troops > Character): name, class name, equipment (up to 2 weapons +
  armour), portrait sprite, soldier sprite, Numbers, Morale, Discipline, and soldier stats (Speed, Defence, Strength,
  Support, Charge, Aggression, MaxHP, weapon skills).
- **`TroopProfile`** is now a list of characters plus `TroopRadius`. Move, BaseMorale, Discipline and the soldier count
  are resolved from the characters with the §1.2 composite rules (one character resolves to its own values).
- `SoldierProfile` and `TroopProfileCount` are removed. Each spawned soldier points at its character
  (`SoldierAgentState.Character`) for its stats and equipment. `Troop.Configure(team, composition)` is removed.
- The Phase 01 generator writes `Boid Profiles/Characters/<Name>` (character) and `Characters/Troops/<Name>` (troop of
  that one character). The old soldier assets were moved to `Characters/` and keep their GUIDs and soldier sprites.

### SU-15 Soldier acceleration follows Speed (Decision)
- Spec §1.5, §2.1.
- Acceleration = Speed × **`TroopTuning.AccelerationPerSpeedPoint`**, default **1.6** units/s² per point, so Speed 5
  keeps the old fixed 8.

### SU-16 Alignment, separation and Brownian motion are shared TroopTuning values (Decision)
- Spec §2.1, §2.2.
- Moved from each soldier type to `TroopTuning` (Soldier steering): `SoldierAlignment` 0.5, `SoldierSeparation` 1.8,
  `SoldierBrownianMotion` 0.04 (the values every asset used). Cross-troop separation scales by
  `SoldierSeparation / 1.8`, which is 1 at the default.

### SU-17 Selected-troop card shows the first character (Decision)
- Spec §6.1.
- Name: the first character's name. Portrait: the first character's portrait, or its soldier sprite in the army colour if
  it has none. Stats are laid out like the legacy unit stat block (tab-indented `Label: value` pairs): troop line
  (Move, Soldiers, Morale, Discipline), then one block per character with soldier stats, Morale, Discipline, weapons with
  skill, and armour.

---

## 2026-10-27 (2): Engagement gap, retreat distance, retreat wave

### SU-07 EngagementGap replaces LineGap (Decision)
- Spec §3.3 "Line-vs-line pressure", §4.1 "Engagement", §7.2 `LineGap`.
- `LineGap` (constant 0.3) is renamed **`EngagementGap`** and moved to `TroopTuning` (Distances), default **2.0** units.
- Two opposing troops are still **engaged** when their footprint gap is within `A_Distance` (5.2). Once engaged, the
  line barrier holds them apart at a footprint gap of `EngagementGap` instead of 0.3.
- Code: `TroopTuning.EngagementGap`; `TroopSegmentPressureRules` reads it; `TroopConstants.LineGap` is removed.

### SU-08 A_Distance and EngagementGap are both footprint gaps (Decision, confirms Q-16)
- Spec §3.3, §4.1, §7.2.
- `A_Distance`, `S_Distance`, `EngagementGap` and the pressure contact ranges all use the footprint gap
  (`FootprintGap.Gap`, the gap between the two troops' footprint rectangles or circles), never pivot or centre-line
  distance. This was already the case for `A_Distance` and `S_Distance`.

### SU-09 Pressure contact starts before EngagementGap (Assumption)
- Spec §3.3 "Line-vs-line pressure".
- The pressure contact range is `max(0.5 × A_Distance, EngagementGap)`, then × 1.25 to start it, × 1.6 to release it.
  With the defaults this is unchanged (2.6 / 3.25 / 5.2). The `max` only matters if `EngagementGap` is tuned above 2.6,
  so the barrier always has a contact to act on.

### SU-10 Engaged lines no longer touch: melee comes from charges (Assumption, consequence of SU-07)
- Spec §4.3-§4.5.
- At a 2.0 gap the front soldiers of two engaged lines are about 2.6 units apart, beyond `ContactDistance` (0.85).
  Soldiers only meet when they charge (charge range 7.8 soldier-to-soldier, so the gap is always in reach). Passive
  contact clashes between standing lines no longer happen. Tune `EngagementGap` down if lines should grind together.

### SU-11 Retreat distance is a fixed TroopTuning value in units (Decision, Q-34)
- Spec §5.5 step 2, §4.9 Surrounded, §7.2 `RetreatDistance`.
- Replaces `RetreatDistance = 1.0 × TroopRadius` (`RetreatDistanceScale`) with **`TroopTuning.RetreatDistance`**,
  default **5.0** units (equal to the old value for the radius-5 troops in use), the same for every troop.
- Used for the retreat itself, the shared-retreat chain (falloff still applies per hop) and the Surrounded path length.
- Any value set on the old `RetreatDistanceScale` in `Troop Tuning.asset` is no longer read.

### SU-12 Retreat is a rear-to-front wave (Decision, spec §2.3, §5.5 step 1)
- When a troop retreats, the soldiers furthest from the attacker fall back first; the others follow with a delay that
  grows toward the front, so the troop stretches and closes up again (accordion).
- New tuning: `RetreatWaveDelaySeconds` (default **0.6 s**: delay between the rearmost and the frontmost soldier
  starting) and `RetreatSoldierMoveSeconds` (default **1.2 s**: time each soldier takes to reach its new place).
- Delay is proportional to how far forward the soldier stands along the attack axis.

### SU-13 Retreat motion has no overshoot and return (Assumption, replaces D-31 timing)
- Spec §5.5; Decisions D-31; Handoff issue-03.
- Before: the pivot jumped back and every soldier headed for the new line at once; the per-soldier delay only staggered
  an extra fall-back past the new line (to about 2× the distance) followed by a return. That hid the wave and caused
  the overshoot in issue-03.
- Now: each soldier holds the ground it stood on until its start time, then eases straight to its place in the new
  line. No hold, no return.
- Retreat (Fleeing) length is now `RetreatWaveDelaySeconds + RetreatSoldierMoveSeconds + 0.5 s settle` = **2.3 s** with
  the defaults (was 3.14 s). The troop cannot break again until it ends (D-30 unchanged).

---

## 2026-10-27 (1): Line fill and battle UI

### SU-01 Battle lines fill their length (Decision, spec §2.1, §3.3)
- In Line and Chain, soldiers are spread evenly along the line instead of bunching at its middle.
- Soldiers still have no slots: each frame the living soldiers are sorted by position along the line core and grouped
  into files of `Ranks` soldiers (`Ranks = ceil(soldiers / floor(L / LineSpacing))`, spec §3.3); each file is pulled
  toward an evenly spaced point along the core. Depth within a file comes from separation and the half-width band.

### SU-02 Line fill details (Assumption)
- Pull strength is `LineFillGain = 0.8` × the field attraction strength (constant in `SoldierSteering`).
- Chargers, followers and retreating soldiers keep their place in the sort (the line does not reshuffle while they are
  away) but are not pulled. Columns, blobs and routs are unchanged.
- Linked ends keep the joint clearance: the fill spans the core after clearance.

### SU-03 Help panel at top centre, 3× size (Decision, spec §6 UI)
- The controls help moves from bottom left to top centre, with 3× text (36 px) and up to 3× width (1440 px). It is
  capped so it never covers the right-hand status panel, and wraps.

### SU-04 Unit card shows the selected troop (Decision, spec §6 UI)
- The bottom-left unit card shows the first selected troop: name, portrait and stats (soldiers alive/total, Move,
  BaseMorale, Discipline; per soldier type: Speed, Defence (+armour), Strength, Support, Charge, Aggression, HP,
  weapons with skill, armour). Hidden while nothing is selected.

### SU-05 Unit card fallbacks (Assumption)
- Name: the troop's legacy stat block name, otherwise its GameObject name.
- Portrait: the legacy stat block portrait, otherwise the sprite of the most numerous soldier type, in army colour.

### SU-06 Status panel (Decision, spec §6 UI)
- The right-hand panel (F3) shows per selected troop: formation (with line length, links, ordered or threat-facing),
  status (Idle, Moving, Pursuing, Holding line, Engaged, Retreating, Routed), Effective Morale vs initial, Shock with
  its break level and decay rate, velocity, soldiers, and nearby or engaged enemies. F5 adds the debug values.
