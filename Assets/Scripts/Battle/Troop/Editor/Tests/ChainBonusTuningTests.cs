using NUnit.Framework;

// Chain bonus (SU-152): Defence and Morale per active link, with an on/off toggle.
public sealed class ChainBonusTuningTests
{
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
    }

    [TearDown]
    public void TearDown()
    {
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    [Test]
    public void ChainBonus_IsPerActiveLink_AndOffWhenToggledOff()
    {
        ChainBonusTuning bonus = _tuning.ChainBonus;
        Assert.IsTrue(bonus.ChainBonus, "On by default");
        Assert.AreEqual(0, bonus.DefenceBonus(0));
        Assert.AreEqual(1, bonus.DefenceBonus(1));
        Assert.AreEqual(2, bonus.DefenceBonus(2));
        Assert.AreEqual(2, bonus.MoraleBonus(2));

        var serialized = new UnityEditor.SerializedObject(bonus);
        serialized.FindProperty("_chainBonus").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.AreEqual(0, bonus.DefenceBonus(2));
        Assert.AreEqual(0, bonus.MoraleBonus(2));
    }
}
