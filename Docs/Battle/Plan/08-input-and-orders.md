# Plan 08: Input, Orders & Chain Manipulation

**Goal:** Move input thresholds to screen pixels, make selection toggle with Shift, classify right-click orders as Pursue,
Move or Line, implement the player-facing chain rules (envelope, exit on release, ghost tether, Alt override), and
implement the line-drag facing and linking rules.

**Spec sections:** §6.1–§6.3.
**Depends on:** Phase 04 (pursue and Assault), phase 06 (line geometry), phase 07 (chain manipulation). Selection changes
(6a) can start after phase 01.
**Blocking questions:** Q-17 (line-drag facing), Q-47 (input units), Q-19 (drag links), Q-14 (move arrival),
Q-22 and Q-23 (ChainSuppressed and Alt). Q-18 and Q-48 have defaults.


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

### 8a. Selection

- [ ] Change `TroopPlayerOrders` thresholds to screen pixels. Box select: `BoxSelectMinDragPixels = 8` (already px).
  Add `OrderDragMinPixels = 8` (Q-47).
- [ ] **Shift toggles** in `TroopSelection`. Current code adds only (`Add` skips selected troops). A selected troop
  clicked with Shift is deselected.
- [ ] **Box select (Q-48).** Select a troop if its pivot or any living soldier is inside the box. Current behaviour kept.
- [ ] **Click radius.** Current click uses `_clickSelectionRadius = 0.6` world units. Change to a screen-space radius so
  it does not depend on zoom.
- [ ] Remove the `OrderDragMinDistance = 1.5` world-unit threshold.

### 8b. Order classification

- [ ] On right-button release, classify the order in `TroopOrderPlanner`:
  - Start and end pixel distance below `OrderDragMinPixels` and the point is an enemy → `Pursue`.
  - Below threshold and empty ground → `Move`.
  - At or above threshold → `Line`.
- [ ] **Move for several troops.** Keep relative offsets. Issue `MoveTo` for every selected troop (spec §6.2). Current
  code issues `OrderLine` with each troop's axis, which changes facing toward the enemy. Remove that and record in Q-14.
- [ ] **Arrival.** Move ends in Idle (blob) (Q-14).

### 8c. Pursue

- [ ] `Troop.Pursue` already cancels line orders and sets the mover target. Keep it.
- [ ] Pursue ends per Q-24. Until answered: target dies or the order is replaced.
- [ ] The pursuing troop refreshes Assault when it re-engages a retreated enemy (phase 04, 4f).

### 8d. Line orders

- [ ] **Facing (Q-17).** Front = normal on the side of the nearest enemy troop. No enemy: right-hand normal of
  `T = D_end − D_start`. Replace the current "flip if enemy behind" logic (`TroopLineOrder.cs:45–47`) with this rule.
- [ ] **Single troop.** Marches to the midpoint of the drag, turns to the facing, and holds the line (`HasIdleLine`).
  The line length is the drag length, clamped to `[0.7, 3.0] × L_nat`.
- [ ] **Multiple troops.**
  - Order along the drag direction (Q-18). Replace the sort in `TroopOrderPlanner` lines 35–42 if needed.
  - Split the span across N troops. Each troop's length is its share, clamped to `[0.7, 3.0] × L_nat`.
  - Adjacent troops' endpoints meet at the joint.
  - **Links (Q-19).** Until answered, rely on automatic attach (phase 07). With explicit links, call the chain
    link API for each adjacent pair.
- [ ] **Ghost preview.** While RMB is held, draw each troop's projected footprint, facing arrows and shared joints in
  `TroopOrderOverlay`. Replace the current dots-only preview.

### 8e. Chain manipulation

- [ ] **Envelope check** (`ChainGeometry.WithinEnvelope`) for a single linked troop's destination.
- [ ] **Staying chained.** Destination inside the envelope → the troop keeps its links. The joint hinges. Adjacent lines
  stretch or angle.
- [ ] **Exiting.** Destination outside the envelope → sever the link on mouse release, at the moment the order is issued
  (not when the troop walks past the boundary).
- [ ] **Ghost tether** (in `TroopOrderOverlay`):
  - Solid green/white: inside the envelope.
  - Dashed red: outside the envelope, or Alt is held.
  - Drawn from the previewed footprint's line point to the linked neighbour's line point.
- [ ] **Middle extraction.** Ensure a removed middle troop does not auto-link to its former neighbours.
- [ ] **Multi-select chain move.** When multiple troops from a chain are selected, they move together and keep their
  relative offsets. They are not unlinked.

### 8f. Alt modifier

- [ ] **Alt + move order on a chained troop:** unlink immediately, even inside the envelope.
- [ ] **Alt + move or line-drag on reserves:** set `ChainSuppressed = true` (Q-23 for duration).
- [ ] Add Alt to `TroopPlayerOrders` input reading (Keyboard `leftAltKey` or `rightAltKey`).

### 8g. ChainSuppressed

- [ ] Implement the end condition from Q-22. Default until answered: suppressed until the order completes or
  `ChainSuppressCooldownSeconds = 3.0` after leaving `R_AttachScale`.

### 8h. Overlay

- [ ] Help panel text: update to the new controls (Shift toggle, Alt, ghost tether colours).
- [ ] Selection box: unchanged.
- [ ] Line preview: replace the orange clamp colour with the new clamp state (`[0.7, 3.0] × L_nat`).

## Files

- New: `Input/ChainOrderRules.cs` (pure, uses `ChainGeometry`), `Input/OrderClassifier.cs` (pure).
- Changed: `Input/TroopPlayerOrders.cs`, `Input/TroopSelection.cs`, `Input/TroopOrderPlanner.cs`, `Input/TroopOrderOverlay.cs`,
  `Input/TroopScreenSpace.cs`, `Runtime/TroopLineOrder.cs` (facing rule), `Runtime/Troop.cs` (Alt suppression, chain unlink
  API calls).

## Acceptance Criteria

- [ ] EditMode tests:
  - `OrderClassifierTests`: 7 px drag on enemy → Pursue; 7 px drag on ground → Move; 9 px drag → Line.
  - `ChainOrderRulesTests`: destination inside/outside envelope; Alt override.
- [ ] Manual checklist in `Level 1 Troop Test` (record results in the phase PR):
  - [ ] Shift click toggles a troop off.
  - [ ] Box select picks up troops with any soldier inside.
  - [ ] Right click on enemy: all selected troops pursue.
  - [ ] Right click on ground with 3 troops: troops keep offsets and arrive Idle.
  - [ ] Drag a line with one troop: it faces the enemy side.
  - [ ] Drag a line with three troops linked: adjacent troops show shared joints.
  - [ ] Single chained troop moved inside the envelope stays linked.
  - [ ] Single chained troop moved outside the envelope unlinks on release.
  - [ ] Ghost tether is solid inside, dashed outside, and dashed while Alt is held.
  - [ ] Alt + move on reserves prevents linking to the chain's wing.
- [ ] No `OrderDragMinDistance` world-unit threshold remains.

## Risks

- Pixel thresholds change behaviour at different zoom levels. Confirm 8 px is a good value at the default camera.
- The overlay runs every frame. Keep the ghost preview to one segment per troop.
- Explicit links from drags (Q-19) create links the player did not directly see form. Test with the Q-19 answer.
