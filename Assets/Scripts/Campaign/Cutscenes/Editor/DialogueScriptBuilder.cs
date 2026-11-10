using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Turns a markdown dialogue scene (format in Cutscene.DialogueMarkdown) into a DialogueScript asset and keeps it in sync:
    // saving the .md file, or changing a portrait image, rebuilds every DialogueScript built from it.
    // Notes in an Obsidian vault (a folder with .obsidian/ in it, e.g. Assets/Leveraged Wars Story) whose properties say
    // "dialogue: true" get their DialogueScript made automatically, and their images resolve the way Obsidian resolves them.
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
                last = CreateFor(mdPath);
            }
            if (last == null) return;
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
        }

        // The DialogueScript built from this .md file; made next to it (<name>.asset) if there is none yet.
        public static DialogueScript CreateFor(string mdPath)
        {
            var script = ScriptsBuiltFrom(mdPath).FirstOrDefault();
            if (script == null)
            {
                script = ScriptableObject.CreateInstance<DialogueScript>();
                script.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(mdPath, ".asset"));
                AssetDatabase.CreateAsset(script, assetPath);
            }
            Rebuild(script);
            return script;
        }

        // Every vault note marked "dialogue: true" that has no DialogueScript yet gets one. Returns the .md paths it made them for.
        public static List<string> CreateMissingVaultScripts()
        {
            var made = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsVaultDialogueNote(path) || ScriptsBuiltFrom(path).Any()) continue;
                CreateFor(path);
                made.Add(path);
            }
            return made;
        }

        [MenuItem("Leveraged Wars/Dialogue/Open Story Vault in Obsidian")]
        static void OpenVault()
        {
            const string vault = "Assets/Leveraged Wars Story";
            if (!Directory.Exists(vault + "/.obsidian"))
            {
                Debug.LogWarning($"No Obsidian vault at {vault}.");
                return;
            }
            // Opens the vault if Obsidian knows it; otherwise use Obsidian's "Open folder as vault" on this folder once.
            Application.OpenURL("obsidian://open?path=" + Uri.EscapeDataString(Path.GetFullPath(vault)));
        }

        [MenuItem("Leveraged Wars/Dialogue/Rebuild All Dialogue Scripts")]
        static void RebuildAll()
        {
            var made = CreateMissingVaultScripts();
            if (made.Count > 0) Debug.Log($"Made dialogue scripts for {made.Count} story notes: {string.Join(", ", made)}");
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
                string vault = FindVaultRoot(folder);
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
                        LeftPortrait = ResolveSprite(folder, vault, p.LeftImage, p.SourceLine, sprites, warnings),
                        RightPortrait = ResolveSprite(folder, vault, p.RightImage, p.SourceLine, sprites, warnings),
                    });
                }
                if (lines.Count == 0)
                    warnings.Add("No text boxes found. Start each speaker with a \"# Name\" heading and put their lines under it.");
            }

            script.SetLines(lines, warnings);
            EditorUtility.SetDirty(script);
            AssetDatabase.SaveAssetIfDirty(script);
            if (logWarnings)
                foreach (var w in warnings) Debug.LogWarning($"Dialogue script {script.name}: {w}", script);
        }

        // Image paths are relative to the .md file, or start with Assets/. "file.png#SpriteName" picks one sprite of a sheet.
        // In an Obsidian vault a path may also be from the vault's root (obsidian:// links) or just a file name, or the end
        // of a path, found anywhere in the vault ([[Jagen.png]]), and the extension may be left out.
        // If the path is still not found but exactly one image in the project has that file name, that image is used.
        static Sprite ResolveSprite(string folder, string vault, string image, int line, Dictionary<string, Sprite> cache,
                                    List<string> warnings)
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
            bool ambiguous = false;
            string found = FindImage(path);
            if (found == null && vault != null)
                found = FindImage(DialogueMarkdown.Combine(vault, file)) ?? FindInVault(vault, file, line, warnings, out ambiguous);

            Sprite sprite = null;
            if (found != null)
            {
                sprite = LoadSprite(found, spriteName);
                if (sprite == null) warnings.Add(NoSpriteMessage(line, found, spriteName));
            }
            else if (!ambiguous)
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

        // The path itself, or the path with an image extension added (Obsidian links may leave it out).
        static string FindImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) return path;
            if (IsImage(path)) return null;
            foreach (var ext in ImageExtensions)
                if (AssetDatabase.LoadMainAssetAtPath(path + ext) != null) return path + ext;
            return null;
        }

        // Obsidian's own rule: a link that is a file name (or the end of a path) matches that image anywhere in the vault.
        static string FindInVault(string vault, string file, int line, List<string> warnings, out bool ambiguous)
        {
            ambiguous = false;
            string want = file.Replace('\\', '/').TrimStart('/');
            if (want.Length == 0 || !Directory.Exists(vault)) return null;
            bool hasExtension = IsImage(want);
            var hits = Directory.GetFiles(vault, "*", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(IsImage)
                .Where(p =>
                {
                    string candidate = hasExtension ? p : p.Substring(0, p.Length - Path.GetExtension(p).Length);
                    return candidate.EndsWith("/" + want, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            if (hits.Count == 1) return hits[0];
            if (hits.Count > 1)
            {
                ambiguous = true;
                warnings.Add($"Line {line}: {hits.Count} images in the story vault match \"{file}\". Link it with its folder, e.g. \"{hits[0].Substring(vault.Length + 1)}\".");
            }
            return null;
        }

        // The Obsidian vault the folder is in (the nearest folder above it with a .obsidian folder), or null.
        public static string FindVaultRoot(string folder)
        {
            for (string dir = folder; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir)?.Replace('\\', '/'))
                if (Directory.Exists(dir + "/.obsidian")) return dir;
            return null;
        }

        // A note in an Obsidian vault, outside its templates folder, with the property "dialogue: true".
        internal static bool IsVaultDialogueNote(string mdPath)
        {
            if (!IsMarkdown(mdPath) || !File.Exists(mdPath)) return false;
            string vault = FindVaultRoot(Path.GetDirectoryName(mdPath)?.Replace('\\', '/'));
            if (vault == null) return false;
            string templates = TemplatesFolder(vault);
            if (templates != null && mdPath.StartsWith(templates + "/", StringComparison.OrdinalIgnoreCase)) return false;
            return DialogueMarkdown.IsTrue(DialogueMarkdown.Parse(File.ReadAllText(mdPath)), "dialogue");
        }

        static string TemplatesFolder(string vault)
        {
            string json = vault + "/.obsidian/templates.json";
            if (!File.Exists(json)) return null;
            var m = Regex.Match(File.ReadAllText(json), "\"folder\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success && m.Groups[1].Value.Trim('/').Length > 0 ? vault + "/" + m.Groups[1].Value.Trim('/') : null;
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

        internal static IEnumerable<DialogueScript> ScriptsBuiltFrom(string mdPath) =>
            AllScripts().Where(s => s.Source != null && AssetDatabase.GetAssetPath(s.Source) == mdPath);

        internal static bool IsMarkdown(string path) =>
            !string.IsNullOrEmpty(path) && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

        internal static bool IsImage(string path) =>
            !string.IsNullOrEmpty(path) && ImageExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    // Rebuilds dialogue scripts after their .md file is saved, or after a portrait image is added, moved or reimported.
    // A story-vault note marked "dialogue: true" that has no script yet gets one.
    class DialogueMarkdownPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var markdown = new HashSet<string>(imported.Concat(moved).Where(DialogueScriptBuilder.IsMarkdown));
            bool images = imported.Concat(moved).Concat(deleted).Any(DialogueScriptBuilder.IsImage);
            if (markdown.Count == 0 && !images) return;

            EditorApplication.delayCall += () =>
            {
                var made = new HashSet<string>();
                foreach (var md in markdown)
                    if (DialogueScriptBuilder.IsVaultDialogueNote(md) && !DialogueScriptBuilder.ScriptsBuiltFrom(md).Any())
                    {
                        DialogueScriptBuilder.CreateFor(md);
                        made.Add(md);
                    }
                foreach (var script in DialogueScriptBuilder.AllScripts().ToList())
                {
                    string source = script.Source != null ? AssetDatabase.GetAssetPath(script.Source) : null;
                    bool own = source != null && markdown.Contains(source);
                    if (source != null && made.Contains(source)) continue;   // just built
                    if (own || images) DialogueScriptBuilder.Rebuild(script, logWarnings: own);
                }
            };
        }
    }
}
