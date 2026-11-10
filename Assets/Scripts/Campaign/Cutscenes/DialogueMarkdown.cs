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
    // Anything before the first subheading that is not a heading, and <!-- comments -->, are notes and are ignored.
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
        static readonly Regex WikiImageRx = new Regex(@"!?\[\[([^\]|]+)(?:\|[^\]]*)?\]\]");
        static readonly Regex LinkRx = new Regex(@"!?\[([^\]]*)\]\((.*)\)");
        static readonly Regex TitleRx = new Regex("^(.*?)\\s+(\"[^\"]*\"|'[^']*')$");
        static readonly Regex WpmRx = new Regex(
            @"<!--\s*wpm\s*[:=]?\s*(\d+(?:\.\d+)?)\s*-->|<\s*(?:wpm\s*[:=]?\s*)?(\d+(?:\.\d+)?)\s*(?:wpm)?\s*/?>",
            RegexOptions.IgnoreCase);
        static readonly Regex BoldRx = new Regex(@"(\*\*|__)(?=\S)(.+?)(?<=\S)\1");
        static readonly Regex ItalicRx = new Regex(@"(?<![\*\w])\*(?=\S)(.+?)(?<=\S)\*(?!\*)");
        static readonly Regex StrikeRx = new Regex(@"~~(?=\S)(.+?)(?<=\S)~~");

        public static Result Parse(string markdown)
        {
            var result = new Result();
            var cast = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
            Character left = null, right = null, speaker = null;
            Line box = null;
            bool paragraphBreak = false, inFence = false, inComment = false;
            float pendingWpm = 0f;
            int boxCount = 0;

            var lines = (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string raw = lines[i];
                string start = raw.TrimStart();
                if (!inComment && (start.StartsWith("```") || start.StartsWith("~~~")))
                {
                    inFence = !inFence;
                    continue;
                }
                if (inFence) continue;

                bool hadText = raw.Trim().Length > 0;
                string line = ExtractWpm(raw, ref pendingWpm, lineNo, result);
                line = StripComments(line, ref inComment);
                string t = line.Trim();
                if (t.Length == 0)
                {
                    if (!hadText && box != null) paragraphBreak = true;
                    continue;
                }

                var h = HeadingRx.Match(t);
                if (h.Success)
                {
                    string content = ClosingHashesRx.Replace(h.Groups[2].Value, "").Trim();
                    if (h.Groups[1].Length == 1)
                    {
                        speaker = Introduce(content, lineNo, cast, ref left, ref right, result);
                        box = null;
                    }
                    else
                    {
                        if (speaker == null && result.Lines.Count == 0)
                            result.Warnings.Add($"Line {lineNo}: text box before any character heading; it plays as narration.");
                        box = new Line
                        {
                            Speaker = speaker != null ? speaker.Name : "",
                            Text = Inline(content),
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
                        if (content.Length == 0) result.Warnings.Add($"Line {lineNo}: empty text box.");
                        pendingWpm = 0f;
                        paragraphBreak = false;
                        if (speaker != null) speaker.LastSpoke = boxCount;
                        boxCount++;
                        result.Lines.Add(box);
                    }
                    continue;
                }

                if (RuleRx.IsMatch(t))
                {
                    box = null;
                    continue;
                }

                // Plain text under a subheading continues that box; anywhere else it is a note.
                if (box != null)
                {
                    box.Text = box.Text.Length == 0 ? Inline(t) : box.Text + (paragraphBreak ? "\n" : " ") + Inline(t);
                    paragraphBreak = false;
                }
            }

            if (pendingWpm > 0f) result.Warnings.Add("A words-per-minute tag at the end of the file has no text box after it.");
            return result;
        }

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

        static string StripComments(string line, ref bool inComment)
        {
            if (!inComment && line.IndexOf("<!--", StringComparison.Ordinal) < 0) return line;
            var sb = new StringBuilder();
            int pos = 0;
            while (pos < line.Length)
            {
                if (inComment)
                {
                    int end = line.IndexOf("-->", pos, StringComparison.Ordinal);
                    if (end < 0) break;
                    pos = end + 3;
                    inComment = false;
                }
                else
                {
                    int open = line.IndexOf("<!--", pos, StringComparison.Ordinal);
                    if (open < 0)
                    {
                        sb.Append(line, pos, line.Length - pos);
                        break;
                    }
                    sb.Append(line, pos, open - pos);
                    pos = open + 4;
                    inComment = true;
                }
            }
            return sb.ToString();
        }

        // Markdown emphasis to TextMeshPro rich text: **bold**, *italic*, ~~strike~~. Other rich text tags pass through.
        public static string Inline(string text)
        {
            text = text.Replace("\\*", "\u0001");
            text = BoldRx.Replace(text, "<b>$2</b>");
            text = ItalicRx.Replace(text, "<i>$1</i>");
            text = StrikeRx.Replace(text, "<s>$1</s>");
            return text.Replace("\u0001", "*").Trim();
        }

        static string Unwrap(string name) => name.Trim().Trim('*', '_').Trim();

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
