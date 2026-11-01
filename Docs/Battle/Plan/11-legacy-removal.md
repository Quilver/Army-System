# Plan 11: Remove the Legacy Unit/Model System

**Goal:** Troops and Soldiers fully replace the old Unit and Model scripts and resources, the old AI is gone (a new AI
comes later), and the scripts move into `Assets/Scripts/` split by system.

**Branch:** `legacy-removal`. Spec log: SU-57 onwards in `SpecificationUpdate.md`.

## Decisions

- Asset serialization is **Force Text** (scenes, prefabs and assets are diffable and searchable).
- The unused GPU prototype (`CollisionMapManager`, `ContextAgent`, projection compute shaders, Trajectory scene) is deleted.
- Old characters with no campaign-character counterpart (Marco Santo, Mary Santo, Wizard Mannus) are removed.
- The save system is reset: new file and version, the old save is deleted (SU-63).
- No assembly definitions per system for now (D-41 stands). The folder split and the dependency rule below are the
  boundary; revisit asmdefs once the split has settled. `[SerializeReference]` achievement data would need
  `[MovedFrom]` before any asmdef or namespace change.

## Target layout

| Folder | Contents |
|---|---|
| `Scripts/Battle/` | Troop system, `Battle`, `Army`, `CapturePoint`, battle-end conditions, `Pathfinding/`, battle sounds, new AI |
| `Scripts/Campaign/` | Campaign data and save, tent hub, post-battle screens, cutscenes, main menu |
| `Scripts/Tween/` | Campaign characters (`CampaignCharacter`, classes, caps, growths, `RosterCharacter`), achievements, `BattleReport`, `BattleFlow`, roster deployment |
| `Scripts/Misc/` | Shared UI (hover tips, float text), camera controls, input map |

Battle prefabs that lived in script folders moved to `Assets/Prefabs/Battle/`; soldier art to `Resources/Battle/Soldiers/`.

Dependency rule: Battle and Misc never reference Tween or Campaign. Tween and Campaign may reference each other
(rewards change campaign data; the campaign stores roster characters). Editor tools may cross the line.

## Steps

- [x] 0. Branch, Force Text, scene and prefab report (`Tools > Legacy Removal`).
- [x] 1. Delete the old AI (done in commit 7653e33).
- [x] 2. Troop independent of `Unit/` (`TroopState`, `BattleEvents`, `Army`, pathfinder moved).
- [x] 3. Battle glue on troops: battle end, battle flow, capture points, deployment, achievements, sounds (SU-58 to SU-64).
- [x] 4. Campaign on `RosterCharacter`; `PCWrapper` removed; save reset (SU-62, SU-63).
- [x] 5. Scenes: clean old objects, give Level 1 and 2 troops (`Tools > Legacy Removal > 3`), delete old test scenes.
- [x] 6. Delete old resources (unit prefabs, NPC/PC data, old weapons, model prefabs); move soldier sprites and
  `ModelMat` out of `Model prefabs/`; delete the GPU prototype.
- [x] 7. Move scripts into `Assets/Scripts/` by system.
- [x] 8. Docs: `AGENTS.md`, `Architecture.md`, `Troop/README.md`, `Questions.md` (Q-49).
- [x] 9. Delete `Assets/Editor/Legacy Removal/`.

## Known issues

- New folders (`Assets/Scripts/...`, `Assets/Prefabs/...`) get their `.meta` files from the Editor on the next refresh;
  commit them.

- `TroopScenarioTests.Retreat_RearRanksFallBackFirstAndFaster` was already failing before this work (12:43 run).
- Stat growth on level-up is not applied yet (SU-62).
