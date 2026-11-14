using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Builds a DialogueScript from its markdown note (format in Cutscene.DialogueMarkdown): text boxes, speakers and
    // portraits. StoryAssets decides when (note saved, image changed, new vault note).
    public static class DialogueScriptBuilder
    {
        const string CreateMenu = "Assets/Create/Cutscenes/Dialogue Script from Markdown";

        [MenuItem(CreateMenu, true)]
        static bool CanCreate() => Selection.objects.Any(o => StoryAssets.IsMarkdown(AssetDatabase.GetAssetPath(o)));

        // Right click a .md file > Create > Cutscenes > Dialogue Script from Markdown. Makes <name>.asset next to it.
        // Story-vault notes with "dialogue: true" do not need this: theirs is made automatically.
        [MenuItem(CreateMenu, false, 81)]
        static void CreateFromSelection()
        {
            MarkdownAsset last = null;
            foreach (var obj in Selection.objects)
            {
                string mdPath = AssetDatabase.GetAssetPath(obj);
                if (StoryAssets.IsMarkdown(mdPath)) last = StoryAssets.CreateFor(mdPath, typeof(DialogueScript));
            }
            if (last == null) return;
            Selection.activeObject = last;
            EditorGUIUtility.PingObject(last);
        }

        public static void Rebuild(DialogueScript script, bool logWarnings = true) => StoryAssets.Rebuild(script, logWarnings);

        internal static void Build(DialogueScript script, string mdPath, string markdown, List<string> warnings)
        {
            var lines = new List<DialogueLine>();
            string folder = StoryAssets.FolderOf(mdPath);
            string vault = StoryAssets.FindVaultRoot(folder);
            var parsed = DialogueMarkdown.Parse(markdown);
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
            script.SetLines(lines, warnings);
        }

        // Image paths are relative to the note, or start with Assets/. "file.png#SpriteName" picks one sprite of a sheet.
        // In an Obsidian vault a path may also be from the vault's root (obsidian:// links) or just a file name, or the end
        // of a path, found anywhere in the vault ([[Jagen.png]]), and the extension may be left out.
        // A link whose folders are out of date still finds a vault image with its file name, with a warning.
        // Outside the vault, exactly one image in the project with that file name is used, with a warning.
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
            if (found == null && vault != null && !ambiguous && Path.GetFileName(file) != file.TrimStart('/'))
            {
                found = FindInVault(vault, Path.GetFileName(file), line, warnings, out ambiguous);
                if (found != null)
                    warnings.Add($"Line {line}: the portrait link \"{file}\" is out of date (the image has moved); using " +
                                 $"{found.Substring(vault.Length + 1)}. Link it by name to fix this, e.g. [[{Path.GetFileName(found)}|Name]].");
            }

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
                    warnings.Add($"Line {line}: portrait \"{image}\" not found. Put the image in the story vault and link it by name, e.g. [[Jagen.png|Jagen]].");
            }

            cache[image] = sprite;
            return sprite;
        }

        // The path itself, or the path with an image extension added (Obsidian links may leave it out).
        static string FindImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) return path;
            if (StoryAssets.IsImage(path)) return null;
            foreach (var ext in StoryAssets.ImageExtensions)
                if (AssetDatabase.LoadMainAssetAtPath(path + ext) != null) return path + ext;
            return null;
        }

        // Obsidian's own rule: a link that is a file name (or the end of a path) matches that image anywhere in the vault.
        static string FindInVault(string vault, string file, int line, List<string> warnings, out bool ambiguous)
        {
            ambiguous = false;
            string want = file.Replace('\\', '/').TrimStart('/');
            if (want.Length == 0 || !Directory.Exists(vault)) return null;
            bool hasExtension = StoryAssets.IsImage(want);
            var hits = Directory.GetFiles(vault, "*", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(StoryAssets.IsImage)
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
                warnings.Add($"Line {line}: {hits.Count} images in the story vault match \"{file}\". Give one of them another " +
                             $"name, or link it with its folder, e.g. \"{hits[0].Substring(vault.Length + 1)}\".");
            }
            return null;
        }

        static Sprite LoadSprite(string path, string spriteName)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
            return spriteName == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(s => s.name == spriteName);
        }

        static string NoSpriteMessage(int line, string path, string spriteName) => spriteName != null
            ? $"Line {line}: {path} has no sprite called \"{spriteName}\"."
            : $"Line {line}: {path} is not a sprite. Set its Texture Type to \"Sprite (2D and UI)\" in the Inspector.";

        public static IEnumerable<DialogueScript> AllScripts() => StoryAssets.All().OfType<DialogueScript>();
    }
}
