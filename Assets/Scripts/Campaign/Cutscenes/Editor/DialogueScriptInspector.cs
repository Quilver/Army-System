using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Shows a DialogueScript as a preview of its text boxes: portraits (the talking one bright), speaker, speed and text.
    [CustomEditor(typeof(DialogueScript))]
    class DialogueScriptInspector : UnityEditor.Editor
    {
        static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f, 1f);
        const float PortraitSize = 48f;

        public override void OnInspectorGUI()
        {
            var script = (DialogueScript)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_source"), new GUIContent("Markdown File"));
            bool sourceChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (sourceChanged) DialogueScriptBuilder.Rebuild(script);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild from Markdown")) DialogueScriptBuilder.Rebuild(script);
                using (new EditorGUI.DisabledScope(script.Source == null))
                    if (GUILayout.Button("Open Markdown")) AssetDatabase.OpenAsset(script.Source);
            }

            foreach (var warning in script.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);

            var lines = script.GetLines();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{lines.Count} text boxes", EditorStyles.boldLabel);
            foreach (var line in lines) DrawLine(line);

            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }

        static void DrawLine(DialogueLine line)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawPortrait(line.LeftPortrait, line.SpeakerSide == DialogueSide.Left);
                using (new EditorGUILayout.VerticalScope())
                {
                    string speaker = string.IsNullOrEmpty(line.Speaker) ? "(narration)" : line.Speaker;
                    string speed = line.WordsPerMinute > 0f ? $"   {line.WordsPerMinute:0} wpm" : "";
                    EditorGUILayout.LabelField(speaker + speed, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(line.Text, EditorStyles.wordWrappedLabel);
                }
                DrawPortrait(line.RightPortrait, line.SpeakerSide == DialogueSide.Right);
            }
        }

        static void DrawPortrait(Sprite sprite, bool speaking)
        {
            var rect = GUILayoutUtility.GetRect(PortraitSize, PortraitSize, GUILayout.Width(PortraitSize), GUILayout.Height(PortraitSize));
            if (sprite == null) return;
            Texture texture = AssetPreview.GetAssetPreview(sprite);
            if (texture == null) texture = AssetPreview.GetMiniThumbnail(sprite);
            if (texture == null) return;
            var old = GUI.color;
            if (!speaking) GUI.color = Dim;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
    }
}
