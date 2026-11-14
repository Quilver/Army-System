using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattlePrep;
using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Builds a LoanOffer from its markdown note (format in BattlePrep.LoanMarkdown). StoryAssets decides when.
    public static class LoanOfferBuilder
    {
        const string CreateMenu = "Assets/Create/Preparations/Loan Offer from Markdown";

        [MenuItem(CreateMenu, true)]
        static bool CanCreate() => Selection.objects.Any(o => StoryAssets.IsMarkdown(AssetDatabase.GetAssetPath(o)));

        // Right click a .md file > Create > Preparations > Loan Offer from Markdown. Story-vault notes with a Gold property
        // do not need this: theirs is made automatically.
        [MenuItem(CreateMenu, false, 82)]
        static void CreateFromSelection()
        {
            MarkdownAsset last = null;
            foreach (var obj in Selection.objects)
            {
                string mdPath = AssetDatabase.GetAssetPath(obj);
                if (StoryAssets.IsMarkdown(mdPath)) last = StoryAssets.CreateFor(mdPath, typeof(LoanOffer));
            }
            if (last == null) return;
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
        }

        internal static void Build(LoanOffer loan, string mdPath, string markdown, List<string> warnings)
        {
            var parsed = LoanMarkdown.Parse(markdown, Path.GetFileNameWithoutExtension(mdPath));
            warnings.AddRange(parsed.Warnings);
            loan.Set(parsed, warnings);
        }
    }

    // Shows a LoanOffer as its card: title, terms and text, with the note it comes from and any warnings.
    [CustomEditor(typeof(LoanOffer))]
    class LoanOfferInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var loan = (LoanOffer)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_source"),
                new GUIContent("Markdown File", "The note this loan is built from. Edit the note, not this asset."));
            bool sourceChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (sourceChanged) StoryAssets.Rebuild(loan);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild from Markdown")) StoryAssets.Rebuild(loan);
                using (new EditorGUI.DisabledScope(loan.Source == null))
                    if (GUILayout.Button("Open Markdown")) AssetDatabase.OpenAsset(loan.Source);
            }

            foreach (var warning in loan.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(loan.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"+{loan.Gold} gold, repay {loan.Cost} in {loan.Months} months " +
                                           $"({loan.InterestPercent}% interest)", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField($"Prestige cost: {loan.PrestigeCost}");
                EditorGUILayout.Space(4);
                var rich = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
                EditorGUILayout.LabelField(loan.Details, rich);
            }
        }
    }
}
