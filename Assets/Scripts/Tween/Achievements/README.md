# Achievements

Achievements reward what happens in a battle (SU-114). They do two jobs:

- **Global achievements** (`GlobalAchievement`) track the battle as a whole: victory, side objectives such as holding a
  capture point. They usually give gold and prestige; XP goes to every roster character.
- **Character achievements** (`CharacterAchievement`) are each class's and character's mini quests, and replace XP for
  kills. They are measured on the troop the deployed character leads; their XP goes to that character.

## Where achievements come from

`AchievementController` (one per battle scene) sets them up a frame after the scene starts:

| Source | Kind | Applies to |
|---|---|---|
| `Resources/Battle Tuning/Achievement Tuning`: Every Battle (Mission Complete) | global | every battle |
| Controller: Global Achievements / Custom Global Achievements | global | the battle |
| `Resources/Battle Tuning/Achievement Tuning`: Every Character | character | every deployed character |
| `Resources/Achievement/Class Achievements`: Classes | character | deployed characters of that class |
| `CampaignCharacter.Achievements` | character | that character |
| Controller: Achievements PC / Custom Pc Achievements | character | every deployed character, this battle only |

Use the "Custom" lists for requirements that point at scene objects (a capture point), since an asset cannot.

## Lifecycle

1. The controller instantiates each achievement asset (the asset itself is never changed) and calls `Begin`. A
   requirement that does not apply (a character requirement without a troop, a capture point that is not set) is dropped.
2. `Tick` runs every frame with battle time (`Time.deltaTime`, so pauses don't count and battle speed applies).
3. On `Battle.BattleOver` every achievement is `Finish`ed: its level is fixed and it unsubscribes from the battle events.
   If the scene closes first, `Abort` unsubscribes.
4. `BattleReport.Achievements` carries them to the post-battle book, which grants every reward once when it opens
   (`PostBattleResults`, `Achievement.GrantRewards`) and then animates the results (SU-115). XP: 10 per level.

The **level** is 0 when not earned, 1 or more when earned. Rewards are multiplied by it (repeatable achievements).

## Requirements

Character requirements derive from `CharacterRequirement` (counting ones from `CountedRequirement`: Required, Repeatable,
Max Level). Global ones derive from `GlobalRequirement`.

| Requirement | Earned when |
|---|---|
| `CharacterSurvived` | the troop was not routed or wiped out when the battle ended |
| `KillTracker` | the troop dealt the killing blow to Required enemy soldiers |
| `RoutEnemy` | an enemy troop routs and the troop is credited (optionally: wiped out) |
| `DefeatWhileOutnumbered` | an enemy troop that outnumbered the troop (> Ratio x) in melee is defeated, credited |
| `BreakOnInitialAssault` | an enemy retreats/routs within Window s of the first contact of an assault |
| `HoldMelee` | Seconds of one melee without retreating; gaps under Grace s don't end it |
| `RangedDisorder` | an enemy troop is disordered by the troop's projectile |
| `CapturePoints` | level = capture points held by the army |
| `Victory`, `ArmyDestroyed`, `CapturedVictoryPoint`, `Default` | global: won / an army is out / point held / fixed level |

**Credit**: the troop dealt the enemy's last damaging hit (`TroopBreakInfo.Cause`, `Troop.LastAttacker`) or is in melee
with it at that moment.

They are built on `BattleEvents` (`SoldierKilled`, `TroopDefeated`, `TroopBroke`) and on the troop's public state
(`Engagements`, `InMelee`, `IsAssaulting`). The battle never references this folder.

## Adding things

- **A requirement**: derive from `CharacterRequirement`, `CountedRequirement` or `GlobalRequirement`, mark it
  `[Serializable]`, subscribe in `OnBegin`, unsubscribe in `OnEnd`, keep state in `[NonSerialized]` fields and return the
  level from `Level`. It appears in the requirement dropdown.
- **A reward**: derive from `Reward`, implement `Grant(in RewardContext)` and `Describe(level)`. `RewardContext` gives
  the achievement, the character (null for global) and the level. A story flag reward would set the flag in the
  campaign data here.
- Keep class names of requirements and rewards: `[SerializeReference]` data is stored by class name. If one must be
  renamed, add `[UnityEngine.Scripting.APIUpdating.MovedFrom]`.
