using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleEnd;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LeveragedWars.Battle.AI.EditorTools
{
    /// <summary>
    /// Builds the AI vs AI test level (AI README §6) from Mission 1's battle map: two armies of 4 spearmen, 2 huskarls and
    /// 2 skirmishers face each other across the field (south and north), each run by its own AI director, and the player
    /// only watches. The battle's player army is the south army (its AI plays the Player team), the enemy army the north.
    /// There is no deployment, briefing or Mission 1 AI. The battle ends when every troop of one army is out.
    /// Each side: Spears (the spearmen) advance as one chain (Chain Line); Skirmishers keep pace on its wings, one beyond
    /// each end, and go round the enemy's flanks once the chains meet; Huskarls follow behind as a reserve and go in when
    /// the spears break or some time after the clash.
    /// Safe to run again on the level: the troops and the AI are rebuilt, the rest of the scene is kept.
    /// </summary>
    public static class AIBattleSetup
    {
        public const string ScenePath = "Assets/Resources/Levels/Test/AI vs AI.unity";
        public const string AIRootName = "AI vs AI";
        const string CharacterFolder = "Assets/Resources/Troop and Character Profiles/Characters/Generic/";
        const string VisualiserPrefabPath = "Assets/Resources/Troop and Character Profiles/Troop Visualiser.prefab";

        /// <summary>Team id the player's controls use in this level: no troop has it, so the player only watches.</summary>
        public const int SpectatorTeamId = -1;

        public enum Side { South, North }

        // Detachment and trigger ids, prefixed with the side ("South Spears").
        public const string Spears = "Spears";
        public const string Skirmishers = "Skirmishers";
        public const string Huskarls = "Huskarls";
        public const string SkirmishersWrap = "SkirmishersWrap";
        public const string HuskarlsCommit = "HuskarlsCommit";

        public const int SpearmenPerSide = 4;
        public const int HuskarlsPerSide = 2;
        public const int SkirmishersPerSide = 2;

        // Layout on the Level 1 map: open ground between the river (x < -18) and the north-east wood (x > 24).
        const float CentreX = 3f;            // middle of each army's line
        const float LineY = 16f;             // the spear lines stand at y = -16 (south) and +16 (north), 32 apart
        const float ReserveBehind = 7f;      // the huskarls, behind the spear line
        const float ReserveApart = 8f;
        const float WingGap = 3f;            // between the chain's ends and the skirmishers on its wings

        public static string Id(Side side, string name) => $"{side} {name}";

        [MenuItem("Leveraged Wars/AI/Create AI vs AI Level")]
        static void CreateFromMenu()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            string message = exists
                ? $"{ScenePath} exists. Rebuild its troops and AI? The rest of the scene is kept."
                : $"Create {ScenePath} from Mission 1's battle map?";
            if (!EditorUtility.DisplayDialog("AI vs AI Level", message, exists ? "Rebuild" : "Create", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Scene scene = CreateOrRebuild(out string report);
                Debug.Log(report);
                Selection.activeGameObject = scene.GetRootGameObjects().FirstOrDefault(g => g.name == AIRootName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI] AI vs AI level failed: {e.Message}\n{e}");
            }
        }

        /// <summary>Copies Mission 1's battle scene to ScenePath if the level does not exist yet, builds it and saves it.</summary>
        public static Scene CreateOrRebuild(out string report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null &&
                !AssetDatabase.CopyAsset(Mission1Setup.ScenePath, ScenePath))
                throw new InvalidOperationException($"Could not copy {Mission1Setup.ScenePath} to {ScenePath}.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            report = Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return scene;
        }

        /// <summary>
        /// Turns an open copy of a battle scene into the AI vs AI level. Returns a summary. Throws if the scene has no
        /// Battle with two armies, or a character profile is missing.
        /// </summary>
        public static string Apply(Scene scene)
        {
            var log = new StringBuilder("[AI] AI vs AI level built in ").Append(scene.name).AppendLine(":");

            global::Battle battle = FindInScene<global::Battle>(scene).FirstOrDefault()
                ?? throw new InvalidOperationException("No Battle component in the scene.");
            if (battle.player == null || battle.enemy == null) throw new InvalidOperationException("Battle needs both armies.");

            var spearman = LoadCharacter("Spearman");
            var huskarl = LoadCharacter("Huskarl");
            var skirmisher = LoadCharacter("Skirmisher");

            // ---- Clear what belongs to Mission 1 (and to an earlier run of this tool)
            int removed = 0;
            foreach (Troop troop in FindInScene<Troop>(scene).ToList()) { Undo.DestroyObjectImmediate(troop.gameObject); removed++; }
            foreach (GameObject go in FindInScene<AIDirector>(scene).Select(d => d.gameObject)
                         .Concat(FindInScene<AIMission>(scene).Select(m => m.gameObject))
                         .Concat(FindInScene<BattleAIBattlefield>(scene).Select(b => b.gameObject))
                         .Distinct().ToList())
                RemoveAI(go);
            foreach (GameObject go in FindInScene<DeploymentZone>(scene).Select(z => z.gameObject)
                         .Concat(FindInScene<Cutscene.CutScene>(scene).Select(c => c.gameObject))
                         .Select(OutermostRoot).Distinct().ToList())
                Undo.DestroyObjectImmediate(go);
            log.AppendLine($"- removed {removed} troops, the Mission 1 AI, the deployment zone and the briefing cutscene");

            // ---- Battle: no deployment; it ends when every troop of one army is out (routed or wiped out)
            var battleSO = new SerializedObject(battle);
            battleSO.FindProperty("_deployment").boolValue = false;
            battleSO.ApplyModifiedProperties();
            foreach (Rout rout in FindInScene<Rout>(scene))
            {
                var so = new SerializedObject(rout);
                so.FindProperty("_routShare").floatValue = 1f;
                so.ApplyModifiedProperties();
            }
            Rename(battle.player.gameObject, "=========SOUTH ARMY (AI)=========");
            Rename(battle.enemy.gameObject, "=========NORTH ARMY (AI)=========");

            // ---- The player watches: the controls' team matches no troop (cards, hover and the combat log still work)
            foreach (TroopPlayerOrders controls in FindInScene<TroopPlayerOrders>(scene))
            {
                var so = new SerializedObject(controls);
                so.FindProperty("_playerTeamId").intValue = SpectatorTeamId;
                so.ApplyModifiedProperties();
            }

            // ---- Camera over the middle of the field
            Camera camera = FindInScene<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera")) ?? FindInScene<Camera>(scene).FirstOrDefault();
            if (camera != null)
            {
                Undo.RecordObject(camera.transform, "Centre camera");
                camera.transform.position = new Vector3(CentreX, 0f, camera.transform.position.z);
            }

            // ---- Troops and AI
            GameObject root = new GameObject(AIRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create AI vs AI");
            SceneManager.MoveGameObjectToScene(root, scene);
            var field = root.AddComponent<BattleAIBattlefield>();

            foreach (Side side in new[] { Side.South, Side.North })
            {
                Army army = side == Side.South ? battle.player : battle.enemy;
                // The battle's player army is team 0 (BattleAIBattlefield's player team), the enemy army team 1.
                int teamId = side == Side.South ? 0 : 1;
                var spears = new List<Troop>();
                var skirmishers = new List<Troop>();
                var huskarls = new List<Troop>();

                float spearWidth = NaturalLineLength(spearman);
                float chainWidth = spearWidth * SpearmenPerSide;
                float wing = chainWidth * 0.5f + WingGap + NaturalLineLength(skirmisher) * 0.5f;
                for (int i = 0; i < SpearmenPerSide; i++)
                    spears.Add(CreateTroop(army, $"{side} Spearmen {i + 1}", spearman, teamId,
                        At(side, (i - (SpearmenPerSide - 1) * 0.5f) * spearWidth, 0f), side));
                for (int i = 0; i < SkirmishersPerSide; i++)
                    skirmishers.Add(CreateTroop(army, $"{side} Skirmishers {i + 1}", skirmisher, teamId,
                        At(side, i == 0 ? -wing : wing, 0f), side));
                for (int i = 0; i < HuskarlsPerSide; i++)
                    huskarls.Add(CreateTroop(army, $"{side} Huskarls {i + 1}", huskarl, teamId,
                        At(side, (i - 0.5f) * ReserveApart, -ReserveBehind), side));

                var ai = new GameObject($"{side} AI");
                Undo.RegisterCreatedObjectUndo(ai, "Create side AI");
                ai.transform.SetParent(root.transform, false);
                var director = ai.AddComponent<AIDirector>();
                var mission = ai.AddComponent<AIMission>();
                var overlay = ai.AddComponent<AIDebugOverlay>();
                mission.Configure(Detachments(side, spears, skirmishers, huskarls, chainWidth), Triggers(side));
                EditorUtility.SetDirty(mission);

                var directorSO = new SerializedObject(director);
                directorSO.FindProperty("battlefield").objectReferenceValue = field;
                directorSO.FindProperty("commander").objectReferenceValue = mission;
                directorSO.FindProperty("aiTeam").enumValueIndex = (int)(side == Side.South ? AITeam.Player : AITeam.Enemy);
                directorSO.FindProperty("beginOnStart").boolValue = true;
                directorSO.ApplyModifiedPropertiesWithoutUndo();
                var overlaySO = new SerializedObject(overlay);
                overlaySO.FindProperty("director").objectReferenceValue = director;
                overlaySO.ApplyModifiedPropertiesWithoutUndo();

                log.AppendLine($"- {side}: {army.name}, team {teamId}, {spears.Count} spearmen (Chain Line), " +
                               $"{skirmishers.Count} skirmishers (wings), {huskarls.Count} huskarls (reserve), AI team " +
                               (side == Side.South ? AITeam.Player : AITeam.Enemy));
            }
            return log.ToString();
        }

        // ---- Detachments and triggers (AI README §6)

        static List<DetachmentSetup> Detachments(Side side, List<Troop> spears, List<Troop> skirmishers, List<Troop> huskarls,
            float chainWidth)
        {
            string spearsId = Id(side, Spears);
            return new List<DetachmentSetup>
            {
                new DetachmentSetup
                {
                    id = spearsId,
                    troops = Adapters(spears),
                    brokenThreshold = 0.5f,
                    startingFormation = new ChainLineDefinition(),
                },
                new DetachmentSetup
                {
                    id = Id(side, Skirmishers),
                    troops = Adapters(skirmishers),
                    brokenThreshold = 0.5f,
                    // The wings: one troop beyond each end of the chain, keeping pace with it. They do not go looking for a
                    // fight until the chains meet (trigger); they fight back if attacked.
                    startingFormation = new HoldDefinition
                    {
                        anchor = new AnchorReference { kind = AnchorKind.Detachment, detachmentId = spearsId },
                        aggroRange = 0f,
                        leashRange = 20f,
                        lineGap = chainWidth + 2f * WingGap,
                    },
                },
                new DetachmentSetup
                {
                    id = Id(side, Huskarls),
                    troops = Adapters(huskarls),
                    brokenThreshold = 0.5f,
                    // The reserve: behind the chain; it meets anything that comes round to it.
                    startingFormation = new HoldDefinition
                    {
                        anchor = new AnchorReference { kind = AnchorKind.Detachment, detachmentId = spearsId, forwardOffset = -ReserveBehind },
                        aggroRange = 9f,
                        leashRange = 14f,
                        lineGap = ReserveApart - NaturalLineLength(LoadCharacter("Huskarl")),
                    },
                },
            };
        }

        static List<TriggerDefinition> Triggers(Side side)
        {
            string spearsId = Id(side, Spears);
            string skirmishersId = Id(side, Skirmishers);
            string huskarlsId = Id(side, Huskarls);
            string wrapId = Id(side, SkirmishersWrap);
            return new List<TriggerDefinition>
            {
                new TriggerDefinition
                {
                    id = wrapId,
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new DetachmentStatus { detachmentId = spearsId, check = DetachmentCheck.Engaged },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = skirmishersId,
                            formation = new FlankDefinition
                            {
                                target = FlankTarget.OpponentEngagedWithDetachment,
                                detachmentId = spearsId,
                                flankDistance = 6f,
                                chargeDistance = 10f,
                                approachTimeout = 15f,
                            },
                        },
                    },
                },
                new TriggerDefinition
                {
                    id = Id(side, HuskarlsCommit),
                    enabledAtStart = true,
                    fireOnce = true,
                    condition = new AnyOf
                    {
                        conditions = new List<TriggerCondition>
                        {
                            new DetachmentStatus { detachmentId = spearsId, check = DetachmentCheck.Broken },
                            new TimeElapsed { seconds = 15f, sinceTriggerId = wrapId },
                        },
                    },
                    actions = new List<TriggerAction>
                    {
                        new SetFormationAction
                        {
                            detachmentId = huskarlsId,
                            formation = new AdvanceDefinition { target = AdvanceTarget.NearestOpponent, keepLine = false },
                        },
                    },
                },
            };
        }

        // ---- Troops

        static Vector2 At(Side side, float across, float ahead)
        {
            Vector2 facing = Facing(side);
            Vector2 right = new Vector2(facing.y, -facing.x);
            Vector2 centre = new Vector2(CentreX, side == Side.South ? -LineY : LineY);
            return centre + right * across + facing * ahead;
        }

        static Vector2 Facing(Side side) => side == Side.South ? Vector2.up : Vector2.down;

        static Troop CreateTroop(Army army, string name, CharacterProfile character, int teamId, Vector2 position, Side side)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create troop");
            go.transform.SetParent(army.transform, false);
            Vector2 facing = Facing(side);
            go.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg));

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
            so.FindProperty("_teamId").intValue = teamId;
            SerializedProperty characters = so.FindProperty("_characters");
            characters.arraySize = 1;
            characters.GetArrayElementAtIndex(0).objectReferenceValue = character;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<TroopAIAdapter>();
            return troop;
        }

        static float NaturalLineLength(CharacterProfile character) =>
            LineGeometry.NaturalLength(TroopFrontageRule.ForSoldierCount(Mathf.Max(1, character.Numbers)));

        static CharacterProfile LoadCharacter(string name) =>
            AssetDatabase.LoadAssetAtPath<CharacterProfile>(CharacterFolder + name + ".asset")
            ?? throw new InvalidOperationException($"No character profile {CharacterFolder}{name}.asset.");

        static List<Component> Adapters(IEnumerable<Troop> troops) =>
            troops.Select(t => (Component)t.GetComponent<TroopAIAdapter>()).ToList();

        // ---- Scene helpers

        static void Rename(GameObject go, string name)
        {
            if (go.name == name) return;
            Undo.RecordObject(go, "Rename army");
            go.name = name;
        }

        static readonly Type[] AIComponents = { typeof(AIDirector), typeof(AIMission), typeof(AIDebugOverlay), typeof(BattleAIBattlefield) };

        // An AI object (Mission 1's EnemyAI with its markers, or this tool's own) goes whole; AI components on an object
        // that has other jobs (the battlefield may sit on the battle manager) go on their own.
        static void RemoveAI(GameObject go)
        {
            if (go == null) return; // went with an AI object above it
            Component[] components = go.GetComponents<Component>();
            if (components.All(c => c is Transform || AIComponents.Contains(c.GetType())))
            {
                Undo.DestroyObjectImmediate(go);
                return;
            }
            foreach (Component c in components)
                if (c != null && AIComponents.Contains(c.GetType())) Undo.DestroyObjectImmediate(c);
        }

        static GameObject OutermostRoot(GameObject go) => PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;

        static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    }
}
