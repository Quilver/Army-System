# Leveraged Wars: Battle System Specification (v0.10)

> **Status (2026-11-08):** v0.10 folds every designer **Decision** recorded in `SpecificationUpdate.md` up to SU-135,
> and the answers to the docs review, into the spec. It replaces v0.9.
> - Text that rests on an unconfirmed **Assumption** is tagged **[A: SU-n]**. Those are listed for review in
>   `AssumptionsReview.md`; once a group is confirmed the tag is removed.
> - Items tagged **[Q-n]** are open and listed in `Questions.md`.
> - Every number in §8 is a **tunable variable**: a field on a Battle Tuning asset that can be adjusted in the
>   Inspector. Values elsewhere in this document are the current ones and point to §8.
> - Decision IDs from the Phase 01–10 build that code still cites (D-01, D-03, D-52, D-54, D-55) are listed in
>   Appendix A.

---

## 1. Entities, Stats & Equipment

### 1.1 Entities

- **Troop:** the unit the player or the AI commands. It holds a list of **Characters**, leader first, and spawns their
  soldiers at the start of the battle. The troop's name and portrait are its leader's (SU-118).
- **Soldier:** an autonomous fighter simulated as plain state (`SoldierAgentState`) and steered by its troop's vector
  field. Soldiers have no fixed slots. Each soldier takes its stats and equipment from its own character.
- **Character:** a `CharacterProfile`: one soldier type with its stats, equipment, class and soldier count (Numbers).
  Characters in the player's roster are `CampaignCharacter` assets, which add stat growths (SU-56, D-56).
- **Composite troop:** a troop of two characters, paired in the pre-battle tent by dragging one deployed character's
  card onto another's (SU-117). At most 2 characters per troop. The battle only sees the list of characters.
- **Detachment / Formation (AI):** the enemy AI groups troops into Detachments and gives each a Formation (its role,
  e.g. Guard, Flank). See `Assets/Scripts/Battle/AI/README.md`. A troop's own shape is its *formation mode* (§3.1).

### 1.2 Composite Troop Resolution

| Troop value | Rule |
|---|---|
| `Move` | Lowest soldier `Speed` in the troop (§1.3). |
| `BaseMorale` | `max` of the characters' Morale. |
| `Discipline` | `max` of the characters' Discipline. |
| `InitialSoldierCount` | Sum of the characters' Numbers. Each character spawns its own soldiers. |
| `Frontage` | From the total soldier count (§3.3, SU-117, SU-118). |

### 1.3 Stat Scale

- **Combat stats are integers 1–20, default 5** (`CombatStatLimits`): Speed, Defence, Strength, Support, Charge,
  Aggression, Morale, Discipline and every weapon skill level.
- **HP (MaxHP) is an integer 10–40, default 15.**
- Numbers (soldier count) is not a combat stat and has no range. Its level-up cap is 20.
- **Speed:** `units/s = Speed × SpeedUnitsPerStatPoint` (0.6, so Speed 5 = 3.0 units/s). Acceleration =
  `Speed × AccelerationPerSpeedPoint` (1.6, so Speed 5 = 8 units/s²) (SU-15).
- **Pivot speed** = troop `Move × SpeedUnitsPerStatPoint`. No soldier is slower than its pivot.

### 1.4 Character Stats

| Stat | Scale | Notes |
|---|---|---|
| `Numbers` | integer ≥ 0 | Soldiers the character brings into battle. |
| `Morale` (BaseMorale) | 1–20 | Morale per soldier (§5.1). |
| `Discipline` | 1–20 | Nearest same-troop soldiers that may follow a charger (§4.4). Minimum 1 **[Q-58]**. |
| `MaxHP` | 10–40 | Combined pool for Bruise and Bleed (§4.6). |
| `Speed` | 1–20 | Lowest value in a troop sets its `Move`. |
| `Defence` | 1–20 | Mitigation (§4.6). |
| `Strength` | 1–20 | Melee damage. |
| `Support` | 1–20 | Cap on supporting allies in a clash (§4.5). |
| `Charge` | 1–20 | Added to Combat Power while charging (§4.5). |
| `Aggression` | 1–20 | Expected charges per `ChargeWindowSeconds` while the soldier may charge (§4.3). |
| `WeaponSkill[class]` | 1–20 | Up to 2 classes. A class that is not listed is unskilled (0) **[Q-2]**. A listed skill is never 0. |

### 1.5 Levels and Growth (campaign → battle)

- A roster character levels up every **10 XP** (SU-115). Each level rolls every stat (Numbers included) and each listed
  weapon skill against its growth chance (default 20%); a success below the class cap adds +1. Class caps come from the
  character's `CharacterClass` (default 20, HP 40, Numbers 20).
- **Gains are used in battle and carry to the next mission** (SU-132). A deployed roster character fights as a runtime
  copy of its `CampaignCharacter` with its gains added (`RosterCharacter.BattleCharacter()`); a Numbers gain adds a
  soldier.
- **Saving is switched off** while the game is prototyped (`CampaignDataManager.SavingEnabled = false`, SU-132). The
  campaign lives in memory for the play session; nothing is read from or written to `campaign.json`.

### 1.6 Equipment

**Loadout (D-52)**
- A character carries **one melee weapon (required)**, **an optional ranged weapon**, and armour (or none).
- Melee weapon classes: **Sword, Axe, Spear, Mace**. Ranged weapon classes: **Bow, Crossbow, Javelin** (Mace and
  Crossbow: D-03).
- Ranged troops always carry a melee weapon and use it in melee.
- **Thrown weapons** (Javelin class: javelin, pilum, throwing axe) make a **hybrid** troop: it still throws on its own
  (§4.7), but the AI orders it as a melee troop (SU-79). Only Bow and Crossbow make a troop *ranged* for the AI.

**Armour**

| Field | Meaning |
|---|---|
| `Deflect : Bruise : Bleed` | A **ratio** of whole numbers ≥ 0 (SU-89). Each point of damage up to `TotalDefence` rolls one of the three with chance proportional to its part. |
| `DefenceBonus` | Added to the soldier's Defence to give `TotalDefence`. |

- **No armour:** every point rolls against **0 : 3 : 7**, with **no TotalDefence cap** (SU-90).

**Melee weapons**

| Field | Meaning |
|---|---|
| `Damage` | Added to Strength in melee damage. |
| `ChargeBonus` | Added to Charge in Combat Power while charging. |
| `SpecialRules` | A list (SU-90), below. |

**Ranged weapons**

| Field | Meaning |
|---|---|
| `Damage` | Damage per hit (Strength is not added). |
| `Range` | World units. |
| `Accuracy` | Added to skill in the hit chance (§4.7). |
| `FireRate` | Shots per `ActionWindowSeconds` (30 s). |
| `Volley` | Off by default: soldiers shoot on their own timing. On: the soldiers with this weapon shoot together (SU-105). |
| `SpecialRules` | A list (SU-90), below. |

**Special rules (SU-90)**

| Rule | Weapons | Effect |
|---|---|---|
| Spear Support | Melee | The bearer's support range is `SupportRadius × SpearSupportRangeMultiplier` (2) when it supports. |
| Couched Lance | Melee | Total melee damage is doubled while the bearer is charging. |
| Piercing X | Melee, ranged | +X to the Bleed part of the target's armour ratio for this weapon's hits. |
| Bludgeoning X | Melee, ranged | +X to the Bruise part of the target's armour ratio for this weapon's hits. |
| Fire & Engage | Ranged | One volley when the troop enters an engagement and one when it begins a Case B retreat from it (§4.7). |

A rule on the wrong kind of weapon has no effect and logs a warning; repeated Piercing or Bludgeoning entries add up
**[A: SU-90]**.

**Data:** weapon and armour values are set on their assets in `Resources/Troop and Character Profiles/`. Values the
character sheets did not give are placeholders until tuned (Q-54, Q-55). Melee weapon damage was cut by 3 to lengthen
melee (SU-108).

---

## 2. Soldier Simulation & Physics

Soldiers are plain state objects simulated by their troop and drawn by view objects.

### 2.1 Vector Field Steering

Each frame the troop builds a `FieldShape`: a **core segment** and a **half-width**.

- **Attraction:** a soldier that is not charging moves toward the nearest point on the core. Speed grows with distance
  outside the half-width and slows near the core, matching the troop's velocity.
- **Line fill:** in a line, living soldiers are sorted along the core and grouped into files; each file is pulled toward
  an evenly spaced point (`LineFillGain` 0.8) (SU-01, SU-02). Chargers, followers and retreating soldiers keep their
  place in the sort but are not pulled.
- **Composite ranks:** in a line (Line or Chain mode), ranged soldiers are steered to the front and melee soldiers to the
  back while not engaged, reversed while engaged (D-55, `CompositeRankGain`, `CompositeRankDepthFraction`). Both kinds
  are spread along the whole line.
- **Density spreading:** soldiers within `SpreadRadius` (1.2) push each other apart along the shape.
- **No cohesion:** soldiers do not attract each other. Alignment only matches velocity.

### 2.2 Separation, Drive Pressure & Mass

- **Intra-troop separation:** same-troop soldiers keep centres at least `SeparationRadius = 3 × SoldierRadius = 0.9`
  apart (soft repulsion, plus a hard pass of 3 iterations when compressed).
- **Drive fraction:** `DesiredSpeed / MaxSpeed`. A driving soldier is up to `1 + PressureMassPerDrive = 4×` heavier
  than a settled one and shoves settled teammates aside.
- **Cross-troop separation:** soldiers of different troops repel with a distance falloff.
- **Contact:** soldiers of different troops closer than `ContactDistance` (0.85) are pushed apart.

### 2.3 Exemptions & Constraints

- **Chargers, their followers and soldiers rushing back** (§4.3) skip intra-troop separation, density spreading and
  drive pressure in both directions, and may leave the troop radius.
- **Retreating soldiers** follow their retreat schedule (§5.5) and may leave the troop radius.
- **Stunned soldiers** are not snapped back inside the radius; they walk back with the field (SU-107).
- **Troop radius clamp:** other soldiers are pulled back inside `0.92 × TroopRadius` and checked against impassable
  terrain with `CircleCast`. A line longer than the troop is wide widens the clamp to reach its line points (D-36). A
  marching column's clamp reaches its tail and shrinks back at 1.5 units/s after the march **[A: SU-120]**.

### 2.4 Friendly Troops

- **Pivot separation:** friendly pivots keep at least `PivotSeparationScale × (r_A + r_B)` (0.25) apart. Line
  endpoints are not part of this check.
- **Outline separation (SU-94):** friendly troops keep their outlines at least `FriendlyGap` (0.5) apart. Right of way:
  a committed troop (engaged, or with an attack order) over a marching or retreating troop, over a standing one; equals
  share the move **[A: SU-94]**. The troop giving way moves at no more than `FriendlyGiveWaySpeedScale` (0.5) × its march
  speed **[A: SU-94]**. Chained troops (or troops with a pending link) and routed troops are exempt **[A: SU-94]**.
- **Committed troops are never moved by friends** (SU-46): chain tension, pivot separation and outline separation move
  the free friend instead.

---

## 3. Formations, Movement & Geometry

### 3.1 Formation Modes

Ascending priority; a higher mode overrides a lower one.

| Priority | Mode | Trigger |
|---|---|---|
| 1 | **Idle (Blob)** | Default when stationary. |
| 2 | **Column** | Executing a Move or Pursue order. |
| 3 | **Line** | An enemy within `S_Distance`, or a held line order. |
| 4 | **Chain** | Linked to a friendly neighbour while in Line. |
| 5 | **Rout** | A Case A break (§5.3). Final. |

- A chained troop that is moving marches in Line, not Column.
- **Disordered** (§5.5) and **retreating** are states on top of the mode, not modes of their own.
- A troop that already faces a threat keeps its line until the footprint gap passes `S_Distance` plus an exit margin of
  at least `BlobRadiusScale × (r_A + r_B)` **[A: SU-24]**; `SDistanceExitHysteresis` (0) applies when larger.

### 3.2 Idle (Blob) & Column

**Idle:** a disc of `BlobRadiusScale × TroopRadius` (0.85) around the pivot.

**Column (SU-120):**
- **Pathing:** the pivot follows a NavMesh path, rebuilt every `PathRebuildIntervalSeconds` (0.5) or when the
  destination changes. The pivot and destination are snapped onto the NavMesh; a straight line is used only when the
  way is clear (SU-73). A march whose path ends short completes where it ends **[A: SU-73]**.
- **Geometry:** always longer than wide. Lanes = `clamp(ceil(Frontage / 2), 1, 3)`, one fewer while there would be no
  more rows than lanes; rows = `ceil(living / lanes)`; the core is `max(LineSpacing, (rows − 1) × LineSpacing)` long
  and starts `0.05 × TroopRadius` behind the pivot **[A: SU-120]**. Soldiers are spread row by row (column fill).
- **Arrival:** the troop becomes Idle.

### 3.3 Troop Size, Battle Line & Footprints

**Size (SU-103, SU-117, SU-131)**
- **Frontage** (soldiers wide in a line) is set when the troop spawns, from its total soldier count
  (`TroopFrontageRule`): up to 4 soldiers, frontage = soldiers; up to 10, half (rounded up); up to 16, a third; more
  than 16, a quarter (minimum 1).
- **Natural line length** `L_nat = Frontage × LineSpacing` (0.9).
- **Troop radius** (blob, soldier clamp, pivot, deployment and path spacing) =
  `max(L_nat / NaturalLineRadiusScale, radius of a blob holding the soldiers packed at LineSpacing / BlobRadiusScale)
  × TroopSizeScale` (1.8, 0.85, **1.5**). It is not a profile value.

**Line triggers**
1. **Automatic:** an opposing troop within `S_Distance` (footprint gap). The troop faces the threat.
2. **Line order** (`TroopLineOrder`, several troops, §6.2): the troop marches to its place, turns to the ordered facing
   and **holds the line after arrival** until the next order (D-38).

**Geometry**
- Pivot **P** and two line points (left and right wing). Soldiers per rank = `floor(L / LineSpacing)`; ranks =
  `ceil(soldiers / per rank)`.
- **Footprint** (engagement, faces, surround): a Line/Chain troop is a rectangle around its line with four faces
  (Front, Left Flank, Right Flank, Rear). Ahead of and behind the line it reaches the furthest soldier on that side +
  `SoldierRadius`, and at least the rank field **[A: SU-123]**. A marching troop's footprint is a capsule, an Idle or
  routed troop's a circle. Chargers and soldiers rushing back are left out (SU-43).
- **Outline** (what the player sees, clicks and hovers): drawn from the formation the soldiers are steered into, not
  from the soldiers, so it does not jitter (SU-44). Picking: a living soldier within the pick radius, then the outline
  the point is deepest inside, then a pivot (SU-42).
- **Joint clearance:** at a linked end the core is pulled in by `min(JointClearance, JointClearanceFraction × length)`
  (0.45, 0.15).
- **Facing:** locked while engaged (§4.1), except for a pursuer engaged with its target (SU-75).

**Line-vs-line pressure (`TroopSegmentPressureRules`)**
- Engaged opposing lines are held `EngagementGap` apart (footprint gap) by a resistive barrier (SU-07). Restoring force
  is shared in proportion to each pivot's stored intrusion, and the resistive term only opposes a troop that is
  advancing. Pressure contact starts at `max(0.5 × A_Distance, EngagementGap) × 1.25` and releases at × 1.6
  **[A: SU-09]**.
- Because engaged lines stand apart, soldiers meet only when they charge (SU-10, SU-95).

**Pivots stay on the map (SU-74):** every pivot movement is limited to the NavMesh's bounding rectangle (inset by the
blob radius) and clipped against terrain colliders, sliding along obstacles.

### 3.4 Chain (Linked Battle Lines)

**Linking**
- A link forms between a free line point of troop A and a free line point of troop B when the points are within
  `AttachScale × L_nat` (0.25), complementary wings meet (A's right with B's left, or the reverse), there is a clear
  line of sight (D-45), and:
  - **automatic links:** the troops' own facings are within `AutoLinkMaxFacingDeg` (45°) (SU-47), and neither troop has
    an attack order (SU-48);
  - **order links:** a multi-troop line order creates **pending** links between adjacent troops that activate when both
    have arrived and drop if either gets a new order (Q-19, D-39). They use only the stretch and hinge envelope.
- At most `MaxChainLinks` (2) links per troop, one per line point. Endpoint-to-endpoint, not pivot-to-pivot.

**Hinge geometry**
- **Joint J:** the weighted average of the two natural line points; a committed troop's point is pinned (weight 1),
  otherwise the midpoint (D-44, SU-46). Each line stays within `ChainMinStretchScale` – `ChainMaxStretchScale` × `L_nat`
  (0.7–3.0).
- **Inner hinge angle θ** at J (straight = 180°). Minimum `ChainMinInnerAngleDeg` (65°).

**Breaking**
- **Stretch:** the length needed to keep J exceeds `ChainMaxStretchScale × L_nat`.
- **Hinge:** θ < 65°.
- **Facing (automatic links only):** the facings drift more than `AutoLinkBreakFacingDeg` (60°) apart **[A: SU-47]**.
- **Obstruction:** terrain between the pivots through J, or at J.
- **Rout:** a routing troop breaks both its links at once.
- **Player unlink:** an order outside the envelope, or with Alt (§6.3).

**Shared retreat**
- When a troop retreats by `d`, each neighbour's joint pulls back. A neighbour that is **not committed** retreats
  sympathetically (no morale cost) when its stretch would pass `ChainRetreatStretchThreshold × L_nat` (1.4) or its bend
  (180° − θ) would pass `ChainRetreatHingeBendDeg` (30°).
- **Falloff:** each hop passes on `1 − ChainRetreatFalloff` (70%) of the previous distance (10 → 7 → 4.9).
- Committed neighbours (engaged, or attacking) never retreat sympathetically; their link is held by tension and snaps
  at the stretch or hinge limit (SU-46).

#### 3.4.1 Edge Cases & Topology

- **No loops:** a link that would close a loop is rejected (`ChainTopology`). Chains are open paths.
- **No link stealing:** linked line points are never candidates.
- **Tie-break:** the shortest endpoint distance, then the lower troop instance ID.
- **Mid-chain rout:** in `1–2–3–4–5`, if 3 routs, both its links sever, leaving `1–2` and `4–5`.
- **Snap-back damping:** a stretched line point whose link breaks relaxes toward `L_nat` at
  `ChainEndpointRelaxSpeed` (2 units/s).
- **Pinned anchor (`AlliedLinkTension`):** a damped spring acts only on the free side; it never pulls a committed troop
  out of a fight.
- **Engaged chain end:** a troop at the end of a chain does not turn to refuse an attack on its open flank by itself.
  Refusing the flank takes a player (or AI) order. (Movement edge case 18.)
- **ChainSuppressed (SU-130, Q-22 option 2):** a troop detached by a single-troop order, or given an order with Alt,
  is flagged `ChainSuppressed`. It stays unlinked until its line points have been outside every attach radius for
  `ChainSuppressCooldownSeconds` (3 s) in a row. Finishing its order does not end it, so a troop that stops inside a
  neighbour's attach radius stays unlinked until it leaves and the 3 s pass.

---

## 4. Combat

### 4.1 Engagement

- **Engaged:** two opposing troops whose footprint gap is `A_Distance` or less. Once engaged they stay engaged until
  the gap passes `A_Distance + EngagementHoldMargin` (SU-45). The hold is per pair; there is no hold on entry.
- Engaged lines are held `EngagementGap` apart (§3.3).
- **In melee** (`Troop.InMelee`): engaged with an enemy troop **[A: SU-104]**.
- **Facing lock:** an engaged troop does not turn, except a troop pursuing its target, which squares up to the face it
  attacks (SU-75).
- All distances between troops (S_Distance, A_Distance, EngagementGap, surround, ranged targeting) are **footprint
  gaps**, never pivot or centre-line distances (SU-08).

### 4.2 Assault & Pursuit

- **Assault:** a troop with an active Move, Pursue or Line order that enters engagement gets, for
  `AssaultDurationSeconds` (10 s), `+AssaultAggressionBonus` (5) Aggression and `+AssaultChargeBonus` (5) Charge while
  charging. Troops without an order never gain Assault. A fight that stays inside the engagement hold does not
  retrigger it (SU-45).
- **Pursuit:** only by an explicit Pursue order. A pursuing troop that re-engages an enemy that retreated refreshes
  Assault. A Pursue ends when its target is destroyed (routed or wiped out) or the player gives a new order **[Q-24]**.
  A pursuer whose target is within `S_Distance` takes its heading from that target alone and squares up to the face it
  attacks (SU-75).

### 4.3 Charges

- **Who may charge:** only soldiers in the **front rank** (SU-106): one rank's worth of living soldiers, taken as those
  standing furthest toward each enemy troop the troop may charge **[A: SU-106]**. Stunned soldiers never charge.
  `FrontRankChargesOnly` switches the rule.
- **Gate:** a troop's soldiers may start charges only while the troop is within `EngagementGap + ChargeGapTolerance`
  (2 + 0.25, footprint gap) of an enemy troop (SU-95) **[A: SU-95 tolerance]**.
- **Rate:** each eligible soldier charges at an expected `EffectiveAggression / ChargeWindowSeconds` per second (45 s,
  SU-112); each frame the chance is `1 − exp(−rate × dt)`. `EffectiveAggression = Aggression + AssaultAggressionBonus`
  during Assault. No cooldown after a charge, no neighbour contagion.
- **Target:** a random pick among the `ChargeCandidateCount` (3) nearest enemy soldiers of gated troops within reach.
  Reach is `A_Distance` measured between the two soldiers' edges, i.e. `A_Distance + 2 × SoldierRadius` centre to centre
  (SU-95) **[A: SU-95]**. A roll with no target in reach does nothing.
- **Speed:** `ChargeSpeedScale` (2×) normal speed and acceleration.
- **One charge = one clash.** The charge ends on contact (§4.5).
- **Cancel:** the target dies, the charger's troop **routs**, or `ChargeTimeoutSeconds` (3 s) pass. A retreat does not
  cancel a charge (SU-43). No retargeting.
- **Rushing back (SU-43):** when a charge ends, the soldier rushes back at charge speed if its troop is retreating, no
  longer engaged, or the soldier is outside `0.92 × TroopRadius`; otherwise it rejoins the line. It counts as home within
  `LineSpacing` of its band; the return gives up after 6 s **[A: SU-43]**.

### 4.4 Discipline & Charge Support

- When a soldier starts a charge, the troop's `Discipline` nearest living soldiers **of the same troop** are selected;
  each follows with probability `ChargeFollowProbability` (0.5).
- **Followers are passive:** they keep within `0.6 × SupportRadius` of the charger at charge speed, skip separation like
  the charger, provide support to its clash, never start a clash of their own, and return when the charger's charge
  ends (D-35). Stunned soldiers do not follow.

### 4.5 Clash & Duel

**Trigger**
- **Contact:** centres closer than `ContactDistance` (0.85).
- A charger in contact with an enemy soldier always clashes (its charge ends). Followers never start a clash. Any other
  contact clashes only when both soldiers are off their `ClashCooldownSeconds` (1 s) clash cooldown.
- **Knock-back:** both soldiers are pushed apart by 0.3 units (SU-103).

**Combat Power** (each side, at contact)

$$\text{CombatPower} = \text{WeaponSkill} + \text{ChargeBonus (if charging)} + \text{SupportBonus}$$

- `WeaponSkill` is the soldier's skill in its melee weapon's class.
- `ChargeBonus = Charge + weapon ChargeBonus + AssaultChargeBonus (if Assault)`. When both soldiers are charging, both
  get it.

**Support**
- Eligible supporters: living, non-fatigued, non-stunned soldiers **of the same army** within `SupportRadius` (3) of the
  clashing soldier; a Spear Support bearer counts within `SupportRadius × SpearSupportRangeMultiplier` (6).
- `SupportBonus = min(eligible, Support) × SupportBonusPerAlly` (2).
- Against an enemy attacking this troop's Flank or Rear face, this troop gets **0** support (§4.8).

**Duel**
- **A** is the charger; if both charge, the earlier charge; if neither, the lower soldier ID.
- $P_A = \dfrac{CP_A}{CP_A + CP_B}$. Roll $R \in [0, 100]$; A wins if $R \le 100\,P_A$.
- **Domination** (winner's bonus damage) = `round(|R − 100 P_A| / DominationDivisor)` (10), rounding half up (D-46).
  Example: `P_A = 60%`, `R = 40`: A wins with Domination 2.
- A winner whose troop is fleeing (retreating or routed), or a stunned winner, deals **no damage** (Q-37, SU-111).

### 4.6 Damage, Mitigation & HP

**Raw melee damage**

$$\text{Damage} = \text{Strength} + \text{weapon Damage} + \text{Domination}$$

- Couched Lance: doubled while charging (Domination included).
- Ranged damage is the weapon's Damage only.

**Mitigation** (`TotalDefence = Defence + armour DefenceBonus`)
- **Up to `TotalDefence`:** each point rolls once against the armour ratio `Deflect : Bruise : Bleed`, with the
  attacking weapon's Piercing added to Bleed and its Bludgeoning to Bruise.
  - **Deflect:** ignored.
  - **Bruise:** temporary HP loss.
  - **Bleed:** permanent HP loss.
- **Above `TotalDefence` (overflow):** every point becomes **Bleed**.
- **No armour:** every point rolls against 0 : 3 : 7 (no TotalDefence cap).

**Shock from damage (SU-128):** every point that becomes Bruise or Bleed (overflow included) adds 1 Shock to the troop
(×3 when flanked, §4.8). Deflected points add none, so armour reduces Shock. Damage beyond the HP a soldier has left
adds none.

**Combined HP pool**
- `CurrentHP = MaxHP − Bleed − Bruise`. Bruise heals `BruiseHealPerSecond` (1) per soldier; bruises can kill.
- **Fatigued:** `CurrentHP ≤ FatigueFraction × MaxHP` (0.5). Fatigued soldiers cannot support.
- **Stunned (SU-107):** a soldier brought to 0 HP with Bruise ≥ Bleed is stunned, not killed (`StunAtZeroHp`). It stays
  alive (it counts for the troop's soldiers, morale and capture points), cannot charge, follow or support, heals from
  0 HP and recovers at `StunRecoverHp` (5 HP). Stunned soldiers die when their troop retreats or routs from melee
  **[A: SU-107 rout]**. Overflow counts as Bleed for the test **[A: SU-107]**.
- **Dead:** `CurrentHP ≤ 0` otherwise. A corpse stays 10 s (SU-35).

### 4.7 Ranged Combat

**Firing condition**
- A troop fires while **stationary, not engaged, not retreating and not routed**. Fire & Engage volleys are the
  exception (below).

**Targeting (D-54)**
- Reach is **per shooter**: a shooter can hit an enemy soldier within its weapon range and within `RangedFrontArcDeg`
  (±45°) of the troop's facing, measured from the shooter.
- **Target troop:** the enemy troop **not engaged in melee** with the soldier nearest to a shooter that can reach it
  (Q-39 a). Troops in melee are never targeted.
- **Target soldier:** each shot goes to the reachable enemy soldier with the fewest of the troop's projectiles already
  aimed at it, then the nearest.
- No friendly fire: only enemy soldiers can be hit; projectiles are not blocked by troops or terrain **[Q-40]**.
- Unlimited ammunition. No flank bonus for ranged hits.

**Timing**
- **Single shots (default, SU-105):** each soldier shoots every `ActionWindowSeconds / FireRate` (± `FireRateJitter`
  10%), its first shot after a random part of that interval.
- **Volley (weapon's Volley on):** those soldiers shoot together every `ActionWindowSeconds / FireRate` (± 10%).
- **Fire & Engage:** one volley when the troop goes from not engaged to engaged, at the enemy it engaged, and one when it
  begins a Case B retreat from an engagement. These ignore the usual firing limits and the front arc; charging soldiers
  fire too. Once per engagement each **[A: SU-90]**.

**Projectiles**
- Projectiles fly at `ProjectileSpeed` (20 units/s). The hit is rolled when they land:

$$P(\text{Hit}) = \frac{\text{Accuracy} + \text{WeaponSkill}}{\text{Accuracy} + \text{WeaponSkill} + \text{BaseShotDC}}$$

  with `BaseShotDC` 10 and the skill for the weapon's class. Example: Long bow (Accuracy 2), skill 6: 8 / 18 ≈ 44%.
- Damage is mitigated as §4.6.

### 4.8 Faces & Flanking

- Each engaged troop's footprint has four faces. The **attacking face** of each opposing troop is the angle from the
  defender's pivot to the attacker's nearest footprint point, against the defender's facing: within
  `FrontFaceHalfAngleDeg` (±45°) **Front**, beyond `RearFaceAngleDeg` (135°) **Rear**, otherwise Left or Right
  **Flank**. Troops without a rectangle use their current heading.
- **Flank or Rear:** in clashes against that enemy the defender gets **0 support** and the damage it takes counts
  `FlankShockMultiplier` (×3) Shock. Clashes against enemies on its front resolve normally.

### 4.9 Surrounded

- A troop is **Surrounded** when the box `RetreatDistance` long behind its rear face is blocked by terrain or by a living
  enemy soldier standing in it (not charging, following or rushing back). Enemy footprints alone do not block it;
  routed enemies and friendly troops never do (SU-122) **[A: SU-122]**.
- Surrounded troops cannot retreat (§5.3).

---

## 5. Morale, Shock, Retreat & Rout

### 5.1 Effective Morale

$$\text{InitialEffectiveMorale} = \text{BaseMorale} \times \text{InitialSoldierCount}$$

$$\text{EffectiveMorale} = \text{BaseMorale} \times \text{CurrentSoldierCount} - \text{RetreatCount} \times \text{RetreatMoralePenalty} - \text{SurroundedBreakCount} \times \text{SurroundedMoralePenalty}$$

- `RetreatMoralePenalty` 5 (retreats and disorders), `SurroundedMoralePenalty` 20. Stunned soldiers count as living.
- Example: Franz (BaseMorale 5, 12 soldiers) starts at 60; after losing 2 soldiers he has 50.

### 5.2 Shock

- **Accumulation:** see §4.6 (every applied point of Bruise or Bleed, ×3 when flanked).
- **Decay:** continuous, also in melee, clamped at 0:
  `ShockDecayFractionPerWindow × max(0, EffectiveMorale) / ActionWindowSeconds` per second (0.2, 30 s). Example: Franz
  at 50 decays 0.33 Shock/s.

### 5.3 Break

- A break happens whenever **Shock > EffectiveMorale** (Shock rises, or EffectiveMorale falls below it).
- A retreating troop cannot break again until its retreat ends; a routed troop never breaks again (D-30). There is no
  break cooldown and no charge lock.

| Case | Condition | Result |
|---|---|---|
| **A: Rout** | `EffectiveMorale < RoutMoraleThresholdFraction × Initial` (0.5) | Rout (§5.4). |
| **B: Retreat** | Not A, not Surrounded, **in melee** | Retreat (§5.5). |
| **B: Disorder** | Not A, not Surrounded, **not in melee** | Disorder (§5.5) (SU-104). |
| **C: Surrounded break** | Not A, Surrounded | Shock 0, `−SurroundedMoralePenalty`, no motion. |

### 5.4 Rout

- **Final.** The troop breaks every link, cancels its orders, scatters (`RoutScatterRadiusScale`) and flees. It ignores
  orders. Its stunned soldiers in melee die **[A: SU-107]**.
- **Flight (SU-121):** with a NavMesh it runs to a goal on the battlefield edge within ±90° of "away from the nearest
  non-routed enemy" (the whole circle when nothing there is reachable), re-planned every 1 s; a troop that moves less
  than 0.5 units in 1 s is stuck and avoids that heading for 5 s **[A: SU-121]**. Without a NavMesh it flees in a
  straight line. It never leaves the map: at the edge it runs along it **[A: SU-74, SU-121]**.
- **Attackable:** yes; a fleeing winner deals no damage (§4.5).
- **Defeated:** the troop is defeated once, at rout (or when its last soldier dies first): `Troop.Routed`,
  `Troop.Defeated` and `BattleEvents.TroopDefeated` are raised then, not at despawn (SU-57).
- **Despawn:** after `RoutDespawnSeconds` (60), wherever it is.

### 5.5 Retreat and Disorder (Case B)

**Retreat (in melee)**
1. Shock resets to 0 and EffectiveMorale drops by `RetreatMoralePenalty`.
2. The pivot slips `RetreatDistance` (5 units, the same for every troop) away from the pivot of the last troop that hit
   it, or straight back when nothing did (SU-11, D-31).
3. **Accordion (SU-12, SU-133):** each soldier holds its ground until its start time, then eases to its place in the
   new line. Start times run from the rear (away from the attacker) to the front over `RetreatWaveDelaySeconds` (1.0 s),
   and move times from `RetreatRearMoveSeconds` (0.8 s) at the rear to `RetreatFrontMoveSeconds` (1.8 s) at the front,
   so the rear falls back first and fastest and the troop stretches out, then closes up. The retreat lasts
   `wave delay + slowest move + 0.5 s` (3.3 s).
4. Charges in flight complete, then those soldiers rush back (SU-43). Stunned soldiers die (SU-107).
5. Chained neighbours may retreat sympathetically (§3.4).

**Disorder (not in melee, SU-104)**
- Shock resets to 0 and EffectiveMorale drops by `RetreatMoralePenalty` (it counts in RetreatCount).
- For `DisorderSeconds` (10 s) the troop cannot move: its Move, Pursue and Line orders are cancelled and new ones are
  refused; it is not marched, pursued, pushed by line pressure or pulled by its chain. It can still fight, charge and
  shoot **[A: SU-104]**.
- Breaking again while disordered restarts the 10 s with another penalty; if it is in melee by then it retreats instead,
  which ends the disorder **[A: SU-104]**.

---
## 6. Player Controls & Orders

Player input (`TroopPlayerOrders`, `TroopSelection`, `TroopOrderPlanner`, `TroopOrderOverlay`) reads the Input System
directly (D-42). Every threshold is in **screen pixels**, so it does not depend on zoom. Selection and orders also work
while the battle is paused; troops act on them once time runs again (SU-88).

### 6.1 Selection (LMB)

- **Click on a troop:** selects just that troop. **Double click** (within `DoubleClickSeconds`, 0.35 s) selects every
  troop in its chain (SU-129).
- **Click on empty ground:** clears the selection.
- **Box select:** a drag of at least `BoxSelectMinDragPixels` (8) that starts off the player's troops draws a marquee;
  every player troop with its pivot or a living soldier inside is selected.
- **Shift:** toggles: a selected troop is deselected, an unselected one added (a Shift double click adds the chain).
- **Picking:** a living soldier within `ClickPickRadiusPixels` (24), then the outline the point is deepest inside, then a
  pivot within the pick radius (SU-42). The same test finds the hovered troop (any team).

### 6.2 Orders

**LMB drag from a troop (SU-129):** press on one of the player's troops, keep the button down and drag at least
`OrderDragMinPixels` (8): on release the selection (that troop, or its whole chain after a double click) moves to the
release point, keeping relative offsets. Chain rules apply as for a Move (§6.3), so a selected chain moves together. A
marker previews each troop's spot while dragging. With Shift held the drag does nothing.

**RMB click (below `OrderDragMinPixels`)**
- **On an enemy troop:** `Pursue` by every selected troop.
- **On empty ground:** `Move`. Selected troops keep their relative offsets; they arrive Idle (Q-14).

**RMB drag (at or above `OrderDragMinPixels`)**
- **Two or more troops: battle line.** The span is split across the troops in order along the drag (Q-18); each length
  is its share, clamped to `[0.7, 3.0] × L_nat`. The front faces the nearest enemy troop, or the right-hand normal of the
  drag if there is none; the player cannot choose the front (Q-17). The order links adjacent troops with pending links
  (§3.4). Each troop holds its line after arriving.
- **One troop: Move** to the release point (SU-129). The player never gives a single troop a facing; its line forms and
  faces the threat by itself (§3.3). A lone troop's facing can be shaped only indirectly, by placing its neighbours in a
  chain.
- **Ghost preview:** while the button is held, each troop's projected footprint (or a marker for a move), the shared
  joints and any chain tethers. No facing arrow (SU-33).

**Order paths:** every selected troop, and the hovered troop of any team, with a Move, Line or Attack order shows its
path with arrows (SU-37, SU-97).

### 6.3 Chain Manipulation

**Envelope.** A destination is inside the chain envelope if the resulting stretch is `≤ 3.0 × L_nat` and the inner
hinge angle is `≥ 65°`.

- **Links inside the selection** move with it (a selected chain moves together).
- **Staying chained:** a troop linked to a troop outside the order, ordered to a destination inside the envelope, keeps
  the link; the joint hinges.
- **Exiting:** outside the envelope the link is severed when the order is issued (on release), and the troop is
  ChainSuppressed (§3.4.1).
- **Ghost tether:** solid green/white = stays linked; dashed red = outside the envelope, or Alt held: release severs.
- **Middle extraction:** removing B from `A–B–C` leaves A and C with free inner line points; they do not auto-link
  across the gap unless they come within the attach distance.
- **Attack orders** break an attacker's links to troops outside the selection.

**Alt**
- **Alt + order on a chained troop:** unlinks it at once, even inside the envelope.
- **Alt + order on an unlinked troop:** sets ChainSuppressed, so reserves can stand close behind a line without
  snapping onto its wings. It ends as in §3.4.1 (SU-130).

### 6.4 Battle Screen

- **Troop cards** (`TroopCardHud`): the selected troop bottom left, the hovered troop bottom right (any team). Name(s)
  joined with " + ", living / starting soldiers, portraits, a blue Effective Morale bar with the red Shock bar on the
  same scale and a white tick at the break point, formation mode or "Disordered Ns"; character side panels open from
  the portraits; stats, weapon skills and armour shown as icons with tooltips (SU-19 to SU-29, SU-91, SU-127).
- **Speed bar** (top right): Pause, ½, Play, 2× (SU-88). **P** / gamepad Start toggles pause, keeping the speed.
- **Combat log:** an arrow tab on the left edge opens a log of every clash and landed shot with its Combat Power parts,
  roll, damage split and HP lost (SU-110, SU-113).
- **Engagement heat map** (F5): a red band across the gap between engaged lines, only where they are within
  `A_Distance` of each other (SU-40, SU-41, SU-96).
- **Range mask:** hovering a troop that can shoot shows the union of its shooters' sectors as a heat map (D-53).
- **Soldier HP bar** over the hovered soldier; injured glow and corpses (SU-31, SU-34, SU-35); hit squash and stretch
  (SU-36, SU-109).

---

## 7. Battle Flow

- **Deployment phase (SU-81, SU-82, SU-101):** with `Battle.Deployment` on, the battle opens paused. The player's
  troops spawn in the deployment zone (one troop per deployed character list, `BattleReport.DeployedTroops`) and stand
  Idle facing the enemy. Selected troops are **placed** instead of ordered: a move places them at the spot (kept inside
  the zone) and Idle, a multi-troop line places them in that line. Attack orders do nothing. **Start Battle** begins the
  fight. The enemy AI starts with the scene and acts once time runs. The camera opens centred on the player's zone
  (SU-126).
- **Battle time** counts only fighting time **[A: SU-81]**.
- **Win and loss:** a `Rout` condition ends the battle when more than `Rout Share` of an army's troops are defeated (1 =
  every troop). Mission 1's enemy uses 0.5; the player's loss stays at every troop (SU-78, confirmed for now).
- **Capture points** count the living soldiers of each army inside them four times a second (SU-59).
- **Battle events** (`BattleEvents`): soldier killed, soldier struck, volley fired, projectile landed, troop broke (rout,
  retreat, disorder, surrounded break, with its cause) and troop defeated. Achievements, the battle report and sounds
  listen to them; battle code never references the campaign (SU-57, SU-114).

---
## 8. Tunable Variables

Every value below is a **variable**: a field on one of the Battle Tuning assets in `Assets/Resources/Battle Tuning/`,
read through `TroopTuning.Active`, and adjustable in the Inspector (menu **Battle Tuning**). The column gives the value
in use now: the asset's value, or the code default for a field the asset has not saved yet (marked *default*). Code
defaults are used when an asset is missing. Values in the rest of this document are these.

**Soldier Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `SpeedUnitsPerStatPoint` | 0.6 | units/s per point | Speed stat to units/s. |
| `AccelerationPerSpeedPoint` | 1.6 | units/s² per point | Acceleration from Speed. |
| `ActionWindowSeconds` | 30 | s | Window for FireRate and Shock decay. |
| `MaxWeaponSkillsPerSoldier` | 2 | int | Weapon classes a character can be skilled in. |
| `MaxWeaponsPerSoldier` | 2 | int | Not read since D-52 (one melee + one ranged weapon). |
| `BruiseHealPerSecond` | 1 | HP/s | Bruise recovery per soldier. |
| `FatigueFraction` | 0.5 | 0–1 | Fatigued at or below this fraction of MaxHP. |
| `StunAtZeroHp` | on (*default*) | bool | 0 HP with Bruise ≥ Bleed stuns instead of killing. |
| `StunRecoverHp` | 5 (*default*) | HP | A stunned soldier recovers at this HP. |
| `SoldierAlignment` | 0.5 | gain | Velocity matching. |
| `SoldierSeparation` | 1.8 | gain | Same-troop separation; cross-troop separation scales with it. |
| `SoldierBrownianMotion` | 0.04 | gain | Random jitter. |

**Formation Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `S_Distance` | 5 | units (footprint gap) | An enemy this close makes a troop form a line. Code default 30. |
| `SDistanceExitHysteresis` | 0 | units | Extra exit distance for S_Distance (the shape-based margin of §3.1 applies too). |
| `A_Distance` | 2 | units (footprint gap) | Engagement distance; also the charge reach (§4.3). Code default 5.2. |
| `EngagementGap` | 1.5 | units (footprint gap) | Engaged lines are held this far apart; charge gate. Code default 2. |
| `EngagementHoldMargin` | 1 | units | Engaged troops stay engaged until A_Distance + this. |
| `NaturalLineRadiusScale` | 1.8 | × | Troop radius from L_nat (radius = L_nat / this, before TroopSizeScale). |
| `TroopSizeScale` | 1.5 (*default*) | × | Multiplies the troop radius so soldiers have room (SU-131). |
| `BlobRadiusScale` | 0.85 | × radius | Idle blob disc. |
| `PivotSeparationScale` | 0.25 | × (r_A + r_B) | Minimum friendly pivot spacing. |
| `FriendlyGap` | 0.5 | units | Minimum gap between friendly outlines. |
| `FriendlyGiveWaySpeedScale` | 0.5 | × march speed | Fastest a troop giving way moves. |
| `PathRebuildIntervalSeconds` | 0.5 | s | Path rebuild interval. |
| `JointClearance` / `JointClearanceFraction` | 0.45 / 0.15 | units / × length | Pull-in at a linked end: min(of the two). |
| `RoutScatterRadiusScale` | 1.6 | × radius | Routed troop scatter disc. |

**Morale Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `RetreatMoralePenalty` | 5 | morale | Per retreat or disorder. |
| `SurroundedMoralePenalty` | 20 | morale | Per surrounded break. |
| `ShockDecayFractionPerWindow` | 0.2 | 0–1 | Shock decay per ActionWindowSeconds, as a fraction of EffectiveMorale. |
| `RoutMoraleThresholdFraction` | 0.5 | 0–1 | Rout below this fraction of the initial EffectiveMorale. |
| `RoutDespawnSeconds` | 60 | s | Routed troops despawn after this. |
| `RetreatDistance` | 5 | units | Retreat distance, the same for every troop; also the surround box length. |
| `RetreatWaveDelaySeconds` | 1.0 | s | Delay between the rearmost and the frontmost soldier starting to fall back. |
| `RetreatRearMoveSeconds` | 0.8 | s | Rearmost soldier's move time (fastest). |
| `RetreatFrontMoveSeconds` | 1.8 | s | Frontmost soldier's move time (slowest). |
| `DisorderSeconds` | 10 (*default*) | s | Time a disordered troop cannot move. |
| `FlankShockMultiplier` | 3 | × | Shock multiplier on flank and rear hits. |
| `FrontFaceHalfAngleDeg` | 45 | deg | Front face half-angle. |
| `RearFaceAngleDeg` | 135 | deg | Rear face starts here. |

**Melee Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `ChargeWindowSeconds` | 45 (*default*) | s | A soldier makes about EffectiveAggression charges per this window. |
| `ChargeGapTolerance` | 0.25 (*default*) | units | Charge gate tolerance beyond EngagementGap. |
| `FrontRankChargesOnly` | on (*default*) | bool | Only the front rank starts charges. |
| `ChargeCandidateCount` | 3 | int | Nearest enemy soldiers a charger picks from. |
| `ChargeSpeedScale` | 2 | × | Charger, follower and rush-back speed and acceleration. |
| `ChargeTimeoutSeconds` | 3 | s | Charge cancel timeout. |
| `ChargeFollowProbability` | 0.5 | 0–1 | Chance each selected follower joins. |
| `ClashCooldownSeconds` | 1 | s | Per-soldier clash rate limit for contacts that are not charges. |
| `SupportRadius` | 3 | units | Support range. |
| `SpearSupportRangeMultiplier` | 2 | × | Spear Support range. |
| `SupportBonusPerAlly` | 2 | int | Combat Power per supporter. |
| `DominationDivisor` | 10 | float | Divisor in the Domination formula. |
| `AssaultDurationSeconds` | 10 | s | Assault length. |
| `AssaultAggressionBonus` | 5 | int | Aggression during Assault. |
| `AssaultChargeBonus` | 5 | int | Charge during Assault, while charging. |

**Chain Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `ChainMinStretchScale` | 0.7 | × L_nat | Shortest linked line. |
| `ChainMaxStretchScale` | 3.0 | × L_nat | Longest linked line; break threshold. |
| `ChainMinInnerAngleDeg` | 65 | deg | Smallest inner hinge angle; break threshold. |
| `AttachScale` | 0.25 | × L_nat | Endpoint attach distance. |
| `AutoLinkMaxFacingDeg` | 45 | deg | Automatic links need facings within this. |
| `AutoLinkBreakFacingDeg` | 60 | deg | An automatic link breaks past this. |
| `ChainSuppressCooldownSeconds` | 3 | s | ChainSuppressed ends after this long outside every attach radius. |
| `ChainRetreatStretchThreshold` | 1.4 | × L_nat | Stretch that triggers a sympathetic retreat. |
| `ChainRetreatHingeBendDeg` | 30 | deg | Bend (180° − inner) that triggers a sympathetic retreat. |
| `ChainRetreatFalloff` | 0.3 | 0–1 | Retreat distance lost per hop. |
| `ChainEndpointRelaxSpeed` | 2 | units/s | Relax rate of a broken, stretched line point. |
| `LinkSlack` / `LinkStiffness` / `LinkMaxForce` | 0.1 / 5 / 10.4 | units / gain / force | Link tension spring. |

**Ranged Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `BaseShotDC` | 10 | int | Shot difficulty. |
| `FireRateJitterFraction` | 0.1 | 0–1 | Random variation on the fire interval. |
| `ProjectileSpeed` | 20 | units/s | Projectile speed. |
| `RangedFrontArcDeg` | 45 | deg | Shooter's arc either side of the troop's facing. |
| `CompositeRankGain` | 0.8 | gain | Strength of composite rank sorting (0 = off). |
| `CompositeRankDepthFraction` | 0.8 | × half-depth | Target depth of the ranged / melee ranks. |

**Input Tuning**

| Variable | Value | Unit | Description |
|---|---|---|---|
| `BoxSelectMinDragPixels` | 8 | px | Minimum drag for a box select. |
| `OrderDragMinPixels` | 8 | px | Minimum drag for a drag order (line, or LMB drag move). |
| `ClickPickRadiusPixels` | 24 | px | Pick radius around a soldier or pivot. |
| `DoubleClickSeconds` | 0.35 (*default*) | s | Two clicks on a troop within this select its chain. |

**Constants (`TroopConstants`, not tunable):** `SoldierRadius` 0.3, `SeparationRadius` 0.9 (3 × soldier radius),
`LineSpacing` 0.9, `SpreadRadius` 1.2, `PressureMassPerDrive` 3, `ContactDistance` 0.85, `NeighborRadius` 2.5,
`MaxChainLinks` 2. Column: at most 3 lanes, 2 frontage per lane, core starting 0.05 × radius behind the pivot.
Stat limits are in `CombatStatLimits` (1–20, default 5; HP 10–40, default 15).

**Other tuning assets:** Achievement Tuning (`Resources/Battle Tuning/`) and Post Battle Tuning
(`Resources/Campaign Tuning/`) hold the achievement lists and post-battle book timings.

---

## 9. Glossary

| Term | Definition |
|---|---|
| **Troop** | The commandable unit: a list of characters, leader first, and their soldiers. |
| **Soldier** | An autonomous fighter steered by its troop's vector field. |
| **Character** | A soldier type with stats, equipment, class and Numbers (`CharacterProfile`; `CampaignCharacter` in the roster). |
| **Composite troop** | A troop of two characters paired in the tent. Min Move, max Morale and Discipline, summed soldiers. |
| **Frontage** | Soldiers wide in a line, from the soldier count. Sets `L_nat` and the troop radius. |
| **Formation mode** | Idle, Column, Line, Chain or Rout (§3.1). |
| **Detachment / Formation (AI)** | The AI's grouping of troops, and that group's role. |
| **Footprint** | The troop's shape for engagement, faces and surround (rectangle, capsule or circle). |
| **Outline** | The steady shape drawn, clicked and hovered (§3.3). |
| **Engaged** | Footprints within `A_Distance` (held to `A_Distance + EngagementHoldMargin`). |
| **Committed** | Engaged, or carrying an attack order. Friends never move a committed troop. |
| **Assault** | 10 s of +5 Aggression and +5 Charge for a troop that enters engagement under an order. |
| **Charge** | A front-rank soldier runs at double speed to clash with one of the 3 nearest enemy soldiers in reach. |
| **Charge support** | Same-troop soldiers chosen by Discipline who follow a charger and support its clash. |
| **Clash & Duel** | A melee contact and its Combat Power roll. |
| **Domination** | Bonus damage to the duel winner. |
| **Deflect / Bruise / Bleed** | Outcomes of a point of damage. Bruise heals at 1 HP/s; Bleed is permanent. |
| **Overflow** | Damage above TotalDefence: always Bleed, counts for Shock. |
| **Fatigued** | `CurrentHP ≤ 50% MaxHP`. Cannot support. |
| **Stunned** | At 0 HP with Bruise ≥ Bleed: alive but out of the fight until healed to 5 HP. |
| **Effective Morale** | `BaseMorale × living soldiers` minus penalties; the Shock threshold. |
| **Shock** | Damage-driven stress; decays continuously. Break when `Shock > EffectiveMorale`. |
| **Retreat / Disorder** | A Case B break in melee / out of melee. |
| **Surrounded** | A troop whose retreat box is blocked by terrain or enemy soldiers. |
| **ChainSuppressed** | A troop that may not auto-link until 3 s outside every attach radius. |
| **Hinge (J), inner angle** | The joint between two chained troops, and the angle at it (straight = 180°). |

---

## Appendix A. Decision IDs still cited in code

The Phase 01–10 decision log (`Decisions.md`) has been retired. These decisions are cited by code comments and stay in
force:

| ID | Decision | Spec |
|---|---|---|
| D-01 | The designer's character sheets were the Phase 01 source of truth for character data. | §1 |
| D-03 | Weapon classes include Mace (melee) and Crossbow (ranged). | §1.6 |
| D-52 | A character has one required melee weapon and an optional ranged weapon (`MeleeWeaponProfile`, `RangedWeaponProfile`). A character without a melee weapon cannot be created or deployed (`MissingMeleeWeaponException`). | §1.6 |
| D-54 | Ranged reach and targeting are per shooter. | §4.7 |
| D-55 | Composite troops sort ranged soldiers to the front and melee to the back in a line, reversed while engaged. | §2.1 |

Other D-numbers mentioned in this document (D-30, D-31, D-35, D-36, D-38, D-39, D-42, D-44, D-45, D-46, D-53, D-56)
are described where they are cited.
