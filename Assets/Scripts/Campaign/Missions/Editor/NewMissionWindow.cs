using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Campaign.Missions.EditorTools
{
    // Leveraged Wars > Missions > New Mission: makes the next mission's folders, sheet, PreparationData and battle scene
    // (MissionTools.CreateMission) and opens the scene.
    public class NewMissionWindow : EditorWindow
    {
        string _title = "";
        int _source = -1;   // -1: not chosen yet (defaults to the last mission); 0: empty scene; n: copy mission n

        [MenuItem("Leveraged Wars/Missions/New Mission...", priority = 0)]
        static void Open()
        {
            var window = GetWindow<NewMissionWindow>(true, "New Mission");
            window.minSize = new Vector2(420, 230);
        }

        void OnGUI()
        {
            var catalog = MissionTools.LoadCatalog();
            var missions = catalog != null ? catalog.missions.Where(m => m != null).ToList() : new System.Collections.Generic.List<MissionDefinition>();
            int number = missions.Count + 1;
            if (_source < 0) _source = missions.Count;

            EditorGUILayout.LabelField($"Mission {number}", EditorStyles.boldLabel);
            _title = EditorGUILayout.TextField(new GUIContent("Title", "Shown in the tent; also names the folder."), _title);

            var options = new[] { "Empty scene" }.Concat(missions.Select(m => $"Copy of {m.DisplayName}")).ToArray();
            _source = EditorGUILayout.Popup(new GUIContent("Battle scene", "Start from an empty scene, or a copy of a " +
                "mission's battle (its map, armies, controllers and AI; the navmesh is copied too)."), Mathf.Clamp(_source, 0, options.Length - 1), options);

            string root = MissionTools.MissionFolder(number, _title);
            string code = MissionTools.Code(number);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"{root}/\n" +
                $"   {code} Mission.asset\n" +
                $"   {MissionTools.PreBattleFolder}/{code} Prep.asset\n" +
                $"   {MissionTools.BattleFolder}/{code} Battle.unity\n" +
                $"   {MissionTools.BattleFolder}/{MissionTools.MapFolder}/\n" +
                $"   {MissionTools.TweenFolder}/" +
                (missions.Count > 0 && missions[^1].nextMission == null ? $"\n\n{missions[^1].DisplayName} will lead to this mission." : ""),
                MessageType.None);

            bool exists = AssetDatabase.IsValidFolder(root);
            if (exists) EditorGUILayout.HelpBox($"{root} already exists.", MessageType.Error);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(exists))
                if (GUILayout.Button("Create Mission", GUILayout.Height(28)))
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    var source = _source > 0 && _source <= missions.Count ? missions[_source - 1] : null;
                    try
                    {
                        var mission = MissionTools.CreateMission(_title, source);
                        Selection.activeObject = mission;
                        EditorGUIUtility.PingObject(mission);
                        Close();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Missions] could not create the mission: {e.Message}\n{e}");
                    }
                    GUIUtility.ExitGUI();
                }
        }
    }
}
