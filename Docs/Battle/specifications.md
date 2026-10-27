# Leveraged Wars: Battle System Specification (v0.9)

> **Status:** Supersedes v0.8 and the WIP `Assets/Battle Scripts/Troop/README.md` where they conflict.
> Items tagged **[Q-n]** are unresolved and listed in `Questions.md`. Items tagged **[PROVISIONAL]** have a working value
> that must be implemented as a named tuning constant, not a magic number.
> Items tagged **[DEPRECATED]** must be removed from code and docs.

---

## 0. Changes from v0.8

| Area | v0.8 | v0.9 |
|---|---|---|
| Hinge limit | `ChainMaxBendAngleDeg = 65°` (ambiguous) | **Minimum inner hinge angle 65°** (angle at the joint between the two troops' lines). |
| Chain break | `R_BreakScale = 3.0` (distance) | **Break when required line length exceeds `ChainMaxStretchScale = 3.0 × L_nat`.** `R_BreakScale` is removed. |
| Domination | `|P_winner − R| / 10` | **`|R − P_charger| / 10`** for either side, rounded to nearest whole number. |
| Line spacing | `0.6` | **`0.9`** (equal to the minimum soldier centre spacing). |
| Natural line length | Soldier-count based | **Based on Troop Radius.** |
| Move | Separate stat | **Derived:** minimum soldier `Speed` in the troop. |
| Discipline | Soldier-level, drives strike beat | **Troop-level.** Selects charge followers only. |
| Melee attacks | Banked attacks, 1 s strike beat, Lunge | **[DEPRECATED].** Replaced by stochastic charges (§4.3). |
| Clash trigger | Attack spent on contact | **Charge reaches contact, or any opposing contact** (rate-limited, §4.5). |
| Morale | `FaultPressure` + non-decaying Shock | **[DEPRECATED].** Shock decays continuously (§5). |
| Clash cooldown / break cooldown | Present | Break cooldown and charge lock **[Q-34]**. Clash rate limit is new **[PROVISIONAL]**. |
| Facing damage multiplier (4×/2×/1×) | Present in code, not in spec | **[DEPRECATED].** Flanking is handled by face penalties (§3.3). **[Q-38]** |
| Support radius | `1.0` (×2 for pikes) | **`3`.** Pike multiplier replaced by the Boar Spear rule (§1.6). |
| Bruise | 30 s timer | **Heals 1 HP per second per soldier.** |
| Selection / order thresholds | World units / pixels mixed | **Screen pixels** (zoom-independent). |
| Friendly (support, pivot separation) | Undefined | **Same army.** |
| `InitialTroopCount` | Name | **`InitialSoldierCount`.** |
| Pursuit | Undefined | **Only by explicit Pursue order.** |
| Weapon classes | Spear, Sword, Axe, Bow, Javelin | **+ Mace, Crossbow** (Phase 01, Q-52). |
| Soldier loadout | Undefined | **Up to 2 weapons (list), first melee and first ranged are used.** Q-3 open for two melee weapons. |
| Combat stat type | Floats, various scales | **All combat stats are integers 1–20, default 5.** Includes BaseMorale, Discipline and weapon skill levels. |
| HP | Average 18, float | **Integer 10–40, default 15.** |
| Discipline range | 0–20 | **1–20, default 5.** Q-10 resolved. Minimum 1 means every troop has at least one possible follower. |

---

## 1. Entities, Stats & Equipment

### 1.1 Entities

- **Troop:** The commandable unit controlled by the player or AI. At battle time it receives a resolved `TroopProfile` and
  spawns a roster of `Soldier` agents from its composition.
- **Soldier:** An autonomous fighter simulated as plain state (`SoldierAgentState`), steered by the troop's vector field.
  Soldiers have no fixed slots.
- **Character:** A campaign commander. Each character commands **one soldier type**.
- **Composite Troop:** Two or more characters paired before battle. The battle system does not know about pairing; it only
  receives the resolved `TroopProfile`.

### 1.2 Composite Troop Resolution (pre-battle)

| Troop stat | Rule |
|---|---|
| `Move` | `min(Speed_i)`, where each character's `Speed_i` is the minimum soldier `Speed` of its soldier type (§1.3). |
| `BaseMorale` | `max(BaseMorale_i)` |
| `Discipline` | `max(Discipline_i)` |
| `InitialSoldierCount` | `Σ SoldierCount_i`. Each character spawns its own soldier type. |

### 1.3 Stat Scale

- **Combat stats are integers from 1 to 20, default 5** (`CombatStatLimits`). This covers Speed, Defence, Strength,
  Support, Charge, Aggression, BaseMorale, Discipline and weapon skill levels.
- **HP (MaxHP) is an integer from 10 to 40, default 15.**
- Soldier counts (Numbers) are not stats and have no range.
- **Speed conversion [PROVISIONAL, Q-1]:** `UnitsPerSecond = StatValue × SpeedUnitsPerStatPoint`, with
  `SpeedUnitsPerStatPoint = 0.6` (Speed 5 = 3.0 units/s, matching the current Swordsmen asset).
- **Pivot speed** = troop `Move` × `SpeedUnitsPerStatPoint`. No soldier is slower than the pivot, because `Move` is the minimum.

### 1.4 Troop-Level Stats

| Stat | Scale | Notes |
|---|---|---|
| `Move` | 1–20 (derived) | Minimum soldier `Speed` (§1.2). Not stored separately. |
| `BaseMorale` | integer 1–20, default 5 | Morale per soldier. `EffectiveMorale` = `BaseMorale × CurrentSoldierCount` minus penalties (§5). |
| `Discipline` | integer 1–20, default 5 | Number of nearest friendly soldiers that may follow a charger (§4.4). |
| `InitialSoldierCount` | integer | Soldiers at deployment. Used for the rout threshold. |
| `CurrentSoldierCount` | integer | Living soldiers. |
| `TroopRadius` | units | Profile value. How it is set for stretched lines is open **[Q-11]**. |

### 1.5 Soldier-Level Stats

| Stat | Scale | Notes |
|---|---|---|
| `MaxHP` | integer 10–40, default 15 | Combined pool for Bruise and Bleed (§4.6). |
| `Speed` | integer 1–20, default 5 | Individual speed along the vector field. Lowest value in a troop sets its `Move`. |
| `Defence` | integer 1–20, default 5 | Mitigation (§4.6). |
| `Strength` | integer 1–20, default 5 | Melee damage bonus. |
| `Support` (`MaxSupportCount`) | integer 1–20, default 5 | Cap on supporting allies in a clash. |
| `Charge` (`ChargeBonus`) | integer 1–20, default 5 | Added to Combat Power when charging. |
| `Aggression` | integer 1–20, default 5 | Expected charges per `ActionWindowSeconds` while engaged. |
| `WeaponSkill[class]` | integer 1–20, default 5 when added **[Q-2]** | Up to 2 classes per soldier. A class that is not listed is unskilled (0). A listed skill is never 0. |

### 1.6 Equipment

**Loadout rules**
- A soldier carries **up to 2 weapons** and has skill in **up to 2 weapon classes**.
- Weapon classes: **Spear, Sword, Axe, Bow, Javelin, Mace, Crossbow**. Mace is melee. Crossbow is ranged. (Mace and
  Crossbow added in Phase 01, Decisions D-03, pending sign-off Q-52.)
- Ranged troops (such as archers) always carry a ranged weapon **and** a melee weapon. In melee they use the melee weapon.
- Which melee weapon is used when a soldier carries two **[Q-3]**.

**Armour**

| Field | Meaning |
|---|---|
| `Resistance` | Deflect / Bruise / Bleed percentages for each point of damage up to `TotalDefence`. Must sum to 100. |
| `DefenceBonus` | Added to the soldier's `Defence` to give `TotalDefence`. |

**Melee weapons**

| Field | Meaning |
|---|---|
| `WeaponClass` | Spear, Sword, Axe. |
| `DamageBonus` | Added to melee damage (`WeaponDamage`). |
| `ChargeBonus` | Added to Charge only while charging **[Q-4]**. |
| `SpecialRule` | Optional (see below). |

**Ranged weapons**

| Field | Meaning |
|---|---|
| `WeaponClass` | Bow, Javelin. |
| `Damage` | Base damage per hit. |
| `Range` | Units. |
| `Accuracy` | Added to skill in the hit formula (§4.7). |
| `FireRate` | Shots per `ActionWindowSeconds` (30 s). |

**Special rules**
- **Boar Spear:** when this soldier provides a support bonus, its support range is doubled (`SupportRadius × 2`) [Q-7].
- **Lance:** total melee damage is doubled while the bearer is charging [Q-8].

**Sample data**

| Item | Values |
|---|---|
| Light armour and shield | Resistance 30 / 50 / 20 (Deflect / Bruise / Bleed); DefenceBonus +4 |
| Heavy armour | Resistance 60 / 30 / 10; DefenceBonus +7 |
| Short sword | Sword; DamageBonus 5; ChargeBonus 3 |
| Boar spear | Spear; DamageBonus 3; special: double support range |
| Lance | Spear; DamageBonus 3; ChargeBonus 8; special: double damage while charging |
| Long bow | Bow; Damage 10; Range 20; Accuracy 2; FireRate 10 |

Armour values above are read as Deflect / Bruise / Bleed. The Light armour values differ from v0.8 (30/30/40) [Q-6]. Javelin and Crossbow values are not yet given [Q-45].

---

## 2. Soldier Simulation, Vector Fields & Physics

Soldiers are plain state objects (`SoldierAgentState`) simulated by their troop and drawn by view objects.

### 2.1 Vector Field Steering

Each frame the troop builds a local `FieldShape`: a **core segment** and a **half-width**.

- **Attraction:** a non-charging soldier moves toward the nearest point on the core. Speed grows with distance outside the half-width, slows as it nears the core, and matches the troop velocity so it does not overshoot.
- **Density spreading:** soldiers within `SpreadRadius` (`1.2`) push each other apart tangentially along the shape. The force fades to zero at `SpreadRadius`.
- **No cohesion:** soldiers have no attraction to each other. Alignment only matches velocity.

### 2.2 Separation, Drive Pressure & Mass

- **Intra-troop separation:** same-troop soldiers keep centres at least `SeparationRadius = SeparationRadiusScale × SoldierRadius = 3.0 × 0.3 = 0.9` apart. A soft repulsion acts when there is room. A hard pass (`ResolveMinimumSeparation`, 3 iterations) enforces the minimum when compressed.
- **Drive fraction:** `DriveFraction = DesiredSpeed / MaxSpeed`.
- **Mass:** a driving soldier is up to `1 + PressureMassPerDrive = 4×` heavier than a settled one (`PressureMassPerDrive = 3.0`). It shoves settled teammates aside and passes approach velocity into them.
- **Cross-troop separation:** soldiers of different troops repel with a distance falloff (`CrossTroopSeparation`).
- **Contact and knock-back:** soldiers of different troops that overlap within `ContactDistance = 0.85` are pushed apart. Knock-back on clash is kept (§4.5).

### 2.3 Exemptions & Constraints

- **Charging soldiers and their passive followers** (§4.4) are **excluded from intra-troop separation, density spreading and drive pressure, in both directions**. They slip through their own ranks.
- **Retreating and charging soldiers** are exempt from the troop radius pull.
- **Retreat (`FaultSlipMotion`):** a retreating soldier queries the field shifted back by its fault offset. Rear ranks fall back first and front ranks catch up. The base shape does not deform.
- **Terrain and radius:** non-exempt soldiers are clamped to the troop radius and checked against impassable terrain with `CircleCast` (`SoldierMotionConstraints`).

### 2.4 Allied Pivot Separation

- Pivots of **friendly** troops (same army) keep a minimum spacing of `PivotSeparationScale × (r_A + r_B)`, with `PivotSeparationScale = 0.25` **[PROVISIONAL]**. This prevents blobbing.
- **Line endpoints are not part of this check.** Endpoint attachment (§3.4) is never blocked by pivot separation.
- Whether linked pairs are exempt from pivot separation **[Q-15]**.

---

## 3. Formations, Movement & Geometry

### 3.1 Formation States

Ascending priority. A higher state overrides a lower one.

| Priority | State | Trigger |
|---|---|---|
| 1 | **Idle (Blob)** | Default when stationary and not in another state. |
| 2 | **Column** | Executing a move order. |
| 3 | **Line** | Enemy within `S_Distance`, or a player line order. |
| 4 | **Chain** | Linked to a friendly neighbour while in Line. |
| 5 | **Rout** | Break with `EffectiveMorale < RoutMoraleThresholdFraction × InitialEffectiveMorale` (§5). |

- A **chained troop that is moving marches in Line formation, not Column**. This is intended.
- `S_Distance` is measured between footprints **[Q-16]**. It has **no hysteresis**.

### 3.2 Idle (Blob) & Column

**Idle (Blob):** a disc of radius `BlobRadiusScale × TroopRadius` (`0.85`) around the pivot, with a zero-length core.

**Column (Marching):**
- **Trigger:** active move order (`Move` or `Pursue`) and not overridden by Line, Chain or Rout.
- **Pathing:** the pivot follows a `NavPathfinder` path toward the destination. The path is rebuilt every `PathRebuildIntervalSeconds = 0.5` or when the destination changes.
- **Pivot speed:** `Move × SpeedUnitsPerStatPoint` (§1.3).
- **Geometry:** the core runs behind the pivot along the travel direction. Lanes are added as the troop grows, so the column stays within the troop radius.
- **Choke points:** in narrow terrain, `SoldierMotionConstraints` and density spreading compress the lanes. High drive pressure pushes trailing soldiers forward.
- **Arrival:** the troop becomes Idle. Any move order is complete.

### 3.3 Battle Line & Footprints

**Triggers**
1. **Automatic:** an opposing troop enters `S_Distance = 30.0` (footprint gap). The troop turns to face the threat.
2. **Player line order** (`TroopLineOrder`): the troop marches its pivot to the line centre, turns to the ordered facing, and holds the line while stationary (`HasIdleLine = true`).

**Geometry**
- Pivot **P** and two line points **L_Left** and **L_Right**.
- **Natural length:** `L_nat = NaturalLineRadiusScale × TroopRadius`, with `NaturalLineRadiusScale = 1.8` **[PROVISIONAL, Q-12]**. This does not depend on the living soldier count.
- **Soldiers per rank:** `floor(L / LineSpacing)`, with `LineSpacing = 0.9`. The number of ranks is derived from the soldier count.
- **Footprint half-width:** the furthest soldier on either side of the centre line plus `SoldierRadius`.
- **Thick line:** an oriented rectangle with four faces: **Front, Left Flank, Right Flank, Rear**.
- **Joint clearance:** at a linked end, the soldier field core is pulled inward by a small clearance so soldiers stay off the joint (`min(0.45, 0.15 × length)`, existing values, **[PROVISIONAL]**).
- **Facing:** locked while engaged (§4.1). The line does not rotate toward a moving enemy during melee.

**Line-vs-line pressure (`TroopSegmentPressureRules`)**
- When two opposing footprints are within `A_Distance = 5.2` (footprint gap), a resistive barrier keeps a clear gap of `LineGap = 0.3` between them.
- **Stored intrusion:** the barrier tracks, per enemy pair, how far each pivot has advanced into the gap along the engagement axis. Restoring force is shared strictly in proportion to stored intrusion. A stationary troop is never pushed back only because an enemy footprint widened.
- **Resistive term:** only opposes a troop that is advancing (has an order).

### 3.4 Chain (Linked Battle Lines)

**Linking**
- A chain forms between a free **Line Point** on troop A and a free Line Point on troop B when:
  - Both are free (not already linked).
  - The endpoints are within `R_AttachScale × L_nat` (`0.25`).
  - Complementary wings meet (`L_A,Right ↔ L_B,Left`, and the reverse), and their forward vectors have a positive dot product (`F_A · F_B > 0`).
- Each troop has at most `MaxChainLinks = 2` links, one per Line Point.
- Attachment is endpoint-to-endpoint, not pivot-to-pivot.

**Hinge geometry**
- **Shared joint J:** the weighted average of the two natural endpoints, clamped so each troop's line length stays within `ChainMinStretchScale = 0.7` to `ChainMaxStretchScale = 3.0` × `L_nat`.
- **Inner hinge angle θ:** the angle at **J** between the two troops' lines (straight chain = 180°).
- **Minimum inner angle:** `ChainMinInnerAngleDeg = 65°`. A sharper hinge breaks the link **[Q-13]**.

**Breaking**
- **Stretch:** a link breaks when the length required to keep J within the envelope exceeds `ChainMaxStretchScale × L_nat` (3.0). Stretch is the only distance-based break rule.
- **Hinge:** a link breaks when θ < 65°.
- **Obstruction:** each frame, if impassable terrain bisects the segment between linked pivots through J, or blocks J, the link breaks (§3.4.1).
- **Rout:** a routing troop breaks both its links immediately (§3.4.1).
- **Player unlink:** on an order outside the envelope (§6.3).
**Shared retreat**
- When a troop retreats by distance `d`, each neighbour's shared joint pulls back immediately.
- A neighbour that is **not engaged** retreats sympathetically when its stretch exceeds `ChainRetreatStretchThreshold = 1.4 × L_nat` or its hinge bend (`180° − θ`) exceeds `ChainRetreatHingeBendDeg = 30°`.
- **Falloff:** each hop passes on `(1 − 0.30) = 70%` of the retreat distance **[PROVISIONAL, Q-21]**.
- Engaged neighbours do not retreat sympathetically **[Q-20]**. Their link is held by tension (§3.4.1).

#### 3.4.1 Edge Cases & Topology

- **No loops:** before linking two free Line Points, `AlliedLineConnector` checks whether both troops are already in one chain. If so, the link is rejected. Chains are always open paths.
- **No link stealing:** already-linked Line Points are excluded from candidate searches. A third troop marching behind a joint can never take it.
- **Tie-break:** if two troops enter `R_AttachScale` of the same free Line Point on the same tick, `LineLinkAssigner` picks the shortest endpoint distance. Exact ties go to the lower troop instance ID.
- **Obstruction bisection:** linked chains are checked every frame for impassable terrain between linked pivots (see Hinge geometry above).
- **Mid-chain rout:** if troop 3 routs in a `1–2–3–4–5` chain, both of its links sever at once. This leaves `1–2` and `4–5`. Shared retreat does not cross the gap.
- **Endpoint snap-back damping:** an inner endpoint that was stretched when its link breaks does not teleport to natural length. It relaxes toward `L_nat` at `ChainEndpointRelaxSpeed = 2.0` units/s.
- **Pinned anchor:** if one end of a chain is engaged while neighbours march or retreat away, `AlliedLinkTension` applies a damped spring to the **unengaged** side only. Tension never forces an engaged troop out of melee. If the stretch limit (§3.4) or hinge limit is exceeded, the link snaps instead of dragging the engaged troop.

---

## 4. Combat

### 4.1 Engagement

- **Engagement:** two opposing troops whose footprints are within `A_Distance = 5.2` (footprint gap). They halt and face each other across `LineGap = 0.3`.
- **Facing lock:** an engaged troop's facing does not rotate while engaged.
- **Entry and exit:** hysteresis on engagement exit is not specified **[Q-16]**.

### 4.2 Assault & Pursuit

- **Assault trigger:** a troop with an active order (`Move`, `Pursue` or `Line`) that moves into engagement with an enemy enters **Assault** for `AssaultDurationSeconds = 10.0`.
- **Pursue end condition:** [Q-24]
- **Assault buffs** (all soldiers in the troop):
  - `AssaultAggressionBonus = +5` to Aggression.
  - `AssaultChargeBonus = +5` to Charge. Applies only while charging.
- **Pursuit:** only by an explicit `Pursue` order. A pursuing troop that re-engages an enemy which retreated **refreshes** Assault (timer and bonuses reset to 10 s).
- Troops without an order do not gain Assault, even when engaged.

### 4.3 Charges

- **Charge range:** `ChargeRangeScale × A_Distance = 1.5 × 5.2 = 7.8`. A soldier may charge if an enemy soldier is within this range **[Q-25]**.
- **Rate:** each soldier charges at an expected rate of `EffectiveAggression / ActionWindowSeconds` per second. `EffectiveAggression = Aggression + AssaultAggressionBonus (if Assault)`. Each frame, the chance is `1 − exp(−rate × dt)`.
- **Cooldown after a charge:** none beyond the rate and the clash rule [Q-29].
- **Target:** a charger picks one of the `ChargeCandidateCount = 3` nearest enemy soldiers at random.
- **Speed:** a charger moves at `ChargeSpeedScale = 2.0×` normal speed (max speed and acceleration).
- **One charge = one clash.** A charge ends on contact (§4.5).
- **Cancel:** the charge is cancelled if the target dies, the charger's troop breaks, or `ChargeTimeoutSeconds = 3.0` elapses **[PROVISIONAL, Q-27]**.
- **Neighbour contagion:** no. The charge rate depends only on the soldier's own Aggression and Assault. The Ising-style neighbour model in the current code is removed **[Q-30]**.

### 4.4 Discipline & Charge Support

- When a soldier starts a charge, the troop's `Discipline` selects the `Discipline` nearest **friendly soldiers in the same troop** **[Q-9]**.
- Each selected soldier follows with probability `ChargeFollowProbability = 0.50`.
- **Followers are passive:**
  - They move with the charger to stay within `SupportRadius` of it.
  - They **bypass** separation, density spreading and drive pressure, the same as the charger.
  - They provide `SupportBonus` to the charger's clash.
  - They **do not** start clashes of their own, even when they overlap an enemy.
  - After the clash, they return to the line.

### 4.5 Clash & Duel

**Contact and trigger**
- **Contact:** centres closer than `ContactDistance = 0.85`.
- A clash occurs when:
  - (a) a charger reaches contact with an enemy soldier, or
  - (b) two opposing soldiers are in contact and neither is charging.
- **Rate limit (case b):** each soldier takes part in at most one clash per `ClashCooldownSeconds = 1.0` **[PROVISIONAL, Q-26]**.
- **Knock-back:** both soldiers are knocked back on clash. Magnitudes are kept from the current code **[PROVISIONAL]**.

**Combat Power (both sides, at contact)**

$$\text{CombatPower} = \text{WeaponSkill} + \text{ChargeBonus (if charging)} + \text{SupportBonus}$$

- `WeaponSkill` is for the class of the weapon in use.
- `ChargeBonus = Charge + weapon ChargeBonus + AssaultChargeBonus (if Assault)`.
- When **both** soldiers are charging, **both** receive the charge bonus.
- `EquipmentClashBonus` is removed. Weapon charge bonuses count only while charging **[Q-4]**.
- The duel roll's attacker/defender labels are defined in the next subsection **[Q-28]**.

**Support**
- Eligible supporters are **non-fatigued, same-army** soldiers within `SupportRadius = 3` of the clashing soldier. A Boar Spear supporter counts within `2 × SupportRadius = 6`.
- `SupportingSoldiers = min(EligibleNearbyAllies, Support)`.
- `SupportBonus = SupportingSoldiers × SupportBonusPerAlly`, with `SupportBonusPerAlly = 2`.
- **Fatigue:** soldiers at or below `FatigueFraction = 0.50` of `MaxHP` cannot support.
- **Flanking:** in clashes against an enemy troop attacking this troop's Flank or Rear face, this troop receives **0** support (§3.3, §4.8).

**Duel roll**
- Let **A** be the charger and **B** the defender. If both are charging, A is the one whose charge began first. If neither is charging, A is the lower soldier ID. **[Q-28]**
- $P_A = \dfrac{\text{CombatPower}_A}{\text{CombatPower}_A + \text{CombatPower}_B}$
- Roll $R \in [0, 100]$. A wins if $R \le P_A \times 100$.

**Domination**
- The winner gains bonus damage:

$$\text{Domination} = \text{round}\left(\frac{|R - P_{A}\%|}{\text{DominationDivisor}}\right), \quad \text{DominationDivisor} = 10$$

- Rounded to the nearest whole number. Worked example: `P_A = 60%`, `R = 40%`. A wins (`40 ≤ 60`) with Domination `|40 − 60| / 10 = 2`.
- If the defender B wins (`R > 60`), it gains `|R − 60| / 10`, using the same formula. The formula is symmetric about `P_A`.

### 4.6 Damage, Mitigation & Combined HP Pool

**Raw melee damage**

$$\text{Damage} = \text{Strength} + \text{WeaponDamageBonus} + \text{Domination}$$

- Lance bearer: total damage is **doubled** while charging **[Q-8]**.

**Defence**
- $\text{TotalDefence} = \text{Defence} + \text{ArmourDefenceBonus}$.
- **Up to `TotalDefence`:** each whole point of damage rolls once against the defender's armour `Resistance`:
  - **Deflect:** point ignored. 0 HP, 0 Shock.
  - **Bruise:** temporary HP loss. Counts for Shock.
  - **Bleed:** permanent HP loss. Counts for Shock.
- **Above `TotalDefence`:** `Damage − TotalDefence` is converted directly to **Bleed**. Counts for Shock.

**Bruise recovery**
- Each soldier's Bruise pool heals **1 HP per second**. This replaces the old bruise timer.
- Bruises can kill, because the HP pool is shared.

**Combined HP pool**

$$\text{CurrentHP} = \text{MaxHP} - \text{BleedDamage} - \text{BruiseDamage}$$

- `CurrentHP ≤ MaxHP × FatigueFraction`: **Fatigued**.
- `CurrentHP ≤ 0`: **dies**.
- **Overflow:** damage beyond the HP a soldier has left does not count toward Shock.

### 4.7 Ranged Combat

**Firing condition**
- A troop with a ranged weapon that is **stationary and not engaged** fires at enemy troops in front of its line, within `WeaponRange`.
- Ranged troops carry a melee weapon for melee (§1.6). Whether they also fire while in melee is covered by [Q-46].

**Targeting**
- The **nearest enemy troop** in range and in front of the line is chosen. The **nearest soldiers** of that troop are shot at **[Q-41]**.
- The troop does **not** fire into melee **[Q-39]**.
- **No friendly fire** **[Q-40]**.
- **Ammunition:** unlimited.

**Projectiles**
- Shots are **projectiles**, not instant hits **[Q-42]**.
- **Ranged attacks do not gain flanking bonuses.**

**Fire timing**
- Interval between shots: `ActionWindowSeconds / FireRate` (30 / 10 = 3 s for a Long bow), with ±`FireRateJitterFraction = 0.10` random variation [Q-43].
- After each shot, the troop waits one interval before the next.

**Hit probability**
- Uses `BaseShotDC = 10`:

$$P(\text{Hit}) = \frac{\text{Accuracy} + \text{WeaponSkill}}{\text{Accuracy} + \text{WeaponSkill} + \text{BaseShotDC}} \quad \text{[Q-44]}$$

- Example: Long bow (`Accuracy = 2`), skill 6: `P(Hit) = 8 / 18 ≈ 44%`.

**Damage**
- `RangedDamage = WeaponDamage` (for Long bow, 10), mitigated by the target's `TotalDefence` and armour `Resistance` (§4.6) **[Q-5]**.

### 4.8 Faces & Flanking (per opponent)

- Each engaged troop's footprint has four faces: **Front, Left Flank, Right Flank, Rear**.
- The **attacking face** is determined for each opposing troop **[Q-31]**. Troops in Blob or Column have no rectangle; how they are classified is open **[Q-32]**.
- **Front:** standard frontal engagement.
- **Flank or Rear:** the clash is **flanked**.
- **Flank penalties, per clash scope:**
  - Clashes against the flanking enemy: **0 support** received, and damage sustained in those clashes counts **×3 Shock** (`FlankShockMultiplier = 3.0`).
  - Clashes against enemies on the front face resolve normally.
- The facing damage multiplier from the current code (4× / 2× / 1× by angle) is **[DEPRECATED]**.

### 4.9 Surrounded

- A troop is **Surrounded** when its retreat is blocked. Retreat is blocked if the path of length `RetreatDistance` behind the rear face is obstructed by terrain or by other troops **[Q-33]**.
- Surrounded troops cannot retreat. See §5.

---

## 5. Morale, Shock, Retreat & Rout

### 5.1 Effective Morale

$$\text{InitialEffectiveMorale} = \text{BaseMorale} \times \text{InitialSoldierCount}$$

$$\text{EffectiveMorale} = (\text{BaseMorale} \times \text{CurrentSoldierCount}) - (\text{RetreatCount} \times \text{RetreatMoralePenalty}) - (\text{SurroundedBreakCount} \times \text{SurroundedMoralePenalty})$$

- `RetreatMoralePenalty = 5`, `SurroundedMoralePenalty = 20`.
- Worked example: Franz (`BaseMorale = 5`, `InitialSoldierCount = 12`) starts at 60. After losing 2 soldiers he has 50.

### 5.2 Shock

- **Accumulation:** each **applied** point of Bruise or Bleed adds 1 Shock. Deflect adds 0. Overflow adds 0 (§4.6). Flank hits are multiplied by `FlankShockMultiplier = 3.0` (§4.8).
- **Continuous decay (`ShockDecayFractionPer30s = 0.20`):** Shock decays at all times, including in melee, clamped at 0:

$$\text{ShockDecayRatePerSecond} = \frac{0.20 \times \max(0, \text{EffectiveMorale})}{30}$$

- Worked example: Franz with 50 EffectiveMorale decays `0.20 × 50 / 30 = 0.33` Shock/s.

### 5.3 Break Trigger

- A break occurs whenever **`Shock > EffectiveMorale`**. This happens when Shock rises, or when EffectiveMorale falls below current Shock through soldier deaths or penalties.
- On a break the troop tests:

| Case | Condition | Result |
|---|---|---|
| **A: Rout** | `EffectiveMorale < 0.50 × InitialEffectiveMorale` (regardless of surround) | §5.4 |
| **B: Retreat** | `EffectiveMorale ≥ 0.50 × InitialEffectiveMorale` and not Surrounded | Retreat (§5.5) |
| **C: Surrounded break** | `EffectiveMorale ≥ 0.50 × InitialEffectiveMorale` and Surrounded | Surrounded break (§5.6) |

### 5.4 Rout

- **Final.** A routed troop does not recover.
- **Effects:** unlinks from any chain at once (§3.4.1), forms a loose blob, and flees.
- **Flee direction:** away from the nearest enemy **[Q-35]**.
- **Attackable:** yes. A fleeing troop receiving a winning clash deals no damage in the current code **[Q-37]**.
- **Counts as destroyed** for `Rout.cs` (`OnUnitDestroyed`) and for the battle win condition. The event is raised at rout, not at despawn **[Q-36]**. Battle-level wiring is out of scope **[Q-49]**.
- **Despawn:** after `RoutDespawnSeconds = 60`, regardless of position.

### 5.5 Normal Retreat (Case B)

1. The troop performs an accordion retreat (`FaultSlipMotion`). Rear soldiers fall back first and front ranks catch up.
2. Retreat distance is `RetreatDistance` **[PROVISIONAL, Q-34]**.
3. Shock resets to 0.
4. `EffectiveMorale` is reduced by `RetreatMoralePenalty = 5`.

### 5.6 Surrounded Break (Case C)

1. The troop **cannot retreat**. No `FaultSlipMotion`.
2. Shock resets to 0.
3. `EffectiveMorale` is reduced by `SurroundedMoralePenalty = 20`.

### 5.7 Cooldowns

- `BreakCooldownSeconds = 1.6` and `ChargeLockAfterBreakSeconds = 1.2` are kept as **[PROVISIONAL, Q-34]**.

---

## 6. Player Controls & Orders

Player input (`TroopPlayerOrders`, `TroopSelection`, `TroopOrderPlanner`, `TroopOrderOverlay`) reads the new Input System
directly. Input thresholds are in **screen pixels** so they do not depend on zoom.

### 6.1 Selection (LMB)

- **Click:** selects the nearest player-team troop under the cursor (pivot/footprint, screen-space radius). Clicking empty ground clears the selection, unless Shift is held.
- **Box select:** a drag of at least `BoxSelectMinDragPixels = 8` px draws a marquee. All player troops with a pivot or any living soldier inside the box are selected [Q-48].
- **Shift:** **toggles** membership of the clicked or boxed troops. A selected troop is deselected, and an unselected troop is added.
- **Hover:** every frame, over any team.

### 6.2 Orders (RMB)

The order type is determined when the button is released.

**Click or drag below `OrderDragMinPixels = 8` px [PROVISIONAL, Q-47]: point order**
- **On an enemy troop:** `Pursue`. Every selected troop pursues. Assault applies on engagement (§4.2).
- **On empty ground:** `Move`. Selected troops keep their relative offsets and translate to the target. On arrival they become Idle [Q-14].

**Drag at or above `OrderDragMinPixels`: battle line (`OrderLine`)**
- **Tangent:** `T = D_end − D_start` is the lateral direction of the line.
- **Facing:** the front face is the normal on the side nearest the nearest enemy troop. If no enemy is present, the front is the right-hand normal of `T` **[Q-17]**.
- **Single troop:** marches to the midpoint of `[D_start, D_end]`, turns to the facing, and holds the line (`HasIdleLine = true`). Line width equals the drag length, clamped to `[MinimumLineWidth, 1.8 × Radius]`.
- **Multiple troops (chain):** the span is split across the `N` selected troops in order along the drag direction **[Q-18]**. Each troop's width is its share of the span. Adjacent troops' line points meet at joints. The order creates the links explicitly for each adjacent pair **[Q-19]**. Each troop's length stays within `0.7–3.0 × L_nat`.
- **Ghost preview:** while the button is held, `TroopOrderOverlay` draws each troop's projected footprint, facing arrows and the shared joints.

### 6.3 Chain Manipulation

**Envelope.** A destination is inside the chain envelope if the resulting stretch is `≤ 3.0 × L_nat` **and** the inner hinge angle is `≥ 65°`.

- **Staying chained:** a single linked troop ordered to a destination **inside the envelope** stays chained. Its joint J hinges, and the adjacent lines stretch or angle.
- **Exiting:** a single linked troop ordered to a destination **outside the envelope** is unlinked **on mouse release**, at the moment the order is issued.
- **Ghost tether** (shown while a single chained troop's destination is previewed):
  - **Solid green/white:** inside the envelope. The troop stays chained.
  - **Dashed red:** outside the envelope, or Alt is held. Release severs the link.
- **Middle extraction:** removing troop B from `A–B–C` leaves A and C with free inner Line Points. They do **not** auto-link across the gap, even within 3.0 × stretch. Auto-linking only uses `R_AttachScale`.
- **ChainSuppressed:** a troop detached by a single-unit order is flagged `ChainSuppressed` so it does not relink while marching past neighbours. The exact end condition is **[Q-22]**.

**Alt modifier**
- **Alt + move order on a chained troop:** forces an immediate unlink, even for a micro-adjustment inside the envelope.
- **Alt + move or line-drag on unlinked reserves:** suppresses automatic chain linking (`ChainSuppressed = true`), so reserves can sit close behind a line without snapping to its wings. Duration **[Q-23]**.

---

## 7. Glossary & Tuning

### 7.1 Glossary

| Term | Definition |
|---|---|
| **Troop** | The commandable unit, composed of Soldiers. |
| **Soldier** | An autonomous fighter steered by its troop's vector field. |
| **Character** | A campaign commander commanding one soldier type. Can be paired pre-battle into a Composite Troop. |
| **Composite Troop** | A troop made from paired Characters. Takes min Move, max Morale, max Discipline, and the sum of soldier counts. |
| **Drive Fraction & Pressure** | Ratio of desired to max speed. Scales dynamic mass up to 4×. |
| **Idle / Column / Line / Chain / Rout** | The five formation states, in ascending priority. |
| **Thick Line (Rectangle)** | The oriented footprint of a Line troop. Its four faces decide flanking. |
| **Engaged** | Two opposing footprints within `A_Distance` (5.2). |
| **Assault** | 10 s buff (+5 Aggression, +5 Charge while charging) granted to an advancing troop that enters engagement. Refreshed by pursuit. |
| **Pursuit** | Following a retreating enemy under an explicit Pursue order. Refreshes Assault. |
| **Surrounded** | A troop whose retreat path is blocked. Cannot retreat. |
| **Charge** | A soldier leaves its line at double speed to clash with one of the 3 nearest enemy soldiers. One charge = one clash. |
| **Charge Support** | Friendly soldiers chosen by Discipline who follow a charger and provide support without starting clashes. |
| **Clash & Duel** | A melee contact (`Clash`), resolved by a probability roll on Combat Power (`Duel`). |
| **Domination** | Bonus damage to the duel winner, from how far the roll beat their win chance. |
| **Deflect / Bruise / Bleed** | Outcomes of rolling damage against Defence. Bruise heals at 1 HP/s. Bleed is permanent. |
| **Fatigue** | `CurrentHP ≤ 50% MaxHP`. Fatigued soldiers cannot support. |
| **Effective Morale** | The troop's morale pool (`BaseMorale × Count` minus penalties). The Shock threshold. |
| **Shock** | Damage-driven stress. Decays continuously. Break when `Shock > EffectiveMorale`. |
| **ChainSuppressed** | Flag preventing a detached troop from auto-relinking. |
| **Hinge (J)** | The joint between two chained troops. |
| **Inner hinge angle** | Angle at J between the two troops' lines. Straight = 180°. |

### 7.2 Tuning Variables

Constants marked **[Q-n]** have an open question. Values marked `PROVISIONAL` may change.

**Stats & Soldiers**

| Name | Value | Unit | Description |
|---|---|---|---|
| `CombatStatMin / Max / Default` | 1 / 20 / 5 | int | Range and default for every combat stat (`CombatStatLimits`). |
| `HpMin / HpMax / HpDefault` | 10 / 40 / 15 | int | Range and default for soldier HP (`CombatStatLimits`). |
| `SpeedUnitsPerStatPoint` | 0.6 | units/s per point **[Q-1]** | Converts Speed/Move to units/s. |
| `ActionWindowSeconds` | 30.0 | s | Window for Aggression and FireRate. |
| `SoldierRadius` | 0.3 | units | Soldier collision radius. |
| `SeparationRadiusScale` | 3.0 | × | `SeparationRadius = 3 × 0.3 = 0.9`. |
| `SpreadRadius` | 1.2 | units | Density spreading radius. |
| `PressureMassPerDrive` | 3.0 | × | Mass gain at full drive (up to 4× total). |
| `PivotSeparationScale` | 0.25 | × (r_A + r_B) **[PROVISIONAL]** | Minimum friendly pivot spacing. |

**Formation & Movement**

| Name | Value | Unit | Description |
|---|---|---|---|
| `LineSpacing` | 0.9 | units | Soldier spacing along a line and between ranks. |
| `NaturalLineRadiusScale` | 1.8 | × TroopRadius **[PROVISIONAL, Q-12]** | `L_nat = 1.8 × TroopRadius`. |
| `PathRebuildIntervalSeconds` | 0.5 | s | Path rebuild interval. |
| `BlobRadiusScale` | 0.85 | × | Idle disc radius. |
| `S_Distance` | 30.0 | units (footprint gap) **[Q-16]** | Skirmish distance. No hysteresis. |
| `A_Distance` | 5.2 | units (footprint gap) | Engagement distance. |
| `LineGap` | 0.3 | units | Clear gap between opposing footprints. |
| `ContactDistance` | 0.85 | units | Centre distance for a clash. |
| `JointClearance` | `min(0.45, 0.15 × length)` **[PROVISIONAL]** | units | Pull-in at linked ends. |

**Combat**

| Name | Value | Unit | Description |
|---|---|---|---|
| `ChargeRangeScale` | 1.5 | × A_Distance | Charge range = 7.8 units. |
| `ChargeSpeedScale` | 2.0 | × | Charger speed multiplier. |
| `ChargeCandidateCount` | 3 | int | Nearest enemy soldiers a charger picks from. |
| `ChargeTimeoutSeconds` | 3.0 **[PROVISIONAL, Q-27]** | s | Charge cancel timeout. |
| `ChargeFollowProbability` | 0.50 | 0–1 | Chance each Discipline-nearest friend follows. |
| `ClashCooldownSeconds` | 1.0 **[PROVISIONAL, Q-26]** | s | Per-soldier clash rate limit (non-charge contacts). |
| `AssaultDurationSeconds` | 10.0 | s | Assault length. |
| `AssaultAggressionBonus` | 5 | int | Aggression bonus during Assault. |
| `AssaultChargeBonus` | 5 | int | Charge bonus during Assault, while charging. |
| `SupportRadius` | 3.0 | units | Support range. |
| `BoarSpearSupportRangeMultiplier` | 2.0 | × | Boar Spear support range. |
| `SupportBonusPerAlly` | 2 | int | Combat Power per supporting ally. |
| `FatigueFraction` | 0.50 | 0–1 | Fatigue threshold. |
| `DominationDivisor` | 10.0 | float | Divisor in the Domination formula. |
| `FlankShockMultiplier` | 3.0 | × | Shock multiplier on flank hits. |
| `BaseShotDC` | 10 | int | Shot difficulty class. |
| `FireRateJitterFraction` | 0.10 | 0–1 | Random variation on fire interval. |
| `BruiseHealPerSecond` | 1 | HP/s | Bruise recovery per soldier. |

**Morale & Retreat**

| Name | Value | Unit | Description |
|---|---|---|---|
| `ShockDecayFractionPer30s` | 0.20 | 0–1 | Shock decay as a fraction of EffectiveMorale per 30 s. |
| `RoutMoraleThresholdFraction` | 0.50 | 0–1 | Rout below this fraction of Initial EffectiveMorale. |
| `RetreatMoralePenalty` | 5 | float | Per normal retreat. |
| `SurroundedMoralePenalty` | 20 | float | Per surrounded break. |
| `RetreatDistance` | `1.0 × TroopRadius` **[PROVISIONAL, Q-34]** | units | Retreat distance. |
| `BreakCooldownSeconds` | 1.6 **[PROVISIONAL, Q-34]** | s | Cooldown after a break. |
| `ChargeLockAfterBreakSeconds` | 1.2 **[PROVISIONAL, Q-34]** | s | No new charges after a break. |
| `RoutDespawnSeconds` | 60 | s | Despawn time after rout. |

**Chain**

| Name | Value | Unit | Description |
|---|---|---|---|
| `MaxChainLinks` | 2 | int | Links per troop (one per Line Point). |
| `ChainMinStretchScale` | 0.7 | × L_nat | Minimum line length. |
| `ChainMaxStretchScale` | 3.0 | × L_nat | Maximum line length. Break threshold. |
| `ChainMinInnerAngleDeg` | 65.0 | deg | Minimum inner hinge angle. Break threshold. |
| `R_AttachScale` | 0.25 | × L_nat | Endpoint-to-endpoint attach distance. |
| `ChainSuppressCooldownSeconds` | 3.0 **[Q-22]** | s | Relink cooldown after leaving attach range. |
| `ChainRetreatStretchThreshold` | 1.4 | × L_nat | Stretch that triggers sympathetic retreat. |
| `ChainRetreatHingeBendDeg` | 30.0 **[Q-13]** | deg | Hinge bend (180° − inner) that triggers sympathetic retreat. |
| `ChainRetreatFalloff` | 0.30 **[PROVISIONAL, Q-21]** | 0–1 | Retreat decay per hop. |
| `ChainEndpointRelaxSpeed` | 2.0 | units/s | Relax rate for an unlinked stretched endpoint. |

**Input**

| Name | Value | Unit | Description |
|---|---|---|---|
| `BoxSelectMinDragPixels` | 8 | px | Minimum drag for box select. |
| `OrderDragMinPixels` | 8 **[PROVISIONAL, Q-47]** | px | Minimum drag for a battle-line order. |

### 7.3 Equipment Sample Values

See §1.6. These are the starting values for the data assets.

### 7.4 Removed Values

| Removed | Replaced by |
|---|---|
| `LungeDuration`, `MeleeBeatInterval`, `PerMeleeTime` (name) | Charge system; `ActionWindowSeconds`. |
| `MaxPendingAttacks` and banked attacks | Stochastic charge rate. |
| `BruiseDurationSeconds` | `BruiseHealPerSecond`. |
| `R_BreakScale` | `ChainMaxStretchScale`. |
| `ChainMaxBendAngleDeg` | `ChainMinInnerAngleDeg`. |
| `PikeSupportRangeMultiplier` | Boar Spear special rule. |
| `BowRange`, `BowAccuracy`, `BowDamage`, `BowFireRate` | Weapon data (§1.6). |
| `FaultPressure` | Removed. |
| Facing damage multiplier (4×/2×/1×) | Removed. Face penalties (§4.8). |
| Neighbour charge contagion | Removed (§4.3). |
