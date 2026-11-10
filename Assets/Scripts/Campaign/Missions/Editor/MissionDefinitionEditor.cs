using System.Linq;
using BattlePrep;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Campaign.Missions.EditorTools
{
    // The mission sheet in three tabs (Docs/Missions/README.md): Pre-battle (the PreparationData, drawn inline), Tween
    // (achievements, next mission) and Battle (the scene). Problems are listed at the top.
    [CustomEditor(typeof(MissionDefinition))]
    public class MissionDefinitionEditor : Editor
    {
        static readonly string[] Tabs = { "Pre-battle", "Tween", "Battle" };
        static int _tab;
        Editor _prepEditor;

        void OnDisable()
        {
            if (_prepEditor != null) DestroyImmediate(_prepEditor);
        }

        public override void OnInspectorGUI()
        {
            var mission = (MissionDefinition)target;
            serializedObject.Update();

            EditorGUILayout.LabelField(mission.Number > 0 ? $"Mission {mission.Number}" : "Not in the Mission Catalog", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MissionDefinition.title)));
            DrawProblems(mission);

            EditorGUILayout.Space();
            _tab = GUILayout.Toolbar(_tab, Tabs);
            EditorGUILayout.Space();
            switch (_tab)
            {
                case 0: DrawPreBattle(mission); break;
                case 1: DrawTween(); break;
                case 2: DrawBattle(mission); break;
            }
            serializedObject.ApplyModifiedProperties();
        }

        void DrawProblems(MissionDefinition mission)
        {
            if (mission.Number == 0)
            {
                EditorGUILayout.HelpBox("This mission is not in the Mission Catalog, so the campaign never plays it.", MessageType.Warning);
                if (GUILayout.Button("Add to the end of the catalog"))
                {
                    var catalog = MissionTools.GetOrCreateCatalog();
                    catalog.missions.Add(mission);
                    EditorUtility.SetDirty(catalog);
                    AssetDatabase.SaveAssets();
                    MissionTools.SyncBuildSettings();
                }
            }
            if (mission.preparation == null) EditorGUILayout.HelpBox("No PreparationData (Pre-battle tab).", MessageType.Warning);
            else
            {
                int mandatory = mission.preparation.mandatoryCharacters?.Count(c => c != null) ?? 0;
                if (mandatory > mission.preparation.maxDeployed)
                    EditorGUILayout.HelpBox($"{mandatory} mandatory characters but at most {mission.preparation.maxDeployed} can deploy.", MessageType.Warning);
            }
            if (!mission.HasBattleScene) EditorGUILayout.HelpBox("No battle scene (Battle tab).", MessageType.Warning);
            else if (!MissionTools.InBuildSettings(mission.BattleScenePath))
                EditorGUILayout.HelpBox("The battle scene is not in Build Settings (Battle tab).", MessageType.Warning);
        }

        void DrawPreBattle(MissionDefinition mission)
        {
            var prep = serializedObject.FindProperty(nameof(MissionDefinition.preparation));
            EditorGUILayout.PropertyField(prep, new GUIContent("Preparation data"));
            if (mission.preparation == null)
            {
                if (GUILayout.Button("Create PreparationData"))
                {
                    string folder = $"{System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(mission)).Replace('\\', '/')}/{MissionTools.PreBattleFolder}";
                    MissionTools.EnsureFolder(folder);
                    var data = CreateInstance<PreparationData>();
                    data.court = new[] { new CourtMember(), new CourtMember(), new CourtMember() };
                    string code = mission.Number > 0 ? MissionTools.Code(mission.Number) : mission.name;
                    AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{code} Prep.asset"));
                    prep.objectReferenceValue = data;
                }
                return;
            }
            EditorGUILayout.HelpBox("Briefing, loans (Buy Orders), court (0 Tactician, 1 Captain, 2 Dispatch-bearer), " +
                                    "mandatory characters, deploy cap and new characters. Shop: not built yet.", MessageType.None);
            CreateCachedEditor(mission.preparation, null, ref _prepEditor);
            _prepEditor.OnInspectorGUI();
        }

        void DrawTween()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MissionDefinition.achievements)), new GUIContent("Global achievements"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MissionDefinition.characterAchievements)), new GUIContent("Character achievements"));
            EditorGUILayout.HelpBox("Achievements tied to scene objects (a capture point) stay on the battle scene's " +
                                    "Achievement Controller. Win and loss conditions are still the scene's Battle End " +
                                    "components (Rout, Captured Point).", MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MissionDefinition.nextMission)));
        }

        void DrawBattle(MissionDefinition mission)
        {
            var current = mission.HasBattleScene ? AssetDatabase.LoadAssetAtPath<SceneAsset>(mission.BattleScenePath) : null;
            var picked = (SceneAsset)EditorGUILayout.ObjectField("Battle scene", current, typeof(SceneAsset), false);
            if (picked != current)
            {
                Undo.RecordObject(mission, "Set battle scene");
                mission.SetBattleScene(picked != null ? AssetDatabase.GetAssetPath(picked) : "");
                EditorUtility.SetDirty(mission);
                MissionTools.SyncBuildSettings();
            }
            if (!mission.HasBattleScene) return;

            EditorGUILayout.LabelField("Path", mission.BattleScenePath, EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open battle scene") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(mission.BattleScenePath, OpenSceneMode.Single);
                if (!MissionTools.InBuildSettings(mission.BattleScenePath) && GUILayout.Button("Sync Build Settings"))
                    MissionTools.SyncBuildSettings();
            }
        }
    }
}
