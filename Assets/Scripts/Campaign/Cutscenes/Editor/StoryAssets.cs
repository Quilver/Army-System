using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BattlePrep;
using Cutscene;
using UnityEditor;
using UnityEngine;

namespace CutsceneEditor
{
    // Keeps the assets built from markdown notes (MarkdownAsset: DialogueScript, LoanOffer) in step with their notes.
    //  - A note in the story vault (a folder with .obsidian/ in it: Assets/Leveraged Wars Story) gets its asset made next
    //    to it as soon as Unity sees it: "dialogue: true" in its properties makes a DialogueScript, a Gold property makes a
    //    LoanOffer. Notes in the vault's templates folder, and other notes, are left alone.
    //  - Saving a note rebuilds every asset built from it; changing an image rebuilds every DialogueScript.
    //  - A note moved or renamed in Unity takes its asset with it. A note moved outside Unity (Obsidian moves the .md but
    //    not its .meta, so Unity sees a new file) is found again by its file name, and the asset moves next to it, keeping
    //    every reference to it.
    public static class StoryAssets
    {
        public const string VaultPath = "Assets/Leveraged Wars Story";

        // Which asset a note becomes on its own, or null. Only notes in a vault, outside its templates folder.
        public static Type KindOf(string mdPath)
        {
            if (!IsMarkdown(mdPath) || !File.Exists(mdPath)) return null;
            string vault = FindVaultRoot(FolderOf(mdPath));
            if (vault == null) return null;
            string templates = TemplatesFolder(vault);
            if (templates != null && mdPath.StartsWith(templates + "/", StringComparison.OrdinalIgnoreCase)) return null;
            return KindOfText(File.ReadAllText(mdPath));
        }

        public static Type KindOfText(string markdown)
        {
            var parsed = DialogueMarkdown.Parse(markdown);
            if (DialogueMarkdown.IsTrue(parsed, "dialogue")) return typeof(DialogueScript);
            if (LoanMarkdown.IsLoan(parsed.Properties)) return typeof(LoanOffer);
            return null;
        }

        // The asset of this kind built from the note; made next to it (<note name>.asset) if there is none yet.
        public static MarkdownAsset CreateFor(string mdPath, Type kind)
        {
            var asset = BuiltFrom(mdPath).FirstOrDefault(kind.IsInstanceOfType);
            if (asset == null)
            {
                asset = (MarkdownAsset)ScriptableObject.CreateInstance(kind);
                asset.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath);
                AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(mdPath, ".asset")));
            }
            Rebuild(asset);
            return asset;
        }

        // Reads the note again. With no note to read, what was built stays and a warning says why.
        public static void Rebuild(MarkdownAsset asset, bool logWarnings = true)
        {
            if (asset == null) return;
            var warnings = new List<string>();
            string mdPath = asset.Source != null ? AssetDatabase.GetAssetPath(asset.Source) : null;
            string text = null;
            if (string.IsNullOrEmpty(mdPath))
                warnings.Add(string.IsNullOrEmpty(asset.SourcePath)
                    ? "No markdown note assigned."
                    : $"Its note ({asset.SourcePath}) is gone. If it was renamed or moved outside Unity, drag the note into Markdown File.");
            else if (!File.Exists(mdPath)) warnings.Add($"{mdPath} not found.");
            else
            {
                text = File.ReadAllText(mdPath);
                asset.SourcePath = mdPath;
            }

            if (text == null) asset.SetWarnings(warnings);
            else if (asset is DialogueScript script) DialogueScriptBuilder.Build(script, mdPath, text, warnings);
            else if (asset is LoanOffer loan) LoanOfferBuilder.Build(loan, mdPath, text, warnings);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (logWarnings)
                foreach (var w in asset.Warnings) Debug.LogWarning($"{asset.name} ({mdPath ?? asset.SourcePath}): {w}", asset);
        }

        [MenuItem("Leveraged Wars/Story/Rebuild All Story Assets")]
        public static void RebuildAll()
        {
            int made = 0, found = 0, count = 0;
            foreach (var md in AllNotes())
            {
                if (BuiltFrom(md).Any()) continue;
                if (Relink(md) != null) found++;
                else
                {
                    var kind = KindOf(md);
                    if (kind == null) continue;
                    CreateFor(md, kind);
                    made++;
                }
            }
            foreach (var asset in All())
            {
                Rebuild(asset);
                count++;
            }
            Debug.Log($"Story assets: rebuilt {count}; made {made} for new notes; found the notes of {found} again.");
        }

        [MenuItem("Leveraged Wars/Story/Open Story Vault in Obsidian")]
        static void OpenVault()
        {
            if (!Directory.Exists(VaultPath + "/.obsidian"))
            {
                Debug.LogWarning($"No Obsidian vault at {VaultPath}.");
                return;
            }
            // Opens the vault if Obsidian knows it; otherwise use Obsidian's "Open folder as vault" on this folder once.
            Application.OpenURL("obsidian://open?path=" + Uri.EscapeDataString(Path.GetFullPath(VaultPath)));
        }

        // An asset whose note disappeared finds this note if it has the same file name and is the only one that could.
        // The asset moves next to the note (if that name is free). Returns it, or null.
        internal static MarkdownAsset Relink(string mdPath)
        {
            string file = Path.GetFileName(mdPath);
            var orphans = All().Where(a => a.Source == null && !string.IsNullOrEmpty(a.SourcePath) &&
                                           string.Equals(Path.GetFileName(a.SourcePath), file, StringComparison.OrdinalIgnoreCase))
                               .ToList();
            if (orphans.Count != 1) return null;
            var asset = orphans[0];
            asset.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath);
            if (asset.Source == null) return null;
            FollowNote(asset, mdPath);
            Rebuild(asset);
            Debug.Log($"{asset.name} found its note again at {mdPath}.", asset);
            return asset;
        }

        // Moves the asset next to its note, named after it, unless something else already has that name.
        static void FollowNote(MarkdownAsset asset, string mdPath)
        {
            string current = AssetDatabase.GetAssetPath(asset);
            string target = Path.ChangeExtension(mdPath, ".asset").Replace('\\', '/');
            if (current == target || AssetDatabase.LoadMainAssetAtPath(target) != null) return;
            string error = AssetDatabase.MoveAsset(current, target);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"Could not move {current} next to its note: {error}", asset);
        }

        internal static void Refresh(HashSet<string> notes, bool images, string[] moved, string[] movedFrom)
        {
            var done = new HashSet<MarkdownAsset>();

            // Notes moved or renamed in Unity: their assets go with them if they were next to them under the note's name.
            for (int i = 0; i < moved.Length; i++)
            {
                if (!IsMarkdown(moved[i])) continue;
                string oldAsset = Path.ChangeExtension(movedFrom[i], ".asset").Replace('\\', '/');
                foreach (var asset in BuiltFrom(moved[i]).ToList())
                    if (AssetDatabase.GetAssetPath(asset) == oldAsset) FollowNote(asset, moved[i]);
            }

            foreach (var md in notes)
            {
                if (BuiltFrom(md).Any()) continue;
                var asset = Relink(md);
                if (asset == null)
                {
                    var kind = KindOf(md);
                    if (kind != null) asset = CreateFor(md, kind);
                }
                if (asset != null) done.Add(asset);
            }

            foreach (var asset in All().ToList())
            {
                if (done.Contains(asset)) continue;
                string source = asset.Source != null ? AssetDatabase.GetAssetPath(asset.Source) : null;
                bool own = source != null && notes.Contains(source);
                if (own) Rebuild(asset);
                else if (images && asset is DialogueScript) Rebuild(asset, logWarnings: false);
            }
        }

        public static IEnumerable<MarkdownAsset> All() =>
            AssetDatabase.FindAssets("t:" + nameof(MarkdownAsset))
                .Select(g => AssetDatabase.LoadAssetAtPath<MarkdownAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null);

        public static IEnumerable<MarkdownAsset> BuiltFrom(string mdPath) =>
            All().Where(a => a.Source != null && AssetDatabase.GetAssetPath(a.Source) == mdPath);

        static IEnumerable<string> AllNotes() =>
            AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(IsMarkdown).Distinct();

        // The Obsidian vault the folder is in (the nearest folder above it with a .obsidian folder), or null.
        public static string FindVaultRoot(string folder)
        {
            for (string dir = folder; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir)?.Replace('\\', '/'))
                if (Directory.Exists(dir + "/.obsidian")) return dir;
            return null;
        }

        static string TemplatesFolder(string vault)
        {
            string json = vault + "/.obsidian/templates.json";
            if (!File.Exists(json)) return null;
            var m = Regex.Match(File.ReadAllText(json), "\"folder\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success && m.Groups[1].Value.Trim('/').Length > 0 ? vault + "/" + m.Groups[1].Value.Trim('/') : null;
        }

        internal static string FolderOf(string path) => Path.GetDirectoryName(path)?.Replace('\\', '/');

        internal static bool IsMarkdown(string path) =>
            !string.IsNullOrEmpty(path) && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

        internal static readonly string[] ImageExtensions =
            { ".png", ".jpg", ".jpeg", ".psd", ".psb", ".tga", ".gif", ".bmp", ".tif", ".tiff", ".exr" };

        internal static bool IsImage(string path) =>
            !string.IsNullOrEmpty(path) && ImageExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }

    // Hands every import of a note or an image to StoryAssets.Refresh.
    class StoryNotePostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var notes = new HashSet<string>(imported.Concat(moved).Where(StoryAssets.IsMarkdown));
            bool images = imported.Concat(moved).Concat(deleted).Any(StoryAssets.IsImage);
            if (notes.Count == 0 && !images) return;
            EditorApplication.delayCall += () => StoryAssets.Refresh(notes, images, moved, movedFrom);
        }
    }
}
