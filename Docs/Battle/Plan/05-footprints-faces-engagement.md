# Plan 05: Footprints, Faces & Engagement

**Goal:** Give each troop an oriented rectangle footprint, measure engagement and charge range from footprint gaps, lock
facing while engaged, and classify the attacking face of each opposing troop (Front, Left Flank, Right Flank, Rear) so
phases 03 and 04 can apply flank penalties.

**Spec sections:** §3.2, §3.3, §4.1, §4.8.
**Depends on:** Phase 02 (for the damage path). Can run in parallel with phases 03 and 04.
**Blocks:** Phase 04 (flank penalties in clashes), phase 06 (S_Distance), phase 09 (front arc).
**Blocking questions:** Q-31 (face determination), Q-16 (S_Distance and engagement exit), Q-32 (Blob and Column faces).


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

- [ ] **Footprint type** `Rules/TroopFootprint.cs` (pure):
  - Input: centre segment (tangent, length), half-width (furthest soldier from the centre line plus `SoldierRadius`).
  - Output: oriented rectangle with four named faces. Each face has a normal and a segment.
  - Blob and Column troops return a circle (Blob) or capsule (Column) footprint, since they have no rectangle (Q-32).
- [ ] **Footprint gap** `Rules/FootprintGap.cs` (pure): signed distance between two footprints (rect–rect, rect–circle,
  capsule–capsule). Used for `A_Distance`, `S_Distance`, and charge range (once Q-25 is decided).
- [ ] **Compute the half-width from soldiers,** not from the profile. Reuse `TroopSegmentPressureRules.LineHalfWidth` logic,
  moved into a shared helper. Cache per frame.
- [ ] **Engagement.** Replace the centre-line segment distance in `ThreatFormationRules.FindEngagements` with footprint gap.
  - Engaged when footprint gap ≤ `A_Distance` (5.2).
  - Exit rule per Q-16. Until answered, no hysteresis for engagement entry and exit (spec §4.1).
- [ ] **Facing lock.** While engaged, `TroopFormationState` does not rotate the facing. Remove the 100° freeze rule for
  engaged troops, since the lock replaces it.
- [ ] **Face classification** `Rules/FaceClassifier.cs` (pure). For each opposing troop:
  - Direction from the defender's pivot to the attacker's nearest footprint point.
  - Angle from the defender's facing (Q-31 proposal): Front ≤ 45°, Flank 45°–135° (left or right by sign), Rear > 135°.
  - Store per opponent, not per troop, so each clash can look up the face (Q-31 wording).
- [ ] **Store the face per engagement.** Add `EngagementRecord` (opponent troop, face, footprint gap) on `Troop`. Updated
  each frame.
- [ ] **Expose the face to clashes.** `ClashResolver` and `SupportRules` look up the face for the clash's two troops.
  - Flanked clash: this troop gets 0 support, and damage taken in the clash has Shock × 3 (`FlankShockMultiplier`).
  - Front clash: normal.
- [ ] **Remove the flank damage multiplier** (`ClashResolver.FlankMultiplier`). The spec removes it (Q-38).
- [ ] **Surround input.** Expose the rear-face state (blocked or not) for phase 03's surround test.
- [ ] **Debug view.** Extend `TroopDebugOverlay` (or `TroopCombatGizmos`) to draw faces, the attacking face label on each
  engagement, and the footprint gap.

## Files

- New: `Rules/TroopFootprint.cs`, `Rules/FootprintGap.cs`, `Rules/FaceClassifier.cs`, `Runtime/EngagementRecord.cs`.
- Changed: `Formation/ThreatFormationRules.cs`, `Runtime/TroopFormationState.cs` (facing lock), `Combat/ClashResolver.cs`
  (face lookup, flank Shock), `Combat/TroopCombatGizmos.cs`, `Diagnostics/TroopDebugOverlay.cs`,
  `Pressure/TroopSegmentPressureRules.cs` (use shared half-width and gap helpers).

## Acceptance Criteria

- [ ] EditMode tests:
  - `FootprintGapTests`: two rectangles 1 unit apart along their normals → gap 1. Overlapping → negative gap.
  - `FaceClassifierTests`: attacker straight ahead → Front. 90° left → Left Flank. 180° → Rear. Boundary values at 45° and 135°.
- [ ] PlayMode: an engaged troop's facing does not change while engaged (log facing for 10 s).
- [ ] PlayMode: a flanking attack shows the attacking face label in the debug overlay.
- [ ] `Level 1 Troop Test`: the existing engagement distance behaves within 1 unit of before, once Q-16 is answered.
- [ ] No `FlankMultiplier` references remain (grep).

## Risks

- Footprint gap is more expensive than a segment distance. Cache footprints per frame and use the spatial grid for pairs.
- Face classification uses the nearest footprint point, which changes when troops move. Check that faces do not flicker
  between Front and Flank for a stationary pair.
