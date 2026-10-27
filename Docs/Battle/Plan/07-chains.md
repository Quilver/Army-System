# Plan 07: Chains (Linked Battle Lines)

**Goal:** Rebuild linking around endpoint-to-endpoint attach, an inner-angle hinge, a stretch-based break, acyclic chains,
shared retreat with falloff, and the edge cases in spec §3.4.1.

**Spec sections:** §3.4, §3.4.1, §6.3 (chain manipulation).
**Depends on:** Phase 06 (geometry), phase 03 (rout), phase 05 (engagement for pinned anchors).
**Blocks:** the chain part of phase 08.
**Blocking questions:** Q-13 (hinge angles), Q-20 (engaged neighbours and sympathetic retreat), Q-21 (falloff),
Q-22 (ChainSuppressed).


**Status (2026-10-26):** Code complete and tested with the Unity CLI (compile clean, 120 EditMode tests passing). See `Docs/Battle/Decisions.md` (Phases 02–10) for decisions D-30 to D-51 and the remaining manual checks.

## Tasks

### 7a. Link rules (pure)

- [ ] `Rules/ChainGeometry.cs`:
  - `InnerHingeAngle(J, pivotA, otherEndA, pivotB, otherEndB)`: angle at J between the two troops' lines (180° = straight).
  - `RequiredLength(...)`: length each troop needs to keep J within the envelope.
  - `WithinEnvelope(...)`: stretch `≤ 3.0 × L_nat` and inner angle `≥ 65°`.
- [ ] `Rules/ChainLinkRules.cs`:
  - Attach: free endpoints within `R_AttachScale × L_nat` (0.25), complementary wings (`A.Right ↔ B.Left` and reverse), and
    `F_A · F_B > 0`.
  - Break: stretch above `3.0 × L_nat` (required length, not endpoint distance), inner angle below 65°.
  - Tie-break: shortest endpoint distance, then lower troop instance ID.

### 7b. Linking (`Formation/`)

- [ ] **Replace the grouping in `AlliedLineConnector.Connect`.** Currently same-team troops group by pivot distance below
  `rA + rB`, and a group needs a threat-facing member. New: group by existing links only. Free endpoints are checked
  directly.
- [ ] **Replace `LineLinkAssigner`.** Use endpoint-to-endpoint attach from 7a. Remove `LinkBreakDistanceScale = 1.25` and
  pivot-distance breaks.
- [ ] **Acyclic check.** Before adding a link, use union-find (or a walk) over the current links to test whether the two
  troops are already in one component. If so, reject.
- [ ] **No link stealing.** Only free endpoints are candidates. Already-linked endpoints are never reassigned.
- [ ] **Tie-break.** In a same-tick race for one endpoint, sort candidates by `(endpoint distance, lower instance ID)` and
  take the first.
- [ ] **Middle extraction.** A removed middle troop leaves two free inner endpoints. Do not auto-link across the gap, since
  auto-linking only uses `R_AttachScale`.

### 7c. Hinge and shared joint

- [ ] **Shared joint J.** Weighted average of the two natural endpoints. Clamp each troop's line length to
  `[0.7, 3.0] × L_nat`.
- [ ] **Replace `LineEndAimer`'s joint** (midpoint of unsnapped endpoints) with the weighted-average J from 7a. Keep the
  aiming of middle troops' neighbours toward J, not the pivot, or record the change in the spec if it differs.
- [ ] **Store the pre-snap gap** as tension (existing `TroopEndpointLink.Tension`), for `AlliedLinkTension`.

### 7d. Breaking

- [ ] Each frame, for each link, check: stretch, inner angle, obstruction, rout (phase 03), and player unlink (phase 08).
- [ ] **Obstruction.** Segment check between the two pivots through J and along each troop's line. If impassable terrain
  bisects the segment or blocks J, sever the link. Use `Physics2D.Linecast` or the same mask as `SoldierMotionConstraints`.
- [ ] **Rout.** Both links of a routing troop break at once (`EnterRout` calls `ChainLinks.BreakAll(troop)`).
- [ ] Remove distance-based break while retreating (`LineLinkAssigner` line 19–22). Rely on the stretch rule. Shared retreat
  (7e) keeps the stretch in range.

### 7e. Shared retreat

- [ ] When troop T retreats by `d`:
  - Each neighbour's shared joint pulls back by `d` immediately (joint moves toward the neighbour's pivot).
  - A neighbour that is not engaged retreats sympathetically when its stretch exceeds `1.4 × L_nat` or its hinge bend
    (`180° − inner`) exceeds 30° (Q-13).
  - The sympathetic distance is `d × (1 − 0.30)` per hop (Q-21). Propagate along the chain until the value is below a
    minimum step or the chain ends.
- [ ] **Engaged neighbours do not retreat** (Q-20 proposed). Their link is held by tension (7f).
- [ ] Shared retreat stops at a broken link.

### 7f. Tension & pinned anchor

- [ ] `AlliedLinkTension`: apply the spring to the **unengaged** side only. If the troop is engaged, do not move it.
- [ ] If the stretch or hinge limit is exceeded and one side is engaged, snap the link (do not drag the engaged troop).
- [ ] Remove the velocity-matching term when one side is engaged (it can push an engaged troop).

### 7g. Endpoint relax

- [ ] When a link breaks while the endpoint is stretched, the free endpoint relaxes toward `L_nat` at
  `ChainEndpointRelaxSpeed = 2.0` units/s. Store the relax target on the troop and move the endpoint each frame until
  it reaches natural length.

### 7h. ChainSuppressed (wait for Q-22)

- [ ] Add a `ChainSuppressed` flag on `Troop` with an end condition from Q-22.
- [ ] Until answered, implement the spec's default: suppressed until the order completes or 3 s after leaving the attach
  radius.

### 7i. Remove the old model

- [ ] Remove the 2-troop `LineEndAimer` special case if the new joint handles it.
- [ ] Remove `LinkedEndRetreat` (soldiers near a linked end retreat 15%). Replace with sympathetic retreat (7e) and
  the joint clearance (phase 06).

## Files

- New: `Rules/ChainGeometry.cs`, `Rules/ChainLinkRules.cs`, `Rules/ChainTopology.cs` (union-find), `Runtime/ChainRelax.cs`.
- Changed: `Formation/AlliedLineConnector.cs`, `Formation/LineLinkAssigner.cs`, `Formation/LineEndAimer.cs`,
  `Formation/LineSegmentBuilder.cs`, `Pressure/AlliedLinkTension.cs`, `Runtime/TroopLineLinks.cs`, `Runtime/FaultSlipMotion.cs`
  (shared retreat hook), `Runtime/LinkedEndRetreat.cs` (removed), `Runtime/Troop.cs`.

## Acceptance Criteria

- [ ] EditMode tests:
  - `ChainGeometryTests`: straight chain inner angle 180°. Right-angle chain 90°. A fold to 60° fails the envelope.
  - `ChainLinkRulesTests`: endpoints at 0.24 L_nat attach, at 0.26 do not. Complementary wings only. Dot product 0 fails.
  - `ChainTopologyTests`: A–B, B–C, then C–A is rejected.
- [ ] PlayMode scenarios (one scene each, from spec §3.4.1):
  - Mid-chain rout (`1–2–3–4–5`): links split into `1–2` and `4–5`. No shared retreat crosses the gap.
  - Endpoint relax: a stretched endpoint shrinks to `L_nat` at 2 u/s when its link breaks.
  - Pinned anchor: an engaged end does not move when the other side marches away. The link snaps at 3.0 L_nat.
  - Obstruction: a wall placed across J breaks the link within one frame.
  - Shared retreat: a 3-troop chain. Troop 1 retreats 10 units; troop 2 moves 7 units (30% falloff), troop 3 moves 4.9.
- [ ] Middle extraction: removing B from A–B–C leaves A and C unlinked, even when they are 2 L_nat apart.
- [ ] No links form between troops of different teams (grep and test).

## Risks

- This is the largest behavioural change. Build the scenario scenes first, then change the code.
- Shared retreat with falloff can produce long chains of small movements. Clamp the sympathetic distance to a maximum.
- Tension without engagement checks dragged troops out of melee in the current code. Confirm the new engaged check with a test.
