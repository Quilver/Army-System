using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BattleEnd;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LeveragedWars.Battle.AI.EditorTools
{
    /// <summary>
    /// Builds the Mission 1 (rout) enemy AI in Level 1: an EnemyAI object with the battlefield, director, mission and
    /// debug overlay, its scene markers, the Archers / Guards / Cavalry detachments and their triggers (AI README §3).
    /// The director starts with the scene (deployment only pauses time), and the enemy army's rout ends the battle at
    /// over half its troops.
    /// Safe to run again: markers that already exist keep their positions; the AI settings are rebuilt.
    /// </summary>
    public static class Mission1Setup
    {
        public const string ScenePath = "Assets/Resources/Levels/Level 1/Level 1.unity";
        public const string EnemyAIName = "EnemyAI";
        public const string MarkersName = "Markers";

        public const string ArcherFallback = "ArcherFallback";
        public const string LureLine = "LureLine";
        public const string CavalryRoute1 = "CavalryRoute1";
        public const string CavalryRoute2 = "CavalryRoute2";

        public const string Archers = "Archers";
        public const string Guards = "Guards";
        public const string Cavalry = "Cavalry";

        public const string CavalryLoop = "CavalryLoop";
        public const string GuardsAdvance = "GuardsAdvance";

        /// <summary>The enemy army routs (player wins) once more than this share of its troops are out.</summary>
        public const float EnemyRoutShare = 0.5f;

        // Default marker placement for the Level 1 map (river arc west of x = -18, enemy north, player deploys south at
        // y = -16). Angle is the red arrow's direction in degrees (0 = +x, 90 = +y, towards the enemy side).
        static readonly (string name, Vector2 position, float angle)[] MarkerDefaults =
        {
            (ArcherFallback, new Vector2(-2.5f, 18.5f), -90f), // just behind the gap between the two guards, facing the player
            (LureLine, new Vector2(0f, -2f), 90f),             // across the middle of the field; fires when a player troop crosses north
            (CavalryRoute1, new Vector2(-35.5f, -14f), 0f),    // the gap south of the river's end
            (CavalryRoute2, new Vector2(-33f, -21f), 0f),      // south-west of the player's deployment line, clear of its left troop
        };

        [MenuItem("Leveraged Wars/AI/Set Up Mission 1 (Level 1)")]
        static void SetUpFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorUtility.DisplayDialog("Set Up Mission 1", $"Open {ScenePath} and set up the Mission 1 AI there?", "Open Level 1", "Cancel"))
                    return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Undo.SetCurrentGroupName("Set Up Mission 1 AI");
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
                Debug.LogError($"[AI] Mission 1 setup failed: {e.Message}\n{e}");
            }
            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Builds (or rebuilds) the Mission 1 AI in the scene. Returns a summary. Throws if the scene lacks what it needs.</summary>
        public static string Apply(Scene scene)
        {
            var log = new StringBuilder("[AI] Mission 1 set up in ").Append(scene.name).AppendLine(":");

            global::Battle battle = FindInScene<global::Battle>(scene).FirstOrDefault()
                ?? throw new InvalidOperationException("No Battle component in the scene.");
            if (battle.enemy == null) throw new InvalidOperationException("Battle has no enemy army.");

            // ---- Enemy troops by role
            Troop[] enemyTroops = battle.enemy.GetComponentsInChildren<Troop>(true);
            var archers = enemyTroops.Where(IsRanged).ToList();
            var cavalry = enemyTroops.Where(t => !IsRanged(t) && IsCavalry(t)).ToList();
            var guards = enemyTroops.Except(archers).Except(cavalry).ToList();
            if (archers.Count == 0 || guards.Count == 0 || cavalry.Count == 0)
                throw new InvalidOperationException(
                    $"Mission 1 needs an archer, infantry and cavalry under {battle.enemy.name}; found {archers.Count} ranged, " +
                    $"{guards.Count} infantry and {cavalry.Count} cavalry (cavalry is a troop whose name or profile contains \"Cav\").");

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

            // ---- Markers (existing ones keep their placement)
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
                    log.AppendLine($"- created marker {name} at {position}");
                }
                marker[name] = t != null ? t : markers.Find(name);
            }

            // ---- Adapters on the enemy troops, so the detachments list them explicitly
            List<Component> Adapters(IEnumerable<Troop> troops) =>
                troops.Select(t => (Component)GetOrAdd<TroopAIAdapter>(t.gameObject)).ToList();

            // ---- Detachments (AI README §3)
            var detachments = new List<DetachmentSetup>
            {
                new DetachmentSetup
                {
                    id = Archers,
                    troops = Adapters(archers),
                    brokenThreshold = 0.5f,
                    startingFormation = new SkirmishDefinition
                    {
                        fallbackPoint = new AnchorReference { kind = AnchorKind.Transform, transform = marker[ArcherFallback] },
                        preferredRangeFraction = 0.85f,
                        minimumRange = 7f,
                        resumeMultiplier = 1.5f,
                        maxDistanceFromFallback = 30f,
                    },
                },
                new DetachmentSetup
                {
                    id = Guards,
                    troops = Adapters(guards),
                    brokenThreshold = 0.5f,
                    startingFormation = new GuardDefinition
                    {
                        // The archer's fallback point, not the archer itself: guarding the archer would send the guards
                        // after every player troop the archer harasses, far from their post.
                        guarded = new AnchorReference { kind = AnchorKind.Transform, transform = marker[ArcherFallback] },
                        guardPosition = new AnchorReference { kind = AnchorKind.StartPosition },
                        aggroRange = 10f,
                        leashRange = 16f,
                        lineGap = 2f,
                    },
                },
                new DetachmentSetup
                {
                    id = Cavalry,
                    troops = Adapters(cavalry),
                    brokenThreshold = 0.5f,
                    startingFormation = new HoldDefinition
                    {
                        anchor = new AnchorReference { kind = AnchorKind.StartPosition },
                        aggroRange = 8f,
                        leashRange = 12f,
                        lineGap = 2f,
                    },
                },
            };

            // ---- Triggers
            var triggers = new List<TriggerDefinition>
            {
                new TriggerDefinition
                {
                    id = CavalryLoop,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new OpponentCrossesLine { line = marker[LureLine], halfLength = 0f },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = Cavalry,
                            formation = new FlankDefinition
                            {
                                target = FlankTarget.RearmostOpponent,
                                route = new List<Transform> { marker[CavalryRoute1], marker[CavalryRoute2] },
                                waypointRadius = 3f,
                                waypointStuckSeconds = 4f,
                                flankDistance = 8f,
                                chargeDistance = 12f,
                                approachTimeout = 20f,
                            },
                        },
                    },
                },
                new TriggerDefinition
                {
                    id = GuardsAdvance,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new DetachmentStatus { detachmentId = Archers, check = DetachmentCheck.Broken },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = Guards,
                            formation = new AdvanceDefinition { target = AdvanceTarget.NearestOpponent },
                        },
                    },
                },
            };

            Undo.RecordObject(mission, "Configure Mission 1");
            mission.Configure(detachments, triggers);
            EditorUtility.SetDirty(mission);
            log.AppendLine($"- {Archers}: {Names(archers)} (Skirmish)  {Guards}: {Names(guards)} (Guard)  {Cavalry}: {Names(cavalry)} (Hold)");
            log.AppendLine($"- triggers {CavalryLoop}, {GuardsAdvance}");

            // ---- Director: starts with the scene. Deployment pauses time, so the AI acts once the fighting begins.
            var directorSO = new SerializedObject(director);
            directorSO.FindProperty("battlefield").objectReferenceValue = field;
            directorSO.FindProperty("commander").objectReferenceValue = mission;
            directorSO.FindProperty("beginOnStart").boolValue = true;
            directorSO.ApplyModifiedProperties();

            var overlaySO = new SerializedObject(overlay);
            overlaySO.FindProperty("director").objectReferenceValue = director;
            overlaySO.ApplyModifiedProperties();

            if (UnwireDeploy(battle, director)) log.AppendLine("- removed AIDirector.BeginBattle from Battle's Deploy event");

            // ---- Win condition: the enemy army routs at over half its troops
            foreach (Rout rout in FindInScene<Rout>(scene))
            {
                var so = new SerializedObject(rout);
                if (so.FindProperty("_army").objectReferenceValue != battle.enemy) continue;
                so.FindProperty("_routShare").floatValue = EnemyRoutShare;
                so.ApplyModifiedProperties();
                log.AppendLine($"- {rout.name} rout: battle won when more than {EnemyRoutShare:P0} of the enemy troops are out");
            }

            return log.ToString();
        }

        /// <summary>Removes director.BeginBattle from Battle's Deploy event (an earlier version of this tool added it).</summary>
        static bool UnwireDeploy(global::Battle battle, AIDirector director)
        {
            FieldInfo info = typeof(global::Battle).GetField("_Deploy", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(info?.GetValue(battle) is UnityEvent deploy)) return false;
            bool removed = false;
            for (int i = deploy.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (!(deploy.GetPersistentTarget(i) is AIDirector) || deploy.GetPersistentMethodName(i) != nameof(AIDirector.BeginBattle))
                    continue;
                if (!removed) Undo.RecordObject(battle, "Start AI with the scene");
                UnityEventTools.RemovePersistentListener(deploy, i);
                removed = true;
            }
            if (removed) EditorUtility.SetDirty(battle);
            return removed;
        }

        public static GameObject FindEnemyAI(Scene scene) =>
            scene.GetRootGameObjects().FirstOrDefault(g => g.name == EnemyAIName)
            ?? FindInScene<AIMission>(scene).Select(m => m.gameObject).FirstOrDefault();

        static bool IsRanged(Troop troop) =>
            troop.Profile != null && troop.Profile.Characters.Any(c => c != null && TroopAIAdapter.IsMissileWeapon(c.RangedWeapon));

        static bool IsCavalry(Troop troop) =>
            Contains(troop.name, "cav") || (troop.Profile != null && Contains(troop.Profile.name, "cav"));

        static bool Contains(string s, string part) => s != null && s.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        static string Names(IEnumerable<Troop> troops) => string.Join(", ", troops.Select(t => t.name));

        static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T existing) ? existing : Undo.AddComponent<T>(go);

        static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));

        static Transform FindByName(Scene scene, string name) =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);

        static void SetIcon(GameObject go)
        {
            if (EditorGUIUtility.IconContent("sv_icon_dot13_pix16_gizmo")?.image is Texture2D icon)
                EditorGUIUtility.SetIconForObject(go, icon);
        }
    }
}
