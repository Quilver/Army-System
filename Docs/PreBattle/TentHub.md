# Pre-Battle Tent Hub: Specification v0.1

Status: approved 2026-11-01, implemented (SU-49). Build the scene with **Leveraged Wars > Pre-Battle > Build Tent Hub**.

Replaces the tabbed Battle Preparation menu (`Assets/Campaign/UI/Battle prep menus/`: Briefing, Unit Selection, Sell,
Buy tabs + `ToBattleButton`). The pre-battle phase now takes place inside Prince Julian's command tent as an in-place
diegetic menu (inspired by Shadow of the Horned Rat). Layout reference: the tent SVG mock-up (1280 x 720).

Tags: **[Assumption]** = filled in to make the spec buildable, confirm or replace. **[PB-Qn]** = open question (§9).
**Wired** = backed by an existing system. **Stub** = clickable, opens a placeholder panel; system to be designed later.

---

## 1. Flow

| State | Entered by | What happens | Leaves to |
|---|---|---|---|
| **Briefing** | Scene load | Mission briefing as Fire Emblem-style dialogue boxes (speaker name, text, left/right portrait) over the dimmed tent. Click / any key advances. | Hub, after the last line |
| **Hub** | Briefing ends; a panel closes | Player hovers and clicks people and objects in the tent. | Panel; Launch |
| **Panel** | Clicking a hotspot | One overlay panel over the dimmed tent. Hub hotspots do not react. Esc / right-click / close button returns to Hub. | Hub; Launch (from the Map panel only) |
| **Launch** | Map panel "March Out", or Tent Flaps | Runs the deployment check (§5). Valid: loads the level scene as today. Invalid: nothing loads, the reason is shown. | Level scene |

- Briefing can be skipped (Esc) and replayed from the Map's mission page **[Assumption, PB-Q3]**.
- Only one panel open at a time. No panel opens another, except Debts (two pages) and Map (two pages).

## 2. Layout & Hotspots

Zones run left to right in the order the player is expected to use them: Logistics -> Court & Deployment -> March Out.
Zone header pills and callout boxes in the mock-up are documentation only and are not drawn in game.

| #   | Object                     | Zone / position                                                               | Hover                                                                          | Click                                               | Status                                      |
| --- | -------------------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | --------------------------------------------------- | ------------------------------------------- |
| 1   | **Gold Chest**             | Left, front edge of the merchant's carpet, lid ajar with coins                | Tooltip: exact gold balance and prestige (current/max) **[Assumption, PB-Q6]** | None **[PB-Q1]**                                    | Wired                                       |
| 2   | **Weapon Rack**            | Left, back-left of carpet                                                     | Name label                                                                     | Opens **Inventory** panel: army's current equipment | Stub                                        |
| 3   | **Merchant**               | Left, back-right of carpet, beside the rack                                   | Name label                                                                     | Opens **Shop** panel: buy / sell equipment          | Stub                                        |
| 4   | **Debts Stack**            | Left, on a raised campaign desk in front of the merchant (large click target) | Name label + count of open repayments                                          | Opens **Debts** panel (§4.1)                        | Wired                                       |
| 5   | **Court: Tactician**       | Centre, leaning behind the war table over the map                             | Name label                                                                     | Talk (dialogue box)                                 | Stub (dialogue wired if a flow is assigned) |
| 6   | **Court: Captain**         | Centre, by the tent pole                                                      | Name label                                                                     | Talk                                                | Stub (as above)                             |
| 7   | **Court: Dispatch-bearer** | Centre-right, on a camp stool holding a sealed dispatch                       | Name label; dispatch glows when a quest is available                           | Talk / quest                                        | Stub (as above)                             |
| 8   | **Roster Ledger**          | Centre, left half of the war table                                            | Name label                                                                     | Opens **Ledger** panel (§4.2)                       | Wired                                       |
| 9   | **Tactical Map**           | Centre, right half of the war table                                           | Name label                                                                     | Opens **Map** panel (§4.3)                          | Wired (pairing Stub)                        |
| 10  | **Tent Flaps**             | Right wall, daylight spilling onto the floor                                  | Readiness text: "March out" or the reason it is blocked (§5)                   | Launch                                              | Wired                                       |

**Draw order (back to front):** tent architecture -> carpet -> weapon rack -> merchant -> captain -> tactician ->
dispatch-bearer -> tent flaps + daylight -> debts desk -> gold chest -> war table (ledger, map).
When hotspots overlap, the front-most one gets the hover (e.g. Debts desk over Merchant).

## 3. Hover, Focus & Input

- Hover: object brightens and gets an outline; a name label appears by the cursor. Hit testing uses the sprite's alpha,
  not its rectangle **[Assumption]**.
- Controller / keyboard: D-pad cycles hotspots in the table order above; focus behaves as hover; confirm = click
  **[Assumption]**. (Existing `UISelectionNavigation` is tab-based and is retired with the tabs.)
- The top line (`TopLineView`: Level, Gold, Prestige) is removed; gold and prestige move to the Gold Chest tooltip, the
  level/mission name to the Map's mission page **[Assumption, PB-Q6]**.

## 4. Panels

All panels: UGUI overlay, title, close button, tent dimmed behind. Existing views are re-hosted, not rewritten.

### 4.1 Debts (Wired; book of taped cards, SU-53)
- Page **Take a Loan**: the mission's loan offers (`PreparationData.buyOrders` -> `SellView`). Needs enough prestige;
  gives gold, costs prestige, adds a repayment order. Can be undone while in the tent.
- Page **Repay**: open repayment orders (`CampaignData.BuyOrders` -> `BuyView`). Costs the order's gold. Can be undone
  while in the tent.

### 4.2 Ledger (Wired, read-only; book, one character per page, SU-54)
- Every roster character: portrait, name, level, XP bar, stats, cost to field (`PCWrapper` data, as `UnitSelectionView`
  shows today). No selecting here; deployment lives on the Map.

### 4.3 Map (Wired; pairing Stub; roster on CampaignCharacter, gold remaining / spent, SU-52, SU-55)
- Page **Mission**: briefing text and image (`PreparationData.briefing`, `.image`), mission/level name, "Replay briefing".
- Page **Deployment**: roster list with cost; click to deploy / undeploy (today's `UnitSelectionView.Select` rules,
  gold deducted and refunded). Deployed characters show as troop cards.
- **Pairing (Stub):** each deployed troop card shows an empty second-character slot, disabled with the tooltip "Pairing
  coming soon". Every deployed character fights as its own troop until pairing is built (spec §1.2) **[PB-Q4]**.
- **March Out** button: same as the Tent Flaps.

### 4.4 Inventory, Shop (Stub)
- Placeholder panel: title, flavour line, close button.

### 4.5 Court dialogue (Stub)
- Clicking a court member plays their `DialogueFlow` for this mission in the briefing dialogue box, if one is assigned;
  otherwise a single default line. Relationships and quests are not built.

## 5. Rules carried over unchanged

- Deploying a character costs `CostToField` gold; undeploying refunds it. A character that costs more than the current
  gold cannot be deployed.
- Launch needs **at least 2** deployed characters; **at most 5** can be deployed (SU-50). Blocked messages (Tent Flaps hover, Map button):
  gold is 0 -> "Needs money, get a loan"; none deployed -> "Select units for battle"; one deployed -> "Need at least 2
  units for battle".
- Launch loads build index `CurrentLevel + 2`, as `ToBattleButton.NextLevel` does now. Scene order is unchanged.
- Gold, prestige and orders change immediately in `CampaignData`, as now.

## 6. Data Changes

- `PreparationData` (per mission) gains: `DialogueFlow briefingFlow`; optional `DialogueFlow` per court member;
  `bool questAvailable` (drives the dispatch glow). `briefing` and `image` stay for the Map's mission page.
- The dialogue box is pulled out of `Cutscene.CutScene` into a reusable component. `CutScene` today freezes
  `Time.timeScale` and needs the battle `CameraControls`; the tent has neither **[Assumption]**.

## 7. Scene & Art

- Same scene: `Resources/Levels/Menu levels/Battle Preparation.unity`, build index 1.
- Rendering: one Screen Space - Camera UGUI canvas, 1280 x 720 reference, scale with screen, 16:9. One `Image` per
  hotspot with alpha hit testing (sprites need Read/Write enabled) **[Assumption]**.
- Placeholder art: the SVG mock-up split into layered PNG sprites at 1280 x 720 coordinates: one background (tent,
  floor, carpet, daylight) and one sprite per hotspot. Callouts, zone pills and the "HOVER: GOLD" pill are dropped.
  Final art replaces sprites one for one.

## 8. Removed / Retired

- Tab buttons, `PreparationContoller` tab switching, `ITab`, `TopLineView`, `UISelectionNavigation`, the separate
  `ToBattleButton` (its rules move to §5). `BuyView`, `SellView`, `UnitSelectionView` are reused inside panels.

## 9. Open Questions

**PB-Q1. Gold Chest click.** Does clicking the chest do anything (e.g. open Debts, or a finance summary)?
Assumption: hover only.

**PB-Q2. Loan repayment formula (existing bug).** `SellOrders.Buy()` computes `(int) percent * gold`: the rate is
truncated to an integer before multiplying, so a 50 % loan repays 0 and a 150 % loan repays 1 x gold. Should repayment be
`gold x rate` (rate 150 % -> repay 1.5 x) or `gold x (1 + rate)` (interest on top)? `PrestigeReward` is also never set
by `Buy()` and the prestige refund on repaying is commented out in `BuyView`. Assumption: leave untouched until answered.

**PB-Q3. Briefing skip and replay.** Assumption: Esc skips; "Replay briefing" on the Map's mission page.

**PB-Q4. Pairing and the roster migration.** The campaign roster and deployment still use the old `PCWrapper` /
`BattleReport.DeployedCharacters` and spawn old Unit prefabs. Pairing into Composite Troops (spec §1.2) needs
`CampaignCharacter` + runtime `TroopProfile`s. Should pairing wait for the roster to move to `CampaignCharacter`?
Assumption: yes; the Map stub keeps the `PCWrapper` flow.

**PB-Q5. Court members.** Are the three the same people every mission (named retinue), or set per mission?
Assumption: three fixed slots, identity and dialogue set per mission in `PreparationData`.

**PB-Q6. Where do prestige and level show?** Assumption: prestige in the Gold Chest tooltip; level/mission name on the
Map's mission page.

**PB-Q7. Leaving the tent without fighting.** Is there a way back to the campaign/main menu from the tent?
Assumption: no (as now).
