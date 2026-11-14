using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Cutscene
{
    public enum DialogueSide { None, Left, Right }

    // Reads a dialogue scene written in markdown. Plain C# (no Unity types) so it runs in the editor, at runtime and in tests.
    // Image paths come back as written; the editor's DialogueScriptBuilder turns them into Sprites.
    //
    //   # ![Aldric](Portraits/Aldric.png)        a character with a portrait starts talking
    //                                            (also "# [Aldric](...)", "# Aldric ![](...)" or "# Aldric ![[Aldric.png]]")
    //   ## Hold the line!                        each subheading is one text box; plain lines under it continue the box
    //   # Brom                                   a character already introduced talks again with the same portrait
    //   # ![Aldric](Portraits/Aldric angry.png)  same name, new image: sprite swap (new expression)
    //   <wpm=90>                                 typing speed of the next text box in words per minute; the last tag wins
    //   # ![Brom](brom.png) {right}              optional: put the character on the left or right
    //   # Narrator                               a name never given an image speaks with no portrait (narration)
    //
    // Two portraits fit on stage. A new character takes an empty side, otherwise replaces whoever spoke longest ago.
    // Anything before the first character heading, and <!-- comments --> or %% comments %%, are notes and are ignored.
    //
    // Obsidian notes (the story vault, Assets/Leveraged Wars Story) can skip the subheadings:
    //   # [Jagen](obsidian://open?vault=...&file=Special%20Character%2FJagen.png)   "Copy Obsidian URL" links work
    //   # [[Jagen.png|Jagen]]                    as do wikilinks, with or without "!" and an alias
    //   My prince, armed thugs are coming.       when a character's section has no "## " subheading, each paragraph
    //                                            under the heading is one text box (a single line break stays a line break)
    //   ---  dialogue: true  ---                 YAML properties at the top (Obsidian "properties") go to Result.Properties
    // In a section that does use subheadings, plain lines before the first one are still notes.
    public static class DialogueMarkdown
    {
        public sealed class Line
        {
            public string Speaker = "", Text = "";
            public float WordsPerMinute;          // 0 = the player's default
            public DialogueSide Side;             // the speaker's side; None = not on stage
            public string LeftName, LeftImage, RightName, RightImage;
            public int SourceLine;                // 1-based line in the .md file
        }

        public sealed class Result
        {
            public readonly List<Line> Lines = new List<Line>();
            public readonly List<string> Warnings = new List<string>();
            // The note's YAML properties (frontmatter), e.g. "dialogue: true". Keys are case-insensitive.
            public readonly Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        sealed class Character
        {
            public string Name, Image;
            public int LastSpoke = -1;
        }

        static readonly Regex HeadingRx = new Regex(@"^(#{1,6})(?:\s+|(?=!?\[))(.*)$");
        static readonly Regex ClosingHashesRx = new Regex(@"\s+#+\s*$");
        static readonly Regex RuleRx = new Regex(@"^(?:-\s*){3,}$|^(?:\*\s*){3,}$|^(?:_\s*){3,}$");
        static readonly Regex SideRx = new Regex(@"\{\s*(left|right)\s*\}", RegexOptions.IgnoreCase);
        static readonly Regex WikiImageRx = new Regex(@"!?\[\[([^\]|]+)(?:\|([^\]]*))?\]\]");
        static readonly Regex LinkRx = new Regex(@"!?\[([^\]]*)\]\((.*)\)");
        static readonly Regex TitleRx = new Regex("^(.*?)\\s+(\"[^\"]*\"|'[^']*')$");
        static readonly Regex WpmRx = new Regex(
            @"<!--\s*wpm\s*[:=]?\s*(\d+(?:\.\d+)?)\s*-->|<\s*(?:wpm\s*[:=]?\s*)?(\d+(?:\.\d+)?)\s*(?:wpm)?\s*/?>",
            RegexOptions.IgnoreCase);
        static readonly Regex BoldRx = new Regex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1");
        static readonly Regex ItalicRx = new Regex(@"(?<![\*\w])\*(?=\S)(.+?)(?<=\S)\*(?!\*)");
        static readonly Regex StrikeRx = new Regex(@"~~(?=\S)(.+?)(?<=\S)~~");
        static readonly Regex InlineImageRx = new Regex(@"!\[\[[^\]]*\]\]|!\[[^\]]*\]\([^)]*\)");
        static readonly Regex InlineWikiRx = new Regex(@"\[\[([^\]|]+)(?:\|([^\]]*))?\]\]");
        static readonly Regex InlineLinkRx = new Regex(@"\[([^\]]+)\]\([^)]*\)");

        public static Result Parse(string markdown)
        {
            var result = new Result();
            var cast = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
            Character left = null, right = null, speaker = null;
            Line box = null;
            bool paragraphBreak = false, inFence = false, inComment = false, inObsidianComment = false;
            bool sawCharacter = false, boxFromText = false;
            float pendingWpm = 0f;
            int boxCount = 0;

            var lines = (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int first = ReadFrontmatter(lines, result.Properties);
            var subheaded = SectionsWithSubheadings(lines, first);

            Line NewBox(string text, int lineNo)
            {
                var b = new Line
                {
                    Speaker = speaker != null ? speaker.Name : "",
                    Text = text,
                    WordsPerMinute = pendingWpm,
                    Side = speaker == null ? DialogueSide.None
                         : speaker == left ? DialogueSide.Left
                         : speaker == right ? DialogueSide.Right : DialogueSide.None,
                    LeftName = left != null ? left.Name : null,
                    LeftImage = left != null ? left.Image : null,
                    RightName = right != null ? right.Name : null,
                    RightImage = right != null ? right.Image : null,
                    SourceLine = lineNo,
                };
                pendingWpm = 0f;
                paragraphBreak = false;
                if (speaker != null) speaker.LastSpoke = boxCount;
                boxCount++;
                result.Lines.Add(b);
                return b;
            }

            for (int i = first; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string raw = lines[i];
                string start = raw.TrimStart();
                if (!inComment && !inObsidianComment && (start.StartsWith("```") || start.StartsWith("~~~")))
                {
                    inFence = !inFence;
                    continue;
                }
                if (inFence) continue;

                bool hadText = raw.Trim().Length > 0;
                string line = ExtractWpm(raw, ref pendingWpm, lineNo, result);
                line = StripComments(line, "<!--", "-->", ref inComment);
                line = StripComments(line, "%%", "%%", ref inObsidianComment);
                string t = line.Trim();
                if (t.Length == 0)
                {
                    if (!hadText && box != null)
                    {
                        if (boxFromText) box = null;   // a paragraph is one text box
                        else paragraphBreak = true;
                    }
                    continue;
                }

                var h = HeadingRx.Match(t);
                if (h.Success)
                {
                    string content = ClosingHashesRx.Replace(h.Groups[2].Value, "").Trim();
                    if (h.Groups[1].Length == 1)
                    {
                        speaker = Introduce(content, lineNo, cast, ref left, ref right, result);
                        sawCharacter = true;
                        box = null;
                    }
                    else
                    {
                        if (speaker == null && result.Lines.Count == 0)
                            result.Warnings.Add($"Line {lineNo}: text box before any character heading; it plays as narration.");
                        if (content.Length == 0) result.Warnings.Add($"Line {lineNo}: empty text box.");
                        box = NewBox(Inline(content), lineNo);
                        boxFromText = false;
                    }
                    continue;
                }

                if (RuleRx.IsMatch(t))
                {
                    box = null;
                    continue;
                }

                // Plain text under a subheading continues that box. Under a character heading whose section has no
                // subheadings it is the dialogue itself, one box per paragraph. Anywhere else it is a note.
                if (box != null)
                {
                    string sep = boxFromText || paragraphBreak ? "\n" : " ";
                    box.Text = box.Text.Length == 0 ? Inline(t) : box.Text + sep + Inline(t);
                    paragraphBreak = false;
                }
                else if (sawCharacter && !subheaded[i])
                {
                    box = NewBox(Inline(t), lineNo);
                    boxFromText = true;
                }
            }

            if (pendingWpm > 0f) result.Warnings.Add("A words-per-minute tag at the end of the file has no text box after it.");
            return result;
        }

        // YAML properties at the very top ("---" lines around "key: value" pairs). Returns the first line after them.
        public static int ReadFrontmatter(string[] lines, Dictionary<string, string> properties)
        {
            if (lines.Length == 0 || lines[0].Trim() != "---") return 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t == "---" || t == "...") return i + 1;
                int colon = t.IndexOf(':');
                if (colon <= 0) continue;
                string key = t.Substring(0, colon).Trim();
                string value = t.Substring(colon + 1).Trim().Trim('"', '\'').Trim();
                properties[key] = value;
            }
            properties.Clear();   // never closed: not frontmatter
            return 0;
        }

        // For each line: does the character section it is in (from one "# " heading to the next) use "## " subheadings?
        static bool[] SectionsWithSubheadings(string[] lines, int first)
        {
            var flags = new bool[lines.Length];
            int sectionStart = first;
            bool has = false, fence = false;
            for (int i = first; i <= lines.Length; i++)
            {
                int level = 0;
                if (i < lines.Length)
                {
                    string s = lines[i].Trim();
                    if (s.StartsWith("```") || s.StartsWith("~~~")) fence = !fence;
                    if (fence) continue;
                    var h = HeadingRx.Match(s);
                    if (h.Success) level = h.Groups[1].Length;
                }
                if (i == lines.Length || level == 1)
                {
                    for (int k = sectionStart; k < i; k++) flags[k] = has;
                    sectionStart = i;
                    has = false;
                }
                else if (level > 1) has = true;
            }
            return flags;
        }

        // The property is set to true (Obsidian checkbox) or yes.
        public static bool IsTrue(Result result, string key) =>
            result.Properties.TryGetValue(key, out var v) &&
            (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase));

        static Character Introduce(string content, int lineNo, Dictionary<string, Character> cast,
                                   ref Character left, ref Character right, Result result)
        {
            string side = null;
            var sm = SideRx.Match(content);
            if (sm.Success)
            {
                side = sm.Groups[1].Value.ToLowerInvariant();
                content = content.Remove(sm.Index, sm.Length).Trim();
            }

            string name, image = null;
            var wm = WikiImageRx.Match(content);
            var lm = LinkRx.Match(content);
            if (wm.Success)
            {
                image = CleanPath(wm.Groups[1].Value);
                name = Unwrap((content.Substring(0, wm.Index) + " " + content.Substring(wm.Index + wm.Length)).Trim());
                if (name.Length == 0 && wm.Groups[2].Success) name = Unwrap(wm.Groups[2].Value);
                if (name.Length == 0) name = FileStem(image);
            }
            else if (lm.Success)
            {
                image = CleanPath(lm.Groups[2].Value);
                name = lm.Groups[1].Value.Trim();
                if (name.Length == 0)
                    name = (content.Substring(0, lm.Index) + " " + content.Substring(lm.Index + lm.Length)).Trim();
                name = Unwrap(name);
            }
            else name = Unwrap(content);

            if (name.Length == 0)
            {
                result.Warnings.Add($"Line {lineNo}: character heading has no name; the next text boxes play as narration.");
                return null;
            }
            if (string.IsNullOrEmpty(image)) image = null;

            Character c;
            if (!cast.TryGetValue(name, out c))
            {
                c = new Character { Name = name, Image = image };
                cast[name] = c;
            }
            else if (image != null) c.Image = image;   // sprite swap

            if (c.Image == null)
            {
                if (side != null) result.Warnings.Add($"Line {lineNo}: {name} has no portrait, so {{{side}}} is ignored.");
                return c;
            }

            bool onLeft = left == c, onRight = right == c;
            if (side == "left" && !onLeft)
            {
                if (onRight) right = null;
                left = c;
            }
            else if (side == "right" && !onRight)
            {
                if (onLeft) left = null;
                right = c;
            }
            else if (!onLeft && !onRight)
            {
                if (left == null) left = c;
                else if (right == null) right = c;
                else if (left.LastSpoke <= right.LastSpoke) left = c;
                else right = c;
            }
            return c;
        }

        static string ExtractWpm(string line, ref float pendingWpm, int lineNo, Result result)
        {
            if (line.IndexOf('<') < 0) return line;
            var matches = WpmRx.Matches(line);
            if (matches.Count == 0) return line;
            foreach (Match m in matches)
            {
                string value = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                float wpm;
                if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out wpm) && wpm > 0f)
                    pendingWpm = wpm;
                else
                    result.Warnings.Add($"Line {lineNo}: words-per-minute tag \"{m.Value}\" needs a number above 0.");
            }
            return WpmRx.Replace(line, "");
        }

        public static string StripComments(string line, string open, string close, ref bool inComment)
        {
            if (!inComment && line.IndexOf(open, StringComparison.Ordinal) < 0) return line;
            var sb = new StringBuilder();
            int pos = 0;
            while (pos < line.Length)
            {
                if (inComment)
                {
                    int end = line.IndexOf(close, pos, StringComparison.Ordinal);
                    if (end < 0) break;
                    pos = end + close.Length;
                    inComment = false;
                }
                else
                {
                    int at = line.IndexOf(open, pos, StringComparison.Ordinal);
                    if (at < 0)
                    {
                        sb.Append(line, pos, line.Length - pos);
                        break;
                    }
                    sb.Append(line, pos, at - pos);
                    pos = at + open.Length;
                    inComment = true;
                }
            }
            return sb.ToString();
        }

        // Markdown emphasis to TextMeshPro rich text: **bold**, *italic*, ~~strike~~. Other rich text tags pass through.
        // Links keep only their text ([[Note|text]], [[Note]], [text](url)); embedded images are dropped.
        public static string Inline(string text)
        {
            text = InlineImageRx.Replace(text, "");
            text = InlineWikiRx.Replace(text, m => m.Groups[2].Success ? m.Groups[2].Value : NoteName(m.Groups[1].Value));
            text = InlineLinkRx.Replace(text, "$1");
            text = text.Replace("\\*", "\u0001");
            text = BoldRx.Replace(text, "<b>$2</b>");
            text = ItalicRx.Replace(text, "<i>$1</i>");
            text = StrikeRx.Replace(text, "<s>$1</s>");
            return text.Replace("\u0001", "*").Trim();
        }

        static string Unwrap(string name) => name.Trim().Trim('*', '_').Trim();

        static string FileStem(string path)
        {
            path = path.Replace('\\', '/');
            string file = path.Substring(path.LastIndexOf('/') + 1);
            int dot = file.LastIndexOf('.');
            return dot > 0 ? file.Substring(0, dot) : file;
        }

        // [[Folder/Note.md#Heading]] -> Note
        static string NoteName(string link)
        {
            link = link.Split('#')[0].Replace('\\', '/');
            link = link.Substring(link.LastIndexOf('/') + 1);
            return link.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? link.Substring(0, link.Length - 3) : link;
        }

        // obsidian://open?vault=Name&file=Folder%2FImage.png -> the file parameter (a path from the vault's root).
        static string ObsidianFile(string url)
        {
            int q = url.IndexOf('?');
            if (q < 0) return url;
            foreach (var pair in url.Substring(q + 1).Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq).Equals("file", StringComparison.OrdinalIgnoreCase))
                    return pair.Substring(eq + 1);
            }
            return url;
        }

        public static string CleanPath(string path)
        {
            path = path.Trim();
            if (path.StartsWith("<"))
            {
                int end = path.IndexOf('>');
                path = end > 0 ? path.Substring(1, end - 1) : path.Substring(1);
            }
            else
            {
                var tm = TitleRx.Match(path);
                if (tm.Success) path = tm.Groups[1].Value;
            }
            if (path.StartsWith("obsidian://", StringComparison.OrdinalIgnoreCase)) path = ObsidianFile(path);
            try { path = Uri.UnescapeDataString(path); } catch (UriFormatException) { }
            path = path.Replace('\\', '/').Trim();
            while (path.StartsWith("./")) path = path.Substring(2);
            return path;
        }

        // Joins an image path to the folder of the .md file. Paths starting with Assets/ or Packages/ are used as they are.
        public static string Combine(string folder, string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(folder))
                return Normalize(path);
            if (path.StartsWith("/")) path = path.TrimStart('/');
            return Normalize(folder.TrimEnd('/') + "/" + path);
        }

        static string Normalize(string path)
        {
            var parts = new List<string>();
            foreach (var part in path.Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..") parts.RemoveAt(parts.Count - 1);
                else parts.Add(part);
            }
            return string.Join("/", parts);
        }
    }
}
