# Specification Update Log

Running log of changes to the battle system and of assumptions made while building it, against
`Docs/Battle/specifications.md` (v0.9). Newest entries first. Each entry gives what changed, the spec sections it
touches, and whether it is a **Decision** (asked for by the designer) or an **Assumption** (filled in to make the
change work; confirm or replace). `specifications.md` itself is not edited; fold entries into it when they are agreed.

---

## 2026-11-07 (5): Missions

### SU-119 Mission sheets, mission catalog and mission folders (Decision + Assumptions, amends SU-50, SU-117)
- Decision: each mission has a **mission sheet** (`MissionDefinition`) holding its pre-battle data (the existing
  `PreparationData`), its tween data (global and character achievements for this mission, **one next-mission link**)
  and its battle scene. `Resources/MissionCatalog.asset` lists the missions in campaign order; a mission's number is
  its place in the list and is what `CampaignData.CurrentLevel` holds (save format unchanged). Workflow:
  `Docs/Missions/README.md`.
- Decision: missions live in `Assets/Missions/Mxx - <Title>/` with `PreBattle/`, `Battle/` (scene, navmesh,
  `Map/` layer images) and `Tween/` folders. **Leveraged Wars > Missions > New Mission** makes the folders, sheet,
  PreparationData and battle scene (empty, or a copy of another mission's with its own navmesh copy) and links the
  previous last mission to it. **Migrate Levels 1 and 2** moves the two levels there (assets keep their GUIDs).
- Decision: `PreparationData` gains **New Characters**: they join the roster at level 1 **when the tent opens**, before
  mandatory characters are deployed; they are not deployed unless also mandatory.
- Decision: the deploy cap (SU-50) is per mission: `PreparationData.maxDeployed`, default 5.
- Decision: the tent launches the current mission's battle scene, and the post-battle Continue goes to the finished
  mission's next mission (none: game over). The battle scene must be in Build Settings; **Sync Build Settings** puts the
  menu scenes at 0-2 and the missions' scenes after them in catalog order.
- Assumption: without a catalog everything falls back to the old flow (tent `PreparationData` list by level, scene
  index `CurrentLevel + 2`, `CurrentLevel++` after a battle).
- Assumption: the mission's achievements are added to those on the scene's Achievement Controller; achievements on
  scene objects (a capture point) stay on the controller. Win and loss conditions stay the scene's Battle End components
  for now.
- Assumption: the tent's map shows "Mission N: Title" when the mission has a title.
- Assumption: migrated missions are titled "Level 1" and "Level 2"; their scenes are renamed "M01 Battle" and
  "M02 Battle", and dialogue assets found in the level folders go to `PreBattle/`.

---

## 2026-11-07 (4): Troops are lists of characters

### SU-118 Troop Profile removed: troops hold their characters and set their own frontage (Decision + Assumptions, amends SU-103, SU-117)
- Decision: the `TroopProfile` ScriptableObject is removed. A `Troop` now holds a list of `CharacterProfile`s (leader
  first: troop name and portrait are the leader's) and resolves its stats from them with `CompositeTroopResolver`
  (spec §1.2). The troop assets ("Julian's Troop", "Spearman Troop", "Composite Troop", ...) are deleted.
- Decision: a troop **determines its frontage at the start of the battle**, when its soldiers spawn, from its total
  soldier count (`TroopFrontageRule`, SU-117). This now applies to every troop: deployed from the tent, a level's
  default troops and troops placed in a scene. The frontages saved on the old troop assets are no longer used.
- Decision: `BattleReport.DeployedTroops` is a list of `DeployedTroop`, each the list of characters for one troop. The
  deployment zone passes each list to a troop (`Troop.Configure(team, characters)`); `RosterTroops.Sync` builds the
  lists from the deployed characters and pairings.
- Assumption: `Troop.Configure` takes an optional frontage override, used only by the tests and the soldier showcase
  scene, which need a fixed width; 0 (the default) uses `TroopFrontageRule`.
- Assumption: the deployment zone spawns every troop first and lays them out afterwards, so the row spacing uses each
  troop's own frontage and size.
- Assumption: scenes, the Troop prefab and the level DataManagers were migrated by replacing each troop asset reference
  with that asset's characters (same order).

---

## 2026-11-07 (3): Battle preparation and deployment

### SU-117 Mandatory characters, frontage from soldier count, composite troops in the tent (Decision + Assumptions, amends SU-50, SU-52, Q-53)
- Decision: each mission's `PreparationData` has **Mandatory Characters**. They are deployed when the tent opens and
  cannot be undeployed. Prince Julian is mandatory in Level 1 and Level 2 (and in the tent's Sample Mission).
- Decision: Julian is free to deploy. Assumption: this applies to every mandatory character (cost 0, shown as "Free"),
  rather than to Julian by name. Mandatory characters still count towards the 2-5 deploy limits.
- Assumption: a mandatory character missing from the roster is added to it at level 1 (logged).
- Decision: a troop made for battle from deployed characters has its **frontage set from its total soldier count**
  (`TroopFrontageRule`): up to 4 soldiers, frontage = soldiers; up to 10, half rounded up; up to 16, a third rounded up;
  more than 16, a quarter rounded up (minimum 1). `TroopProfile.CreateComposite` uses the same rule, replacing the Q-53
  "largest part" assumption.
- Assumption: deployed characters always get a troop made at runtime, so the per-character troop assets
  (e.g. "Julian's Troop") and their saved frontage are no longer used for deployment. A level played on its own still
  uses its DataManager's default troop assets unchanged. The soldier count is the characters' `Numbers`
  (level-up Numbers gains are not applied to spawning yet).
- Decision: **composite troops** are made on the Map's deployment page by dragging a deployed character's card onto
  another deployed character's card. The dragged character serves under the one it was dropped on; the two fight as one
  troop led by that character (leader first: troop name and portrait are the leader's). At most **2 characters** per
  composite troop (`RosterTroops.MaxCharactersPerTroop`).
- Decision (UI): the pairing slot box is removed from the deployed cards. A follower's card is listed straight after its
  leader's and **indented** under it (e.g. Sir Jagen, then Lyra indented below).
- Assumptions (UI): both cards of a composite are tinted. Dragging a follower off its leader (dropping anywhere but a
  card) splits the pair. Dropping onto a character that serves under someone, or dragging a
  character that already leads a composite, is refused with the reason shown on the readiness line. A follower dropped on
  another leader moves to it. Undeploying either character splits the troop. Each character keeps its own cost; the
  deploy cap counts characters, not troops. Pairing is mouse only for now (no keyboard / controller path).
- Assumption (post-battle records): in a composite troop, a casualty goes to the fallen soldier's own character, kills go
  to the leader, and a rout marks both characters routed. Achievements already find a follower's troop.

---

## 2026-11-07 (2): Post-battle book fixes

### SU-116 Books lose the bookmark; characters always reach the post-battle book (Decision + Assumptions, amends SU-115)
- Decision: no bookmark ribbon on any book (Roster Ledger, Debts, Map, post-battle book); removed from the builder and
  from the Battle Preparation and Post battle scenes.
- Decision: the BattleReport is the go-between for deployed characters. A level played on its own (no characters from
  the tent) now deploys the campaign characters found in the player's troops (their roster entries if the campaign has
  them, else new level 1 entries), so achievements, kills and XP still reach the post-battle book
  (`RosterTroops.EnsureDeployedCharacters`). A battle scene without an AchievementController gets one at battle start.
  The controller and the book log what they track and receive.
- Decision: in the editor, a Post battle scene with no deployed characters shows debug data (`PostBattleDebugData`):
  Julian and Lyra deployed and surviving, Julian also earning Ten Kills, Routed Foe, Against the Odds and Shock Assault
  (14 XP, a level-up), and the battle won (Victory, Mission Complete). Toggle: Debug Data When Empty on the book.
- Decision: the Battle Report's figures (and a character's kills and casualties) count up together.

---

## 2026-11-07: Post-battle book, 10-XP levels, stat growth, Achievement Tuning

### SU-115 Post-battle book and levelling (Decision + Assumptions, amends SU-62, SU-114)
- Decision: the achievements every character has (Survivor, Earned Your Spurs, Routed Foe) move from the class table
  to **Achievement Tuning** (`Resources/Battle Tuning/Achievement Tuning`, `AchievementTuning`, menu Battle Tuning), which
  also lists the global achievements of every battle. New: **Mission Complete** (Victory, 200 gold). The class mini
  quests of SU-114 stay in `Resources/Achievement/Class Achievements` as guessed.
- Decision: a level is **10 XP** (`RosterCharacter.XpPerLevel`, was 100). Achievement XP is divided by 10, rounded half
  up: Survivor 2, Earned Your Spurs 2, Ten Kills 3, Twenty Kills 4, Routed Foe 3, Against the Odds 3, Shock Assault 3,
  Held the Line 2, Volley Disorder 2. The global XP rewards round to 0 (Victory 3 -> 0, test achievements too).
  Save version 3: older saves are discarded. Tent sample XP divided by 10.
- Decision: **stat growth on level-up** (closes the SU-62 open item): each level rolls every stat (Soldiers, HP, Morale,
  Discipline, Speed, Defence, Strength, Support, Charge, Aggression) and each listed weapon skill against its growth
  chance; a success below the class cap adds +1. Gains are saved on the `RosterCharacter` (`statGains`, `skillGains`)
  and shown by the ledger (`RosterCharacter.Stat`, `SkillLevel`).
- Decision: XP shows as **10 notches** (glowing when full, black when empty) on the ledger and in the post-battle book.
- Decision: the post-battle screen is a **book** like the Roster Ledger (build: Leveraged Wars > Post-Battle > Build
  Post-Battle Book; the old canvas is switched off, renamed "(retired)").
  - Spread 1: **Battle Report** (time taken, enemy slain, casualties, enemy troops routed, our troops routed, gold
    earned, prestige earned when not 0, achievements won) facing **Battle Honours** (global achievements stamped one by
    one while the treasury counts up).
  - One spread per deployed character: its ledger page facing its **Battle Record** (enemy slain, casualties,
    experience gained, its achievements stamped one by one).
  - Animations: figures count up; stamps come down, land and glow; each stamp's XP lights notches one by one; a full row
    is a level-up: all notches pulse together, then each stat glows in turn and gains a green +1 or returns to normal.
  - Timings, colours and sounds: **Post Battle Tuning** (`Resources/Campaign Tuning/Post Battle Tuning`, menu Campaign
    Tuning). Defaults: count-up 0.9 s, stamp drop 0.28 s from 2.4x, glow 0.9 s, 0.45 s between stamps, 0.16 s per notch,
    3 pulses of 0.3 s at 1.4x, 0.22 s glow + 0.18 s result per stat. Sounds from Temporary Assets/Sound effects/UI.
- Assumption: a spread per character (ledger page left, record right), since one 548 x 556 page cannot hold the ledger
  page and the battle record. The book opens on the report; pages turn with the buttons, Q / E, Page Up / Down or the
  shoulders; Space / Enter / click skips a spread's animation; a spread animates once and shows its end state if reopened.
- Assumption: every reward is **granted when the book opens** (`PostBattleResults`), and the animations replay what was
  recorded; Continue behaves as the old Next button.
- Assumption: kills and casualties per character count enemy soldiers killed by, and soldiers lost from, the
  character's troop (`BattleReport.CharacterRecords`); routed troops count routs of each side (`BattleEvents.TroopBroke`).
- Open item: stat gains are saved and shown but **not yet applied in battle** (the troop still reads the
  CampaignCharacter asset's stats).
- Tests: `AchievementTests` (tuning, 10 XP per level and growth rolls, post-battle results granted once).

---

## 2026-11-06 (6): Achievement system refactor and character mini quests

### SU-114 Achievements: break events, character mini quests, rewards (Decision + Assumptions, amends SU-61)
- Decision: achievements do two jobs. **Global** achievements track the battle (victory, side objectives such as a held
  capture point) and give gold and prestige. **Character** achievements are each class's and character's mini quests and
  replace XP for kills: they are measured on the troop the deployed character leads and their XP goes to that character.
  Rewards are gold, prestige and XP for now; new kinds (e.g. story flags) derive from `Reward`.
- Decision: the requested character achievements, with their icons (`Campaign/UI/After battle menus/Symbols/Achievements/`,
  SVG sources in its `Source~` folder) and assets in `Resources/Achievement/`:
  | Achievement | Requirement | Reward |
  |---|---|---|
  | Survivor (Survived) | `CharacterSurvived`: the troop was not routed or wiped out when the battle ended | 20 XP (+2 gold, 1 prestige kept) |
  | Earned Your Spurs / Ten Kills / Twenty Kills | `KillTracker` 5 / 10 / 20 kills | 15 / 25 / 40 XP (+12 gold kept on Spurs) |
  | Routed Foe | `RoutEnemy`: an enemy troop routs, credited to the troop | 25 XP |
  | Against the Odds | `DefeatWhileOutnumbered`: an enemy troop that outnumbered the troop in melee is defeated | 30 XP |
  | Shock Assault | `BreakOnInitialAssault`: an enemy retreats or routs in the first clash of an assault | 25 XP |
  | Held the Line | `HoldMelee`: 60 s of battle time in one melee | 20 XP |
  | Volley Disorder | `RangedDisorder`: an enemy troop is disordered by the troop's projectiles | 20 XP |
- Battle side: `BattleEvents.TroopBroke` (`TroopBreakInfo`: troop, kind Rout / Retreat / Disorder / SurroundedBreak,
  cause, cause was ranged) is raised at every morale break; for a rout, before `TroopDefeated`. `Troop.LastAttacker`,
  `LastAttackWasRanged` and `LastAttackTime` record the enemy troop whose hit last did damage. No battle behaviour changes.
- Tween side: one `Achievement` base (icon, title, description, rewards); `Requirement` base with a battle lifecycle
  (Begin, Tick, Finish at battle end, Abort) that unsubscribes from the static battle events, so a second battle in the
  same session no longer receives the first battle's handlers. Each battle tracks instantiated copies, so assets are never
  modified. Class mini quests live in `Resources/Achievement/Class Achievements` (`ClassAchievements`), since
  `CharacterClass` is battle data. Character achievements listed on the battle's controller now apply to every deployed
  character (they were created but never set up). Rewards are granted once per achievement, as its card appears or, for
  any left, when the player leaves the post-battle screen (they were only granted by the card before). See
  `Assets/Scripts/Tween/Achievements/README.md`.
- Assumption: **credit** for an enemy's break or defeat goes to the troop that dealt its last damaging hit, or to a troop
  in melee with it at that moment (so several troops can earn it from one enemy).
- Assumption: **outnumbered** means the enemy troop's living soldiers exceeded ours (Ratio 1, adjustable) at any moment
  while the two were in melee; only the two troops count, not allies on either side.
- Assumption: **initial assault** is our troop's first melee with that enemy troop, entered while our troop has Assault
  (it attacked with an order, spec §4.2); a retreat or rout within 15 s of first contact, before our troop leaves melee,
  counts. "Require Assault" off counts any first contact, including being charged.
- Assumption: **lasting 1 minute in melee** is 60 s of battle time (battle speed applies, pauses do not count) in melee
  without our troop retreating or routing; gaps out of melee shorter than 2 s do not end the engagement.
- Assumption: **disordered by ranged attacks** means the hit that broke the enemy into disorder (SU-104) was a
  projectile from our troop; a rout or retreat caused by projectiles counts only with "Count Other Breaks".
- Assumption: class mini quests (all adjustable in the table): every character: Survivor, Earned Your Spurs, Routed Foe;
  Archer: Volley Disorder, Ten Kills; Skirmisher: Volley Disorder, Against the Odds; Cavalry: Shock Assault, Ten Kills;
  Huscarl: Held the Line, Against the Odds, Twenty Kills; Spearmen: Held the Line, Shock Assault.
- Open item: the "Reclaimed farm" capture-point achievement in Level 1 (and the troop test level) has no capture point
  assigned, so it never paid out; it is now skipped with a warning until the point is set on the AchievementController.
- Tests: `AchievementTests` (level rules, melee timer, outnumbered rule, lifecycle, rewards granted once, asset checks).

---

## 2026-11-06 (5): Charge window 45 s; combat log readable

### SU-112 Charges per 45 s, set in Melee Tuning (Decision, amends spec §4.3, §1.5 Aggression)
- Decision: a soldier makes about **EffectiveAggression charges per 45 s** (was per `ActionWindowSeconds`, 30 s). The
  window is a new Melee Tuning value, `ChargeWindowSeconds` = 45. Charges are 1.5x rarer: a Spearman (Aggression 3)
  charges about once every 15 s while it may charge (front rank, within the gate).
- Assumption: only charges use it. Ranged fire rate (shots per window) and Shock decay keep `ActionWindowSeconds` (30 s,
  Soldier Tuning).
- Effect (`MeleeDurationTests`, project data): Spearmen melees now last **about 3.5 to 4.3 minutes** (was about 2.75).
  Measured ways back to about 3 minutes: a 40 s window (about 3.2 min), or melee weapon damage -2 instead of -3 with
  the 45 s window (about 3.1 min). Not changed; the designer's call. `SpearmenMelee_LastsAboutThreeMinutes` now accepts
  2 to 5 minutes.
- Effect: with fewer charges to shuffle the ranks, a composite troop's archers take longer to drop behind its melee
  soldiers once engaged (D-55). They are well behind by 12-16 s (they were by 4-8 s); the test now samples then.
- Tests: `TroopScenarioTests.ChargeRate_IsAboutAggressionPerChargeWindow` (was `ChargeRate_Over30Seconds_IsAboutAggression`;
  6 to a rank and 90 s, so enough front-rank charges are rolled), `OnlyTheFrontRank_StartsCharges` runs 30 s.

### SU-113 Combat log fits its panel and breaks down the charge bonus (Decision + Assumption, amends SU-110)
- Found in play: entries ran past the right edge of the panel, and the charge bonus looked too large ("24 charge" for
  Prince Julian, Charge 11).
- The charge value was right by spec §4.5: `ChargeBonus = Charge + weapon ChargeBonus + AssaultChargeBonus` =
  11 + 8 (Lance) + 5 (Assault) = 24. The log now shows the parts: "24 charge (11 Charge + 8 Lance + 5 Assault)".
- Entries wrap to the panel's width, and each step is on its own line: who fought (A and B), A's Combat Power, B's
  Combat Power, the chance, roll and winner, the damage and the armour roll, and what the loser lost.

---

## 2026-11-06 (4): Stunned soldiers deal no damage

### SU-111 A stunned soldier that wins a clash deals no damage (Decision, amends SU-107, spec §4.5, §4.6)
- Decision: a stunned soldier caught in a clash still rolls the duel as before, but if it wins it deals **no damage**
  (like a fleeing winner, Q-37). If it loses it takes the hit as usual (nothing while it is on 0 HP).
- The combat log tags it "(stunned)" and shows "No damage (winner stunned)".
- Tests: `TroopScenarioTests.StunnedWinner_DealsNoDamage`.

---

## 2026-11-06 (3): Longer melees; frontage, disorder, volley fire, combat log

### SU-103 Troop radius becomes troop frontage (Decision + Assumptions, amends spec §1.4, §3.2, §3.3, Q-11, Q-12, Q-53)
- Decision: the troop profile's **Troop Radius** is renamed **Frontage**: about how many soldiers wide the troop stands
  in line formation. Default **5**. The natural line is `L_nat = Frontage x LineSpacing` (5 x 0.9 = 4.5 units), so a
  frontage of 5 puts 5 soldiers in each rank (16 Spearmen stand 4 ranks deep).
- Assumption: saved assets keep their number (old `_troopRadius` reads as `_frontage`): Spearmen, Archer, Skirmisher,
  Jagen, Robin and the Composite troop are 5 wide; Julian, Laurice, Cav and Huskarl are **3 wide**. Check those four.
  Before, every radius-5 troop stood about 10 wide (L_nat 9), so lines are now about half as wide.
- Assumption: the troop still has a size in world units for the blob, the soldier clamp, pivot spacing, deployment
  spacing and pathing, now derived: `L_nat / NaturalLineRadiusScale` (the old radius-to-line ratio, so frontage 10 is
  the old radius 5), but never smaller than the radius whose blob (`BlobRadiusScale` x radius) holds the troop's
  soldiers packed at line spacing. Spearmen: 2.5 units (was 5). It is no longer a profile value
  (`Troop.Radius`, `TroopProfile.Radius`, `LineGeometry.TroopRadius`).
- Assumption: a composite troop takes the largest part frontage (Q-53 as before).
- Clash knock-back keeps the 0.3 units every radius-5 troop had (it was 0.06 x the smaller radius), so clashes are
  unchanged.
- The hidden fallback radius on scene Troop components (16.5) is not carried over; a troop without a profile is 5 wide.
- Code: `TroopProfile.Frontage`, `Troop.Frontage`, `LineGeometry.NaturalLength / TroopRadius`,
  `TroopTuning.NaturalLineLength(frontage)`, `DeploymentZone`, `RosterTroops.DefaultTroopFrontage`.
- Tests: `LineGeometryTests`; the scenario tests pass frontage = 2 x their old radius, which keeps their geometry.

### SU-104 Retreats only from melee; out of melee a break disorders the troop (Decision + Assumptions, amends spec §5.3, §5.5)
- Decision: a Case B break (§5.3) retreats only when the troop is **engaged in melee**. Out of melee (for example under
  arrow fire) the troop becomes **disordered**: it cannot move for `DisorderSeconds` = **10 s** (Morale Tuning).
- Assumption: "engaged in melee" is `Troop.InMelee` (engaged with an enemy troop, within A_Distance plus the hold
  margin).
- Assumptions, what disorder does: Shock resets to 0 and EffectiveMorale drops by the retreat penalty (5), as for a
  retreat (it counts in `RetreatCount`). Its Move, Pursue and line orders are cancelled, and new orders are refused until
  it ends. The pivot does not march, pursue or get pushed by line pressure; friends can still nudge it (SU-94) and a
  chain does not pull it along (shared retreat). It can still fight, charge and shoot. The troop card's Formation row
  shows "Disordered Ns". The AI treats it like a retreating troop and re-issues its order afterwards.
- Assumption: breaking again while disordered restarts the 10 s (another penalty); if the troop is in melee by then it
  retreats, which ends the disorder. Case A (rout) and Case C (surrounded break) are unchanged.
- Tests: `TroopScenarioTests.BreakOutOfMelee_Disorders_NoRetreat_NoMovementForTenSeconds`, `BreakInMelee_StillRetreats`.
  Retreat tests now put the troop in melee first.

### SU-105 Ranged weapons fire in volleys only when Volley is set (Decision + Assumptions, amends spec §4.7)
- Decision: ranged weapons have a **Volley** box, off by default. With Volley the troop's soldiers with that weapon
  shoot in sync, all at once, as every troop did before. Without it each soldier shoots on its own timing.
- Assumptions: a soldier without Volley shoots once every `ActionWindowSeconds / FireRate` (+/- 10% jitter), its first
  shot after a random part of that interval, so the troop's shots spread out. The firing conditions, target troop and
  hit roll are unchanged. Each single shot goes to the reachable enemy soldier with the fewest of the troop's projectiles
  already flying at it (nearest on a tie). In a troop with both kinds, the volley soldiers fire together and the others
  on their own.
- Fire & Engage volleys (SU-90) stay volleys whatever the box says.
- No existing weapon has Volley ticked (Short Bow, Javelin): every archer and skirmisher now shoots on its own timing.
  Tick it on the weapon asset for drilled volley fire.
- Code: `RangedWeaponProfile.Volley`, `RangedFireController` (`SingleShots` counts the single shots).
- Tests: `ChainAndRangedScenarioTests.ArchersWithoutVolley_ShootOnTheirOwnTiming`; the volley test uses a Volley bow.

### SU-106 Only the front rank charges (Decision + Assumptions, amends spec §4.3; to lengthen melee)
- Decision: only soldiers in the **front rank** may start charges.
- Assumptions: the front rank is one rank's worth of soldiers (the line's soldiers per rank; the troop's frontage when
  it is not in a line), taken as the living soldiers standing furthest toward each enemy troop the troop may charge
  (from its pivot toward that troop's pivot), so a flank attacker is met by the soldiers on that side. Soldiers out
  charging or running back keep their place in that count, so rear soldiers do not step up while they are away.
  Followers (§4.4) are unchanged: any of the Discipline nearest may follow.
- Melee Tuning `FrontRankChargesOnly` (on) switches it.
- Tests: `TroopScenarioTests.OnlyTheFrontRank_StartsCharges`; the 30 s charge-rate test counts front-rank soldiers.

### SU-107 Stunned soldiers (Decision + Assumptions, amends spec §4.6; to lengthen melee)
- Decision: a soldier brought to **0 HP with at least half of that damage Bruise** is **stunned**, not killed. It
  cannot charge while stunned, and dies if its troop **retreats in melee**. It recovers once it has healed to **5 HP**.
- Assumptions:
  - "At least half" is Bruise >= Bleed (Bruise >= MaxHP / 2 at 0 HP). Overflow counts as Bleed.
  - A stunned soldier is alive: it counts toward the troop's soldiers, EffectiveMorale and capture points. Its bruises
    heal at the usual 1 HP/s from 0 HP, so a fully bruised soldier is down for 5 s.
  - It neither charges nor follows, and cannot support (as a fatigued soldier). It can still be charged, and it still
    takes part in a clash it is caught in, though it deals no damage if it wins (SU-111). On 0 HP it has no HP left to take, so a hit
    then does nothing; once healed above 0 a hit that brings it back to 0 stuns it again or, with Bleed over half,
    kills it.
  - It is not snapped back inside the troop radius and walks back to its troop with the field; it does not rush back
    after a charge.
  - "Retreats in melee": a Case B retreat (only possible in melee, SU-104). Assumption: a **rout while in melee** also
    leaves its stunned behind. A disorder or a chain's shared retreat does not. The dead are credited to the enemy troop
    it was engaged with (`BattleEvents.SoldierKilled`).
  - Drawn greyed like a corpse, without its glow, until it recovers.
- Soldier Tuning `StunAtZeroHp` (on) and `StunRecoverHp` (5).
- Code: `SoldierLifeState.Stunned`, `SoldierHealth.IsKnockedOut`, `SoldierAgentState.IsStunned`, `Troop.KillStunnedSoldiers`.
- Tests: `TroopScenarioTests.ZeroHpMostlyBruise_Stuns_AndRecoversAtFiveHp`, `ZeroHpMostlyBleed_Kills_AndStunsCanBeSwitchedOff`,
  `StunnedSoldiers_DoNotCharge`, `RetreatFromMelee_KillsTheStunned_DisorderDoesNot`.

### SU-108 Melee weapon damage -3; spearmen melee lasts about 3 minutes (Decision, amends spec §1.6 sample data)
- Decision: melees between two Spearman troops should last about **3 minutes** from engagement to rout, without changing
  stats or clash resolution, using SU-106, SU-107 and a cut to melee weapon base damage. Measured first: about 1 minute.
- Decision: melee weapon base damage is cut by **3** (the designer chose 3 over the 2 first proposed, which measured
  about 2.1 minutes): Arming Sword 5 -> 2, Boar Spear 4 -> 1, Dagger 2 -> 0, Great Axe 8 -> 5, Lance 3 -> 0 (Couched
  Lance still doubles the total while charging).
- Measured (`MeleeDurationTests`, two Spearman Troops on the project tuning, one attacking and re-attacking after each
  retreat, as a player would): before 1.0 min; with frontage 5 (SU-103) 1.5 min; with SU-106 + SU-107 + damage -2,
  2.1 min; with damage -3, **about 2.75 min on average** (2.0 to 3.2 min across seeds). Both troops attacking: about
  2.5 min.
- Tests: `MeleeDurationTests.SpearmenMelee_LastsAboutThreeMinutes` checks 2 to 4 minutes with the project data. The
  lever sweeps (`SpearmenMelee_Levers`, `SpearmenMelee_AllLevers`) are explicit: run them by name to re-tune.
- Test clean-up: Edit mode never calls `OnDestroy`, so destroyed troops left their soldier sprites in the scene. They
  piled up from test to test and shifted every later troop's random stream (`Troop.StreamId` uses the hierarchy), so
  results depended on test order. Scenario tests now clean up through `TestScene.Destroy`, and a troop destroyed in battle
  removes its sprites at once. `CompositeTroop_ArchersLeadWhenFree_AndFallBackWhenEngaged` averages four seeds (one seed
  had passed only on its leftover-sprite stream).

### SU-109 Hit squash and stretch doubled (Decision, amends SU-36)
- Decision: the squash and stretch of a soldier taking damage is twice as strong: ModelMat Squash 0.4 -> **0.8**,
  Stretch 0.35 -> **0.7** (also the shader defaults). Assumption: Taper (0.8) is the shape of the stretch, not its
  amount, and stays: doubling it would turn the front of the sprite inside out. Timing is unchanged.

### SU-110 Combat log panel (Decision + Assumptions, spec §6 UI)
- Decision: a pop-out **combat log**: an arrow tab on the left edge of the battle screen shows or hides it. It records
  every clash and every ranged attack with its bonuses and the damage done.
- Each **clash**: the two soldiers (troop and soldier number; A is the charger, the earlier charger or the lower id,
  Q-28), charging / Assault / flanked tags, each side's Combat Power as skill + charge + support, P(A), the roll, the
  winner and Domination, then the damage: Strength + weapon + Domination (x2 lance) = raw, split into deflect, bruise,
  bleed and overflow, and the HP lost, with killed / stunned / HP left. A winner whose troop is fleeing deals none (Q-37).
- Each **shot** (logged when it lands): shooter and target, weapon, hit chance and roll, hit or miss, and on a hit the
  same damage line.
- Assumptions: top-left panel, half the screen high, newest entry at the bottom; it follows the newest entry until
  scrolled up. Clashes and Shots toggles filter it. Each entry has a bar in the attacker's army colour. The battle keeps
  the newest 600 entries; times are battle time (m:ss, nothing while paused). Clicks on the panel are not orders and the
  mouse wheel over it scrolls the log instead of zooming the camera.
- Code: `CombatLog` (records, `Combat/CombatLog.cs`), `CombatLogHud` (panel, drawn by `TroopPlayerOrders`),
  `TroopCombatWorld.BattleTime`, `CameraControls.ZoomBlocked`.
- Tests: `TroopScenarioTests.CombatLog_RecordsEveryClash_WithItsBonusesAndDamage`; the single-shot archer test checks the
  logged shots.

---

## 2026-11-06 (2): Illustrated art for the pre-battle tent

### SU-102 Tent hub placeholder sprites replaced by ink illustrations (Decision + Assumptions, TentHub.md §2, §7)
- Decision: the SVG mock-up sprites are replaced one for one (TentHub.md §7) by the designer's sepia ink illustrations:
  Background, Weapon Rack, Debts desk, Gold Chest, Ledger, Map, Dispatch-bearer, Merchant (the man inspecting a coin)
  and Tactician (the robed monk). Their white backgrounds and die-cut sticker borders were cut away to transparency.
- Still placeholders, unchanged art: Captain, War Table and the dispatch glow.
- Assumption: the new background already shows the open tent door, so the Tent Flaps sprite is now a cut-out of that
  doorway from the background (feathered edges) rather than a separate drawing; it still brightens and scales on hover
  and only the doorway takes clicks.
- Assumption: the layout (`TentLayout.json`) is re-fitted to the new background and to each sprite's aspect ratio,
  keeping the left-to-right zones and draw order of §2. Changed positions: the weapon rack, merchant, debts desk and
  chest stand left of the tent pole; the Captain beside the pole; the Tactician behind the war table; the war table
  moved 36 px left and 16 px down so it clears the doorway; the Dispatch-bearer sits on his stool just inside the
  entrance, below the doorway hotspot, holding the dispatch toward the table (the glow follows the dispatch).
- The scene's hotspot rectangles were updated to match `TentLayout.json`, so **Build Tent Hub** reproduces them.

---

## 2026-11-06: Placed troops show their real formation while deploying

### SU-101 Spawned and right-click-placed troops are Idle; a placed line forms at once (Decision, amends SU-82, SU-92)
- Found in play: after SU-92 the outlines of deployed troops showed a line while their soldiers stood in the spawn blob.
  Every placement (the spawn, a right-click and a right-drag) gave the troop a held line, so its formation was Line or
  Chain, but time stands still while deploying, so its soldiers never walked into the line.
- Decision: troops spawned by the deployment zone, and troops placed with a right-click, stand **Idle** (no line
  order, blob outline, card says Idle), like a troop arriving from a Move order in battle. They keep the facing they were
  given.
- Decision: a right-drag line places the troops in that line, and their soldiers are put straight into its ranks, so
  the troop, its outline and its card all show the line. A single troop's drag is a Move (SU-16), so it stands Idle.
- Assumption: the soldiers are laid out as the line fill spreads them (files evenly spaced along the line, ranks
  0.9 apart, first rank in front). Placing a lined-up troop with a right-click scatters its soldiers back into a blob.
- Assumption: a troop placed within S_Distance of an enemy still forms a threat-facing line from the next frame, as in
  battle, while its soldiers stay in the blob until time runs. Deployment zones are normally further away than that.
- Code: `Troop.PlaceAt(center, facing, length, holdLine)`, `TroopRoster.ArrangeInLine` / `ScatterInBlob`,
  `ITroopPlacement.Place(..., holdLine)`.
- Tests: `ChainEngagementScenarioTests.Placement_IdleShowsTheBlob_ALineShowsTheLine_WhileTimeStandsStill`.

---

## 2026-11-05 (2): Fixes from play-test clip Movie_015; charge gate, heat map reach, order arrows

### SU-92 A placed troop's outline moves with it (Decision, bug fix, SU-82)
- Found in play: while deploying, a troop moved with right-click kept its outline (the line or chain box) where it had
  stood before. The outline eases toward the formation over time, and nothing eases while time stands still, so it
  stayed offset by the old position for the whole deployment.
- `Troop.PlaceAt` now rebuilds the troop's line at the new spot at once, and `TroopOutline` snaps to the formation
  whenever there is no time step (deploying or paused). In play the outline still eases as before.
- Tests: `TroopOutlineTests.NoTimeStep_SnapsToTheTarget`.

### SU-93 The deployment zone's border hides with the zone (Decision, bug fix, SU-83)
- Found in play: after Start Battle the zone's white border stayed on screen for the rest of the battle. Only the area
  sprite was hidden; the border is a separate child (`Border`).
- `DeploymentZone` now shows and hides every renderer under it (area and border) with the deployment phase.
- Not changed: the `Border` child still carries a dynamic `Rigidbody2D` and a solid `PolygonCollider2D` from the old
  drag-and-drop zone. Nothing reads them now; remove them from the prefab in the Editor.
- Tests: `DeploymentTests.Zone_AreaAndBorderShowOnlyWhileDeploying`; `PlayedDeployment` also checks it.

### SU-94 Friendly troops do not overlap (Decision + Assumptions, amends spec §2.4)
- Found in play: enemy infantry marched into its own archers and stood in them (29-31 s of the clip), and another
  enemy troop stood in the archers around 50 s. Only friendly pivots were kept apart (0.25 x (r_A + r_B)), so friendly
  troops could stand almost wholly inside each other.
- Decision: friendly troops keep their **outlines** (the formation shape, `Troop.Outline`) at least `FriendlyGap`
  apart. The pivot rule stays as well.
- Assumption: who gives way. An engaged or attacking troop (SU-46) has right of way over a marching or retreating
  troop, which has right of way over a standing one; the troop with right of way is not moved, equals share the move.
- Assumption: the troop giving way moves at no more than `FriendlyGiveWaySpeedScale` (0.5) x its own march speed, so
  its soldiers keep up and a marching troop is slowed while it passes a friend but never blocked for good. So a troop
  can still pass through a friend that is engaged; it just takes longer.
- Assumption: troops linked in a chain, or with a pending link from a line order, are exempt (their lines meet at the
  joints). Routed troops are exempt. Nothing moves while paused or deploying; troops placed on top of each other in
  deployment move apart once the battle starts.
- Assumption: it uses the outline, not the footprint: the footprint grows with straggling soldiers, which would keep
  pushing a troop whose soldiers lag behind its pivot.
- New Formation Tuning values: `FriendlyGap` = 0.5 units, `FriendlyGiveWaySpeedScale` = 0.5.
- Watch for: two AI troops given slots that overlap (for example a guard post on the archers' fallback point) will
  now be shuffled apart, and the AI may keep walking them back to their slots. Space such slots apart.
- Code: `FriendlyOverlapResolver.SeparateOutlines`, `FootprintGap.SeparationDirection`.
- Tests: `ChainEngagementScenarioTests` (standing friends move apart sharing the move, a marching troop has right of
  way and arrives, an attacker is not moved, linked friends are not pushed, nothing moves at a zero time step),
  `FootprintGapTests.SeparationDirection_*`.

### SU-95 Soldiers charge only within the engagement gap; charge reach is A_Distance (Decision + Assumptions, amends spec §4.3, Q-25)
- Decision: soldiers start charges only while their troop is within `EngagementGap` (footprint gap) of an enemy troop.
  Being engaged (within A_Distance) is no longer enough. A soldier's charge distance is `A_Distance`.
- Assumption: the gate allows `ChargeGapTolerance` (0.25 units, Melee Tuning) beyond `EngagementGap`, so lines held at
  the gap by the barrier do not flick in and out of charging.
- Assumption: the charge distance is measured between the two soldiers' edges, like the footprint gap: centre to
  centre it is A_Distance + 2 soldier radii (2.6 units with the project's A_Distance of 2). Lines held EngagementGap
  apart have their front ranks EngagementGap + 2 radii apart, so with the project's A_Distance = EngagementGap = 2 a
  centre-to-centre reach of 2 would almost never find a target.
- Assumption: a charger picks its target only from enemy troops that pass the gate.
- `ChargeRangeScale` (Melee Tuning, 1.5 x A_Distance) is removed; `TroopTuning.ChargeRange` is the new reach and
  `TroopTuning.ChargeGap` the gate.
- Effect at the spec defaults (A_Distance 5.2, EngagementGap 2): lines that engage at 5.2 but stand off do not fight
  until one closes in (an attack order, or the AI). With the project's tuning (both 2) little changes.
- Tests: `ChainEngagementScenarioTests.Charges_StartOnlyWithinTheEngagementGap`, `ChargeReach_IsADistanceBetweenSoldierEdges`;
  `TroopScenarioTests.ChargeRate_Over30Seconds_IsAboutAggression` counts eligible soldiers with the new gate.

### SU-96 The heat map shows only the frontage within A_Distance (Decision + Assumption, amends SU-40, SU-41)
- Decision: the engagement heat map covers only the stretch of front where the two troops are within A_Distance of
  each other. Lines at an angle are closer at one end; the band now stops where their footprints are further apart
  than A_Distance (it used to cover the whole overlap of the lines).
- Assumption: measured across the band between the two footprints' edges (line gap minus both footprint half-widths),
  the same metric as the engagement gap. Facing lines that are nowhere within A_Distance (still engaged by the hold
  margin) show nothing.
- The flank and rear fallback (lines not facing each other) links soldiers within A_Distance + 2 soldier radii +
  the reach slack (it used EngagementGap). With the project's tuning this is the same 3.05 units.
- Tests: `BattleViewTests.WithinReach_KeepsThePartOfTheFrontThatIsCloseEnough`,
  `HeatStrip_AngledLines_StopWhereTheyAreFurtherApartThanADistance`.

### SU-97 Selected troops always show their order arrows (Decision, amends SU-37)
- Decision: every selected troop with a Move, Line or Attack order shows its order path and arrows all the time, not
  only while hovered. The hovered troop (any team) still shows its own; a troop is drawn once.

### SU-98 The victory screen shows the deployed troops (Decision + Assumption)
- Found in play: the post-battle screen's troop panel was empty. It listed the whole campaign roster
  (`CampaignData.Roster`), which was empty here.
- Decision: one card per troop the player deployed. From the campaign these are the characters deployed in the tent
  (`BattleReport.DeployedCharacters`), with their level and XP bar.
- Assumption: a level played on its own shows the troops the zone deployed (`BattleReport.DeployedTroops`): the
  roster character if that troop's character is in the roster, otherwise the character's portrait, name and stats
  without level or XP.

### SU-99 Saved roster characters are found in the mission folders (Decision, bug fix, SU-52)
- The campaign characters now sit in `Characters/Campaign/Mission 1`, `Mission 2`, ... A save names each roster
  character by asset name and loaded it from `Characters/Campaign/<name>`, so after the move no saved character was
  found and loading dropped them all from the roster. `RosterCharacter.ResolveCharacter` now also searches the
  subfolders.
- Tests: `CampaignCharacterTests.SavedRosterCharacter_IsFoundInAMissionSubfolder`.

### SU-100 The deployment hint fits the screen (Decision, bug fix, SU-85)
- Found in play: the hint "Deployment: select troops and right-click or drag inside the zone to place them" was cut off
  at both ends. Its box was 1100 wide on a canvas with a 960-wide reference resolution.
- `DeploymentPanel` now spans the hint across the canvas with a small margin and lets it wrap and shrink to fit; the
  setup tool builds it that way.

---

## 2026-11-05: Icons in place of stat, class and weapon skill names

### SU-91 Stat, class, weapon skill and armour icons on the troop card and in the tent (Decision + Assumptions)
- Decision: the UI shows **icons instead of names** for stats, classes and weapon skills, and shows the **name in a
  tooltip** while the cursor is over an icon. Applies to the battle troop card (§6.1, `TroopCardHud`) and to the tent's
  Roster Ledger page and Map roster / deployed cards.
  - **Stats** (Soldiers, HP, Morale, Discipline, Speed, Defence, Strength, Support, Charge, Aggression): icon, then the
    value. The troop card's Morale bar is led by the Morale icon.
  - **Class**: a `CharacterClass` has an **Icon** field. When it is empty the class **name** is shown as before. Archer,
    Cavalry, Huscarl, Skirmisher and Spearmen have icons; the other classes show their names until given one.
  - **Weapon skill**: the weapon class icon (tooltip "Sword skill") then the level. On the troop card a weapon row is
    icon, skill, weapon name, damage; in the ledger each skill is icon, level / cap.
  - **Armour**: a stat row **below Defence**: the armour icon (tooltip: Armour and the armour's name), then Deflect,
    Bruise and Bleed, each after its own icon. No armour shows the unarmoured ratio 0 : 3 : 7 (SU-90). The ledger did
    not show the ratio before; it now has this row in its right column.
- Assumption: stat, weapon class and armour icons are held by one asset, `Resources/UI/Game Icons` (`GameIcons`). Any
  icon left empty falls back to its name as text. Mace and Crossbow have no icon yet and show their names.
- Assumption: in the tent the icons sit on small dark badges, since they are drawn for a dark background and the ledger
  is parchment. The tent icon rows are built in code over the existing text fields, so the Tent Hub scene needs no
  rebuild.
- Not changed: the ledger's equipment lines, the roster card's "Speed / Morale" detail line and the post-battle unit
  card still use words.

## 2026-11-05: Weapon special rules as a list; Fire & Engage; unarmoured ratio

### SU-90 Special rules are a list; Piercing / Bludgeoning become special rules; Fire & Engage; renames (Decision + Assumptions, replaces part of SU-89)
- Decision: a weapon's **special rules are a list** (`WeaponProfile.SpecialRules`), on **melee and ranged** weapons.
  Each entry is a rule plus a value X, used only by the rules that take one. The rules (§1.6):

  | Rule | Weapons | Effect |
  |---|---|---|
  | **Spear Support** (was Boar Spear Support) | Melee | Support range ×`SpearSupportRangeMultiplier` (2) when supporting. |
  | **Couched Lance** (was Lance Charge Double) | Melee | Total melee damage doubled while charging. |
  | **Piercing X** | Melee, ranged | +X to the Bleed part of the target's armour ratio for this weapon's hits. |
  | **Bludgeoning X** | Melee, ranged | +X to the Bruise part of the target's armour ratio for this weapon's hits. |
  | **Fire & Engage** | Ranged | One volley when the troop enters an engagement and one when it retreats from it. |

  Piercing and Bludgeoning are no longer separate weapon fields (SU-89); they are special rules like Couched Lance.
- Existing assets: Pike keeps its rule as Spear Support and Lance as Couched Lance (same saved numbers); the other
  melee weapons have an empty list. The Melee Tuning field is renamed `SpearSupportRangeMultiplier` (the saved value is
  kept). No weapon has Piercing, Bludgeoning or Fire & Engage yet.
- Decision: **no armour defaults to 0 : 3 : 7** (Deflect : Bruise : Bleed), so Piercing and Bludgeoning affect
  unarmoured soldiers. The *Unarmoured* armour asset is also set to 0 : 3 : 7 (its DefenceBonus 4 is unchanged). The
  troop card shows 0 : 3 : 7 for a character with no armour.
- Assumption: a soldier with **no armour** still has **no TotalDefence cap**: every point rolls against 0 : 3 : 7, so
  all of it counts as Shock as before (decision 7). The change is that about 30% now lands as Bruise (heals) instead of
  Bleed. Seeded scenario tests that use unarmoured characters may need their expectations re-checked.
- Decision: **Fire & Engage** fires **once at the start of each engagement and once on the retreat**, per engagement
  (not once per battle):
  - Start: the frame the troop goes from not engaged to engaged, at the enemy troop it is engaged with (the nearest).
  - Retreat: when the troop begins a **Case B retreat** (§5.5) while engaged (or the frame after the engagement ended),
    at the enemy it was engaged with. Once per engagement.
- Assumption: "retreat" is the morale **Case B retreat** (§5.5). A rout, a chain sympathetic retreat and an ordered
  move away from a fight do not trigger the retreat volley.
- Assumption: these volleys ignore the usual limits (stationary, not engaged, not retreating, target not engaged) and the
  front arc: a shooter needs only its weapon range to the target soldier. Soldiers that are charging also fire. Hit
  chance and damage are the normal ranged rules. The volleys count toward `Volleys` (and `FireAndEngageVolleys`), raise
  the normal volley event, and do not change the normal fire cooldown. Outside these two moments a Fire & Engage
  weapon fires normally.
- Assumption: a rule on the wrong kind of weapon (Fire & Engage on melee, Spear Support or Couched Lance on ranged) has
  no effect and logs a warning; repeated Piercing or Bludgeoning entries add up.
- Tests: `WeaponSpecialRuleTests`, `MitigationResolverTests` (no-armour cases) and
  `ChainAndRangedScenarioTests.FireAndEngageArchers_FireOnceWhenTheEngagementStarts` (EditMode).

---

## 2026-11-04 (5): Armour resistance as ratios; Piercing and Bludgeoning weapon abilities

### SU-89 Armour Deflect / Bruise / Bleed is a ratio; weapons gain Piercing X and Bludgeoning X (Decision + Assumptions)
- Decision: armour **Resistance** (§1.6, §4.6) is a **Deflect : Bruise : Bleed ratio** instead of percentages that sum
  to 100. Each point of damage up to `TotalDefence` rolls Deflect with chance `Deflect / (Deflect + Bruise + Bleed)`,
  and likewise for Bruise and Bleed. Overflow above `TotalDefence` is unchanged (always Bleed, no Shock).
- Decision: existing armour keeps its ratio, divided by 10 and **rounded up**:

  | Armour | Was (%) | Now (ratio) |
  |---|---|---|
  | Light Armour, Light Armour + Buckler, Light Armour + Shield | 30 / 50 / 20 | 3 : 5 : 2 |
  | Chainmail | 60 / 30 / 10 | 6 : 3 : 1 |
  | Mail + Shield | 70 / 25 / 5 | 7 : 3 : 1 |
  | Unarmoured (asset) | 15 / 45 / 40 | 2 : 5 : 4 |

  Rounding up shifts Mail + Shield (Deflect 70% → 64%, Bleed 5% → 9%) and Unarmoured (Deflect 15% → 18%, Bleed 40% →
  36%); the others are exact. Spec sample data (§1.6) reads 3 : 5 : 2 for light armour and shield and 6 : 3 : 1 for
  heavy armour. The Phase 01 data generator uses the same ratios.
- Decision: two new **weapon abilities**, each with a value X:
  - **Piercing X:** when rolling damage done by this weapon against armour, the **Bleed** part of the ratio is
    increased by X (e.g. Piercing 2 against 6 : 3 : 1 rolls as 6 : 3 : 3).
  - **Bludgeoning X:** when rolling damage done by this weapon against armour, the **Bruise** part of the ratio is
    increased by X (e.g. Bludgeoning 2 against 6 : 3 : 1 rolls as 6 : 5 : 1).
- `ArmourProfile` parts are now any whole number ≥ 0 (no 0–100 slider); an armour is invalid only when all three
  parts are 0 (a warning is logged; if it is used anyway every rolled point is Bleed). The troop card shows armour as
  `Deflect : Bruise : Bleed`.
- Assumption: Piercing and Bludgeoning are **fields on every weapon** (`WeaponProfile.Piercing`, `.Bludgeoning`, 0 =
  none), so both **melee and ranged** weapons can have them, and one weapon may have both. They are separate from the
  single melee `SpecialRule` (Boar Spear, Lance), so a weapon can carry a special rule as well.
- Assumption: a melee hit uses the winner's **melee weapon**; a projectile uses the **ranged weapon that fired it**.
- Assumption: a soldier with **no armour at all** still takes every point as Bleed with no roll (decision 7), so
  Bludgeoning does not turn that damage into Bruise. A character wearing the *Unarmoured* asset does roll, and the
  abilities apply.
- Assumption: no existing weapon has Piercing or Bludgeoning yet (all 0); set them per weapon in the Inspector.
- Tests: `MitigationResolverTests` (EditMode) updated to ratios, plus ratio scaling, Piercing, Bludgeoning, overflow
  and all-zero cases.

---

## 2026-11-04 (4): Battle speed bar; orders while paused (Assets/Scripts/Battle/Troop/View/BattleSpeedHud.cs)

### SU-88 Pause and speed controls, with orders allowed while paused (Decision + Assumptions)
- Decision: a speed bar in the **top-right corner** of the battle screen has four buttons: **Pause**, **½ speed**,
  **Play** (normal speed) and **2× speed**. The button for the current state is lit (Pause in orange, a speed in
  blue); hovering a button names it under the bar. It is drawn by `TroopPlayerOrders` (IMGUI, like the troop cards),
  so every scene with player controls has it: Level 1, Level 2 and Level 1 Troop Test. No scene or prefab changes.
- Decision: while paused the player can still **move and zoom the camera, select troops and give orders** (§6). Troops
  carry the orders out when time runs again. (Before, selection and orders were blocked while paused.)
- `GamePause` now owns the game speed (`GamePause.Speed`, `SetSpeed`): `Time.timeScale` is the speed while nothing
  holds the pause and 0 while paused. Everything timed by game time (troops, AI, capture points, path rebuilds)
  speeds up or slows down with it; the camera and HUD run on unscaled time.
- Assumption: **Pause keeps the speed**; P (or gamepad Start) toggles pause and resumes at that speed. ½, Play and 2×
  set their speed and resume.
- Assumption: the bar is **hidden while deploying** (deployment already holds the pause) and once the battle is over.
  A speed button pressed while a cutscene holds the pause sets the speed but does not end the cutscene's pause.
- Assumption: every battle starts at normal speed, and leaving the battle scene resets the speed to normal so the
  campaign scenes are unaffected.
- The mid-screen **Paused** label no longer blocks clicks (its graphics are made non-raycast at runtime), since orders
  can be given beneath it.
- Assumption: 2× speed doubles the simulation step per frame rather than running two steps; no extra stability
  handling was added. Watch for jitter in large melees at 2× on low frame rates.
- Tests: `BattleSpeedTests` (EditMode).

---

## 2026-11-04 (3): Default troops on the battle report; sprite defaults removed

### SU-86 Level default troops live in BattleReport.DeployedTroops (Decision, replaces part of SU-84)
- Decision: `BattleReport.DeployedRoster` is renamed **DeployedTroops**, and it is the only list the deployment zone
  deploys (`DeploymentZone.TroopSource`, set by `RosterTroops`). The zone's **Default Troops** field is removed.
- Each level scene's **DataManager** holds that level's default troops in its BattleReport's Deployed Troops. Played
  from the campaign, the persistent DataManager's report (filled from the tent's characters) is used and the level's
  own DataManager is destroyed, so the chosen roster overrides the defaults with no extra logic.
- `BattleReport` resolves to the scene's own report if read before any report has woken up, so the zone can read
  it from its Awake when a level is played on its own.
- Assumption: a report with no troops deploys none.
- The setup tool moves the zone's old Default Troops (Level 1: Julian's, Bandit and Franz's Troop; Level 2: Julian's,
  Franz's, Jagen's and Robin's Troop) onto each level's DataManager.

### SU-87 Troop Profile Sprite Defaults removed (Decision)
- The `TroopProfileSpriteDefaults` script and its **Troop Profile Sprite Defaults** objects (Level 1, Level 2, Level 1
  Troop Test) are removed as redundant. `CharacterProfile.SetSpriteIfMissing` stays (`TroopSoldierShowcase` uses it).

---

## 2026-11-04 (2): Deployment phase (Assets/Scripts/Battle/Controller/DeploymentZone.cs)

### SU-81 Optional deployment phase that pauses the battle (Decision + Assumptions)
- Decision: `Battle` has **Deployment** (Inspector, default on). With it, the battle opens in the **Deployment** phase:
  the battle simulation is paused (time stands still) and the player places troops inside the deployment zone. The
  **Start Battle** button (`Battle.StartBattle`) ends the phase and the fighting begins (`Deploy` event, then
  `PhaseChanged`). `Battle.Phase` is Deployment, Fighting or Over.
- Decision: the **enemy AI starts with the scene** (`AIDirector` Begin On Start), not on Deploy (supersedes SU-76's
  Deploy wiring). It gives its first orders while paused and acts once time runs.
- Assumption: a battle with Deployment on but **no deployment zone for the player army** starts fighting at once (the
  Level 1 Troop Test scene, the CLI smoke test).
- Assumption: battle time (`ElapsedTime`, the report's time taken) counts only fighting time.
- Decision: pausing is shared (`Misc/GamePause.cs`): deployment, the player's pause key (P) and cutscenes each hold the
  pause, and time runs only when all have released it, so a cutscene ending cannot cut deployment short. P does
  nothing while deploying.
- Clicks on UI (the Start Battle button) are no longer read as selection or orders.

### SU-82 Placing troops during deployment (Decision + Assumptions)
- Decision: while deploying, the player's usual order gestures **place** troops instead of marching them: select, then
  right-click (Move) or right-drag (Line) and each troop is put straight at its planned spot with that facing and line
  length (`Troop.PlaceAt`). Attack orders do nothing while deploying.
- Assumption: a spot outside the zone is **moved inside it**, keeping the whole troop (its blob radius) in the zone
  where the zone is big enough. Troops placed on top of one another are not separated.
- Assumption: placed soldiers keep their blob around the pivot while paused and form the line once the battle runs. (Replaced by SU-101: a placed line forms at once; other placements stand Idle.)

### SU-83 Deployment zone prefab refactored (Decision)
- The prefab is renamed **Deployment Zone** (same GUID; scenes keep their instances). Its component is now
  `DeploymentZone` (`Battle/Controller`, the old `DeployUnitFromBattleReport` script moved and rewritten). The area is
  the object's unit square (scale it); the sprite shows it only while deploying. The old drag-and-drop
  `BoxCollider2D` and `Rigidbody2D` are removed, and the zone no longer needs hiding through the Deploy event.
- At scene start the zone spawns one troop per profile (from the Troop prefab) under its army, in rows inside the area,
  facing the enemy army. Fields: Army, Team Id, **Default Troops**, Troop Prefab, Gap.

### SU-84 Deployed roster is a list of troops; level default troops (Decision + Assumptions)
- Decision: `BattleReport.DeployedRoster` (now **DeployedTroops**, SU-86) is a list of **TroopProfile** assets: the troops the next battle
  deploys. The tent's chosen characters move to `BattleReport.DeployedCharacters` (gold, XP and achievements still
  follow characters), and `RosterTroops` rebuilds `DeployedRoster` from them whenever a character is deployed or
  withdrawn.
- Decision: each level's zone has **Default Troops**, deployed when no roster was chosen in battle preparation (a
  level played on its own). A chosen roster replaces them.
- Assumption: a character's troop is the troop asset in `Resources/Troop and Character Profiles/Characters/Campaign`
  made of that character alone (Franz's, Jagen's, Julian's and Robin's Troop). Characters without one (Brom, Gareth,
  Lyra, Richter) get a runtime troop of radius 5.
- Level defaults set by the setup tool: **Level 1** = the three player troops that were placed in the scene (Julian's
  Troop, Bandit Troop, Franz's Troop), which are removed from the scene and now spawn from the zone; **Level 2** (no
  placed player troops) = Julian's, Franz's, Jagen's and Robin's Troop.

### SU-85 Setup tool (Decision)
- **Leveraged Wars > Battle > Set Up Deployment (Levels 1 and 2)** makes the prefab changes above, adds the
  **Deployment Panel** (hint text and Start Battle button, `DeploymentPanel`) to the battle Canvas prefab, and in each
  level turns Deployment on, fills the zone's defaults, clears the old Deploy event calls (missing targets, the zone
  hiding itself, `AIDirector.BeginBattle`) and sets the enemy AI to start with the scene, then saves the scenes.

---

## 2026-11-04 (1): Mission 1 enemy AI in Level 1 (Assets/Scripts/Battle/AI/Editor/Mission1Setup.cs)

### SU-76 Mission 1 AI set up by a tool (Decision + Assumptions)
- Decision: **Leveraged Wars > AI > Set Up Mission 1 (Level 1)** builds the Mission 1 AI in Level 1: an `EnemyAI`
  object (`BattleAIBattlefield`, `AIDirector`, `AIMission`, `AIDebugOverlay`), its markers under `EnemyAI/Markers`,
  the detachments and the triggers of the AI README §3. Running it again keeps existing markers where they are and
  rebuilds the AI settings.
- Detachments: **Archers** (the archer, Skirmish: fallback `ArcherFallback`, minimum range 7, at most 30 from the
  fallback), **Guards** (Alpha and Beta, Guard: aggro 10, leash 16), **Cavalry** (Cav, Hold at its start: aggro 8,
  leash 12). Triggers: **CavalryLoop** (a player troop crosses `LureLine` → Cavalry Flank the rearmost player troop
  via `CavalryRoute1`, `CavalryRoute2`) and **GuardsAdvance** (Archers broken → Guards Advance).
- Assumption: marker placement for the Level 1 map: `ArcherFallback` (-2.5, 18.5) just behind the gap between the
  guards; `LureLine` across the field at y = -2, firing northwards; `CavalryRoute1` (-35.5, -14) in the gap south of
  the river's end; `CavalryRoute2` (-33, -21) south-west of the player's deployment line.
- Assumption: the archer's minimum range is **7** (README: 6) because Level 1's archer has a Short Bow (range 10, so
  it stands at 8.5); 6 would let a chaser reach it. The cavalry's aggro and leash are **8 / 12** (README: 4 / 7) so it
  fights back if a player troop finds it.
- Decision: `AIDirector` starts on Battle's **Deploy** event (Begin On Start unticked), so the AI waits during
  deployment.

### SU-77 Guards guard the archer's fallback point (Decision, change to AI README §3)
- Found in play: guarding the **Archers detachment** sent the guards after every player troop the archer harassed,
  out to their leash and back. The Guards now guard the `ArcherFallback` marker, so they wait at their post and
  intercept what follows the archer back to them, as in the Mission 1 plan.

### SU-78 Rout mission: the battle is won at over half the enemy troops (Decision + Assumptions)
- Decision: `Rout` gains **Rout Share**: the battle ends when more than that share of the army's troops are out
  (routed or wiped out). 1 (the default) keeps the old rule, every troop. Level 1's enemy-army rout uses **0.5**: rout
  or destroy over half the enemy army (3 of its 4 troops) to win.
- Assumption: the **player's loss** in Level 1 is unchanged (every player troop out). Set its Rout Share to 0.5 too if
  the rule should apply to both sides.
- Assumption: the share counts **troops**, not soldiers, and a troop that routed and left the field still counts
  towards the total.

### SU-79 Thrown weapons do not make a troop ranged for the AI (Decision)
- Found while setting up Mission 1: Level 1's Bandits carry Throwing Axes (range 3), so the AI treated the two
  guarding infantry as ranged troops; ranged troops never charge in Guard and Hold, so they would have stood and
  thrown instead of intercepting.
- `TroopAIAdapter.IsRanged` is now true only for a troop with a living soldier carrying a **Bow or Crossbow**.
  Javelin-class weapons (javelin, pilum, throwing axe) are thrown by infantry before contact: the AI orders those
  troops as melee, and their `RangedFireController` still throws by itself.

### SU-80 Flank skips an unreachable waypoint (Assumption)
- A Flank route waypoint is skipped, with a console warning, when the detachment gets no closer to it for **4 s**
  (Flank: Waypoint Stuck Seconds), e.g. a marker placed off the walkable ground. Previously the cavalry waited there
  for ever.

---

## 2026-11-03 (2): Pathing, map edges and pursuit facing (bug fixes)

### SU-73 Troops path around terrain on the NavMesh (Decision + Assumptions)
- Decision (bug: troops did not path around the river): `NavPathfinder` snaps the troop pivot (within 6 units) and the
  destination (within 25 units) onto the NavMesh before planning. A straight line is used only when both
  `NavMesh.Raycast` and the troop-sized terrain box cast are clear; otherwise `NavMesh.CalculatePath` is used. Spec
  §3.2 Pathing.
- Found while fixing: **Level 1's NavMeshSurface has no baked NavMesh assigned** (`m_NavMeshData` is empty on the
  `==========MAP====================` object; the Level 1 Troop Test scene has one). Without a NavMesh no route could
  be planned, so a blocked order did nothing. Bake the surface in Level 1.
- Assumption: with no NavMesh under the troop the path falls back to a straight line and logs one warning per troop;
  terrain then stops the pivot (SU-74).
- Assumption: a Move or line-order march whose path ends short of the destination (destination off the NavMesh, or no
  route) completes where the path ends, instead of staying in Column for ever. Pursuit keeps replanning.

### SU-74 Troop pivots stay off terrain and on the map (Decision + Assumptions)
- Decision (bug: troops ran off the map edge, seen when pursuing routing troops): every pivot movement (march, line
  push, retreat slip, rout flight, `ShiftPivot`) is limited each frame by `Troop.ConstrainPivotStep`:
  - the **battlefield edge** is the bounding rectangle of the scene's NavMesh (`BattlefieldBounds`), inset by the blob
    radius (`BlobRadiusScale × TroopRadius`) so soldiers stay on the map. Level 2 has no boundary walls and relies on
    it;
  - the pivot is **clipped against terrain colliders** (the troop's obstacle mask: Terrain, Water, Water/flat
    impassible terrain), so boundary walls and rivers stop it; the rest of the step slides along the obstacle.
- Assumption: **routed troops do not leave the map**: they run along the edge until they despawn after
  `RoutDespawnSeconds`, and can be caught there. Previously soldiers were carried across terrain with the pivot.
- Assumption: a pivot already inside a collider is let out (2D physics queries that start in a collider ignore it).

### SU-75 A pursuer squares up to the face it attacks (Decision + Assumption)
- Decision (bug: the pursuing troop did not align to the enemy when engaging): a troop with a Pursue order whose target
  is within S_Distance takes its threat heading from that target alone (not the weighted blend of nearby enemies). The
  heading runs from the direction to the target's pivot at S_Distance to square with the attacked face at A_Distance:
  facing against the target's Front, flank or Rear normal, so the lines meet parallel. A target without a line is
  faced pivot to pivot. Spec §3.3, §4.1.
- Decision: the engaged **facing lock is lifted for a pursuer engaged with its target**, so it finishes turning
  (heading smoothing, 0.8 s) after contact. It still applies while the pursuer is engaged only with other enemies, and
  to every troop without a Pursue order.
- Assumption: the attacked face is classified as for flanking (`FaceClassifier`), so a pursuer that hits a line's flank
  aligns with that flank.

---

## 2026-11-03 (1): Player troops in Level 1 on the player team (Assets/Resources/Levels/Level 1/Level 1.unity)

### SU-72 Level 1 player troops set to team 0 (Decision)
- Decision: the three troops under `PLAYER ARMY` in Level 1 now override `Troop._teamId` to `0`. They are instances of
  `Assets/Prefabs/Battle/Troop.prefab`, whose default is `1`, so they were on the enemy team: `TroopPlayerOrders`
  (player team 0) could not select or order them, and the AI (`TroopAIAdapter.PlayerTeamId` 0) counted them as its own.

---

## 2026-11-02 (1): Enemy AI, Basic tier (Missions 1 and 2) (Assets/Scripts/Battle/AI/README.md)

### SU-67 Basic enemy AI added (Decision)
- Decision: the enemy AI from the *Enemy AI Specification & Plan* is added under `Assets/Scripts/Battle/AI/`
  (namespace `LeveragedWars.Battle.AI`): `AIDirector` runs a commander (`AIMission`, the Basic tier) that gives each
  **Detachment** a **Formation** (Hold, Guard, Skirmish, Support fire, Advance, Flank, Capture, Fall back) and changes
  them with authored triggers. Troops follow their formation's order plus reflexes. Setup and the Mission 1 and 2
  configurations are in the AI folder's README.
- Code names follow the glossary: the AI README's `AISquadDefinition` / `SquadTaskDefinition` / `ISquadTask` are
  `Detachment`, `FormationDefinition` and `Formation`. Its Planning and Analysis folders are left for the Advanced tier.
- The top layer is pluggable: `AIDirector` drives any `IAICommander`; the Army Planner will implement the same one.
- Formations, conditions and actions are authored in the Inspector with `[SerializeReference]` and the MackySoft
  `SubclassSelector` already in the project.

### SU-68 Changes to the AI spec (Decision)
- **Fall back takes a position only** (spec edit removing "facing the enemy where possible"). On arrival the formation
  issues a Hold with a facing.
- **Anchors can be offset** (side and forward). Mission 2's first-line cavalry holds 10 units to the left of the
  advancing FirstLine rather than at its start position.
- **Mission 2 archers stand in front of the second line** (Support fire stand-off −5), matching the Mission 2 plan
  image. Support fire allows a negative stand-off for this.
- **Mission 2 bypass trigger** is a Hold at a WallGap marker plus disabling SecondLineGo, instead of a Fall back
  followed by a Hold.

### SU-69 The AI on the troop system (Decision + Assumptions)
- Decision: the AI reaches the game only through `IAITroop`, `IAIObjective` and `IAIBattlefield`. The adapters
  (`TroopAIAdapter`, `CapturePointAIObjective`, `BattleAIBattlefield`) are the only AI code that knows `Troop`,
  `CapturePoint` and `Battle`. `BattleAIBattlefield` adds them to every troop and capture point in the scene.
- Decision: the battle is 2D, so the AI works on the XY plane and a facing is the transform's right (red arrow), as
  `Troop.Facing`. Scene markers are oriented by their red arrow.
- Order mapping: Hold = `OrderLine` at the position and facing with the natural line length; Move and Fall back =
  `MoveTo`; Attack and Charge on a melee troop = `Pursue`. A ranged troop told to Attack stands facing the target (and
  walks to 85% of its range first if the target is beyond 105% of it); `RangedFireController` keeps choosing what to
  shoot (spec §4.7).
- Assumption: troops have **one speed**, so the AI's run flag is ignored and an enemy marching at 60% of its Move speed
  towards a holding troop counts as a charge for the brace reflex.
- Assumption: there is **no ammunition** yet; a ranged troop can always shoot.
- Assumption: a **retreating** troop (fault slip) is treated like a routing one: the AI leaves it alone and re-issues
  its order once it recovers. A routed troop never recovers (spec §5.4).
- Assumption: a troop is **in melee** when it is engaged or has soldiers in melee; its melee opponent is
  `Troop.EngagedEnemy`.
- Assumption: a troop is on the AI team when it is under `Battle.enemy`; troops under neither army use team id 0 = player.
- Assumption: a line order holds the troop's facing, so the AI's facing is kept while holding. To confirm in play: the
  troop's own threat facing may turn it instead.

### SU-70 Capture points report contest and radius (Assumption)
- `CapturePoint` gains `IsContested` (both armies had living soldiers inside at the last count) and `Radius` (the 2D
  collider's larger half-extent, or the fallback radius without a collider). Behaviour is unchanged.
- Objective ids: a `CapturePointAIObjective` on the point sets the id (e.g. `Primary`); without one the id is the
  GameObject's name.

### SU-71 AI behaviour details (Assumptions)
- **Distances are in world units** (troop radius 3–8, bow range 10–20). The AI plan's metre values are scaled down by
  about 3.5; all are Inspector values to tune in play.
- Troops are only re-ordered when the order changes, with tolerances of 0.75 units and 12°.
- **Engaged** means the formation reports Engaged, any troop is in melee, or a ranged troop is shooting.
- **Broken** means the share of troops still fighting is strictly below the threshold, or no troops are left.
- **Triggers fire on the rising edge** of their condition. A re-enabled trigger fires straight away if its condition
  already holds.
- **Reflex priorities:** Fall back is never interrupted; fighting back in melee overrides everything else; ranged
  troops shoot and others brace only while holding at their position.
- **Ranged targeting** keeps the current target while it stays valid and within 1.05 × range. Enemies already in melee
  are never chosen (as Q-39 for the fire controller).
- **Rearmost opponent** means the opponent furthest from the AI army's centre.
- **Flank** charges once within charge distance and beside or behind the target. If it starts in front, it first swings
  out to the nearer side. It charges anyway after a 20 s approach.
- **Skirmish** resumes harassing once back at the fallback point and the nearest enemy is beyond 1.5 × minimum range. It
  never stands more than 30 units from the fallback point.
- **Support fire** archers shoot only once at their slot, not while moving to keep up.
- **Capture** holds the objective once taken; this is also the Mission 2 bandit's behaviour after capture, until that
  open question is settled. A detachment of several troops lines up on the objective, so the outer troops may stand
  outside the point.

---

## 2026-11-01 (12): Legacy Unit/Model system removed (Docs/Battle/Plan/11-legacy-removal.md)

### SU-57 Troops stand alone; the legacy Unit/Model code is deleted (Decision, answers part of Q-49)
- Decision: the old `Unit/` scripts (units, models, formations, unit movement, old ranged combat, old stats), the old
  Player IO controllers (`PlayerController`, `PlayerSelectUnits`, `PlayerUnitOrders`, `PlayerOrderUnits`,
  `PlayerSelectionView`, `PlayerCursor`), the uGUI `BattleUI/UnitCard`, `PCWrapper`/`StatWrapper`, `Reinforcements`
  and the old audio hooks are deleted. `NavPathfinder`/`IPathfinder` moved to `Pathfinding/` (same GUIDs).
- `Troop` is a plain MonoBehaviour. `TroopState` (Idle, Moving, Fleeing; old enum values kept) replaces `UnitState`;
  the legacy stats field is gone (Architecture §11 #13). `Troop` raises `StateChanged`, `EnteredMelee`/`ExitedMelee`,
  `Routed` and `Defeated`, and exposes `Army`.
- New `BattleEvents` (battle-wide, cleared on entering play mode): `TroopDefeated`, `SoldierKilled` (with the attacking
  troop), `SoldierStruck`, `VolleyFired`, `ProjectileLanded`. The simulation only raises them.
- Assumption: a troop is **defeated** once, at rout or when its last soldier dies before it routs. This replaces the
  old `IUnit.OnUnitDestroyed` and keeps its timing (Q-36, D-32).
- Code: `Runtime/Troop.cs`, `Runtime/TroopState.cs`, `Combat/BattleEvents.cs`, `DuelResolver`, `TroopProjectiles`,
  `RangedFireController`. Test: `Rout_RaisesTheDestroyedEventOnce_AndDespawnsLater` now listens to `TroopDefeated`.

### SU-58 Battle end, report and scene flow on troops (Decision + Assumptions, answers part of Q-49)
- `Army` lists its child troops; `ActiveTroopCount` counts troops not yet defeated.
- `Rout` ends the battle when every troop of its army is defeated (was: one unit left).
- `Battle` no longer loads scenes or touches campaign data. It raises `Started` and `Ended`; the new `BattleFlow`
  (battle-to-campaign bridge) loads Post battle (index 2) on victory, and on defeat discards the campaign data and loads
  the Main menu (index 0), as before.
- Assumption: the battle report counts **kills** as enemy soldiers that died and **deaths** as player soldiers that
  died, whoever dealt the blow. Time taken is from the battle scene's start to the end of the battle.

### SU-59 Capture points count soldiers (Assumption, answers part of Q-49)
- A capture point counts the living soldiers of each army inside its 2D collider (or a radius without one) four times a
  second. Control moves at (player soldiers - enemy soldiers, capped at ±30) / 50 per second, as before.
- Assumptions: the point belongs to the player at `holder ≥ threshold`, to the enemy at `holder ≤ -threshold`, neutral
  between (the old enemy test compared against `+threshold`). `capturedBy`, `PointCaptured` and `CapturedBy` fire once
  when the controller changes (`CapturedBy` used to fire every frame above 0.8).

### SU-60 Deploying the tent's roster (Assumption, answers part of PB-Q4)
- `DeployUnitFromBattleReport` spawns one troop per character in `BattleReport.DeployedRoster`, each with a runtime
  `TroopProfile` holding just that character, in a grid across the deployment zone, facing the zone's up, team 0.
- Assumptions: 20 units between troop centres; troop radius 16.5 (the `TroopProfile` default); the troop visualiser
  prefab is added to each troop. Pairing into composite troops still waits on PB-Q4.

### SU-61 Character achievements on the new character system (Assumptions)
- Each `CampaignCharacter` lists its character achievements; the achievement controller sets them up for the troop the
  deployed character leads. Rewards go to the `RosterCharacter`.
- Assumptions: **Kill tracker** counts enemy soldiers whose killing blow came from the character's troop (melee or
  ranged). **Character survived** means the troop was not defeated. **Capture points** counts points held by the
  troop's army. The old PC data assets' achievement lists are not carried over; assign achievements on the campaign
  characters.

### SU-62 XP and levels for roster characters (Assumption, open item)
- `RosterCharacter.AddXp`: 100 XP per level (`XpPerLevel`), carried over. The post-battle card fills its XP bar and plays
  the level-up sound when it wraps.
- Open item: stat growth on level-up (growth chances and class caps, spec §1.1) is **not applied yet**, so the
  post-battle card no longer lists stat gains.

### SU-63 Save system reset (Decision)
- Saves go to `campaign.json` with `SaveVersion` 2. A save with another version is deleted and a new game starts; the
  old `data.game` is deleted on start-up. The roster is saved by campaign character asset name (object references do not
  survive a JSON save) and resolved on load. `CampaignData.Characters` (`PCWrapper`) is removed.

### SU-64 Battle sounds from troop events (Assumption)
- `ModelSoundEffects` (the Model SFX prefab, one per battle) plays the strike sound on a damaging melee hit, the shot
  sound on each volley, a death grunt per soldier killed, and an optional projectile-hit sound. Assumptions: each sound
  type is limited to one every 0.08 s; sounds are not positioned.

### SU-66 Pause in the troop controls; battle canvas leftovers removed (Decision + Assumptions, spec §6)
- Decision: P (or Start on a gamepad), the old Pause binding, pauses and resumes the battle (`Time.timeScale`) and shows
  the canvas's "Paused" text. Assumptions: no selection or orders while paused; pausing does nothing while a cutscene has
  stopped time.
- Decision: the Deploy button (old deployment phase) and the unused "Victory and Defeat" text are removed from the
  battle canvas, and Level 1's "Dynamic Obstacle" (old Unit layer) from the scene.

### SU-65 Old tabbed prep menu removed (Decision)
- The tab-based Battle Preparation UI (`PreparationContoller`, `ToBattleButton`, the tabs, buy/sell/unit selection
  views, `UISelectionNavigation`) and its prefabs are deleted; the tent hub replaces it. `PreparationData`, `BuyOrders`
  and `SellOrder` stay.

---

## 2026-11-01 (11): Roster always holds the campaign characters

### SU-56 The tent fills the roster from Characters/Campaign (Decision + Assumption, amends SU-52)
- Fix: the roster was empty whenever the campaign data had no `Roster` (a real save from the main menu, or a sample
  made before SU-52).
- Decision: every `CampaignCharacter` in `Resources/Troop and Character Profiles/Characters/Campaign` is in the
  roster. At tent start (`TentHubController.EnsureRoster`): an empty roster is first copied from the sample's roster
  (mock level, XP, cost); then any character from that folder still missing is added at level 1, 0 XP, cost 25.
- Assumption: this is for the mock phase. `_fillRosterFromCampaignFolder` on the controller turns it off once the
  campaign recruits characters itself.

---

## 2026-11-01 (10): Tent roster on the new character system; book panels

### SU-52 The tent roster uses the new character system (Decision + Assumptions, answers part of PB-Q4)
- Decision: roster and deployment in the tent use `CampaignCharacter` assets, not `PCWrapper`.
- New `Campaign.RosterCharacter`: a `CampaignCharacter` plus campaign progress the asset does not hold: `level`,
  `xp` (0-99, 100 per level) and `costToField` (5-50 gold). Stored in the new `CampaignData.Roster`.
- Deployed characters go to the new `BattleReport.DeployedRoster` (`TentDeployment`: deploy costs gold, undeploy
  refunds, cap of 5).
- The sample roster is every `CampaignCharacter` in `Resources/Troop and Character Profiles/Characters/Campaign`
  (8), with mock level, XP and cost. Mock court: Lyra (tactician), Jagen (captain), Robin (dispatch-bearer).
- Assumptions / open:
  - XP is 100 per level (Fire Emblem style); level-ups do not change the asset's stats yet.
  - **The battle does not read `DeployedRoster` yet.** Level scenes still spawn from the old
    `DeployedCharacters`, which the tent no longer fills, so marching out from the tent gives no player troops
    until the battle spawns troops from `DeployedRoster` (next step of PB-Q4).
  - A real save has an empty `Roster` until the campaign moves to the new system. JsonUtility saves asset references
    as instance IDs, which do not survive a restart; the save format needs asset paths or GUIDs.

### SU-53 Loans and debts are cards taped into a book (Decision + Assumption, amends TentHub.md §4.1)
- Decision: the Debts panel is an open book. Loan offers are cards taped to the left page, debts owed to the right
  page. Each card: title, terms, a status line (what a click does, or why it cannot), and the lender's note. A
  stamp shows TAKEN / REPAID. The cover shows gold and prestige. Replaces the old Sell / Buy prefabs.
- Rules unchanged from SellView / BuyView, plus an assumption: a taken loan can only be given back while its gold is
  still in the chest (before, giving it back could take gold you had already spent, clamping to 0).

### SU-54 Roster Ledger is a book, one character per page (Decision, amends TentHub.md §4.2)
- Decision: the Ledger is an open book with one character per page, two per spread. Page turning: corner buttons,
  Q / E, Page Up / Down, gamepad shoulders.
- Each page: portrait, name, class, level and XP bar, soldier stats with class caps (Soldiers, HP, Morale,
  Discipline, Speed, Defence, Strength, Support, Charge, Aggression), weapon skills with caps, melee / ranged /
  armour, cost to field and whether the character is deployed.

### SU-55 Deployment shows gold remaining and gold spent (Decision, amends TentHub.md §4.3)
- Decision: next to the deployed troops, the Map shows gold remaining and gold spent on troops; each deployed card
  shows its own cost. The roster and deployed cards are remade to fit the 1280 x 720 screen (roster card 600 x 68,
  deployed card 470 x 56).

---

## 2026-11-01 (9): Deploy cap and tent sample data

### SU-50 At most 5 characters can be deployed (Decision, pre-battle rules, amends TentHub.md §5)
- Decision: a mission takes 2 to 5 deployed characters. Deploying a sixth is refused, like deploying one you cannot
  afford. The Map shows "Deployed troops (n/5)" and the ready text shows the count.
- Code: `LaunchRules.MaximumDeployed`, checked in `UnitSelectionView.Select`.

### SU-51 Tent sample data for playing Battle Preparation directly (Decision + Assumptions)
- Fixes a NullReferenceException in `TentHubController.Awake` when the scene was played without coming from the main
  menu: there was no `CampaignDataManager`.
- The controller now reads campaign data in `Start` (after every Awake). With no `CampaignDataManager` it creates one
  from its `TentSampleData` (`CampaignDataManager.UseSampleData`, never loads or saves a file) and a `BattleReport`
  if none exists.
- Menu **Leveraged Wars > Pre-Battle > Create Tent Sample Data** creates `Tent/Sample/`: a 5-line mock briefing, court
  lines (the tactician has a 2-line flow), the quest cue on, 3 loan offers, 2 open repayments, 200 gold, prestige
  20/40, and a roster of every PC_Stats asset (6) with mixed XP and cost, using the first LevelTable found.
- Assumptions:
  - Mock court: Mannus as tactician, Sir Jeagan as captain, Robin Shering as dispatch-bearer.
  - The sample manager stays alive (DontDestroyOnLoad) for the rest of that play session, so moving on to other
    scenes keeps the sample data.

---

## 2026-11-01 (8): Pre-battle tent hub

### SU-49 Battle Preparation becomes a diegetic tent hub (Decision + Assumptions, pre-battle UI)
- Decision: the tabbed Battle Preparation menu is replaced by Prince Julian's command tent: briefing as dialogue boxes,
  then clickable people and objects (Gold Chest, Weapon Rack, Merchant, Debts, three Court members, Ledger, Tactical Map,
  Tent Flaps). Launch from the Map or the Tent Flaps.
- Decision: Inventory, Shop, Court relationships/quests and Composite Troop pairing are stubs for now.
- Full spec, assumptions and open questions (PB-Q1 to PB-Q7): `Docs/PreBattle/TentHub.md`. Deployment rules (cost to
  field, at least 2 characters) are unchanged.
- Code: `Assets/Campaign/UI/Battle prep menus/Tent/` (TentHubController, TentHotspot + Gold Chest / Debts / Court / Tent
  Flaps hotspots, TentTooltip, DialogueBox, LaunchRules, panels Debts / Ledger / Map / Stub, DeployedTroopCard).
  `PreparationData` gains briefingFlow, briefingSpeaker, court[3] (CourtMember) and questAvailable. Scene built by
  menu **Leveraged Wars > Pre-Battle > Build Tent Hub**, which also deactivates the old tab canvas (not deleted).
  Placeholder art: `Tent/Art/Tent_*.png` + `TentLayout.json`, split from the SVG mock-up at 2x.
- Assumptions made while building:
  - With no briefingFlow, the existing `briefing` text plays as one dialogue line, speaker "Briefing".
  - Court members with no dialogue assigned say a default placeholder line; names default to Tactician / Captain /
    Dispatch-bearer.
  - Clicking the Tent Flaps when the deployment is invalid does nothing; the hover text already gives the reason.
  - Esc / right click / gamepad East close panels and skip dialogue; arrows / A-D / d-pad start object navigation.

---

## 2026-11-01 (7): Chain engagement lock

The clip "Chain engagement lock" showed three allied troops on three sides of one enemy, chained into a U, holding each
other out of melee. `ChainEngagementScenarioTests` reproduces it with the project's Formation Tuning (A_Distance =
EngagementGap = 2). The barrier holds engaged lines right on the engagement edge, so 'engaged' flickered. Each time a
fighter dropped to 'free', chain tension pulled it toward its chain neighbours (about 6 units in the test). An attacking
troop that passed a free chain end snapped onto it and was held at the joint. Diagrams:
`Docs/Battle/MovementEdgeCases/24-chain-wraps-enemy.svg`, `25-engagement-hold.svg` and `26-attacker-passes-chain.svg`
(26 settles the open question in 23).

### SU-45 Engagement hold (Decision + Assumption, answers Q-16, amends spec §4.1)
- Decision: engagement has an exit hold. It applies to everything that reads 'engaged': charges, facing lock, Assault,
  ranged fire, chain pinning (SU-46), the heat map and the troop card.
- Two opposing troops become engaged at a footprint gap of A_Distance or less, as before. Once engaged they stay engaged
  until the gap passes A_Distance + `EngagementHoldMargin`. There is no hold on entry: a pair that is not engaged has to
  come back within A_Distance.
- Assumptions:
  - `EngagementHoldMargin` = 1.0 units (new, Formation Tuning). With the project's A_Distance of 2.0 the hold edge is 3.0.
  - The hold is per enemy: each engagement record remembers whether that pair was engaged the frame before. Both troops of
    a pair release in the same frame.
  - A held enemy keeps its engagement record even when S_Distance is smaller than A_Distance + the margin.
  - Assault still starts only when a troop enters engagement, so a fight that stays inside the hold no longer retriggers
    it.
- Code: `TroopEngagementSystem`, `EngagementRecord`, `Troop.WasEngagedWith`, `FormationTuning`, `TroopTuning`. Tests:
  `ChainEngagementScenarioTests`.

### SU-46 Friends never move an engaged or attacking troop (Decision + Assumption, amends spec §2.4, §3.4, §3.4.1)
- Decision: chain tension and pivot separation never hold back a troop that is engaged or attacking. A free friend in its
  way gives way instead.
- `Troop.IsCommitted`: not routed, and either engaged (with the SU-45 hold) or carrying an attack (Pursue) order. It
  replaces 'engaged' in:
  - `AlliedLinkTension`: the spring and velocity matching act only on a troop that is not committed.
  - Joint weight: the joint stays on a committed troop's line point. When both or neither are committed it is the
    midpoint.
  - Shared retreat (spec §3.4): committed neighbours do not retreat with their chain.
  - `FriendlyOverlapResolver` (spec §2.4): the free troop takes the whole correction. When both or neither are committed
    they share it.
- Assumption: an attacking troop counts as committed before it is engaged. A free troop still linked to an attacker is
  pulled along until the link snaps at 3.0 x L_nat (the pinned anchor rule). The order planner already breaks an
  attacker's links to troops outside the selection, so this matters mostly for orders given in code (the AI).
- Code: `Troop`, `AlliedLinkTension`, `FriendlyOverlapResolver`, `ChainLinker`. Tests: `ChainEngagementScenarioTests`.

### SU-47 Auto-links need facings within 45 degrees (Decision + Assumption, amends spec §3.4 Linking and Breaking)
- Decision: two free line points auto-link only when the troops' facings are within `AutoLinkMaxFacingDeg` = 45 degrees.
  The spec rule was F_A . F_B > 0, so any angle under 90. Troops on adjacent sides of one enemy (about 90 degrees apart) no
  longer chain round it. Links made by a player line order keep the old rules (the stretch and hinge envelope only).
- Assumptions:
  - An automatic link breaks when the facings drift more than `AutoLinkBreakFacingDeg` = 60 degrees apart, so there are
    15 degrees of hysteresis. New break reason: `ChainBreakReason.Facing`.
  - The facings compared are each troop's own facing (toward its threat, or the ordered facing), not the facing of the
    bent chain line.
  - Each link records whether a line order made it (`TroopEndpointLink.FromOrder`).
- Both values are on Chain Tuning.
- Code: `ChainLinkRules`, `ChainLinker`, `TroopEndpointLink`, `TroopLineLinks`, `ChainTuning`, `TroopTuning`. Tests:
  `ChainLinkRulesTests`, `ChainEngagementScenarioTests`.

### SU-48 No auto-link while attacking (Decision, amends spec §3.4 Linking, fixes issue 02)
- A troop with an attack (Pursue) order does not auto-link until the order ends: its target is destroyed, or it gets a new
  order. Links it already had are kept, so chained troops ordered to attack together stay chained.
- Code: `ChainLinker.CanAutoLink`. Tests: `ChainEngagementScenarioTests`.

### Tuning and test notes (no behaviour change)
- The three new values are not in the Battle Tuning assets yet. They use their code defaults (1.0, 45, 60) until Formation
  Tuning and Chain Tuning are next saved in the Inspector.
- `TroopScenarioTests.Retreat_RearRanksFallBackFirstAndFaster` (SU-14) fails at commit 4be238d, independently of this
  change. That merge kept a uniform `RetreatSoldierMoveSeconds` and `RetreatAccelerationHeadroom` 3 in `FaultSlipMotion` /
  `SoldierSteering`, so the rear ranks no longer move faster than the front.

---

## 2026-11-01 (6): Steady outline; charges survive a retreat

### SU-43 Charges are completed through a retreat, then the soldier rushes back (Decision + Assumption, amends spec §4.3, §5.5)
- Decision: when a troop retreats (break Case B, or a chain's sympathetic retreat), soldiers in the middle of a charge
  are no longer stranded. They **complete their charge** and then **rush back** to their troop.
- Spec §4.3 "the charge is cancelled if ... the charger's troop breaks" now reads: cancelled if the charger's troop
  **routs**. A retreat does not cancel charges or charge support; a rout still does (and no return follows a rout).
- Assumptions:
  - The accordion retreat (FaultSlipMotion) only moves the soldiers in formation. Chargers, followers and soldiers
    already returning are left out of its wave. Before, they were pinned to the spot they stood on, which stranded them.
  - Return: when a charge ends (contact, timeout, target lost), the soldier rushes back if its troop is retreating or no
    longer engaged, or it is outside 0.92 x the troop radius. Otherwise it rejoins the line as before, so ordinary melee
    is unchanged. Followers do the same when their charger's charge ends.
  - A returning soldier moves at charge speed (`ChargeSpeedScale`, 2x speed and acceleration) toward its place in the
    formation. It may be outside the troop radius without being clamped (the old clamp could snap it to the radius edge),
    and it does not start or follow a new charge. It counts as home within `LineSpacing` (0.9) of its formation band
    and inside 0.92 x the troop radius. The return gives up after 6 s.
  - Returning soldiers, like chargers, are left out of the footprint, so a soldier running back does not widen the
    troop's footprint (and so its engagement gap).
- Code: `SoldierChargeState`, `SoldierAgentState`, `SoldierSteering`, `SoldierMotionConstraints`, `SoldierFieldPlanner`,
  `ChargeCoordinator`, `FaultSlipMotion`, `Troop`, `TroopFootprintBuilder`. Tests: `TroopOutlineTests`.

### SU-44 The outline is drawn from the formation, not the soldiers (Decision + Assumption, replaces the SU-42 stabilising)
- Decision: the outline must be stable. It jittered in a marching column and pulsed thicker and thinner because it was
  built from the furthest soldiers. It is now drawn from the shape the soldiers are steered into (`TroopOutline`):
  - Line / Chain: line point to line point, half-width `RankHalfWidth(length, living soldiers)` plus one soldier radius.
    It changes only when the line moves, turns or stretches, or a casualty drops a rank.
  - Column: the marching lane (from 0.05 to 0.85 x troop radius behind the pivot, as wide as its ranks), along a smoothed
    march heading (0.35 s). The column stays for 0.3 s after the troop stops, so stop-start pathing does not flick it
    to the blob.
  - Idle: the blob disc (`BlobRadiusScale` x troop radius); Rout: the scatter disc. Both plus one soldier radius.
- Assumptions:
  - The outline is kept relative to the pivot, so it moves exactly with a marching troop. Changes of size, facing and
    shape (a line forming into a column, say) ease in over 0.2 s.
  - The outline can now differ from where soldiers actually stand (a soldier shoved out of the line, or a charger, is
    outside it). Picking still finds those soldiers by their own position (SU-42).
  - The melee width hold and width growth limit of SU-42 are removed; they are not needed.
- Code: `Rules/TroopOutline.cs` (was `TroopOutlineStabiliser.cs`), `Troop.Outline`. Tests: `TroopOutlineTests`.

---

## 2026-11-01 (5): Troop visualiser shows the formation outline

### SU-42 Troop visualiser draws a steadied formation outline; click and hover use it (Decision + Assumption, spec §3.3 / §6.1 UI)
- Decision: the flock visualiser is now the **troop visualiser**: the prefab is `Troop Visualiser` (and its children
  `Troop Pivot`, `Troop Radius`); the class was already `TroopVisualiser`.
- Decision: instead of the radius disc, the visualiser draws the troop's **formation outline** (its footprint shape: the
  thick line of a Line / Chain troop, the capsule of a Column, the circle of a Blob / Rout) as a faint fill and an army
  colour stroke. Like the old disc it is invisible until the troop is hovered or selected (fill 15/255 hovered, 40/255
  selected; stroke 110/255 hovered, 230/255 selected; 2 px, 3 px selected). The radius disc is kept behind a
  `Show Radius` tick on the visualiser (off).
- Decision: the outline must not flare in melee. The **footprint is unchanged** (it still drives engagement, faces and
  surround); the outline is a separate, steadied copy (`Troop.Outline`, `TroopOutlineStabiliser`).
- Decision: a troop is selected (and hovered) by pointing at one of its living soldiers, or inside its outline.
- Assumptions:
  - Stabilising: the outline's length, width and rounding ease toward the footprint (0.15 s); its centre and facing
    follow it closely (0.04 s) so a marching troop's outline does not trail its soldiers. A Line / Chain outline widens
    at most 1.5 units per second. While the troop is engaged, assaulting or in melee, a Line / Chain outline is held to
    the width it had when the fight began; it can still narrow with casualties. Column and Blob outlines are only eased.
  - Picking order: a living soldier within `ClickPickRadiusPixels` beats the outline of a troop standing over it (nearest
    soldier wins); then the outline the point is deepest inside; then a pivot within the pick radius. The same test picks
    the hovered troop (any team) and the enemy under an order's end point.
  - Hovering no longer triggers anywhere inside the troop's radius circle, only on a soldier or inside the outline.
- Code: `Rules/TroopOutlineStabiliser.cs`, `Rules/TroopOutlinePolygon.cs`, `View/TroopVisualiser.cs`, `Troop.Outline`,
  `TroopEngagementSystem`, `TroopSelection`, `TroopPlayerOrders`. Tests: `TroopOutlineTests`.

---

## 2026-11-01 (4): Heat map is a smooth gradient

### SU-41 Soldier count no longer affects the heat map (Decision, amends SU-40)
- Decision: the number of soldiers fighting does not change the heat. The heat map is a smooth gradient from the two
  troop lines alone: 4v(1 - v) across the gap, times a smoothstep fade over `End Fade` (2 units) at each end of the
  engagement. `Density Weight` is removed.
- To keep the gradient smooth, the mesh has a column every 0.25 units along the front and 16 rows across the gap.
- Flank and rear attacks still use the engaged soldiers only to place the band (now a straight band at their mean
  positions); the heat inside it follows the same gradient.

---

## 2026-11-01 (3): Engagement heat map from the troop lines

### SU-40 Engagement heat map replaces the lines (Decision + Assumption, amends SU-38)
- Decision: the engagement view is the heat map only; the red-lines mode is removed. **F5** toggles it on and off
  (on by default).
- Decision: the heat map is built from the two troops' lines (their footprint centre lines), across the gap between
  them, with less heat the nearer a point is to either troop: most intense in the middle of the line gap, least intense
  at the ends of the engagement and at each troop's line.
- Assumptions:
  - The band runs where the two lines overlap along the front, from the first troop's line to the second's.
  - Across the gap the heat is 4v(1 - v) (v = 0 on one line, 1 on the other): 0 on each line, 1 midway.
  - Along the front it fades to 0 over 2 world units at each end of the overlap (smoothstep; `End Fade`).
  - It is still scaled by how many soldiers fight at each point (the SU-38 soldier links, per 0.9-unit cell, smoothed),
    so a stretch where nobody fights shows no heat. `Density Weight` (default 1) sets how much; 0 uses the lines only.
  - Colour runs from light to dark red with the heat, and opacity from 0 to 0.75 (no minimum opacity any more).
  - Lines more than 45 degrees apart (a flank or rear attack), or that do not overlap, have no shared gap; there the
    band falls back to the engaged soldiers: from one side's engaged soldiers to the other's, along their extent.
- Code: `Rules/EngagementHeatStrip.cs`, `View/TroopEngagementView.cs`, `TroopPlayerOrders` (settings). Tests:
  `BattleViewTests`.

---

## 2026-11-01 (2): Hover order paths, engagement view, Battle Tuning split

### SU-37 Hovered troop shows its order path (Decision + Assumption, spec §6 UI)
- Decision: hovering a troop that has a Move or Attack order draws arrows along the path it is taking to the order's
  waypoint (the navmesh waypoints still ahead, from the pivot).
- Assumptions: shown for any team (like the hover troop card and range mask). Line orders count while the troop marches
  to the line, and the ordered line's footprint is drawn at the end. Move/Line paths are off-white with a dot or line
  ghost at the end; Attack paths are red and end at the pursued troop's pivot, with its footprint outlined. Chevrons
  march along the path toward the waypoint, and an arrowhead marks the end. A troop holding a line, idle, retreating or
  routed shows nothing. Drawn over the battle (IMGUI), under the troop cards. Colours are on `TroopPlayerOrders`.
- Code: `View/TroopOrderPathView.cs`, `Troop.CurrentOrder` / `CollectOrderPath` / `TryGetLineOrder`,
  `TroopPathFollower.CollectRemainingPath`, `Runtime/TroopOrderKind.cs`. Tests: `BattleViewTests`.

### SU-38 Engagement view: engaged frontage as lines or a heat map (Decision + Assumption, spec §4.1 / §6 UI)
- Decision: engaged troops show how much of their frontage is engaged, in one of two modes with a toggle:
  (A) thin red lines between the engaged soldiers; (B) a red heat map in the gap between the engaged lines, darker and
  more opaque where more soldiers are engaged.
- Decision: shown for every engaged pair on the field. **F5** cycles Lines -> Heat map -> Off; the starting mode is set
  on `TroopPlayerOrders` (default Lines).
- Assumptions: a soldier is engaged when a living soldier of the opposing engaged troop is within
  EngagementGap + 2 x SoldierRadius + 0.45 (3.05 units at the defaults): the front rank counts, the rank behind it
  (LineSpacing 0.9 back) does not. Each engaged soldier is linked to its nearest such enemy; a pair that picks each
  other is drawn as one line. Lines are 1.5 px wide. The heat map bins the link midpoints into 0.9-unit cells along the
  contact (one soldier per side per cell is fully engaged), smooths each cell with its neighbours (1/4, 1/2, 1/4), and
  spans the gap from front rank to front rank, fading to 35% opacity at the two edges. Opacity runs 0.15-0.75.
  Routed troops are not shown. Drawn on the Terrain sorting layer (order 13): above the range mask, below the soldiers.
- Code: `Combat/EngagementFrontage.cs`, `Rules/EngagementHeatStrip.cs`, `View/TroopEngagementView.cs`,
  `Resources/Shaders/TroopOverlayColor.shader`, `TroopPlayerOrders` (F5, styles). Tests: `BattleViewTests`.

### SU-39 Tuning split into Battle Tuning assets; profiles folder renamed (Decision + Assumption, spec §7.2)
- Decision: `Assets/Resources/Boid Profiles` is renamed `Troop and Character Profiles` (GUIDs kept, so scenes and
  profiles still link).
- Decision: Troop Tuning is split into ScriptableObjects by section, in the new folder `Assets/Resources/Battle Tuning`.
- Assumption (grouping, chosen by the designer from two options): Soldier (stats and actions, soldier steering),
  Formation (distances, formation and movement), Morale (morale and retreat, faces), Melee (charges and clashes,
  support and duel, Assault), Chain (chains, link spring), Ranged (ranged, composite troops), Input.
- The old `Troop Tuning.asset` is removed; its values were copied into the new assets unchanged (including the
  test-scene A_Distance 2.0 and S_Distance 5.0 noted under SU-31). Values it did not hold yet (soldier steering,
  composite ranks) take their code defaults, as before. No behaviour change.
- Code reads every value through `TroopTuning.Active` as before (same property names); `TroopTuning` is now a plain
  class that loads the seven sections. Tests use `TroopTuning.CreateDefaults()`.

### Test note (no behaviour change)
- The composite-troop scenario tests depend on the random stream each troop takes from its place in the hierarchy, and
  soldier sprites outlive their troop in Edit mode (Troop.OnDestroy does not run there). `BattleViewTests` removes its
  soldier sprites in TearDown so it does not shift the streams of later fixtures. Other fixtures still leave them.

---

## 2026-11-01: Hit squash and stretch

### SU-36 Squash and stretch when a soldier is hit (Decision + Assumption, spec §4.5 / §6 UI)
- Decision: a soldier that takes melee damage and is knocked back squashes in, then its back stretches outward (wider
  and longer, the front narrowing), then returns to normal. Done in the vertex stage of `Unlit/ModelShader`.
- Assumptions: it plays for 0.4 s along the push direction (from the duel winner toward the loser): squash over the first
  20%, spring to full stretch by 50%, settle by 100%. Only the soldier that takes damage plays it (not the winner, and not
  a clash that deals 0 damage). Ranged hits do not play it, since they have no knock-back.
- Shape amounts are material properties on ModelMat (Squash 0.4, Stretch 0.35, Taper 0.8); per-soldier `_HitDir` and
  `_Deform` are set through the property block. Diagram: `Docs/Battle/Visuals/hit-squash-stretch.svg`.
- Code: `ModelShader.shader` (SquashStretch), `TroopSoldierViews.PlayHitSquash` / `HitDeform`, `DuelResolver`.
  Tests: `SoldierViewTests` (curve timing, both soldier shaders compile).

---

## 2026-10-31 (6): Grey injured glow, corpses

### SU-34 Fatigued glow is grey (Decision, replaces SU-32's injured dimming)
- Injured soldiers that are not fatigued keep the full glow. (ModelMat's glow is white, about 1.2 HDR, so dimming it
  to a third already read as grey and could not be told apart from fatigued.)
- Fatigued soldiers (HP <= FatigueFraction x MaxHP, 50% by default) glow grey at a third of the healthy emission's
  luminosity (Rec. 709 luminance of the material colour, divided by 3). Clash flashes play on top of either.

### SU-35 Corpses (Decision + Assumption, spec §6 UI)
- Decision: a dead soldier's sprite stays where it fell for 10 s with no emission (emission mask colour black), drawn
  behind the living, with its army colour at half saturation, and its opacity fading from 1 to 0.6 over the 10 s.
- Assumptions: "the background" means behind living soldiers on the Unit sorting layer (sorting order -1; living
  soldiers are 1). The corpse is hidden at the end of the 10 s (at 0.6 opacity, no final fade).
- Decision (amended): corpses have no collider. On death the soldier's collider is disabled and its kinematic body
  leaves the physics simulation.
- The opaque model shader cannot fade, so corpses switch to `Unlit/ModelCorpseShader` (Resources/Battle prefabs/Model
  prefabs), an alpha-blended copy that draws the emission mask in black.
- Tests: `Editor/Tests/SoldierViewTests.cs` (grey injured glow; corpse timing, sorting, emission and fade).

---

## 2026-10-31 (5): Injured soldiers dimmed, no facing arrow in order previews

### SU-32 Injured soldiers glow at a third (Decision + Assumption, spec §6 UI)
- Decision: an injured soldier's emission (the glowing mark, material colour `_Color` on ModelMat, or `_Light` where a
  material has it) is dimmed to 1/3 luminosity. Clash flashes still play on top, from the dimmed level.
- Assumption: "injured" means current HP below MaxHP (any Bruise or Bleed). A soldier whose bruises fully heal glows
  normally again. Alternative if preferred: only fatigued soldiers (HP <= 50%).
- Code: `TroopSoldierViews.UpdateEmission`, `ClashPulseView.Apply(..., brightness)`.

### SU-33 No facing arrow in order previews (Decision, spec §6.2)
- The ghost preview while giving an order no longer draws a facing arrow, because troops set their own facing (toward
  the threat, or from the chain line). Ghost footprints (front edge still drawn brighter), joints, markers and tethers
  are unchanged.

### Diagrams
- `Docs/Battle/MovementEdgeCases/` 01-13 (movement) and 14-23 (engagements with chains) show the current rules.
  Open questions flagged there: line fill inside terrain (12), engagement exit hysteresis (13), whether an engaged chain
  end may refuse its flank (18), and flankers snapping into chains, issue 02 (23).

---

## 2026-10-31 (4): Soldier hover HP bar

### SU-31 HP bar over the hovered soldier (Decision, spec §4.6 / §6 UI)
- In Play mode, a small bar appears just above the soldier under the cursor (any team, living soldiers only; picked
  within 1.25 soldier radii). The whole bar is MaxHP: green = current HP, red = Bruise, black = Bleed, with a thin white
  border. It is at least 30 px wide and widens with zoom. Hidden while the cursor is over a troop card.
- Code: `View/SoldierHealthBar.cs`, driven by `TroopPlayerOrders`.

### Test note (no behaviour change)
- The scenario tests now run on a fresh default `TroopTuning` instead of the project asset, whose test-scene values
  (A_Distance 2.0, S_Distance 5.0) made 4 tests fail. Open question for the designer: the asset's A_Distance equals
  EngagementGap (2.0), so engaged lines are held right at the engagement edge and charge range is only 3.

---

## 2026-10-31 (3): Troop card redesign, soldier HP inspector

### SU-25 Troop card main block (Decision, amends SU-19 and SU-23)
- Spec §6.1.
- Title: the characters' names joined with ` + `, then the soldier count in brackets as living / starting, e.g.
  `Gareth + Lyra (14/24)`. The separate "Models" cell is removed.
- Portraits of every character in the troop side by side (the main block widens when more than two would be narrow).
- Morale is a blue bar (Effective Morale / initial) with a red Shock bar just below.
- Formation state stays, on its own row.

### SU-26 Morale and Shock bars share one scale (Assumption)
- Spec §5.3.
- Both bars use the initial Effective Morale as full width, so the troop breaks when the red bar passes the end of the
  blue one. A white tick on the Shock bar marks the current Effective Morale (the break point).

### SU-27 Character side panels on demand (Decision, amends SU-19)
- Spec §6.1.
- Side panels are hidden by default. Clicking a character's portrait shows or hides that character's panel; an open
  panel's portrait has an army-colour outline. The choice is kept per troop and character for the session.
- Stats are one column: HP, Defence (+armour bonus), **Armour** (Deflect / Bruise / Bleed, spec §1.6), Support, Charge,
  Aggression. Speed and Strength are removed.
- Each equipped weapon shows its skill in brackets and its damage, e.g. `Gladius (6)   11 dmg`.

### SU-28 Weapon damage shown (Assumption)
- Spec §4.6, Q-5.
- Melee weapons show Strength + weapon damage (the raw damage before Domination). Ranged weapons show the weapon's damage
  only, because ranged damage does not add Strength (Q-5).

### SU-29 Hover card lingers (Assumption, amends SU-20)
- The hover card stays for 1 s after the cursor leaves the troop, and for as long as the cursor is over the card, so
  the cursor can reach it and open an enemy character's panel (enemy troops cannot be selected).

### SU-30 Soldier HP in the Inspector (Decision, tooling)
- Each soldier's sprite object (`Soldier <id>`) carries a `SoldierView` component. Selecting it in Play mode shows the
  soldier's troop, character, life state, an HP bar, current / max HP, Bleed, Bruise, fatigue and charge role.
- The Troop Roster component's Inspector lists every soldier of the troop with an HP bar (foldout "Soldier HP").

---

## 2026-10-31 (2): Troop card fixes, S_Distance exit margin

### SU-23 Troop card text and layout (Decision, amends SU-19 and SU-20)
- Spec §6.1.
- The "Selected" / "Hover Ally / Enemy" tags are removed.
- The troop's name is every character's name joined with ` + ` (e.g. `Gareth + Lyra`). A long name wraps onto more
  lines and the card grows taller.
- All card text is 1.5× larger; panel widths and line heights grow to fit (main block 320, side panels 300 at 1080 p).
- Each equipped weapon is shown with the character's skill in its class next to it, e.g. `Gladius (6)`; 0 means
  unskilled. The weapon class is no longer shown.

### SU-24 S_Distance exit margin from footprint shape (Assumption, amends D-37 and Q-16)
- Spec §3.1, §3.3.
- Bug: a troop near the S_Distance edge flickered between Idle / Column and Line. Forming a line turns its footprint
  from a circle into a thin rectangle, which widens the footprint gap past S_Distance; the troop drops the line, the
  circle comes back, and the gap closes again.
- Fix: a troop that already faces a threat keeps it until the gap exceeds S_Distance plus an exit margin of at least
  `BlobRadiusScale × (r_A + r_B)` (8.5 units for two radius-5 troops). `SDistanceExitHysteresis` still applies when it
  is larger. Entry is unchanged (footprint gap ≤ S_Distance).

---

## 2026-10-31: Troop cards replace the help panel, status panel and unit card

### SU-18 Help panel and status panel removed (Decision, supersedes SU-03 and SU-06)
- Spec §6 UI.
- The top-centre controls help and the right-hand troop status panel (F3, F5) are gone. `TroopDebugOverlay` now only
  draws gizmos (F4). The controls are still listed in `Troop/README.md`.
- With no help panel, nothing at the top of the screen blocks clicks any more. The old fixed top-left no-click area
  (500 × 140 px, kept for the objectives text) is also removed (Assumption: the objectives text is not shown in the
  troop scenes). Only the troop cards block clicks.

### SU-19 Troop card layout (Decision, supersedes SU-04, SU-05 and SU-17)
- Spec §6.1.
- **Main block**, top to bottom: the first character's name; the first character's portrait;
  `Models | Effective Morale / Initial Effective Morale | Formation state`.
- **Side panels**, one per character in the troop: character name, class, living / starting soldiers of that
  character, soldier stats (Speed, HP, Defence (+armour bonus), Strength, Support, Charge, Aggression), each weapon with
  its skill, and armour.
- Drawn by `TroopCardHud` (IMGUI) from `TroopPlayerOrders`. `TroopSelectionCard` is removed.

### SU-20 Selected card bottom left, hover card bottom right (Decision + Assumption)
- Spec §6.1.
- Decision: the selected troop's card is in the bottom-left corner, and a second card shows the troop under the cursor.
- Assumptions: the hover card sits in the bottom-right corner with its side panels extending to the left; it shows
  troops of any team, tagged Ally or Enemy; it is hidden while the cursor is over the selected troop (already shown);
  it stays on its troop while the cursor is over the card itself, so it does not flicker when a troop is under it.
  With several troops selected the selected card shows the first one and is tagged `+N`.

### SU-21 Troop card details (Assumption)
- "Models" is the troop's living soldier count. Living / starting counts per character are on the side panels.
- Morale turns red below 50% of its initial value, a side panel's count turns red below half its starting soldiers,
  and the Formation cell turns red when the troop routs.
- A character with no portrait shows its soldier sprite in the army colour.
- Sizes are set for a 1080 px tall screen and scale with screen height (0.8× to 2.5×).

### SU-22 Legacy unit card hidden (Assumption)
- The scene's uGUI unit card (`BattleUI.UnitCard`, and the Canvas's `SelectedUnit` object) sat where the selected card
  is now drawn. It is hidden at runtime. It can be deleted from the Canvas prefab in the Editor.

---

## 2026-10-29: Character and Troop pre-battle data

### SU-14 SoldierProfile is removed; Character holds the soldier data (Decision)
- Spec §1.1, §1.2, §1.4, §1.5, §1.6.
- New ScriptableObject **`CharacterProfile`** (menu Troops > Character): name, class name, equipment (up to 2 weapons +
  armour), portrait sprite, soldier sprite, Numbers, Morale, Discipline, and soldier stats (Speed, Defence, Strength,
  Support, Charge, Aggression, MaxHP, weapon skills).
- **`TroopProfile`** is now a list of characters plus `TroopRadius`. Move, BaseMorale, Discipline and the soldier count
  are resolved from the characters with the §1.2 composite rules (one character resolves to its own values).
- `SoldierProfile` and `TroopProfileCount` are removed. Each spawned soldier points at its character
  (`SoldierAgentState.Character`) for its stats and equipment. `Troop.Configure(team, composition)` is removed.
- The Phase 01 generator writes `Boid Profiles/Characters/<Name>` (character) and `Characters/Troops/<Name>` (troop of
  that one character). The old soldier assets were moved to `Characters/` and keep their GUIDs and soldier sprites.

### SU-15 Soldier acceleration follows Speed (Decision)
- Spec §1.5, §2.1.
- Acceleration = Speed × **`TroopTuning.AccelerationPerSpeedPoint`**, default **1.6** units/s² per point, so Speed 5
  keeps the old fixed 8.

### SU-16 Alignment, separation and Brownian motion are shared TroopTuning values (Decision)
- Spec §2.1, §2.2.
- Moved from each soldier type to `TroopTuning` (Soldier steering): `SoldierAlignment` 0.5, `SoldierSeparation` 1.8,
  `SoldierBrownianMotion` 0.04 (the values every asset used). Cross-troop separation scales by
  `SoldierSeparation / 1.8`, which is 1 at the default.

### SU-17 Selected-troop card shows the first character (Decision)
- Spec §6.1.
- Name: the first character's name. Portrait: the first character's portrait, or its soldier sprite in the army colour if
  it has none. Stats are laid out like the legacy unit stat block (tab-indented `Label: value` pairs): troop line
  (Move, Soldiers, Morale, Discipline), then one block per character with soldier stats, Morale, Discipline, weapons with
  skill, and armour.

---

## 2026-10-27 (2): Engagement gap, retreat distance, retreat wave

### SU-07 EngagementGap replaces LineGap (Decision)
- Spec §3.3 "Line-vs-line pressure", §4.1 "Engagement", §7.2 `LineGap`.
- `LineGap` (constant 0.3) is renamed **`EngagementGap`** and moved to `TroopTuning` (Distances), default **2.0** units.
- Two opposing troops are still **engaged** when their footprint gap is within `A_Distance` (5.2). Once engaged, the
  line barrier holds them apart at a footprint gap of `EngagementGap` instead of 0.3.
- Code: `TroopTuning.EngagementGap`; `TroopSegmentPressureRules` reads it; `TroopConstants.LineGap` is removed.

### SU-08 A_Distance and EngagementGap are both footprint gaps (Decision, confirms Q-16)
- Spec §3.3, §4.1, §7.2.
- `A_Distance`, `S_Distance`, `EngagementGap` and the pressure contact ranges all use the footprint gap
  (`FootprintGap.Gap`, the gap between the two troops' footprint rectangles or circles), never pivot or centre-line
  distance. This was already the case for `A_Distance` and `S_Distance`.

### SU-09 Pressure contact starts before EngagementGap (Assumption)
- Spec §3.3 "Line-vs-line pressure".
- The pressure contact range is `max(0.5 × A_Distance, EngagementGap)`, then × 1.25 to start it, × 1.6 to release it.
  With the defaults this is unchanged (2.6 / 3.25 / 5.2). The `max` only matters if `EngagementGap` is tuned above 2.6,
  so the barrier always has a contact to act on.

### SU-10 Engaged lines no longer touch: melee comes from charges (Assumption, consequence of SU-07)
- Spec §4.3-§4.5.
- At a 2.0 gap the front soldiers of two engaged lines are about 2.6 units apart, beyond `ContactDistance` (0.85).
  Soldiers only meet when they charge (charge range 7.8 soldier-to-soldier, so the gap is always in reach). Passive
  contact clashes between standing lines no longer happen. Tune `EngagementGap` down if lines should grind together.

### SU-11 Retreat distance is a fixed TroopTuning value in units (Decision, Q-34)
- Spec §5.5 step 2, §4.9 Surrounded, §7.2 `RetreatDistance`.
- Replaces `RetreatDistance = 1.0 × TroopRadius` (`RetreatDistanceScale`) with **`TroopTuning.RetreatDistance`**,
  default **5.0** units (equal to the old value for the radius-5 troops in use), the same for every troop.
- Used for the retreat itself, the shared-retreat chain (falloff still applies per hop) and the Surrounded path length.
- Any value set on the old `RetreatDistanceScale` in `Troop Tuning.asset` is no longer read.

### SU-12 Retreat is a rear-to-front wave (Decision, spec §2.3, §5.5 step 1)
- When a troop retreats, the soldiers furthest from the attacker fall back first; the others follow with a delay that
  grows toward the front, so the troop stretches and closes up again (accordion).
- New tuning: `RetreatWaveDelaySeconds` (default **0.6 s**: delay between the rearmost and the frontmost soldier
  starting) and `RetreatSoldierMoveSeconds` (default **1.2 s**: time each soldier takes to reach its new place).
- Delay is proportional to how far forward the soldier stands along the attack axis.

### SU-13 Retreat motion has no overshoot and return (Assumption, replaces D-31 timing)
- Spec §5.5; Decisions D-31; Handoff issue-03 (now `Docs/Archive/Handoff/`).
- Before: the pivot jumped back and every soldier headed for the new line at once; the per-soldier delay only staggered
  an extra fall-back past the new line (to about 2× the distance) followed by a return. That hid the wave and caused
  the overshoot in issue-03.
- Now: each soldier holds the ground it stood on until its start time, then eases straight to its place in the new
  line. No hold, no return.
- Retreat (Fleeing) length is now `RetreatWaveDelaySeconds + RetreatSoldierMoveSeconds + 0.5 s settle` = **2.3 s** with
  the defaults (was 3.14 s). The troop cannot break again until it ends (D-30 unchanged).

---

## 2026-10-27 (1): Line fill and battle UI

### SU-01 Battle lines fill their length (Decision, spec §2.1, §3.3)
- In Line and Chain, soldiers are spread evenly along the line instead of bunching at its middle.
- Soldiers still have no slots: each frame the living soldiers are sorted by position along the line core and grouped
  into files of `Ranks` soldiers (`Ranks = ceil(soldiers / floor(L / LineSpacing))`, spec §3.3); each file is pulled
  toward an evenly spaced point along the core. Depth within a file comes from separation and the half-width band.

### SU-02 Line fill details (Assumption)
- Pull strength is `LineFillGain = 0.8` × the field attraction strength (constant in `SoldierSteering`).
- Chargers, followers and retreating soldiers keep their place in the sort (the line does not reshuffle while they are
  away) but are not pulled. Columns, blobs and routs are unchanged.
- Linked ends keep the joint clearance: the fill spans the core after clearance.

### SU-03 Help panel at top centre, 3× size (Decision, spec §6 UI)
- The controls help moves from bottom left to top centre, with 3× text (36 px) and up to 3× width (1440 px). It is
  capped so it never covers the right-hand status panel, and wraps.

### SU-04 Unit card shows the selected troop (Decision, spec §6 UI)
- The bottom-left unit card shows the first selected troop: name, portrait and stats (soldiers alive/total, Move,
  BaseMorale, Discipline; per soldier type: Speed, Defence (+armour), Strength, Support, Charge, Aggression, HP,
  weapons with skill, armour). Hidden while nothing is selected.

### SU-05 Unit card fallbacks (Assumption)
- Name: the troop's legacy stat block name, otherwise its GameObject name.
- Portrait: the legacy stat block portrait, otherwise the sprite of the most numerous soldier type, in army colour.

### SU-06 Status panel (Decision, spec §6 UI)
- The right-hand panel (F3) shows per selected troop: formation (with line length, links, ordered or threat-facing),
  status (Idle, Moving, Pursuing, Holding line, Engaged, Retreating, Routed), Effective Morale vs initial, Shock with
  its break level and decay rate, velocity, soldiers, and nearby or engaged enemies. F5 adds the debug values.
