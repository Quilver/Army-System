# Issue 02: Snapping into Chains When Flanking Breaks the Surround

**Status:** Not confirmed. **Logging:** Chain, Pivot, Engagement (see [movement-testing.md](movement-testing.md)).

## Summary

An allied troop moving round an enemy to flank or surround it gets grouped with, linked to and snapped into the
line of a nearby ally. Its line is re-aimed and its ends are moved to a shared joint, which pulls it out of the
flanking position.

## Symptom

During a flank or surround, the flanking troop's line swings round to join its neighbour's line instead of facing
the enemy's side or rear. The surround collapses into one long line.

## Repro

1. Engage an enemy troop from the front with one allied troop.
2. Order a second allied troop to pursue the same enemy from its side, so it passes close to the first troop.
3. Optionally add a third troop from the rear (Surrounded edge case).
4. Watch the flanking troop's formation and links as it closes.

## Suspected cause

- **Grouping is by distance only.** `TakeConnectedGroup` adds any ally within the radius sum
  (`Formation/AlliedLineConnector.cs:44`).
- **Linking is by distance only.** `LinkAvailableEnds` links any pair within the radius sum and picks the closest
  free ends, without checking facings or orders (`Formation/LineLinkAssigner.cs:43-70`).
- **Re-aiming and snapping move the line.** `AimEndsAtMiddleTroops` re-aims a neighbour's segment at a middle troop,
  and `SnapLinkedEnds` moves both ends to their midpoint and publishes the formation
  (`Formation/LineEndAimer.cs:11-89`).
- **Tension pulls the pivots together.** `AlliedLinkTension` then pulls the linked troops toward the joint
  (`Pressure/AlliedLinkTension.cs`).
- Around an enemy, troops on adjacent sides are close enough to group, so a surround is easy to turn into a chain.

## Logging needed

- Chain events, with both troops' threat-facing directions, the angle between them, and whether either has a move
  or pursue order.
- Pivot: displacement and formation start/end changes in the frames after a link forms.
- Engagement: each troop's engaged enemy, to tell whether both troops are engaging the same enemy from different
  sides.

## Confirmation criteria

Confirmed if links form between troops whose threat-facing directions differ by a large angle (for example more
than 45°), or where one troop has an active pursue or move order, and those links are followed by a large change in
that troop's formation facing or a pivot jump.

## Fix direction

Restrict linking (and preferably grouping) to troops that should form one line:
- **Facing check:** only link when the troops' threat-facing directions are within a threshold angle.
- **Side-by-side check:** only link when the offset between pivots lies mostly along the line tangent, not along the
  facing (front to back) direction.
- **Order check:** do not link a troop that is pursuing or has a move order (shared with issue 01).
- Keep existing links to the same rules in `LineLinkAssigner.Prune`, so a link that no longer fits is cleared.

## Verification

- A surround with three or more troops holds: each troop keeps facing the enemy from its own side.
- Normal side-by-side chains still form and hold.
- No new link flicker (link created and cleared repeatedly) at the threshold angle. Add hysteresis if needed.
- Re-run the Chain row and the Flanked and Surrounded columns of the results matrix.
