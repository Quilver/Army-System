using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Turns a markdown dialogue scene (format in Cutscene.DialogueMarkdown) into a DialogueScript asset and keeps it in sync:
    // saving the .md file, or changing a portrait image, rebuilds every DialogueScript built from it.
    public static class DialogueScriptBuilder
    {
        const string CreateMenu = "Assets/Create/Cutscenes/Dialogue Script from Markdown";
        static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".psd", ".psb", ".tga", ".gif", ".bmp", ".tif", ".tiff", ".exr" };

        [MenuItem(CreateMenu, true)]
        static bool CanCreate() => Selection.objects.Any(o => IsMarkdown(AssetDatabase.GetAssetPath(o)));

        // Right click a .md file > Create > Cutscenes > Dialogue Script from Markdown. Makes <name>.asset next to it.
        [MenuItem(CreateMenu, false, 81)]
        static void CreateFromSelection()
        {
            DialogueScript last = null;
            foreach (var obj in Selection.objects)
            {
                string mdPath = AssetDatabase.GetAssetPath(obj);
                if (!IsMarkdown(mdPath)) continue;
                var script = ScriptsBuiltFrom(mdPath).FirstOrDefault();
                if (script == null)
                {
                    script = ScriptableObject.CreateInstance<DialogueScript>();
                    script.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(mdPath, ".asset"));
                    AssetDatabase.CreateAsset(script, assetPath);
                }
                Rebuild(script);
                last = script;
            }
            if (last == null) return;
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
        }

        [MenuItem("Leveraged Wars/Dialogue/Rebuild All Dialogue Scripts")]
        static void RebuildAll()
        {
            int count = 0;
            foreach (var script in AllScripts())
            {
                Rebuild(script);
                count++;
            }
            Debug.Log($"Rebuilt {count} dialogue scripts.");
        }

        public static void Rebuild(DialogueScript script, bool logWarnings = true)
        {
            if (script == null) return;
            var lines = new List<DialogueLine>();
            var warnings = new List<string>();
            string mdPath = script.Source != null ? AssetDatabase.GetAssetPath(script.Source) : null;

            if (string.IsNullOrEmpty(mdPath)) warnings.Add("No markdown file assigned.");
            else if (!File.Exists(mdPath)) warnings.Add($"{mdPath} not found.");
            else
            {
                string folder = Path.GetDirectoryName(mdPath)?.Replace('\\', '/');
                var parsed = DialogueMarkdown.Parse(File.ReadAllText(mdPath));
                warnings.AddRange(parsed.Warnings);
                var sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in parsed.Lines)
                {
                    lines.Add(new DialogueLine
                    {
                        Speaker = p.Speaker,
                        Text = p.Text,
                        WordsPerMinute = p.WordsPerMinute,
                        SpeakerSide = p.Side,
                        LeftName = p.LeftName,
                        RightName = p.RightName,
                        LeftPortrait = ResolveSprite(folder, p.LeftImage, p.SourceLine, sprites, warnings),
                        RightPortrait = ResolveSprite(folder, p.RightImage, p.SourceLine, sprites, warnings),
                    });
                }
                if (lines.Count == 0) warnings.Add("No text boxes found. Each \"## \" subheading is one text box.");
            }

            script.SetLines(lines, warnings);
            EditorUtility.SetDirty(script);
            AssetDatabase.SaveAssetIfDirty(script);
            if (logWarnings)
                foreach (var w in warnings) Debug.LogWarning($"Dialogue script {script.name}: {w}", script);
        }

        // Image paths are relative to the .md file, or start with Assets/. "file.png#SpriteName" picks one sprite of a sheet.
        // If the path is wrong but exactly one image in the project has that file name, that image is used.
        static Sprite ResolveSprite(string folder, string image, int line, Dictionary<string, Sprite> cache, List<string> warnings)
        {
            if (string.IsNullOrEmpty(image)) return null;
            if (cache.TryGetValue(image, out var cached)) return cached;

            string file = image, spriteName = null;
            int hash = image.LastIndexOf('#');
            if (hash > 0)
            {
                spriteName = image.Substring(hash + 1);
                file = image.Substring(0, hash);
            }

            string path = DialogueMarkdown.Combine(folder, file);
            Sprite sprite = null;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                sprite = LoadSprite(path, spriteName);
                if (sprite == null) warnings.Add(NoSpriteMessage(line, path, spriteName));
            }
            else
            {
                string fileName = Path.GetFileName(file);
                var matches = AssetDatabase.FindAssets("t:Texture2D")
                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .Where(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();
                if (matches.Count == 1)
                {
                    sprite = LoadSprite(matches[0], spriteName);
                    warnings.Add(sprite != null
                        ? $"Line {line}: \"{image}\" is not at {path}; using {matches[0]}. Fix the path to silence this."
                        : NoSpriteMessage(line, matches[0], spriteName));
                }
                else if (matches.Count > 1)
                    warnings.Add($"Line {line}: \"{image}\" is not at {path}, and {matches.Count} images share its name. Use the full path.");
                else
                    warnings.Add($"Line {line}: image \"{image}\" not found (looked for {path}).");
            }

            cache[image] = sprite;
            return sprite;
        }

        static Sprite LoadSprite(string path, string spriteName)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
            return spriteName == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(s => s.name == spriteName);
        }

        static string NoSpriteMessage(int line, string path, string spriteName) => spriteName != null
            ? $"Line {line}: {path} has no sprite called \"{spriteName}\"."
            : $"Line {line}: {path} is not a sprite. Set its Texture Type to \"Sprite (2D and UI)\" in the Inspector.";

        public static IEnumerable<DialogueScript> AllScripts() =>
            AssetDatabase.FindAssets("t:" + nameof(DialogueScript))
                .Select(g => AssetDatabase.LoadAssetAtPath<DialogueScript>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null);

        static IEnumerable<DialogueScript> ScriptsBuiltFrom(string mdPath) =>
            AllScripts().Where(s => s.Source != null && AssetDatabase.GetAssetPath(s.Source) == mdPath);

        internal static bool IsMarkdown(string path) =>
            !string.IsNullOrEmpty(path) && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

        internal static bool IsImage(string path) =>
            !string.IsNullOrEmpty(path) && ImageExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    // Rebuilds dialogue scripts after their .md file is saved, or after a portrait image is added, moved or reimported.
    class DialogueMarkdownPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var markdown = new HashSet<string>(imported.Concat(moved).Where(DialogueScriptBuilder.IsMarkdown));
            bool images = imported.Concat(moved).Concat(deleted).Any(DialogueScriptBuilder.IsImage);
            if (markdown.Count == 0 && !images) return;

            EditorApplication.delayCall += () =>
            {
                foreach (var script in DialogueScriptBuilder.AllScripts().ToList())
                {
                    string source = script.Source != null ? AssetDatabase.GetAssetPath(script.Source) : null;
                    bool own = source != null && markdown.Contains(source);
                    if (own || images) DialogueScriptBuilder.Rebuild(script, logWarnings: own);
                }
            };
        }
    }
}
