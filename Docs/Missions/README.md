# Missions

How a mission is put together and the steps to make a new one (SU-119). Tools: **Leveraged Wars > Missions**.

## 1. Where things live

```
Assets/Missions/
  M03 - <Title>/
    M03 Mission.asset          the mission sheet (MissionDefinition)
    PreBattle/
      M03 Prep.asset           PreparationData: briefing, loans, court, deployment, new characters
      ...                      dialogue flows for the briefing and the court
    Battle/
      M03 Battle.unity         the battle scene
      M03 Battle/              its baked navmesh (NavMeshPlus writes here)
      Map/                     the map's layer images: display, water, trees, ...
    Tween/                     achievement assets made for this mission
Assets/Resources/MissionCatalog.asset   every mission in campaign order
```

Campaign characters stay in `Resources/Troop and Character Profiles/Characters/Campaign/`: saves find them there by name.

## 2. The mission sheet

Select a mission's `Mxx Mission.asset`. The inspector lists problems at the top (not in the catalog, no preparation
data, no scene, scene not in Build Settings, more mandatory characters than the deploy cap), then three tabs.

| Tab | What you set | Used by |
|---|---|---|
| **Pre-battle** | Briefing text or briefing flow, image, loans (*Buy Orders*), court (0 Tactician, 1 Captain, 2 Dispatch-bearer: name, portrait, dialogue or default line), quest cue, **Mandatory Characters**, **Max Deployed**, **New Characters**. Shop: not built yet. | The tent (`TentHubController`) |
| **Tween** | Global and character achievements for this mission only; **Next Mission**. | `AchievementController`, the post-battle Continue |
| **Battle** | The battle scene; open it; sync Build Settings. | The tent's March Out |

- **New characters** join the roster at level 1 when the tent opens. Make one mandatory as well if it must fight.
- **Mandatory characters** are deployed for free and locked in. They count towards *Max Deployed*.
- **Next mission** is the one link out of a mission. Leave it empty on the last mission: Continue then ends the game.
- Achievements tied to a scene object (a capture point) stay on the scene's Achievement Controller.
- Win and loss conditions are still the battle scene's Battle End components (`Rout`, `CapturedPoint`).

## 3. Making a new mission

1. **Leveraged Wars > Missions > New Mission...**: give a title, and choose whether the battle scene starts empty or as a
   copy of an earlier mission's battle (map, armies, controllers, AI, with a copy of its navmesh so rebaking does not
   touch the original). This creates the folders and assets, adds the mission to the end of the catalog and Build
   Settings, links the previous last mission to it, and opens the scene.
2. Fill in the **Pre-battle** and **Tween** tabs.
3. Put the layer images in `Battle/Map/` and build the map in the scene. (Automatic map building from the layers is
   the next step of the workflow; until then, set up the Display, Water and Trees objects as in the existing levels and
   bake the navmesh.)
4. Place the deployment zone, objective zones and enemy troops in the scene; set up the enemy AI.
5. Play from the main menu, or from the Battle Preparation scene with Tent Sample Data.

Renaming or moving a mission's folder or scene is fine: the sheet keeps the scene by GUID and Build Settings follow.
To reorder missions, reorder the catalog (**Select Mission Catalog**), fix the *Next Mission* links, then
**Sync Build Settings**.

## 4. Build Settings

Indices 0-2 are the menu scenes (Main menu, Battle Preparation, Post battle); code loads them by index. The missions'
battle scenes follow in catalog order, then any other scene that was listed. **Sync Build Settings** restores this
order; New Mission and scene moves run it for you.

## 5. Moving Levels 1 and 2 (one-off)

**Leveraged Wars > Missions > Migrate Levels 1 and 2** moves `Resources/Levels/Level 1` and `Level 2` into
`Assets/Missions/M01 - Level 1` and `M02 - Level 2`, creates their sheets (Level 1 leads to Level 2) and the catalog,
and updates Build Settings. Assets are moved, not copied, so all references survive. `Mission1Setup` and
`DeploymentSetup` find the scenes through the catalog afterwards. Safe to run twice.
