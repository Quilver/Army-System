using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Cutscene;

namespace BattlePrep
{
    // Reads a loan offer written as a markdown note. Plain C# (no Unity types) so it runs in the editor and in tests.
    //
    //   ---
    //   Gold: 500             gold the player receives when taking the loan
    //   Cost: 2000            gold the player must repay
    //   Time left: 10         months until it must be repaid
    //   Prestige Cost: 12     prestige needed to take it, and spent when it is taken
    //   Title: Seed loan      optional: the card's title. Default: the note's file name
    //   ---
    //   His majesty of Moricia has great hopes in you.    the text on the card: line breaks kept, **bold**, *italic*
    //   *Easy money always comes with a hook*              and ~~strike~~ shown, %% comments %% and <!-- --> hidden
    //
    // A note is a loan when its properties include Gold. Property names ignore case and spaces ("Time Left", "timeleft").
    public static class LoanMarkdown
    {
        public sealed class Result
        {
            public string Title = "", Details = "";
            public int Gold, Cost, Months, PrestigeCost;
            public readonly Dictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Warnings = new List<string>();
        }

        static readonly string[] GoldKeys = { "gold" };
        static readonly string[] CostKeys = { "cost", "repay", "repayment" };
        static readonly string[] MonthKeys = { "timeleft", "months", "time" };
        static readonly string[] PrestigeKeys = { "prestigecost", "prestige", "prestigerequired" };
        static readonly string[] TitleKeys = { "title", "name" };
        static readonly Regex HeadingRx = new Regex(@"^#{1,6}\s+");
        static readonly Regex NumberRx = new Regex(@"-?\d+");

        // Whether a note with these properties is a loan offer.
        public static bool IsLoan(IReadOnlyDictionary<string, string> properties) => Find(properties, GoldKeys) != null;

        public static Result Parse(string markdown, string fileName)
        {
            var r = new Result();
            var lines = (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int first = DialogueMarkdown.ReadFrontmatter(lines, r.Properties);
            if (first == 0) r.Warnings.Add("No properties found. A loan note starts with Gold, Cost, Time left and Prestige Cost between --- lines.");

            r.Gold = Number(r, GoldKeys, "Gold", 1);
            r.Cost = Number(r, CostKeys, "Cost", 0);
            r.Months = Number(r, MonthKeys, "Time left", 1);
            r.PrestigeCost = Number(r, PrestigeKeys, "Prestige Cost", 0);

            string title = Find(r.Properties, TitleKeys);
            r.Title = !string.IsNullOrWhiteSpace(title) ? title.Trim() : (fileName ?? "").Trim();
            if (r.Title.Length == 0) r.Warnings.Add("The loan has no title. Add a Title property or name the note.");

            var details = new StringBuilder();
            bool inComment = false, inObsidianComment = false, inFence = false;
            for (int i = first; i < lines.Length; i++)
            {
                string start = lines[i].TrimStart();
                if (!inComment && !inObsidianComment && (start.StartsWith("```") || start.StartsWith("~~~")))
                {
                    inFence = !inFence;
                    continue;
                }
                if (inFence) continue;
                string line = DialogueMarkdown.StripComments(lines[i], "<!--", "-->", ref inComment);
                line = DialogueMarkdown.StripComments(line, "%%", "%%", ref inObsidianComment);
                string t = HeadingRx.Replace(line.Trim(), "");
                if (t.Length == 0) continue;
                if (details.Length > 0) details.Append('\n');
                details.Append(DialogueMarkdown.Inline(t));
            }
            r.Details = details.ToString();
            return r;
        }

        static int Number(Result r, string[] keys, string label, int min)
        {
            string value = Find(r.Properties, keys);
            if (value == null)
            {
                r.Warnings.Add($"No \"{label}\" property.");
                return 0;
            }
            var m = NumberRx.Match(value.Replace(",", ""));
            if (!m.Success || !int.TryParse(m.Value, out int n))
            {
                r.Warnings.Add($"\"{label}\" should be a whole number, not \"{value}\".");
                return 0;
            }
            if (n < min) r.Warnings.Add($"\"{label}\" should be {min} or more (it is {n}).");
            return n;
        }

        // The value of the first property whose name, ignoring case, spaces, "_" and "-", is one of the keys.
        static string Find(IReadOnlyDictionary<string, string> properties, string[] keys)
        {
            if (properties == null) return null;
            foreach (var pair in properties)
            {
                string key = pair.Key.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
                if (Array.IndexOf(keys, key) >= 0) return pair.Value;
            }
            return null;
        }
    }
}
