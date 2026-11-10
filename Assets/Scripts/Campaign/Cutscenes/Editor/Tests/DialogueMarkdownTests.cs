using Cutscene;
using NUnit.Framework;

// The markdown dialogue format (Cutscene.DialogueMarkdown): speakers, text boxes, sprite swaps, stage sides and typing speed,
// and the Obsidian story-vault notes (no subheadings, obsidian:// and [[wiki]] links, properties, %% comments %%).
[Category("Dialogue")]
public sealed class DialogueMarkdownTests
{
    [Test]
    public void SubheadingsAreTextBoxesOfTheLastSpeaker()
    {
        var r = DialogueMarkdown.Parse("# ![Aldric](a.png)\n## Hold the line!\n## Steady.\n");
        Assert.AreEqual(2, r.Lines.Count);
        Assert.AreEqual("Aldric", r.Lines[1].Speaker);
        Assert.AreEqual("Steady.", r.Lines[1].Text);
        Assert.AreEqual(DialogueSide.Left, r.Lines[0].Side);
        Assert.AreEqual("a.png", r.Lines[0].LeftImage);
        Assert.IsEmpty(r.Warnings);
    }

    [Test]
    public void SecondCharacterTakesTheRightAndTheOtherStaysOnStage()
    {
        var r = DialogueMarkdown.Parse("# ![Aldric](a.png)\n## One\n# [Brom](b.png)\n## Two\n# Aldric\n## Three");
        Assert.AreEqual(DialogueSide.Right, r.Lines[1].Side);
        Assert.AreEqual("Aldric", r.Lines[1].LeftName);
        Assert.AreEqual("b.png", r.Lines[1].RightImage);
        Assert.AreEqual("Aldric", r.Lines[2].Speaker);
        Assert.AreEqual(DialogueSide.Left, r.Lines[2].Side);
        Assert.AreEqual("b.png", r.Lines[2].RightImage);
    }

    [Test]
    public void SameNameWithNewImageSwapsTheSprite()
    {
        var r = DialogueMarkdown.Parse("# ![Aldric](a.png)\n## Calm\n# ![Aldric](a angry.png)\n## Angry");
        Assert.AreEqual("a angry.png", r.Lines[1].LeftImage);
        Assert.AreEqual(DialogueSide.Left, r.Lines[1].Side);
        Assert.IsNull(r.Lines[1].RightName);
    }

    [Test]
    public void ThirdCharacterReplacesWhoeverSpokeLongestAgo()
    {
        var r = DialogueMarkdown.Parse("# ![A](a.png)\n## 1\n# ![B](b.png)\n## 2\n# A\n## 3\n# ![C](c.png)\n## 4");
        Assert.AreEqual("A", r.Lines[3].LeftName);
        Assert.AreEqual("C", r.Lines[3].RightName);
        Assert.AreEqual(DialogueSide.Right, r.Lines[3].Side);
    }

    [Test]
    public void SideTagMovesACharacter()
    {
        var r = DialogueMarkdown.Parse("# ![A](a.png) {right}\n## 1");
        Assert.AreEqual(DialogueSide.Right, r.Lines[0].Side);
        Assert.IsNull(r.Lines[0].LeftName);
    }

    [Test]
    public void WpmTagAppliesToTheNextBoxOnlyAndTheLastTagWins()
    {
        var r = DialogueMarkdown.Parse("# ![A](a.png)\n<wpm=300>\n<wpm=90>\n## Slow\n## Default\n## <120> Inline");
        Assert.AreEqual(90f, r.Lines[0].WordsPerMinute);
        Assert.AreEqual(0f, r.Lines[1].WordsPerMinute);
        Assert.AreEqual(120f, r.Lines[2].WordsPerMinute);
        Assert.AreEqual("Inline", r.Lines[2].Text);
    }

    [Test]
    public void CommentWpmAndOtherSpellingsWork()
    {
        var r = DialogueMarkdown.Parse("# A\n<!-- wpm: 60 -->\n## a\n<wpm 75>\n## b\n<80 wpm>\n## c");
        Assert.AreEqual(60f, r.Lines[0].WordsPerMinute);
        Assert.AreEqual(75f, r.Lines[1].WordsPerMinute);
        Assert.AreEqual(80f, r.Lines[2].WordsPerMinute);
    }

    [Test]
    public void NameWithoutImageIsNarrationOffStage()
    {
        var r = DialogueMarkdown.Parse("# ![A](a.png)\n## Hi\n# Narrator\n## Night falls.");
        Assert.AreEqual("Narrator", r.Lines[1].Speaker);
        Assert.AreEqual(DialogueSide.None, r.Lines[1].Side);
        Assert.AreEqual("A", r.Lines[1].LeftName);
    }

    [Test]
    public void PlainLinesContinueABoxAndNotesAreIgnored()
    {
        var r = DialogueMarkdown.Parse("Scene notes\n# ![A](a.png)\nstage direction\n## First part\nsecond part\n\nnew paragraph\n<!-- note -->\n## Next");
        Assert.AreEqual(2, r.Lines.Count);
        Assert.AreEqual("First part second part\nnew paragraph", r.Lines[0].Text);
    }

    [Test]
    public void ImagePathFormsAreCleaned()
    {
        Assert.AreEqual("Dark King.png", DialogueMarkdown.Parse("# ![K](<Dark King.png>)\n## x").Lines[0].LeftImage);
        Assert.AreEqual("Dark King.png", DialogueMarkdown.Parse("# ![K](Dark%20King.png \"title\")\n## x").Lines[0].LeftImage);
        Assert.AreEqual("k.png", DialogueMarkdown.Parse("# King ![](./k.png)\n## x").Lines[0].LeftImage);
        var wiki = DialogueMarkdown.Parse("# King ![[k.png]]\n## x").Lines[0];
        Assert.AreEqual("King", wiki.Speaker);
        Assert.AreEqual("k.png", wiki.LeftImage);
    }

    [Test]
    public void EmphasisBecomesRichText()
    {
        Assert.AreEqual("<b>Now</b>, <i>quietly</i>.", DialogueMarkdown.Inline("**Now**, *quietly*."));
    }

    [Test]
    public void CombineResolvesRelativePaths()
    {
        Assert.AreEqual("Assets/Art/a.png", DialogueMarkdown.Combine("Assets/Campaign/Dialogue", "../../Art/a.png"));
        Assert.AreEqual("Assets/Art/a.png", DialogueMarkdown.Combine("Assets/Campaign/Dialogue", "Assets/Art/a.png"));
        Assert.AreEqual("Assets/Campaign/Dialogue/p/a.png", DialogueMarkdown.Combine("Assets/Campaign/Dialogue", "p/a.png"));
    }

    const string Briefing =
        "*Introduction to Julian's Backstory*\n" +
        "# [Bard](obsidian://open?vault=Leveraged%20Wars%20Story&file=Special%20Character%2FPreferred%20Style%2FBard.png)\n" +
        "10 years and a day it has been.\n" +
        "# [Jagen](obsidian://open?vault=Leveraged%20Wars%20Story&file=Special%20Character%2FPreferred%20Style%2FJagen.png)\n" +
        "My prince, armed thugs are coming.\n" +
        "# [Prince Julian](obsidian://open?vault=Leveraged%20Wars%20Story&file=Special%20Character%2FPreferred%20Style%2FDark%20Prince.png) \n" +
        "Sound the horn.\n" +
        "# Prince Julian\n" +
        "It is time.";

    [Test]
    public void ObsidianNoteWithoutSubheadingsPlaysTheLinesUnderEachHeading()
    {
        var r = DialogueMarkdown.Parse(Briefing);
        Assert.AreEqual(4, r.Lines.Count);
        Assert.AreEqual("Bard", r.Lines[0].Speaker);
        Assert.AreEqual("10 years and a day it has been.", r.Lines[0].Text);
        Assert.AreEqual("Special Character/Preferred Style/Bard.png", r.Lines[0].LeftImage);
        Assert.AreEqual("Special Character/Preferred Style/Jagen.png", r.Lines[1].RightImage);
        Assert.AreEqual("Prince Julian", r.Lines[2].Speaker);
        Assert.AreEqual("Special Character/Preferred Style/Dark Prince.png", r.Lines[2].LeftImage);
        Assert.AreEqual(DialogueSide.Left, r.Lines[3].Side);
        Assert.IsEmpty(r.Warnings);
    }

    [Test]
    public void EachParagraphIsOneBoxAndASingleLineBreakStays()
    {
        var r = DialogueMarkdown.Parse("# Jagen\nFirst line\nsame box\n\nSecond box\n<wpm=90>\n\nThird box");
        Assert.AreEqual(3, r.Lines.Count);
        Assert.AreEqual("First line\nsame box", r.Lines[0].Text);
        Assert.AreEqual("Second box", r.Lines[1].Text);
        Assert.AreEqual(90f, r.Lines[2].WordsPerMinute);
    }

    [Test]
    public void PropertiesAreReadAndNotShown()
    {
        var r = DialogueMarkdown.Parse("---\ndialogue: true\nmission: \"M01\"\n---\n# Jagen\nHello");
        Assert.IsTrue(DialogueMarkdown.IsTrue(r, "dialogue"));
        Assert.AreEqual("M01", r.Properties["Mission"]);
        Assert.AreEqual(1, r.Lines.Count);
        Assert.AreEqual(6, r.Lines[0].SourceLine);
        Assert.IsFalse(DialogueMarkdown.IsTrue(DialogueMarkdown.Parse("# A\nHi"), "dialogue"));
    }

    [Test]
    public void ObsidianCommentsAreIgnored()
    {
        var r = DialogueMarkdown.Parse("# Jagen\nHold %%not this%% fast\n%%\nwhole\nblock\n%%\n\nNext");
        Assert.AreEqual(2, r.Lines.Count);
        Assert.AreEqual("Hold  fast", r.Lines[0].Text);
        Assert.AreEqual("Next", r.Lines[1].Text);
    }

    [Test]
    public void WikilinkHeadingsTakeTheAliasOrTheFileName()
    {
        var alias = DialogueMarkdown.Parse("# [[Dark Prince.png|Prince Julian]]\nHi").Lines[0];
        Assert.AreEqual("Prince Julian", alias.Speaker);
        Assert.AreEqual("Dark Prince.png", alias.LeftImage);
        Assert.AreEqual("Jagen", DialogueMarkdown.Parse("# ![[Jagen.png]]\nHi").Lines[0].Speaker);
    }

    [Test]
    public void LinksInTextKeepOnlyTheirText()
    {
        Assert.AreEqual("Ask Jagen about Amsteradan, now.",
            DialogueMarkdown.Inline("Ask [[Characters/Jagen.md|Jagen]] about [[Amsteradan#History]], [now](https://x.y).![[map.png]]"));
    }
}
