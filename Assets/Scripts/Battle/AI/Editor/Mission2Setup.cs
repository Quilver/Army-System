using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AchievementSystem;
using BattleEnd;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static LeveragedWars.Battle.AI.EditorTools.Mission1Setup;

namespace LeveragedWars.Battle.AI.EditorTools
{
    /// <summary>
    /// Builds Mission 2 (Level 2, capture) from the designer's plan (AI README §4): the enemy army (a first line of two
    /// spearmen with a cavalry troop on its wing, three archers in front of a second line of two spearmen with its own
    /// cavalry, a bandit and a village guard at the wall gap), the two objectives (Primary inside the walls wins the battle;
    /// Secondary, the village across the river, gives gold and is destroyed if the enemy takes it), the WallGap marker and
    /// the EnemyAI with its detachments and triggers.
    /// Safe to run again: troops, markers and the secondary objective that already exist keep their placement; enemy troops
    /// not in the plan are removed; the AI settings and the secondary objective's achievement are rebuilt.
    /// </summary>
    public static class Mission2Setup
    {
        public const string LegacyScenePath = "Assets/Resources/Levels/Level 2/Level 2.unity";
        // Mission 2's battle scene, from the Mission Catalog (SU-119).
        public static string ScenePath => global::Campaign.Missions.EditorTools.MissionTools.BattleScenePath(2, LegacyScenePath);

        public const string WallGap = "WallGap";

        // Objective ids (CapturePointAIObjective) and their GameObjects' names.
        public const string Primary = "Primary";
        public const string Secondary = "Secondary";
        public const string PrimaryObjectName = "Primary objective";
        public const string SecondaryObjectName = "Secondary objective";

        // Detachments
        public const string FirstLine = "FirstLine";
        public const string FirstCavalry = "FirstCavalry";
        public const string Archers = "Archers";
        public const string SecondLine = "SecondLine";
        public const string SecondCavalry = "SecondCavalry";
        public const string Bandit = "Bandit";
        public const string VillageGuard = "VillageGuard";

        // Triggers
        public const string FirstCavalryFlank = "FirstCavalryFlank";
        public const string SecondLineGo = "SecondLineGo";
        public const string Bypass = "Bypass";

        /// <summary>The secondary objective's side achievement: held by the player when the battle ends.</summary>
        public const string SecondaryAchievementTitle = "Secondary objective";
        public const int SecondaryGold = 70;

        /// <summary>The first line's cavalry holds this far to the line's right (west, the plan's left as seen from the player).</summary>
        public const float FirstCavalrySideOffset = 10f;
        /// <summary>The archers' fallback point: this far in front of the second line.</summary>
        public const float ArchersAhead = 5f;
        /// <summary>The archers never stand further than this from their fallback point.</summary>
        public const float ArchersLeash = 15f;

        const string CharacterFolder = "Assets/Resources/Troop and Character Profiles/Characters/Generic/";
        const string VisualiserPrefabPath = "Assets/Resources/Troop and Character Profiles/Troop Visualiser.prefab";
        const string CapturePointPrefabPath = "Assets/Prefabs/Battle/Capture point.prefab";
        const string SecondaryIconPath = "Assets/Campaign/UI/After battle menus/Symbols/Wall of symbols.jpg";
        const int EnemyTeamId = 1;
        const float ObjectiveScale = 7f; // as the primary objective: a circle of radius 3.5

        // Red arrow directions (degrees): the enemy faces the player, who deploys to the south.
        public const float South = -90f;
        public const float North = 90f;

        /// <summary>One enemy troop of the plan: its GameObject's name, detachment, generic character and default placement.</summary>
        public readonly struct TroopPlacement
        {
            public readonly string Name;
            public readonly string Detachment;
            public readonly string Character;
            public readonly Vector2 Position;
            public readonly float Angle;

            public TroopPlacement(string name, string detachment, string character, Vector2 position, float angle)
            {
                Name = name;
                Detachment = detachment;
                Character = character;
                Position = position;
                Angle = angle;
            }
        }

        // Default placement on the Level 2 map ("Twin Villages": the walled village in the north-west with its gap facing
        // north, the wood east of the middle, the river and bridge to the south-east, the second village beyond it in the
        // south-east corner; the player deploys south of the middle, zone top at y = -5).
        public static readonly TroopPlacement[] Plan =
        {
            new TroopPlacement("First Line Spearmen 1", FirstLine, "Spearman", new Vector2(-11.7f, 6f), South),
            new TroopPlacement("First Line Spearmen 2", FirstLine, "Spearman", new Vector2(-4.3f, 6f), South),
            new TroopPlacement("First Line Cavalry", FirstCavalry, "Cav", new Vector2(-18f, 6f), South),
            new TroopPlacement("Archers 1", Archers, "Archer", new Vector2(-11.6f, 17f), South),
            new TroopPlacement("Archers 2", Archers, "Archer", new Vector2(-6f, 17f), South),
            new TroopPlacement("Archers 3", Archers, "Archer", new Vector2(-0.4f, 17f), South),
            new TroopPlacement("Second Line Spearmen 1", SecondLine, "Spearman", new Vector2(-9.7f, 22f), South),
            new TroopPlacement("Second Line Spearmen 2", SecondLine, "Spearman", new Vector2(-2.3f, 22f), South),
            new TroopPlacement("Second Line Cavalry", SecondCavalry, "Cav", new Vector2(9f, 23f), South),
            new TroopPlacement("Bandit", Bandit, "Skirmisher", new Vector2(9f, 9f), South),
            new TroopPlacement("Village Guard", VillageGuard, "Huskarl", new Vector2(-29f, 25f), North),
        };

        static readonly (string name, Vector2 position, float angle)[] MarkerDefaults =
        {
            (WallGap, new Vector2(-29f, 32f), North), // just outside the gap in the walls, facing out (north)
        };

        static readonly Vector2 SecondaryPosition = new Vector2(44f, -30.5f); // open ground inside the south-east village

        [MenuItem("Leveraged Wars/AI/Set Up Mission 2 (Level 2)")]
        static void SetUpFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorUtility.DisplayDialog("Set Up Mission 2", $"Open {ScenePath} and set up Mission 2 there?", "Open Level 2", "Cancel"))
                    return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            List<Troop> extra = TroopsNotInPlan(scene);
            if (extra.Count > 0 && !EditorUtility.DisplayDialog("Set Up Mission 2",
                    "These enemy troops are not in the Mission 2 plan and will be removed:\n\n" + string.Join("\n", extra.Select(t => t.name)),
                    "Remove and set up", "Cancel"))
                return;

            Undo.SetCurrentGroupName("Set Up Mission 2");
            int group = Undo.GetCurrentGroup();
            try
            {
                string report = Apply(scene);
                Debug.Log(report);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = FindEnemyAI(scene);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI] Mission 2 setup failed: {e.Message}\n{e}");
            }
            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Builds (or rebuilds) Mission 2 in the scene. Returns a summary. Throws if the scene lacks what it needs.</summary>
        public static string Apply(Scene scene)
        {
            var log = new StringBuilder("[AI] Mission 2 set up in ").Append(scene.name).AppendLine(":");

            global::Battle battle = FindInScene<global::Battle>(scene).FirstOrDefault()
                ?? throw new InvalidOperationException("No Battle component in the scene.");
            if (battle.enemy == null) throw new InvalidOperationException("Battle has no enemy army.");

            // ---- Enemy army: the plan's troops (existing ones keep their placement), nothing else
            foreach (Troop troop in TroopsNotInPlan(scene))
            {
                log.AppendLine($"- removed {troop.name} (not in the plan)");
                Undo.DestroyObjectImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(troop.gameObject) ?? troop.gameObject);
            }
            var troops = new Dictionary<string, Troop>();
            foreach (TroopPlacement placement in Plan)
            {
                Troop troop = battle.enemy.GetComponentsInChildren<Troop>(true).FirstOrDefault(t => t.name == placement.Name);
                if (troop == null)
                {
                    troop = CreateTroop(battle.enemy, placement);
                    log.AppendLine($"- created {placement.Name} ({placement.Character}) at {placement.Position}");
                }
                GetOrAdd<TroopAIAdapter>(troop.gameObject);
                troops[placement.Name] = troop;
            }

            // ---- EnemyAI object and components
            GameObject ai = FindEnemyAI(scene);
            if (ai == null)
            {
                ai = new GameObject(EnemyAIName);
                SceneManager.MoveGameObjectToScene(ai, scene);
                Undo.RegisterCreatedObjectUndo(ai, "Create EnemyAI");
                log.AppendLine("- created EnemyAI");
            }
            var field = GetOrAdd<BattleAIBattlefield>(ai);
            var director = GetOrAdd<AIDirector>(ai);
            var mission = GetOrAdd<AIMission>(ai);
            var overlay = GetOrAdd<AIDebugOverlay>(ai);
            Dictionary<string, Transform> marker = Markers(scene, ai, log);

            // ---- Objectives
            CapturePoint primary = SetUpPrimary(scene, log);
            CapturePoint secondary = SetUpSecondary(scene, log);
            AchievementController achievements = FindInScene<AchievementController>(scene).FirstOrDefault();
            if (achievements == null)
            {
                achievements = Undo.AddComponent<AchievementController>(battle.gameObject);
                log.AppendLine($"- added an Achievement Controller to {battle.name}");
            }
            SetSecondaryAchievement(achievements, secondary);
            log.AppendLine($"- {SecondaryAchievementTitle} achievement: hold {secondary.name} at the end for {SecondaryGold} gold");

            // ---- Detachments and triggers (AI README §4)
            List<Component> Adapters(string detachment) => Plan.Where(p => p.Detachment == detachment)
                .Select(p => (Component)troops[p.Name].GetComponent<TroopAIAdapter>()).ToList();
            DetachmentSetup MakeDetachment(string id, FormationDefinition formation) => new DetachmentSetup
            {
                id = id,
                troops = Adapters(id),
                brokenThreshold = 0.5f,
                startingFormation = formation,
            };

            var detachments = new List<DetachmentSetup>
            {
                // Marches on the player as a line and fights.
                MakeDetachment(FirstLine, new AdvanceDefinition { target = AdvanceTarget.NearestOpponent }),
                // Keeps pace on the first line's right (west) wing until the line engages, then goes round (trigger).
                MakeDetachment(FirstCavalry, new HoldDefinition
                {
                    anchor = new AnchorReference { kind = AnchorKind.Detachment, detachmentId = FirstLine, sideOffset = FirstCavalrySideOffset },
                    aggroRange = 5f,
                    leashRange = 8f,
                    lineGap = 2f,
                }),
                // Stand in front of the second line, walk out to shoot at the player while the first line fights, fall back
                // to the second line when the player comes for them. Their fallback point moves with the second line.
                MakeDetachment(Archers, new SkirmishDefinition
                {
                    fallbackPoint = new AnchorReference { kind = AnchorKind.Detachment, detachmentId = SecondLine, forwardOffset = ArchersAhead },
                    preferredRangeFraction = 0.85f,
                    minimumRange = 7f,
                    resumeMultiplier = 1.5f,
                    maxDistanceFromFallback = ArchersLeash,
                    lineGap = 2f,
                }),
                // Holds until the player comes close or the first line breaks (trigger).
                MakeDetachment(SecondLine, new HoldDefinition
                {
                    anchor = new AnchorReference { kind = AnchorKind.StartPosition },
                    aggroRange = 8f,
                    leashRange = 13f,
                    lineGap = 2f,
                }),
                MakeDetachment(SecondCavalry, new HoldDefinition
                {
                    anchor = new AnchorReference { kind = AnchorKind.StartPosition },
                    aggroRange = 5f,
                    leashRange = 8f,
                    lineGap = 2f,
                }),
                // Goes round the wood and over the bridge to take the village, which is destroyed once taken.
                MakeDetachment(Bandit, new CaptureDefinition { objectiveId = Secondary, aggroRange = 6f }),
                // Stands in the wall gap and meets anything near it or near the primary objective.
                MakeDetachment(VillageGuard, new GuardDefinition
                {
                    guarded = new AnchorReference { kind = AnchorKind.Objective, objectiveId = Primary },
                    guardPosition = new AnchorReference { kind = AnchorKind.StartPosition },
                    aggroRange = 6f,
                    leashRange = 16f,
                    lineGap = 2f,
                }),
            };

            var triggers = new List<TriggerDefinition>
            {
                new TriggerDefinition
                {
                    id = FirstCavalryFlank,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new DetachmentStatus { detachmentId = FirstLine, check = DetachmentCheck.Engaged },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = FirstCavalry,
                            formation = new FlankDefinition { target = FlankTarget.OpponentEngagedWithDetachment, detachmentId = FirstLine },
                        },
                    },
                },
                new TriggerDefinition
                {
                    id = SecondLineGo,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new AnyOf
                    {
                        conditions = new List<TriggerCondition>
                        {
                            new OpponentNearDetachment { detachmentId = SecondLine, radius = 13f },
                            new DetachmentStatus { detachmentId = FirstLine, check = DetachmentCheck.Broken },
                        },
                    },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction { detachmentId = SecondLine, formation = new AdvanceDefinition { target = AdvanceTarget.NearestOpponent } },
                        new SetFormationAction
                        {
                            detachmentId = SecondCavalry,
                            formation = new FlankDefinition { target = FlankTarget.NearestOpponentToDetachment, detachmentId = SecondLine },
                        },
                    },
                },
                new TriggerDefinition
                {
                    // A player who gets round to the wall gap without meeting the second line finds it there.
                    id = Bypass,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new AllOf
                    {
                        conditions = new List<TriggerCondition>
                        {
                            new OpponentNearPoint { point = marker[WallGap], radius = 13f },
                            new DetachmentStatus { detachmentId = SecondLine, check = DetachmentCheck.Waiting },
                        },
                    },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = SecondLine,
                            formation = new HoldDefinition
                            {
                                anchor = new AnchorReference { kind = AnchorKind.Transform, transform = marker[WallGap] },
                                aggroRange = 8f,
                                leashRange = 12f,
                                lineGap = 2f,
                            },
                        },
                        new EnableTriggerAction { triggerId = SecondLineGo, enabled = false },
                    },
                },
            };

            Undo.RecordObject(mission, "Configure Mission 2");
            mission.Configure(detachments, triggers);
            EditorUtility.SetDirty(mission);
            foreach (DetachmentSetup d in detachments)
                log.AppendLine($"- {d.id}: {string.Join(", ", d.troops.Select(t => t.name))} ({d.startingFormation.GetType().Name.Replace("Definition", "")})");
            log.AppendLine($"- triggers {FirstCavalryFlank}, {SecondLineGo}, {Bypass}");

            // ---- Director: starts with the scene. Deployment pauses time, so the AI acts once the fighting begins.
            var directorSO = new SerializedObject(director);
            directorSO.FindProperty("battlefield").objectReferenceValue = field;
            directorSO.FindProperty("commander").objectReferenceValue = mission;
            directorSO.FindProperty("aiTeam").enumValueIndex = (int)AITeam.Enemy;
            directorSO.FindProperty("beginOnStart").boolValue = true;
            directorSO.ApplyModifiedProperties();

            var overlaySO = new SerializedObject(overlay);
            overlaySO.FindProperty("director").objectReferenceValue = director;
            overlaySO.ApplyModifiedProperties();

            if (UnwireDeploy(battle, director)) log.AppendLine("- removed AIDirector.BeginBattle from Battle's Deploy event");
            log.AppendLine($"- win: the player captures {primary.name}");
            return log.ToString();
        }

        /// <summary>Enemy troops whose names are not in the plan (the tool removes them).</summary>
        public static List<Troop> TroopsNotInPlan(Scene scene)
        {
            global::Battle battle = FindInScene<global::Battle>(scene).FirstOrDefault();
            if (battle == null || battle.enemy == null) return new List<Troop>();
            var names = new HashSet<string>(Plan.Select(p => p.Name));
            return battle.enemy.GetComponentsInChildren<Troop>(true).Where(t => !names.Contains(t.name)).ToList();
        }

        /// <summary>The scene's objective with this id (or GameObject name), or null.</summary>
        public static CapturePoint FindObjective(Scene scene, string id, string objectName) =>
            FindInScene<CapturePoint>(scene).FirstOrDefault(p =>
                (p.TryGetComponent(out CapturePointAIObjective o) && o.Id == id) || p.name == objectName);

        // ---- Markers

        static Dictionary<string, Transform> Markers(Scene scene, GameObject ai, StringBuilder log)
        {
            Transform markers = ai.transform.Find(MarkersName);
            if (markers == null)
            {
                var go = new GameObject(MarkersName);
                Undo.RegisterCreatedObjectUndo(go, "Create AI markers");
                Undo.SetTransformParent(go.transform, ai.transform, "Parent AI markers");
                go.transform.localPosition = Vector3.zero;
                markers = go.transform;
            }
            var marker = new Dictionary<string, Transform>();
            foreach (var (name, position, angle) in MarkerDefaults)
            {
                Transform t = FindByName(scene, name);
                if (t == null)
                {
                    var go = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(go, "Create AI marker");
                    Undo.SetTransformParent(go.transform, markers, "Parent AI marker");
                    go.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.Euler(0f, 0f, angle));
                    SetIcon(go);
                    t = go.transform;
                    log.AppendLine($"- created marker {name} at {position}");
                }
                marker[name] = t;
            }
            return marker;
        }

        // ---- Objectives

        // The capture point inside the walls (the scene's existing one): id Primary; the player wins by capturing it.
        static CapturePoint SetUpPrimary(Scene scene, StringBuilder log)
        {
            CapturePoint primary = FindObjective(scene, Primary, PrimaryObjectName)
                ?? FindInScene<CapturePoint>(scene).FirstOrDefault(p => p.GetComponent<CapturedPoint>() != null)
                ?? FindInScene<CapturePoint>(scene).FirstOrDefault(p => FindObjective(scene, Secondary, SecondaryObjectName) != p)
                ?? throw new InvalidOperationException("Mission 2 needs the capture point inside the walls (the primary objective).");
            Rename(primary.gameObject, PrimaryObjectName);
            SetObjectiveId(primary, Primary);

            if (!primary.TryGetComponent(out CapturedPoint win))
            {
                win = Undo.AddComponent<CapturedPoint>(primary.gameObject);
                log.AppendLine($"- {primary.name}: added the win condition");
            }
            var so = new SerializedObject(win);
            so.FindProperty("WinCondition").boolValue = true;
            so.FindProperty("playerControlled").boolValue = true;
            so.ApplyModifiedProperties();
            return primary;
        }

        // The village across the river: id Secondary, destroyed if the enemy takes it. No win condition.
        static CapturePoint SetUpSecondary(Scene scene, StringBuilder log)
        {
            CapturePoint secondary = FindObjective(scene, Secondary, SecondaryObjectName);
            if (secondary == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CapturePointPrefabPath)
                    ?? throw new InvalidOperationException($"No capture point prefab at {CapturePointPrefabPath}.");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create secondary objective");
                go.transform.SetPositionAndRotation(new Vector3(SecondaryPosition.x, SecondaryPosition.y, 0f), Quaternion.identity);
                go.transform.localScale = Vector3.one * ObjectiveScale;
                go.name = SecondaryObjectName;
                secondary = go.GetComponent<CapturePoint>();
                log.AppendLine($"- created {SecondaryObjectName} at {SecondaryPosition}");
            }
            Rename(secondary.gameObject, SecondaryObjectName);
            SetObjectiveId(secondary, Secondary);

            var destroyed = GetOrAdd<DestroyedOnCapture>(secondary.gameObject);
            var so = new SerializedObject(destroyed);
            so.FindProperty("_destroyedBy").enumValueIndex = (int)CapturePoint.PointController.Enemy;
            so.ApplyModifiedProperties();
            return secondary;
        }

        static void SetObjectiveId(CapturePoint point, string id)
        {
            var objective = GetOrAdd<CapturePointAIObjective>(point.gameObject);
            var so = new SerializedObject(objective);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedProperties();
        }

        // The secondary objective's gold: a custom global achievement on the scene's controller (it names a scene object),
        // earned when the player holds the point at the end of the battle. Replaces an earlier one of the same title or point.
        static void SetSecondaryAchievement(AchievementController controller, CapturePoint point)
        {
            var so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("_CustomGlobalAchievements");
            int index = -1;
            for (int i = 0; i < list.arraySize && index < 0; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                SerializedProperty target = e.FindPropertyRelative("_requirement")?.FindPropertyRelative("capturePoint");
                if (e.FindPropertyRelative("_title").stringValue == SecondaryAchievementTitle ||
                    (target != null && target.objectReferenceValue == point))
                    index = i;
            }
            if (index < 0)
            {
                index = list.arraySize;
                list.arraySize++;
            }

            SerializedProperty element = list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("_title").stringValue = SecondaryAchievementTitle;
            element.FindPropertyRelative("_description").stringValue =
                "Hold the village across the river when the battle ends. The enemy destroys it if they take it.";
            element.FindPropertyRelative("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SecondaryIconPath);
            element.FindPropertyRelative("_requirement").managedReferenceValue = new CapturedVictoryPoint();
            SerializedProperty rewards = element.FindPropertyRelative("rewards");
            rewards.arraySize = 1;
            rewards.GetArrayElementAtIndex(0).managedReferenceValue = new AchievementSystem.Gold();
            so.ApplyModifiedProperties();

            // The new references' own fields, once they exist.
            so.Update();
            element = so.FindProperty("_CustomGlobalAchievements").GetArrayElementAtIndex(index);
            element.FindPropertyRelative("_requirement").FindPropertyRelative("capturePoint").objectReferenceValue = point;
            element.FindPropertyRelative("rewards").GetArrayElementAtIndex(0).FindPropertyRelative("GoldPerLevel").intValue = SecondaryGold;
            so.ApplyModifiedProperties();
        }

        // ---- Troops

        static Troop CreateTroop(Army army, TroopPlacement placement)
        {
            CharacterProfile character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(CharacterFolder + placement.Character + ".asset")
                ?? throw new InvalidOperationException($"No character profile {CharacterFolder}{placement.Character}.asset.");

            var go = new GameObject(placement.Name);
            Undo.RegisterCreatedObjectUndo(go, "Create troop");
            go.transform.SetParent(army.transform, false);
            go.transform.SetPositionAndRotation(new Vector3(placement.Position.x, placement.Position.y, 0f),
                Quaternion.Euler(0f, 0f, placement.Angle));

            var visualiserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualiserPrefabPath);
            if (visualiserPrefab != null)
            {
                var visualiser = (GameObject)PrefabUtility.InstantiatePrefab(visualiserPrefab, go.transform);
                visualiser.name = visualiserPrefab.name;
                visualiser.transform.localPosition = Vector3.zero;
                visualiser.transform.localRotation = Quaternion.identity;
            }

            var troop = go.AddComponent<Troop>();
            var so = new SerializedObject(troop);
            so.FindProperty("_teamId").intValue = EnemyTeamId;
            SerializedProperty characters = so.FindProperty("_characters");
            characters.arraySize = 1;
            characters.GetArrayElementAtIndex(0).objectReferenceValue = character;
            so.ApplyModifiedPropertiesWithoutUndo();
            return troop;
        }

        static void Rename(GameObject go, string name)
        {
            if (go.name == name) return;
            Undo.RecordObject(go, "Rename objective");
            go.name = name;
        }
    }
}
