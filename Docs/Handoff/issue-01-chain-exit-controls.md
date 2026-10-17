# Issue 01: Chains Missing Controls for Exiting

**Status:** Not confirmed. **Logging:** Chain, Pivot (see [movement-testing.md](movement-testing.md)).

## Summary

Once allied troops link into a chain, the player has no way to take one troop out of it. Links are created and
broken automatically by distance, and player orders do not affect them.

## Symptom

A troop in a chain that is ordered to move away is held back by its links, or only leaves after it has dragged far
enough to break them.

## Repro

1. Place two or three allied troops side by side near an enemy so they form a chain.
2. Select one member and order it to move away from the chain (move order or line order).
3. Watch its pivot and the link state.

## Suspected cause

- **Links form automatically.** Any two allies within the sum of their radii are linked when their group has a
  threat-facing troop (`Formation/LineLinkAssigner.cs:43`, `Formation/AlliedLineConnector.cs:13`).
- **Links only break by distance or by losing the anchor.** `Prune` clears a link when pivots are more than
  `LinkBreakDistanceScale` (1.25) × the radius sum apart (`Formation/LineLinkAssigner.cs:7, 22`).
  `ClearGroupLinks` clears them when the group has no threat-facing troop (`Formation/AlliedLineConnector.cs:15`).
  Distance never breaks a link while either troop is retreating.
- **Orders do not touch links.** `Troop.MoveTo`, `Troop.Pursue` and `Troop.OrderLine` cancel line orders and set
  destinations but never clear links (`Runtime/Troop.cs:215-235`).
- **Tension resists leaving.** `AlliedLinkTension` applies a spring of up to `LinkMaxForce` (10.4) plus velocity
  matching (`Pressure/AlliedLinkTension.cs:7-32`), which pulls the leaving troop back through `LinePushVelocity`.
- **Relinking.** Even after a link breaks, the troop relinks on the next frame if it is still within the radius sum.

## Logging needed

- Chain events: link created and cleared, with the reason (distance, reciprocal mismatch, group lost anchor).
- Chain samples: tension vector and tension force on the leaving troop.
- Pivot: the leaving troop's `_pivotVelocity`, `LinePushVelocity` and displacement.

## Confirmation criteria

Confirmed if, after a move order away from the chain, either:
- the troop's net displacement per second is clearly below its order speed while linked, because the tension
  force opposes `_pivotVelocity`; or
- the link only clears once the 1.25 × radius sum distance is reached, or the troop relinks right after.

## Fix direction

- When a player issues `MoveTo`, `Pursue` or `OrderLine`, clear the troop's links (and the reciprocal links).
- Stop the troop relinking while it has an active player order, or for a short cooldown after the order.
  `LineLinkAssigner.LinkAvailableEnds` and `AlliedLineConnector.TakeConnectedGroup` need to respect this.
- Optional: an explicit "detach" input. Add it in `PlayerInputMap.inputactions` and let Unity regenerate
  `PlayerInputMap.cs`; route it through `TroopPlayerOrders`.
- Consider whether a line order that places troops side by side should form a chain on arrival (it should keep
  doing so).

## Verification

- An ordered troop leaves the chain at its normal speed with no pull-back, and does not relink until its order
  completes.
- The remaining members re-form a valid chain (no dangling links, correct joints).
- Normal chain forming is unchanged for troops with no orders.
- Re-run the Chain row and the Enter/exit chain column of the results matrix.
