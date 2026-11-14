using BattlePrep;
using Cutscene;
using NUnit.Framework;

// Loan offers written as markdown notes (BattlePrep.LoanMarkdown): properties, card text, title and warnings.
[Category("Dialogue")]
public sealed class LoanMarkdownTests
{
    const string Easy =
        "---\nGold: \"500\"\nTime left: \"10\"\nCost: \"2000\"\nPrestige Cost: \"12\"\n---\n" +
        "His majesty of Moricia has great hopes in you.\n*Easy money always comes with a hook*\n";

    [Test]
    public void ThePropertiesSetTheTerms()
    {
        var r = LoanMarkdown.Parse(Easy, "Loan, easy");
        Assert.AreEqual("Loan, easy", r.Title);
        Assert.AreEqual(500, r.Gold);
        Assert.AreEqual(2000, r.Cost);
        Assert.AreEqual(10, r.Months);
        Assert.AreEqual(12, r.PrestigeCost);
        Assert.IsEmpty(r.Warnings);
    }

    [Test]
    public void TheTextKeepsItsLinesAndEmphasis()
    {
        var r = LoanMarkdown.Parse(Easy, "Loan, easy");
        Assert.AreEqual("His majesty of Moricia has great hopes in you.\n<i>Easy money always comes with a hook</i>", r.Details);
    }

    [Test]
    public void NamesIgnoreCaseAndSpacesAndATitleOverridesTheFileName()
    {
        var r = LoanMarkdown.Parse("---\ngold: 1,000\nREPAY: 1200\nmonths: 3\nprestige: 4\ntitle: Crown levy\n---\n%% note %%\n# Text\nPay up.", "file");
        Assert.AreEqual(1000, r.Gold);
        Assert.AreEqual(1200, r.Cost);
        Assert.AreEqual(3, r.Months);
        Assert.AreEqual(4, r.PrestigeCost);
        Assert.AreEqual("Crown levy", r.Title);
        Assert.AreEqual("Text\nPay up.", r.Details);
        Assert.IsEmpty(r.Warnings);
    }

    [Test]
    public void MissingOrBadValuesWarn()
    {
        var r = LoanMarkdown.Parse("---\nGold: lots\nTime left: 0\n---\nHi", "x");
        Assert.AreEqual(0, r.Gold);
        Assert.AreEqual(4, r.Warnings.Count);   // Gold not a number, no Cost, Time left below 1, no Prestige Cost
    }

    [Test]
    public void ANoteWithGoldIsALoan()
    {
        Assert.IsTrue(LoanMarkdown.IsLoan(DialogueMarkdown.Parse(Easy).Properties));
        Assert.IsFalse(LoanMarkdown.IsLoan(DialogueMarkdown.Parse("---\ndialogue: true\n---\n# A\nHi").Properties));
    }
}
