# Open Questions: Battle System

Questions the battle spec (`specifications.md` v0.10) leaves open. The spec references them as **[Q-n]**. Numbers are
kept from v0.9 so older log entries still point at the right question.

- **Open:** no answer yet; the spec or code uses the value shown until there is one.
- **Working answer:** built as shown in the spec, but never confirmed by the designer. Confirm or change.
- **Resolved:** answered; kept for traceability.

Unconfirmed **Assumptions** from the build log are not repeated here; they are in `AssumptionsReview.md`. Tuning values
are variables (spec §8) and are not questions.

Last swept: 2026-11-08.

---

## Open

**Q-2 (P3). Unskilled weapon skill.** A character with no skill in its weapon's class: is the skill 0, 1, or 5?
> Uses **0** (a listed skill is never 0, so 0 means "not listed"). The soldier gets no WeaponSkill term in Combat Power or
> the hit chance.

**Q-24 (P3). When does a Pursue order end?**
> Ends when the target is destroyed (routed or wiped out) or the player gives a new order. It never ends on arrival or
> by distance.

**Q-40 (P2). Line of fire.** Should projectiles be blocked by terrain, or by troops between the shooter and the target?
> Not blocked by anything. Only enemy soldiers can be hit; friendly soldiers never are (this is "no friendly fire").

**Q-46 (P3). Partly engaged ranged troops.** When some soldiers of a ranged troop are in melee and others are not,
should the free ones keep shooting?
> The troop stops firing while it is engaged (troop-level).

**Q-58 (P3). Minimum Discipline.** Discipline is 1–20, so every charge can pick at least one follower (each joins with
50% chance). Should a troop be able to have no followers (minimum 0)?
> Minimum 1.

**Q-59 (P3). Line fill inside terrain** (movement edge case 12, `MovementEdgeCases/12-terrain-choke.svg`). When part
of a troop's line falls inside impassable terrain, should the line fill skip those places?
> No: soldiers whose place is inside terrain stop at its edge and crowd the open part of the line.

---

## Working answers (confirm or change)

| Q | Question | Working answer (spec) |
|---|---|---|
| Q-4 | Does a weapon's ChargeBonus count only while charging? | Yes, only in Combat Power while charging (§4.5). |
| Q-5 | Ranged damage? | Weapon Damage only, no Strength (§4.6). |
| Q-7 | Spear Support range? | Doubled only when the bearer is the supporter, measured to the clashing soldier (§4.5). |
| Q-8 | Couched Lance doubling? | Raw damage including Domination, before mitigation, while charging (§4.6). |
| Q-9 | Charge followers' pool? | Same troop, nearest first (§4.4). |
| Q-11 | Stretched lines and the troop radius? | Option (a): the soldier clamp widens to reach a stretched line's points (D-36, §2.3). |
| Q-13 | Hinge angles? | 65° break limit is the inner angle; the 30° retreat threshold is bend (180° − inner) (§3.4). |
| Q-15 | Are linked pairs exempt from pivot separation? | Not from pivot separation; they are exempt from outline separation (§2.4). |
| Q-18 | Order of troops in a multi-troop line? | By position along the drag (§6.2). |
| Q-20 | Do engaged chain neighbours retreat sympathetically? | No; committed neighbours hold by tension (§3.4). |
| Q-21 | Shared-retreat falloff? | Each hop passes on 70% (§3.4). |
| Q-26 | Clash rate for contacts that are not charges? | One clash per soldier per 1 s. Rare now that engaged lines stand apart (§4.5). |
| Q-27 | Does a charger retarget when its target dies? | No; it waits for its next roll (§4.3). |
| Q-28 | Duel A/B labels? | Charger, else earlier charger, else lower soldier ID (§4.5). |
| Q-29 | Cooldown after a charge? | None; the rate is the only limit (§4.3). |
| Q-30 | Neighbour charge contagion? | Removed (§4.3). |
| Q-31 | How is the attacking face decided? | Defender's pivot to the attacker's nearest footprint point; Front ±45°, Rear beyond 135° (§4.8). |
| Q-32 | Faces of Blob and Column troops? | Same angle rule, using the current heading (§4.8). |
| Q-39 | "No shooting into melee"? | Option (a): never target a troop engaged in melee (§4.7). |
| Q-42 | When is the hit rolled? | When the projectile lands; 20 units/s (§4.7). |
| Q-44 | Hit skill? | The skill for the weapon's class (§4.7). |
| Q-56 | The character sheets' "charge bonus"? | The character's Charge stat, not a weapon value. |
| Q-57 | Weapon value types? | Integers ≥ 0 with no cap; Range is a float. |

---

## Resolved

| Q | Topic | Resolution | Source |
|---|---|---|---|
| Q-1 | Speed conversion | `SpeedUnitsPerStatPoint` 0.6, a tunable variable. | SU-135 |
| Q-3 | Two melee weapons | One required melee and an optional ranged weapon. | D-52 |
| Q-6, Q-54 | Armour values | Ratios on the armour assets; tunable data. | SU-89, SU-134 |
| Q-10 | Discipline range | 1–20 (see Q-58). | D-24 |
| Q-12 | Natural line length | `L_nat = Frontage × LineSpacing`; frontage is calculated from the soldier count. | SU-103, SU-117 |
| Q-14 | Move arrival | The troop arrives Idle. | Spec §6.2, SU-101 |
| Q-16 | Engagement hysteresis | Exit hold `EngagementHoldMargin`; S_Distance exit margin. | SU-24, SU-45 |
| Q-17 | Line facing | The player cannot choose a front, and a lone troop gets no facing; in a chain it can be shaped by placing neighbours. | SU-129 |
| Q-19 | Links from a line drag | Created explicitly as pending links. | D-39 |
| Q-22 | ChainSuppressed end | 3 s after leaving every attach radius (option 2). | SU-130 |
| Q-23 | Alt suppression on reserves | Ends the same way as Q-22. | SU-130 |
| Q-25 | Charge range and who charges | Reach A_Distance edge to edge, gated at EngagementGap; front rank only. | SU-95, SU-106 |
| Q-33 | Surround test | Terrain or enemy soldiers in the box; footprints and routed enemies do not block. | SU-122 |
| Q-34 | Retreat mechanics | 5 units, rear-first wave (3.3 s); no break cooldown or charge lock; no re-break during a retreat. | SU-11, SU-133, D-30 |
| Q-35 | Rout flee direction | NavMesh flight to the battlefield edge away from enemies. | SU-121 |
| Q-36 | Rout destroyed event | Raised once, at rout. | D-32, SU-57 |
| Q-37 | Fleeing winners | Deal no damage (also stunned winners). | D-33, SU-111 |
| Q-38 | Facing damage multiplier | Removed; flanking acts through support and ×3 Shock. | Spec §4.8 |
| Q-41 | Front arc | Per shooter. | D-54 |
| Q-43 | Fire interval | `30 / FireRate` ± 10%; single shots unless Volley. | SU-105 |
| Q-45, Q-55 | Missing weapon values | Weapon data on the assets; tunable. Melee damage cut by 3. | SU-108, SU-134 |
| Q-47 | Input units | Screen pixels, tunable. | Spec §6 |
| Q-48 | Box select | Pivot or any living soldier inside. | Spec §6.1 |
| Q-49 | Scope | Deployment, battle end, capture points and the Basic AI are built; missions are in the mission catalog. | SU-57 to SU-60, SU-67, SU-119 |
| Q-50 | Character level in battle | Level-up gains are used in battle and carry to the next mission. | SU-132 |
| Q-51 | Thrown weapons | Hybrid troops: the AI orders them as melee; they still throw. | SU-79, SU-135 |
| Q-52 | Mace and Crossbow | Added. | D-03 |
| Q-53 | Composite size | Frontage from the total soldier count. | SU-117 |
| — | Overflow Shock | Overflow Bleed counts as Shock. | SU-128 |
| — | Movement edge case 18 | An engaged chain end does not refuse its flank by itself; that needs an order. | SU-135 |
| — | Player loss rule | Every player troop out; fine for now. | SU-78, SU-135 |
