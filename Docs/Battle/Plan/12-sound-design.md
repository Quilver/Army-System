# Plan 12: Battle Sound Design (proposed)

**Goal:** the battle sounds like a battle and, above all, *tells the player what is happening*: where the fighting
is, whether their armour is holding, when a troop is about to break, when it breaks, and when the enemy does. Weight
and atmosphere come second, but the same system carries both.

**Status:** proposed 2026-11-08, nothing built yet. The open questions at the end need the designer's answers before
Phase B.

## Decisions (designer, 2026-11-08)

- **Unity's built-in audio** (AudioSource + AudioMixer). No FMOD or Wwise. What the middleware would give us (voice
  limits, layered loops, adaptive music) is built as a small layer in `Scripts/Battle/Audio/`.
- **Sources:** AI-generated sound effects (as the current grunts and screams are) and bought or free libraries. The
  asset brief below is written so that a sound designer or composer can be hired against it later.

## Where things stand

| What | State |
|---|---|
| `ModelSoundEffects` (Model SFX prefab, in M01, M02 and Level 1 Troop Test) | Listens to 4 `BattleEvents`: strike on a damaging melee hit, bow sound per `VolleyFired`, death grunt per `SoldierKilled`, projectile hit (no clips or source assigned, so silent). SU-64. |
| How it plays | One AudioSource per type and `Play()` with a new clip, so each new sound **cuts off the last**: at most one strike, one shot and one grunt are ever audible. 0.08 s limit per type in scaled time, random pitch 0.8–1.2, all 2D and unpositioned. |
| Silent moments | Fully deflected clashes (common against heavy armour such as Jagen's), charges, engagements starting, breaks, routs, orders, selection, capture points, battle start and end. `SoldierStruck` is only raised when damage gets through. |
| `Master.mixer` | Master > Music; Effects > Model, Unit, UI. One snapshot, no exposed parameters, no effects. **The Master group's Mute is on in the asset** (`m_Mute: 1`). That mutes everything routed through the mixer in Play mode and may be why the Movie_017 recording had no audio, though the Recorder captures audio. Check it in the Audio Mixer window. |
| Ambience and music | Battle scenes loop `Temporary Assets/forest.ogg` on the **Music** group. The menus loop "Heroic Demise" (`Menu Music.prefab`). No battle music. |
| Unused clips | `Unit/Selected`, `Unit/Ordered Move` (orders, "yes"), `Unit/Flee+Shatter` (screams), `Model/Marching` (infantry and cavalry), `Arrow Whistle`, `Spell_04`, UI `Jingle_Win` and `Jingle_Lose`. The unit and model ones lost their hooks with the legacy code (SU-57). |
| Pause and speed | Pausing stops new sounds (the limit runs on `Time.time`) but playing sounds and music carry on. At 2× the 0.08 s limit is 0.04 s of real time. |
| Project audio settings | 32 real voices, 512 virtual (defaults). |
| Names | Mixer groups and the prefab still use the old glossary (Model, Unit). |

## Principles

1. **Readability first.** Every change the player should react to has a sound, and the sound for "good for me" is
   different from "bad for me". The order of work follows this: morale and engagement cues come before polish.
2. **Three scales.** *Soldier* sounds are one-shots (swings, impacts, deaths) and dominate when zoomed in. *Troop*
   sounds are loops and shouts (marching, a melee bed per engagement, war cries, breaks). *Battle* sounds are the
   ambience, the army murmur and the music. Past a handful of simultaneous soldier events, more one-shots only make
   mush: the troop-scale bed gets louder instead.
3. **Player side vs enemy side.** Spoken barks and UI confirmations come from the player's troops only. Breaks and
   routs sound different depending on whose troop it is.
4. **Heard from the camera.** The camera is orthographic and top-down, so sounds are placed by screen position (pan)
   and distance from the view, and the zoom level shifts the mix between soldier and troop scale.
5. **The simulation never references audio** (as now). Audio listens to `BattleEvents`, `Troop`, `Battle`,
   `CapturePoint` and `GamePause` events. New events are added only where a cue has no hook, and they stay in
   `Battle/` (the dependency rule).
6. **Every number is a tunable variable** on a Battle Audio Tuning asset, and every sound is a data asset (a *cue*),
   so clips can be swapped, varied and balanced without code.

## Sound map

Hook: **existing** event, or **new** (to add). Phase: when it is built (Steps).

### Melee

| Moment | Hook | Sound | Notes | Phase |
|---|---|---|---|---|
| Engagement starts (two troops meet) | new `BattleEvents.Engaged(a, b)` / `Disengaged(a, b)` from `TroopEngagementSystem` (`Troop.EnteredMelee` is per troop, not per pair) | Line crash: shields and bodies meeting. Heavier for charging cavalry and Couched Lance. | One per troop pair. Starts that engagement's melee bed. | B |
| Assault starts (a troop with an order engages, §4.2) | new `BattleEvents.AssaultStarted(troop)` | War cry from the troop. | The 10 s Assault window is exactly when the charge surge happens. | B |
| Soldier charges | new `BattleEvents.ChargeStarted(troop, soldier)` | Short effort shout and running feet. | Few audible at once (cue limit); the rest are absorbed by the bed. | C |
| Clash resolved | new `BattleEvents.Clashed(in ClashInfo)` (see below) | Swing by weapon class (Sword, Axe, Spear, Mace), then impact by result: **deflect** = clang on mail or thock on a shield; **bruise** = blunt thud; **bleed** = cut. | Replaces `SoldierStruck` as the sound hook. Deflects get a sound, so the player hears armour working. | B (impacts), C (materials) |
| Flank or rear hit (×3 Shock, §4.8) | `ClashInfo.Flanked` | Player troop: a "Flanked!" bark (once per engagement per face). Impact layer a little harsher. | Pure readability: this is the hit that breaks troops. | B |
| Soldier stunned (SU-107) | `ClashInfo` result | Groan, lighter than a death. | | B |
| Soldier killed | existing `BattleEvents.SoldierKilled` | Death vocal plus a body-and-kit fall. | Limits per troop and overall. | A |
| Melee bed (per engaged pair) | `Engaged` / `Disengaged` + clash rate | Looping crowd melee (clatter, shouting), placed between the two troops. | Volume follows clashes per second over the last ~1 s and the soldiers involved; fades out on `Disengaged`. | C |

`ClashInfo` (new, filled in `DuelResolver`): world position; attacker and defender troops; attacker weapon class;
defender armour; charging and flanked flags; the `MitigationResult` (deflected, bruised, bled, overflow); outcome
(no damage, deflected only, bruise, bleed, stunned, killed). `SoldierStruck` stays for the battle report and
achievements.

### Ranged

| Moment | Hook | Sound | Notes | Phase |
|---|---|---|---|---|
| Shot or volley fired | existing `VolleyFired`, extended with shot count and weapon class | Bow twang, crossbow thunk, javelin throw. A **volley** plays one layered "many bows" sound scaled by the count, not one sound per archer. | Single shots (SU-105) stay individual, within the cue's limit. | A (as now), B (count and class) |
| Fire & Engage volley | same | As a volley, louder. | | B |
| Arrows incoming | `VolleyFired` + flight time (distance / `ProjectileSpeed`) | Arrow whistle at the target, starting ~0.4 s before landing, **only when the target is one of the player's troops** or on screen. | "Incoming" warning. Arced shots only. | C |
| Projectile lands | existing `ProjectileLanded`, extended with the mitigation result | Hit: thunk into shield, mail or flesh. Miss: quiet ground thud. | | B |

### Morale

| Moment | Hook | Player's troop | Enemy troop | Phase |
|---|---|---|---|---|
| Near breaking | polled: Shock above a fraction (0.8) of Effective Morale, 4 times a second | Low tension layer (heartbeat or uneasy murmur) on that troop while it lasts. | None | B |
| Retreat (Case B, in melee) | existing `TroopBroke` (Retreat) | "Fall back!" shout, kit shuffle, low warning horn. | Shout only. | B |
| Disorder (Case B, out of melee) | `TroopBroke` (Disorder) | Confused shouting, warning horn. | Shouting. | B |
| Surrounded break (Case C) | `TroopBroke` (SurroundedBreak) | Panicked shouting, harsher horn. | Panicked shouting. | B |
| Rout (Case A) | `TroopBroke` (Rout) | Scream of fear (current Flee+Shatter clips), loud defeat horn. | Screams and a cheer from the nearest player troop. | B |
| Routed troop running | existing `Troop.Routed`, until it leaves the field | Running feet fading with distance. | Same. | C |

### Movement

| Moment | Hook | Sound | Notes | Phase |
|---|---|---|---|---|
| Troop marching | `Troop.StateChanged` (Moving / Idle) | Loop by class: infantry feet and kit (light or heavy), cavalry walk; gallop at charge speed. | Volume by soldier count. Only the nearest few troops loop at once. | C |
| Troop halts and forms its line | `StateChanged` → Idle | Short "halt" kit settle. | | C |

### Player feedback (UI bus, unpositioned)

| Moment | Hook | Sound | Phase |
|---|---|---|---|
| Troop selected | new `TroopSelection.Changed` | Acknowledgement by class or character (current `Selected` clips). Box select: one sound for all. | B |
| Move, line or pursue order | new `TroopPlayerOrders.OrderIssued(kind, troops)` | Leader bark: "Yes" / "Forward!" for a move or line, "Attack!" for a pursue (current `Ordered Move` clips). Deployment placement: soft click. | B |
| Order ignored (a routed, retreating or disordered troop, §5.5) | `OrderIssued` lists the troops whose order methods return early in these states | `negative.wav` plus a "Can't!" bark. Today an ignored order is silent. | B |
| Chain link made or severed (Alt) | new `OrderIssued` detail | Soft latch / unlatch click. | C |
| Pause, speed bar, log, heat map | `GamePause.Changed`, buttons | UI clicks. Pause also switches the mixer to the Paused snapshot. | A |

### Battle flow

| Moment | Hook | Sound | Phase |
|---|---|---|---|
| Deployment | `Battle.PhaseChanged` (Deployment) | Ambience and calm pre-battle music. | A |
| Start Battle | `PhaseChanged` (Fighting) | Horn blast and drum hit; music moves to the battle layers. | B |
| Capture point taken or lost | existing `CapturePoint.PointCaptured` | Positive or negative horn stinger. | B |
| Victory or defeat | existing `Battle.Ended(battle, won)` | Win or lose jingle (current clips), sounds of the field fade, music out. `BattleFlow`'s scene change must not cut the jingle off. | B |

### Ambience and music

- **Ambience:** one bed per mission (wind, birds, the current forest loop), moved from the Music group to a new
  Ambience group. Later, point sources placed in the scene (river, woods). An **army murmur** bed whose level follows
  the number of living soldiers.
- **Music:** vertical layers that follow battle intensity. 3–4 stems of the same tempo, key and length started
  together with `AudioSource.PlayScheduled` and faded in and out: *deployment* (drone), *manoeuvre* (low drums),
  *engaged* (full percussion), *climax* (everything). Intensity = share of soldiers in melee, recent deaths and player
  troops near breaking, smoothed over a few seconds so it never flickers. Stingers for battle start, victory and
  defeat play over it.

## Architecture

All in `Scripts/Battle/Audio/` (namespace `AudioSystem`), data in `Assets/AudioSystem/`.

| Piece | Job |
|---|---|
| `SoundCue` (ScriptableObject) | Clips, volume and pitch ranges, mixer group, priority, max instances, cooldown, positioned or not, no immediate repeat of the same clip. One asset per sound in the map. |
| `BattleAudioTuning` (ScriptableObject, `Resources/Battle Tuning/Battle Audio Tuning`) | Every audio number: voice caps, bed and loop counts, near-break threshold, zoom curve, ducking, music intensity weights. |
| `BattleAudioPlayer` | A pool of AudioSources (one-shots and loops). Plays cues, enforces each cue's max instances and cooldown and a global cap, and steals the lowest-priority, oldest, furthest voice when full. Replaces the one-source-per-type cut-off. |
| `ScreenSpatializer` | Sets each positioned voice's `panStereo` from its viewport x and its volume from its distance to the view (falling off quickly off screen). Zoom (orthographic size 5–20) scales soldier one-shots down and troop beds up as the camera pulls out. `spatialBlend` stays 0: Unity's 3D rolloff does not react to an orthographic zoom. |
| `BattleSoundDirector` | Subscribes to the events, turns them into cues, keeps per-troop and per-engagement state (melee beds, marching loops, near-break polling) and drives the music intensity. Replaces `ModelSoundEffects`; the prefab becomes **Battle Audio**. |
| Audio debug overlay | Toggle key (like the F5 heat map): active voices by cue, steals per second, beds, music intensity. |

**Mixer** (rename to the current glossary):

```
Master
├── Music
├── Stingers            (battle start, capture, breaks, victory/defeat)
├── SFX
│   ├── Combat          (swings, impacts, deaths, ranged)
│   ├── Voices          (barks, war cries, break shouts)
│   ├── Movement        (marching, fleeing)
│   └── Ambience        (field bed, army murmur)
└── UI
```

- **Exposed parameters:** Master, Music, SFX, Voices and UI volume, for an options menu later.
- **Snapshots:** Deployment, Battle, Paused (low-pass and duck SFX, music a little quieter), End.
- **Ducking:** Voices and Stingers send to a Duck Volume on Combat and Movement, so barks and break cues cut through a
  big melee.

**Time:** cue cooldowns and voice limits run on unscaled time, so 2× speed does not double the density past the caps
and nothing is pitched up. On pause the pool pauses its SFX voices and the Paused snapshot comes in; music, ambience
and UI keep playing. Deployment opens paused, so its ambience and music run regardless of `GamePause`.

**Import settings:** one-shots Force To Mono, Decompress On Load, Vorbis or ADPCM. Loops and music Streaming, Vorbis,
from WAV or OGG sources: MP3 adds padding that leaves a gap at the loop point.

## Starting budgets (all tunable)

| Budget | Start |
|---|---|
| Real voices (Project Settings > Audio) | 64 (now 32) |
| One-shot pool | 40 |
| Melee beds / marching loops at once | 4 / 4 |
| Per cue at once | impacts 6, swings 4, deaths 4, shots 4, war cries 2, barks 1 across the army, stingers 1 |
| Per cue cooldown | impacts 0.05 s, deaths 0.12 s, barks 1.5 s, the same bark on the same troop 6 s |
| Priority (high to low) | stingers > barks > break cues > engagement start > deaths > impacts > swings > shots > movement > ambience |

## Asset brief

Generation and library notes: short (0.2–1.5 s), dry (no reverb: the mix adds space), mono, trimmed tight,
loudness-matched within a cue and peaking around −1 dBFS. Make 2–3 times the variations needed and keep the best.
Libraries: Sonniss GameAudioGDC bundles, the Unity Asset Store, freesound.org (licence per file). Record every
non-original file's source and licence in `Assets/AudioSystem/LICENSES.md` and check commercial-use terms for each
source, AI services included.

| Group | Cues | Variations each | Count |
|---|---|---|---|
| Swings | Sword, Axe, Spear, Mace | 4 | 16 |
| Impacts | deflect on mail, deflect on shield, deflect on light armour, bruise, bleed | 6 | 30 |
| Big impacts | infantry line crash, cavalry charge impact | 4 | 8 |
| Melee beds (loops, 20–40 s) | small, medium, large | 1–2 | 5 |
| Vocals | death, pain / stun groan, charge effort | 8 / 6 / 6, across voice types | 20 |
| Ranged | bow, crossbow, javelin throw, volley layer, arrow whistle | 4 | 20 |
| Ranged hits | shield, mail, flesh, ground | 4 | 16 |
| Movement (loops) | light infantry, heavy infantry, cavalry walk, cavalry gallop, running (flee) | 1–2 | 8 |
| Shouts | war cry, fall back, disorder confusion, surrounded panic, rout scream, cheer | 3–4 | 20 |
| Barks (per voice set) | select, move, attack, ignored order, flanked | 3 | 15 per set; 2–3 generic sets |
| Stingers | battle start horn, capture / lost, retreat horn, rout horn, victory, defeat | 1–2 | 10 |
| Ambience (loops) | one per biome (forest first), army murmur | 1 | 2+ |
| Music | battle stems (4, same tempo, key and length, 60–120 s loops), deployment piece | | 5 |

Roughly 190–200 files for the full set. Phase A needs nothing new. Phase B needs a little over half (impacts by
result, death and stun vocals, shots and hits, shouts, one bark set, stingers), and the current temporary clips cover
some of it.

## Steps

Each step ends as AGENTS.md asks: compile and EditMode green, SU entries for behaviour or tuning changes, the new
tunables added to spec §8, and a commit.

**Phase A: foundation (current temporary clips, no new sounds)**
- [ ] 1. Mixer: unmute Master (unless the mute is deliberate), restructure and rename the groups, add snapshots and
  exposed parameters; move the forest loop to Ambience. Raise real voices to 64.
- [ ] 2. `SoundCue`, `BattleAudioTuning`, `BattleAudioPlayer` and `ScreenSpatializer`, with EditMode tests for the
  pool's limits, cooldowns and stealing (plain C#, no clips needed).
- [ ] 3. `BattleSoundDirector` takes over SU-64's four sounds through cues, now positioned and not cutting each other
  off; delete `ModelSoundEffects`, rename the prefab Battle Audio in the three scenes.
- [ ] 4. Pause and speed handling; deployment ambience; audio debug overlay.

**Phase B: readability**
- [ ] 5. New events: `Clashed` (`ClashInfo`), `Engaged` / `Disengaged`, `AssaultStarted`, `VolleyFired` count and
  weapon class, `TroopSelection.Changed`, `OrderIssued` (with ignored troops).
- [ ] 6. Impacts by result (deflect, bruise, bleed, stun) and the flank bark.
- [ ] 7. Morale cues with the player / enemy split, and the near-break tension layer.
- [ ] 8. Selection and order barks, ignored orders.
- [ ] 9. Battle flow: start horn, capture points, victory and defeat.
- [ ] 10. Playtest with a recording (the Recorder captures audio) and review the mix.

**Phase C: weight and atmosphere**
- [ ] 11. Melee beds per engagement, marching and flee loops, army murmur.
- [ ] 12. Impact materials from armour (a new `ImpactMaterial` on `ArmourProfile`: cloth, leather, mail, shield) and
  swings by weapon class.
- [ ] 13. Charge shouts, incoming-arrow whistles.
- [ ] 14. Layered battle music with intensity.

**Phase D: content and polish**
- [ ] 15. Replace the temporary clips (AI-generated and library), fill `LICENSES.md`, loudness pass.
- [ ] 16. Voice sets for named characters (a `VoiceSet` on the character profile; generics share sets by class).
- [ ] 17. Volume sliders in the options menu (Campaign side, using the exposed parameters).
- [ ] 18. Final mix pass on recordings of Missions 1 and 2.

## Open questions for the designer

1. **Barks:** spoken English, an invented battle language, or wordless shouts only? This decides the AI-generation
   prompts and whether barks need localising later.
2. Should the player hear the **enemy's** shouts (war cries, breaks) as well as its combat? Proposed: yes, but no
   spoken barks.
3. **Near-break tension layer:** wanted? It repeats what the morale bar already shows, but the player can't watch
   every card.
4. **Music style:** orchestral, folk and percussion, or drums only? And should music react to intensity (proposed) or
   just play a track?
5. Should the **selected** troops be a little louder (around +3 dB) than the rest, so the player hears their own
   orders play out?
6. **SU-64's assumptions:** this plan changes "sounds are not positioned" and replaces the 0.08 s limit per type with
   per-cue limits. Mark SU-64 `[~]` in `AssumptionsReview.md` if agreed.
