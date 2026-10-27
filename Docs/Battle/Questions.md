# Open Questions: Battle System Specification v0.9

Each entry is referenced from `specifications.md` as `[Q-n]`. "Blocks Phase NN" means the question must be answered (or its
assumption accepted) before that Plan phase (see `Plan/00-overview.md`).

Each entry has a **Current assumption**: the value the spec or Plan currently uses. Where neither document gives a value,
the assumption is marked **Working assumption** and is not yet in `specifications.md`.

Priority:
- **P1:** blocks implementation of a phase.
- **P2:** affects tuning or behaviour, but a default is in place.
- **P3:** wording or minor edge case.

---

## Stats, Equipment & Data

**Q-1 (P2, blocks Phase 01). Speed unit conversion.**
Spec uses `SpeedUnitsPerStatPoint = 0.6` (Speed 5 = 3.0 units/s, matching the current Swordsmen asset).
Confirm, or give the intended units/s for the 1–20 scale.
> **Current assumption:** `SpeedUnitsPerStatPoint = 0.6`. Speed 5 = 3.0 units/s. Move uses the same scale.

**Q-2 (P1, blocks Phase 01). Scales for Defence, Charge, WeaponSkill, and unskilled default.**
- Defence, Charge and WeaponSkill are assumed to be 1–20 with base 5. Confirm.
- When a soldier has no skill in a weapon class, is the skill 0, 1, or 5?
- Does `Charge` have a base value of 5, and does it affect Combat Power while not charging?
> **Current assumption:**
> - **Resolved (2026-10-26):** every combat stat is an integer from 1 to 20, default 5. This includes Defence, Charge and WeaponSkill. HP is an integer from 10 to 40, default 15 (Decisions D-22, D-23). The character sheets give no Defence, so every soldier uses 5 (D-08).
> - **Still open:** the unskilled value. A class the soldier has no skill in is 0 (working assumption). A listed skill is never 0, so 0 is reserved for "not listed". The soldier then gets no WeaponSkill term in Combat Power.
> - `Charge` affects Combat Power **only while charging** (spec §4.5). It has no effect while the soldier is not charging.

**Q-3 (P2, blocks Phase 01). Two melee weapons.**
A soldier can carry 2 weapons. Which melee weapon is used in melee: the first listed, the one with the higher skill, or a random choice?
Are weapons assigned per soldier profile or per troop?
> **Current assumption:** Weapons are assigned **per soldier profile** (up to 2 in a list). `MeleeWeapon` is the first melee weapon listed (Decisions D-04).
> **Affected sheets:** Vance (Lance + Arming Sword), Franz (Pike + Dagger) and Borgo (Spiked Club + Knife) each carry two melee weapons. Under the current assumption the second one is carried but never used. Confirm, or choose a rule (random, higher skill, or a separate effect).

**Q-4 (P2, blocks Phase 04). Weapon ChargeBonus.**
Proposed: the weapon's `ChargeBonus` is added to `Charge` only while charging, and only to Combat Power. Confirm. (This replaces `EquipmentClashBonus`.)
> **Current assumption:** Yes. Weapon `ChargeBonus` counts only while charging (spec §4.5).

**Q-5 (P2, blocks Phase 09). Ranged damage.**
Proposed: `RangedDamage = WeaponDamage` only, with no Strength. Confirm. Long bow damage is 10.
> **Current assumption:** Yes. `RangedDamage = WeaponDamage` (Long bow 10), mitigated by the target's `TotalDefence` and armour. Strength is not added.

**Q-6 (P3). Armour sample values.**
Light armour and shield is now Deflect/Bruise/Bleed 30/50/20 (v0.8: 30/30/40). Heavy armour is 60/30/10 (same as v0.8 "Chainmail+Shield"). Confirm the new Light values and the column order.
> **Current assumption:** Light armour and shield is 30/50/20, +4 defence. Heavy armour is 60/30/10, +7 defence. Columns are Deflect / Bruise / Bleed.

**Q-7 (P2, blocks Phase 04). Boar Spear support range.**
Proposed: a Boar Spear supporter's eligible range is `2 × SupportRadius = 6`, measured from the supporter to the clashing soldier. Confirm the direction of the rule and whether it applies only when the bearer is the supporter, not the charger.
> **Current assumption:** The doubled range (6) applies only when the **Boar Spear bearer is the supporter**, measured from the supporter to the clashing soldier. A Boar Spear carried by the charger has no effect on its own range.

**Q-8 (P3, blocks Phase 04). Lance doubling.**
Proposed: total raw damage is doubled before mitigation, while the bearer is charging. Confirm, including whether Domination is included.
> **Current assumption:** Raw damage (Strength + weapon bonus + Domination) is doubled before mitigation while the Lance bearer is charging. Domination is included in the doubled total.

**Q-9 (P2, blocks Phase 04). Charge followers' pool.**
Proposed: `Discipline` nearest soldiers from the **same troop**. The spec's "friendly" definition (same army) is used for support only. Confirm.
> **Current assumption:** Yes. Followers are drawn from the same troop, nearest first, up to `Discipline`. "Friendly" (same army) applies to support only.

**Q-10 (P3). Discipline range.**
`SoldierProfile` allowed 0–20 in code. v0.8 says 1–20 for Discipline. Proposed: 0–20 (0 = no followers). Confirm.
> **Resolved (2026-10-26):** Discipline is an integer from **1 to 20**, default 5 (D-24). The old proposal of 0 (no followers) is dropped.
> **Consequence to confirm (Q-58):** with a minimum of 1, every charge can pick at least one follower, so followers are never switched off. If you want a troop with no followers, the minimum has to go back to 0.

**Q-57 (P3, Phase 01). Integer weapon values.**
Weapon `Damage`, `ChargeBonus`, `Accuracy` and `FireRate` are now integers (the spec samples are whole numbers). `Range` stays a float in world units. The 1–20 cap applies to soldier stats only, so weapon values are not capped.
> **Current assumption (working):** Weapon values are integers with a minimum of 0 and no maximum. Confirm, or set a cap.

**Q-58 (P2, blocks Phase 04). Minimum Discipline of 1.**
See Q-10. Every troop can pick at least one charge follower, and each follower joins with 50% chance (`ChargeFollowProbability`), so a charger with no follower is still possible.
> **Current assumption:** Minimum 1 (D-24). Confirm.

---

## Geometry & Formation

**Q-11 (P1, blocks Phase 06). Troop radius and stretched lines.**
Soldiers are clamped to `TroopRadius`, but a line stretched to `3.0 × L_nat` cannot fit inside it. Options:
- (a) Linked-line soldiers may sit outside the radius, with a larger clamp when stretched.
- (b) `TroopRadius` grows when the line is stretched.
- (c) Lower the maximum stretch.
- (d) Something else.
Which do you want?
> **Current assumption:** No option chosen. The spec still has the radius clamp and the 3.0 × L_nat stretch limit, which cannot both hold. Until decided, implement the radius clamp unchanged and treat the stretch limit as a break threshold only (working assumption). Phase 06 cannot finish until this is answered.
> **Implemented (2026-10-26):** option (a), the soldier clamp widens to reach a stretched line's points (Decisions D-36). Confirm.

**Q-12 (P1, blocks Phase 06). Natural line length.**
Proposed: `L_nat = 1.8 × TroopRadius` (matching the current 1.8 × Radius cap), so it does not depend on soldier count. Confirm the scale, and how `TroopRadius` is set for each profile.
> **Current assumption:** `L_nat = 1.8 × TroopRadius`, independent of soldier count. `TroopRadius` is a `TroopProfile` field (code default 16.5). The character sheets do not give a radius. The generated sample troops use **radius 5 and line width 9** (1.8 × 5), which matches the existing Swordsmen profile (Decisions D-14). Confirm the radius per character. Note that 16.5 is much larger than the sample troops need.

**Q-13 (P1, blocks Phase 07). Hinge angle definitions.**
- Confirmed: the 65° limit is the **inner** angle at the joint. A hinge with an inner angle below 65° breaks.
- Not confirmed: whether the 30° retreat threshold is measured as bend (180° − inner) or as inner angle. Spec uses bend.
> **Current assumption:** The 65° break limit is the **inner** angle (180° = straight). The 30° sympathetic-retreat threshold is the **bend** (180° − inner), so 30° of bend means an inner angle of 150°.

**Q-14 (P3, blocks Phase 08). Move order arrival.**
Spec: troop becomes Idle (blob) on arrival. Current code keeps the last line facing after a multi-troop click. Which should happen?
> **Current assumption:** On arrival the troop becomes **Idle (blob)** (spec §6.2). Today's code keeps the line facing after a multi-troop click, which is the behaviour being replaced.

**Q-15 (P2, blocks Phase 06). Pivot separation and linked pairs.**
Spec exempts line endpoints from pivot separation. Should linked pairs also be exempt from pivot separation? Proposed: no, because linked pivots are already about `L_nat` apart.
> **Current assumption:** Linked pairs are **not** exempt. Pivot separation (`0.25 × (r_A + r_B)`) applies to all friendly pivots. Endpoints are exempt because they are not pivots.

**Q-16 (P2, blocks Phase 05). S_Distance and engagement measurement.**
Proposed: both `S_Distance` and `A_Distance` use footprint gap. The current code uses centre-line segment distance, with exit hysteresis at `A_Distance × 1.5`. The spec now has no S_Distance hysteresis. Does engagement exit have hysteresis? If so, what value?
> **Current assumption:** Both `S_Distance` (30) and `A_Distance` (5.2) use **footprint gap**. **No hysteresis** on either entry or exit. The current code's 1.5× exit hysteresis is removed.

**Q-17 (P1, blocks Phase 08). Line-drag facing.**
Spec: front faces the nearest enemy. If no enemy, the right-hand normal of the drag direction.
Can the player choose the front, for example with a modifier key or by dragging in the other direction? Confirm.
> **Current assumption:** The front faces the **nearest enemy**, or the right-hand normal of the drag if no enemy is present (spec §6.2). The player **cannot** choose the front. No modifier or drag-direction override exists.

**Q-18 (P3, blocks Phase 08). Multi-troop drag order.**
v0.8 says "left-to-right". Current code sorts by projection along the drag direction. Proposed: projection along the drag direction. Confirm.
> **Current assumption:** Troops are ordered by **projection along the drag direction** (spec §6.2). "Left-to-right" from v0.8 is dropped.

**Q-19 (P2, blocks Phase 08). Explicit links from drag.**
Proposed: a multi-troop drag creates links between adjacent troops explicitly. Alternative: rely on automatic attachment within `0.25 × L_nat`. Which?
> **Current assumption:** Spec §6.2 says the order **creates the links explicitly** for each adjacent pair. **Conflict:** Plan 08 §8d says to rely on automatic attachment until this is answered. Implement the spec's explicit links unless you decide otherwise, and update Plan 08 to match.
> **Implemented (2026-10-26):** explicit links, created as pending links that activate when both troops arrive (D-39).

**Q-20 (P1, blocks Phase 07). Engaged neighbour and sympathetic retreat.**
Spec (v0.8, §3.4 and §3.4.1) says a sympathetic retreat must not force an engaged troop out of melee. Proposed: engaged neighbours do not retreat sympathetically. Their link holds by tension, and it snaps at `3.0 × L_nat` or inner angle `< 65°`. Confirm.
> **Current assumption:** Engaged neighbours **do not** retreat sympathetically. Their link is held by tension and snaps at the 3.0 × L_nat stretch or the 65° inner-angle limit.

**Q-21 (P3, blocks Phase 07). Shared retreat falloff.**
Proposed: "30% falloff per hop" means each hop passes on 70% of the previous distance. Alternative: 30% of the retreat is passed on each hop (70% falloff). Confirm.
> **Current assumption:** Each hop passes on **70%** of the previous distance (30% falloff). Example: 10 units → 7 → 4.9.

**Q-22 (P1, blocks Phase 07). ChainSuppressed end condition.**
A troop detached by a single-unit order is flagged `ChainSuppressed`. The end condition is not clear. Example:

> Chain `A–B–C` runs left to right. The player orders B forward 10 units, past A's free endpoint. B leaves A's attach radius while marching, passes it, and completes its move order inside A's attach radius.
>
> Which should happen?
> 1. B relinks at the end of the order, because it is inside the attach radius.
> 2. B stays unlinked until it leaves the attach radius and then 3 s pass (`ChainSuppressCooldownSeconds`).
> 3. B stays unlinked until the player orders it back into the chain.
> 4. Something else.

> **Current assumption:** Plan 07 §7h says suppression ends when the **order completes or 3 s after leaving the attach radius, whichever comes first**. Under that wording, the example gives **option 1**: B relinks as soon as its order completes inside A's attach radius. This is not what the spec's "until it completes its move order (or until...)" was meant to say, so confirm it.

**Q-23 (P3, blocks Phase 08). Alt suppression on reserves.**
How long does Alt suppress auto-linking for a reserve troop: only during the order, until the troop next leaves engagement, or until the next order?
> **Current assumption (working; not in spec):** Alt suppression lasts **until the order completes**. It is set when the order is issued and cleared on arrival.

---

## Combat

**Q-24 (P2, blocks Phase 04). Pursue end condition.**
When does a Pursue order end: the target dies, the target leaves the map, the target is out of sight for N seconds, or the player issues a new order?
> **Current assumption:** Plan 04 §4g: a Pursue order ends when the **target dies or the player issues a new order**. The current code also ends pursuit when the target is null or the troop breaks. Pursuit never ends on arrival (it has no stop distance today).

**Q-25 (P2, blocks Phase 04). Charge range measurement.**
`ChargeRangeScale × A_Distance = 7.8` units. Is this soldier-to-soldier distance or footprint-based? Can any engaged soldier charge, or only front-rank soldiers?
> **Current assumption:** The 7.8-unit range is **soldier-to-soldier** distance. **Any engaged soldier** may charge (spec §4.3), not only front-rank soldiers. Footprint-based range is not used.

**Q-26 (P1, blocks Phase 04). Clash rate for contacts without a charge.**
Soldiers in contact with no charger clash every frame unless limited. Proposed: each soldier takes part in at most one clash per `ClashCooldownSeconds = 1.0`. Alternative: a per-pair cooldown, or no limit with a lower damage rate. Which?
> **Current assumption:** Each soldier takes part in at most **one clash per 1.0 s** (`ClashCooldownSeconds`, provisional). Charge contacts are not limited by this rule.

**Q-27 (P2, blocks Phase 04). Charge timeout and cancellation.**
Proposed: cancel on target death or break, with a 3 s timeout. When the target dies, does the charger retarget to the next nearest enemy, or cancel and wait for the next roll?
> **Current assumption:** Charge is cancelled on target death, the charger's troop breaking, or a 3 s timeout. **No retargeting**: the charger waits for the next roll. This matches the current code.

**Q-28 (P3, blocks Phase 04). Duel A/B labels.**
Proposed: A is the charger. If both charge, the earlier charge is A. If neither charges, the lower soldier ID is A. The outcome is symmetric, so this only matters for determinism and for the Domination formula. Confirm.
> **Current assumption:** Spec §4.5 uses this rule. A is the charger; if both charge, the earlier charge is A; if neither, the lower soldier ID is A.

**Q-29 (P2, blocks Phase 04). Charger cooldown.**
The current code has a 1.2–3 s cooldown after each charge. The spec has none. Proposed: no cooldown. The rate alone controls charges. Confirm.
> **Current assumption:** **No cooldown** after a charge. The rate (Aggression / 30 s) is the only limit (spec §4.3).

**Q-30 (P2, blocks Phase 04). Neighbour contagion.**
The current `ChargeCoordinator` uses an Ising-style neighbour model. The spec does not mention it. Proposed: remove it, so the charge rate depends only on Aggression and Assault. Confirm.
> **Current assumption:** Removed. The charge rate depends only on Aggression and Assault (spec §4.3).

**Q-31 (P1, blocks Phase 05). Face determination.**
Which point decides the attacking face: the attacker's pivot, or the contact point? And at what angles are the faces defined? Proposed: from the defender's pivot to the attacker's nearest footprint point. Front within ±45° of facing, flank 45°–135°, rear beyond 135°. Confirm.
> **Current assumption:** Angle is measured from the **defender's pivot** to the **attacker's nearest footprint point**, relative to the defender's facing. Front is within ±45°, flank 45°–135° (left or right by sign), rear beyond 135°. The spec does not give these angles; they come from this proposal (Plan 05).

**Q-32 (P2, blocks Phase 05). Face classification for Blob and Column.**
Blob and Column troops have no rectangle. Are they always treated as flanked, never flanked, or classified by the same angle rule? Proposed: same angle rule, using the pivot's facing (the current heading).
> **Current assumption:** The **same angle rule** applies, using the pivot's current heading as facing.

**Q-33 (P2, blocks Phase 03). Surrounded test.**
A troop is surrounded if its retreat path is blocked by terrain or "other troops". Do friendly troops block the path? Do routing troops block it? Proposed: terrain and enemy troops block; friendly troops do not. The path is a rectangle `RetreatDistance` long behind the rear face.
> **Current assumption:** Terrain and **enemy** troops block the path. Friendly troops do not. A routed troop belongs to its own team, so it blocks only if it is an enemy. The path is a rectangle `RetreatDistance` (1.0 × TroopRadius) long behind the rear face.

**Q-34 (P1, blocks Phase 03). Retreat mechanics.**
- Retreat distance and duration.
- Can a retreating troop break again? If it does, is it a second retreat, a rout, or a surrounded break?
- Are `BreakCooldownSeconds = 1.6` and `ChargeLockAfterBreakSeconds = 1.2` kept?
> **Current assumption:**
> - Retreat distance is `1.0 × TroopRadius` (provisional, spec §7.2). Duration is **not specified** (working assumption: the existing `FaultSlipMotion` timing, 3.14 s: 1 s delay + 1 s move + 0.26 s hold + 0.88 s return).
> - A retreating troop **cannot re-break** until its retreat ends (working assumption; the spec is silent).
> - `BreakCooldownSeconds = 1.6` and `ChargeLockAfterBreakSeconds = 1.2` are kept as provisional (spec §5.7).
> **Implemented (2026-10-26):** break cooldown and charge lock removed (user plan, D-30). Retreat = 1.0 × TroopRadius away from the last attacker with the FaultSlipMotion timing (D-31). No re-break during a retreat.

**Q-35 (P2, blocks Phase 03). Rout flee direction.**
The answer was "away from enemies or towards map edge". Which, or the nearest of the two? If a routed troop is cornered, what happens?
> **Current assumption:** Spec §5.4 says **away from the nearest enemy**. The map-edge option is not used. If cornered, the behaviour is **not defined** (working assumption: the troop keeps fleeing until it despawns at 60 s).

**Q-36 (P2, blocks Phase 03). Rout destroyed event.**
Proposed: `OnUnitDestroyed` is raised once, at rout. Despawn at 60 s raises nothing. Confirm. (Battle wiring is covered by Q-49.)
> **Current assumption:** `OnUnitDestroyed` is raised **once, at rout**. The 60 s despawn raises nothing (spec §5.4). Battle-level wiring is out of scope (Q-49).
> **Implemented (2026-10-26):** `Troop.Routed` and `IUnit.OnUnitDestroyed` raised once at rout; despawn raises nothing (D-32).

**Q-37 (P2, blocks Phase 04). Fleeing troops and damage.**
The current code: when the winner of a clash is fleeing, it records the hit but deals no damage (`ClashResolver.ResolveWinner`). Proposed: keep this behaviour. Confirm.
> **Current assumption:** Kept. A fleeing troop that wins a clash records the hit but **deals no damage** (spec §5.4).

**Q-38 (P2). Facing damage multiplier removed.**
The current `ClashResolver.FlankMultiplier` gives 4× damage when the attacker is in front, which looks inverted. The spec removes it. Confirm it should not be replaced by a damage modifier, and that flanking now acts only through support loss and Shock.
> **Current assumption:** Removed with **no replacement** damage modifier. Flanking acts only through lost support (§4.8) and ×3 Shock (§5.2).

---

## Ranged Combat

**Q-39 (P1, blocks Phase 09). "No shooting into melee".**
Options:
- (a) Do not fire at a target troop that is engaged in melee.
- (b) Do not fire when a friendly troop is engaged in melee between the shooter and target.
- (c) Do not fire when any troop is in melee within the line of fire.
Which?
> **Current assumption:** Option **(a)**: a troop does not fire at a target troop that is engaged in melee (Plan 09).

**Q-40 (P1, blocks Phase 09). Line of fire and friendly fire.**
Projectiles can hit troops in the way. Are they blocked by enemy troops, friendly troops, terrain, or none? "No friendly fire" needs a definition.
> **Current assumption (working; not decided):** Projectiles are **not blocked** by anything. Only **enemy soldiers** can be hit. Friendly soldiers are never hit (this is how "no friendly fire" is read). Terrain does not stop shots.

**Q-41 (P2, blocks Phase 09). Front arc and target soldiers.**
"Situated in front of their line" is not defined. Proposed: within ±45° of the troop's facing, measured from the pivot. Targeting picks the nearest enemy soldier of the nearest troop for each shot. Confirm.
> **Current assumption:** Targets lie within **±45° of the troop's facing**, measured from the pivot. Each shot goes to the **nearest enemy soldier** of the **nearest troop** in that arc.

**Q-42 (P2, blocks Phase 09). Hit resolution timing.**
Roll P(Hit) when the shot is fired, or when the projectile arrives? Projectile speed?
> **Current assumption:** P(Hit) is rolled **when the projectile lands** (Plan 09). **Projectile speed is not defined** (working assumption: 20 units/s, to be tuned).

**Q-43 (P3, blocks Phase 09). Fire interval.**
Proposed: interval = `30 / FireRate` seconds (3 s for the Long bow) with ±10% jitter. Confirm.
> **Current assumption:** Interval = `30 / FireRate` seconds, with ±10% jitter (spec §4.7). The Long bow fires every 3 s.
> **Implemented (2026-10-26):** per-troop volleys every 30 / FireRate s ± 10 % (D-43).

**Q-44 (P3, blocks Phase 09). Hit skill.**
P(Hit) uses the skill for the weapon class (Bow). Confirm, and confirm that Javelin uses the Javelin skill.
> **Current assumption:** P(Hit) uses the soldier's **WeaponSkill for the weapon's class** (Bow or Javelin). A soldier with no skill in that class uses the unskilled default (0, see Q-2).

**Q-45 (P2, blocks Phase 09). Missing weapons.**
Javelin and Crossbow stats are not given.
> **Current assumption:** Javelin and Crossbow have **no stats** and are not implemented. Note that **Crossbow is not in the spec's weapon class list** (Spear, Sword, Axe, Bow, Javelin). Crossbow is now a weapon class (Q-52, Decisions D-03), used by Richter's sheet.

**Q-46 (P2, blocks Phase 09). Mixed melee and ranged troops.**
When some soldiers in a ranged troop are engaged and others are not, does the troop stop firing entirely, or keep firing with the unengaged soldiers?
> **Current assumption:** The troop **stops firing entirely** while **any** soldier is engaged (Plan 09 default).

---

## Input & Orders

**Q-47 (P2, blocks Phase 08). Input units.**
Spec uses screen pixels for both box-select (8 px) and order-drag (`OrderDragMinPixels = 8`, provisional). The old drag threshold was 1.5 world units. Confirm pixels, and confirm the value.
> **Current assumption:** Both thresholds are **screen pixels**: box select 8 px and order drag 8 px (provisional, spec §6.2 and §7.2).

**Q-48 (P3, blocks Phase 08). Box select.**
Should box select include a troop when any soldier is inside the box, or only when the pivot is inside? Current code uses any soldier.
> **Current assumption:** A troop is selected if its **pivot or any living soldier** is inside the box (spec §6.1). This matches the current code.

---

## Scope & Integration

**Q-49 (P1). Scope of this handoff.**
Confirm whether these are in scope for this handoff, or separate specs:
- Mapping campaign characters (`PCWrapper`, `PC_Stats`) to `TroopProfile` and deploying troops in place of old `Unit` prefabs.
- Battle end and win/lose wiring for Troops. Current code ends battles through `Rout.cs` and `OnUnitDestroyed`.
- Capture points for Troops. Current `CapturePoint` needs `IUnitData` colliders, which soldiers do not have.
- AI for enemy Troops. None exists.
- Adding the Troop test scenes to Build Settings.
> **Current assumption:** **All five are out of scope.** This handoff covers `Assets/Battle Scripts/Troop/` only.

---

## Phase 01 Data (raised by the character sheets)

**Q-50 (P2, blocks Phase 06 balance). Character level.**
All 11 sheets are Level 1. The spec gives Lvl 1 as the stat baseline but does not say how levels change stats in battle.
> **Current assumption:** Battle uses the sheet values directly. Level is not stored on the profile, and no level scaling exists.

**Q-51 (P2, blocks Phase 09). Thrown weapons in the secondary slot.**
Several sheets put a javelin-class weapon in a secondary slot: Gaius (Pilum), Brom (Throwing Hatchet), Skarde (Throwing Axe). Lyra's primary is a Javelin. Does a soldier with a thrown weapon fire it, and does that make the troop ranged (spec §4.7 "troops equipped with ranged weapons")?
> **Current assumption (working):** A soldier with a ranged weapon counts as ranged, and a troop with at least one such soldier is a ranged troop. That makes Gaius, Brom, Skarde and Lyra ranged troops too. Confirm, because it changes the army mix a lot.

**Q-52 (P2, blocks Phase 09 sign-off). Weapon classes Mace and Crossbow.**
Spec §1.6 lists Spear, Sword, Axe, Bow and Javelin. The sheets add Mace (Borgo) and Crossbow (Richter). Spec §1.6 allows more classes later.
> **Current assumption:** Both classes are added (Decisions D-03). Crossbow is ranged. Mace is melee. Confirm, and update spec §1.6 to match.

**Q-53 (P2, blocks Phase 06). Composite radius and line width.**
Spec §1.2 covers Move, Morale, Discipline and soldier count. It does not cover the radius or line width of a composite.
> **Current assumption:** Composite radius and line width are the **largest** value of the paired parts (`TroopProfile.CreateComposite`).

**Q-54 (P2, blocks Phase 01 sign-off). Armour values.**
The sheets name 7 armours. The spec gives values for only two: "Light armour and shield" (30/50/20, +4) and "Heavy armour" (60/30/10, +7). Mapping used (Decisions D-07, all provisional):

| Sheet armour | Assumed Deflect/Bruise/Bleed | Defence bonus | Used by |
|---|---|---|---|
| Light Armour | 30/50/20 | +4 | Franz, Robin, Kaelen, Gareth |
| Light Armour + Buckler | 30/50/20 | +4 | Lyra |
| Light Armour + Round Shield | 30/50/20 | +4 | Skarde |
| Thick Furs | 30/50/20 | +4 | Brom |
| Padded Vest | 30/50/20 | +4 | Borgo |
| Chainmail + Shield | 60/30/10 | +7 | Gaius, Richter |
| Heavy Mail + Shield | 60/30/10 | +7 | Vance |

> **Current assumption:** As in the table. Light Armour without a shield has the same values as with one, which is probably wrong. Supply the real values.

**Q-55 (P1, blocks Phase 01 sign-off). Weapon values.**
Only three weapons have spec values (Short Sword, Lance, Longbow). The rest need a damage bonus (melee) or damage, range, accuracy and fire rate (ranged).
Missing for: Gladius, Pilum, Short Bow, Dagger, Spiked Club, Knife, Crossbow, Broadsword, Javelin, Greatsword, Felling Axe, Throwing Hatchet, Bearded Axe, Throwing Axe, Pike, Arming Sword.
> **Current assumption:** Melee weapons use 0 damage bonus and 0 charge bonus. Ranged weapons use the v0.8 bow baseline (damage 5, range 10, accuracy 4, fire rate 5). All are provisional (Decisions D-06).

**Q-56 (P2, blocks Phase 01 sign-off). "Charge bonus" on the sheets.**
The sheets give a "charge bonus" for each character. The spec has two separate values: the soldier **Charge** stat (1–20) and a weapon **ChargeBonus**.
> **Current assumption:** The sheet value is the soldier's **Charge** stat (Decisions D-20). Weapon charge bonuses come only from the weapon values in Q-55.

---

## Resolved in v0.9

For traceability. Each answer is applied in `specifications.md`.

| Topic | Decision |
|---|---|
| Hinge limit | 65° is the minimum inner angle. |
| Chain break | Stretch limit is `ChainMaxStretchScale = 3.0 × L_nat`. `R_BreakScale` removed. |
| Domination | `|R − P_charger| / 10`, rounded to the nearest whole number. |
| Line spacing | 0.9. |
| Clash cooldown and attacks | Clash on contact is kept. Banked attacks removed. |
| Move | Derived: minimum soldier Speed. |
| HP | Integer 10–40, default 15 (superseded the earlier average of 18; D-23). |
| Combat stats | All integers 1–20, default 5, including BaseMorale, Discipline and weapon skill levels (D-22). |
| Discipline range | 1–20 (Q-10 resolved; see Q-58 for the minimum-1 consequence). |
| Equipment | Armour, melee and ranged weapons defined (§1.6). Two weapons, two skills, ranged troops carry melee. |
| Support | Radius 3. Friendly = same army. |
| Naming | `InitialSoldierCount`. |
| A_Distance and charge range | Footprint gap. Charge range 1.5 × A_Distance. |
| Followers | Bypass separation like chargers. |
| Assault | Triggered by an order. Pursuit only by order. |
| Bruise | Heals 1 HP per second. Bruises can kill. Overflow not counted for Shock. |
| Faces | Locked while engaged. |
| Surround | Retreat blocked by terrain or troops. |
| Rout | Final. Counts as destroyed. Despawn after 60 s. Attackable. |
| S_Distance hysteresis | Removed. |
| Shared-retreat falloff | 30%, provisional. |
| Ranged | Projectiles. Fire at nearest troop and nearest soldiers. Not into melee. No friendly fire. No ammo limit. Jitter ±10%. No flank bonus. |
| Pivot separation | Kept for friendly pivots. Line endpoints exempt. |
| Chain outranks column | Intended. |
