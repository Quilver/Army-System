using Cutscene;
using NUnit.Framework;

// The markdown dialogue format (Cutscene.DialogueMarkdown): speakers, text boxes, sprite swaps, stage sides and typing speed.
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
}
