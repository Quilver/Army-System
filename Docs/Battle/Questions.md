# Open Questions: Battle System

Questions the battle spec (`specifications.md` v0.10) leaves open. The spec references them as **[Q-n]**. Numbers are
kept from v0.9 so older log entries still point at the right question.

- **Open:** no answer yet; the spec or code uses the behaviour described until there is one.
- **Resolved:** answered or confirmed by the designer; kept for traceability. (Every former "working answer" was
  confirmed on 2026-11-08.)

Unconfirmed **Assumptions** from the build log are not repeated here; they are in `AssumptionsReview.md`. Tuning values
are variables (spec §8) and are not questions.

Last swept: 2026-11-08 (designer's answers and confirmations folded in, SU-136 to SU-141).

---

## Open

Raised while building SU-136 to SU-140; each is built as shown until answered.

**Q-60 (P3). Who blocks direct fire?** Line of sight is blocked by living soldiers of any third troop, friend or enemy.
Should friendly troops block (as built), or only enemies?

**Q-61 (P3). The target's own front rank.** The target troop's own soldiers never block, so archers can hit the back
ranks through the front rank. Should the front rank screen the ranks behind it?

**Q-62 (P3). Crossbows.** Bows are arced; crossbows (and thrown weapons) are direct fire. Is that right for crossbows?

**Q-63 (P3). Water and sight.** Water blocks movement but not sight (`SightBlockingLayers` = Terrain). Confirm.

**Q-64 (P3). Routing at the edge.** A troop that routs while already at the battlefield edge leaves the field at once.
Also, where walls or terrain keep the NavMesh away from its bounding rectangle, a routed troop cannot reach the edge on
that side and falls back to the 60 s despawn. Should reaching a flee goal snapped from the edge count as leaving?

**Q-65 (P3). Skirmisher equipment.** The generic Skirmisher carries an Arming Sword (Sword) but has skills in Axe and
Javelin, so it fights with Sword skill 0 and warns (SU-140). Give it an axe, or Sword skill?

---

## Resolved

| Q | Topic | Resolution | Source |
|---|---|---|---|
| Q-2 | Unskilled weapon skill | 0; a character carrying a weapon it has no skill in gets a warning (not a blocker). | SU-140 |
| Q-24 | Pursue end | Not on the target's rout; ends when the pursuer retreats, the target is wiped out or leaves the field, or a new order. | SU-139 |
| Q-40 | Line of fire | `ArcedShot` on ranged weapons: arced shots ignore obstructions; direct shots need line of sight (no terrain or troops in between). | SU-136 |
| Q-46 | Partly engaged ranged troops | The troop stops firing while it is engaged (troop level). | Designer, 2026-11-08 |
| Q-58 | Minimum Discipline | 1. | Designer, 2026-11-08 |
| Q-59 | Line fill inside terrain | Not skipped: soldiers whose place is inside terrain stop at its edge and crowd the open part. | Designer, 2026-11-08 |
| Q-4 | Weapon ChargeBonus | Only in Combat Power while charging. | Confirmed 2026-11-08 |
| Q-5 | Ranged damage | Weapon Damage only, no Strength. | Confirmed 2026-11-08 |
| Q-7 | Spear Support range | Doubled only when the bearer is the supporter, measured to the clashing soldier. | Confirmed 2026-11-08 |
| Q-8 | Couched Lance doubling | Raw damage including Domination, before mitigation, while charging. | Confirmed 2026-11-08 |
| Q-9 | Charge followers' pool | Same troop, nearest first. | Confirmed 2026-11-08 |
| Q-11 | Stretched lines and the radius | The soldier clamp widens to reach a stretched line's points (D-36). | Confirmed 2026-11-08 |
| Q-13 | Hinge angles | 65° break limit is the inner angle; the 30° retreat threshold is bend. | Confirmed 2026-11-08 |
| Q-15 | Linked pairs and separation | Not exempt from pivot separation; exempt from outline separation. | Confirmed 2026-11-08 |
| Q-18 | Order of troops in a line drag | By position along the drag. | Confirmed 2026-11-08 |
| Q-20 | Engaged neighbours and shared retreat | Committed neighbours do not retreat; they hold by tension. | Confirmed 2026-11-08 |
| Q-21 | Shared-retreat falloff | Each hop passes on 70%. | Confirmed 2026-11-08 |
| Q-26 | Clash rate without a charge | One clash per soldier per 1 s. | Confirmed 2026-11-08 |
| Q-27 | Charger retargeting | None; it waits for its next roll. | Confirmed 2026-11-08 |
| Q-28 | Duel A/B labels | Charger, else earlier charger, else lower soldier ID. | Confirmed 2026-11-08 |
| Q-29 | Cooldown after a charge | None. | Confirmed 2026-11-08 |
| Q-30 | Neighbour charge contagion | Removed. | Confirmed 2026-11-08 |
| Q-31 | Attacking face | Defender's pivot to the attacker's nearest footprint point; Front ±45°, Rear beyond 135°. | Confirmed 2026-11-08 |
| Q-32 | Faces of Blob and Column troops | Same angle rule, using the current heading. | Confirmed 2026-11-08 |
| Q-39 | No shooting into melee | Never target a troop engaged in melee. | Confirmed 2026-11-08 |
| Q-42 | Hit timing | Rolled when the projectile lands; 20 units/s. | Confirmed 2026-11-08 |
| Q-44 | Hit skill | The skill for the weapon's class. | Confirmed 2026-11-08 |
| Q-56 | Sheet "charge bonus" | The character's Charge stat. | Confirmed 2026-11-08 |
| Q-57 | Weapon value types | Integers 0–20; Range is a float. | Confirmed with the 20 cap, SU-137 |
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
